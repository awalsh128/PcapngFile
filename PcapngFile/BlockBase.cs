/*
Copyright (c) 2013-2026, Andrew Walsh
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

namespace PcapngFile
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Base class of all blocks read from a PCAP-NG file.
    /// </summary>
    public abstract class BlockBase
    {
        private const byte Crc32HashAlgorithm = 2;
        private const byte MD5HashAlgorithm = 3;
        private const byte Sha1HashAlgorithm = 4;
        private const byte TwosComplementHashAlgorithm = 0;
        private const byte XorHashAlgorithm = 1;

        private const UInt16 CommentOptionCode = 1;
        private const UInt32 PacketFlagsOption = 2;

        /// <summary>
        /// The option code (opt_endofopt) that marks the end of an options list.
        /// </summary>
        protected const UInt16 EndOptionCode = 0;

        /// <summary>
        /// The full option field (code and length) that marks the end of an options list.
        /// </summary>
        protected const UInt32 EndOptionField = 0;

        /// <summary>
        /// The byte boundary that option values are padded to.
        /// </summary>
        protected const int OptionValueAlignmentBoundary = 4;

        internal const int BlockTotalLengthLength = 4;
        internal const int BlockTypeLength = 4;

        /// <summary>
        /// The stream position at the start of this block, before any of it was read.
        /// </summary>
        protected long PreReadPosition;

        private readonly bool isBigEndian;

        /// <summary>
        /// Gets the comment (opt_comment) attached to the block, or <c>null</c> if there is none.
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// Gets the block type as stored in the file.
        /// </summary>
        public BlockType StoreType { get; private set; }

        /// <summary>
        /// Gets the total length of the block in bytes, including its header and trailer.
        /// </summary>
        public UInt32 TotalLength { get; private set; }

        internal BlockBase(BinaryReader reader)
        {
            this.PreReadPosition = reader.BaseStream.Position;
            this.isBigEndian = reader is EndianBinaryReader endianReader && endianReader.IsBigEndian;
            this.StoreType = (BlockType)reader.ReadUInt32();
            this.TotalLength = reader.ReadUInt32();
        }

        /// <summary>
        /// Gets the hash algorithm for a stored algorithm code.
        /// </summary>
        /// <param name="value">The algorithm code stored in the file.</param>
        /// <returns>The matching <see cref="HashAlgorithm"/>.</returns>
        protected static HashAlgorithm GetHashAlgorithm(byte value)
        {
            return value switch
            {
                Crc32HashAlgorithm => HashAlgorithm.Crc32,
                MD5HashAlgorithm => HashAlgorithm.MD5,
                Sha1HashAlgorithm => HashAlgorithm.Sha1,
                TwosComplementHashAlgorithm => HashAlgorithm.TwosComplement,
                XorHashAlgorithm => HashAlgorithm.Xor,
                _ => throw new ArgumentOutOfRangeException("Value not recognized as a valid constant (value = " + value + ")."),
            };
        }

        /// <summary>
        /// Gets the stored algorithm code for a hash algorithm.
        /// </summary>
        /// <param name="value">The hash algorithm.</param>
        /// <returns>The algorithm code as stored in the file.</returns>
        protected static byte GetHashAlgorithmCode(HashAlgorithm value)
        {
            return value switch
            {
                HashAlgorithm.Crc32 => Crc32HashAlgorithm,
                HashAlgorithm.MD5 => MD5HashAlgorithm,
                HashAlgorithm.Sha1 => Sha1HashAlgorithm,
                HashAlgorithm.TwosComplement => TwosComplementHashAlgorithm,
                HashAlgorithm.Xor => XorHashAlgorithm,
                _ => throw new ArgumentOutOfRangeException("Value not recognized as a valid constant (value = " + value + ")."),
            };
        }

        /// <summary>
        /// Reads a timestamp stored as its high 32 bits followed by its low 32 bits.
        /// </summary>
        /// <param name="reader">The reader positioned at the timestamp.</param>
        /// <returns>The raw timestamp.</returns>
        protected static long ReadTimestamp(BinaryReader reader)
        {
            long high = reader.ReadUInt32();
            long low = reader.ReadUInt32();
            return (high << 32) | low;
        }

        // Gets a timestamp from an option value stored as its high 32 bits followed by its low 32 bits.
        private protected long ToTimestamp(byte[] value)
        {
            long high = this.ToUInt32(value);
            long low = this.ToUInt32(value.AsSpan(4).ToArray());
            return (high << 32) | low;
        }

        // Option values are stored in the byte order of the section.
        private protected int ToInt32(byte[] value) =>
            this.isBigEndian ? BinaryPrimitives.ReadInt32BigEndian(value) : BinaryPrimitives.ReadInt32LittleEndian(value);

        private protected uint ToUInt32(byte[] value) =>
            this.isBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(value) : BinaryPrimitives.ReadUInt32LittleEndian(value);

        private protected long ToInt64(byte[] value) =>
            this.isBigEndian ? BinaryPrimitives.ReadInt64BigEndian(value) : BinaryPrimitives.ReadInt64LittleEndian(value);

        /// <summary>
        /// Gets the number of bytes of this block read so far plus the length of the trailing total length field.
        /// </summary>
        /// <param name="reader">The reader positioned inside this block.</param>
        /// <returns>The length in bytes.</returns>
        protected long GetRemainingLength(BinaryReader reader)
        {
            var readLength = reader.BaseStream.Position - this.PreReadPosition;
            return readLength + BlockTotalLengthLength;
        }

        /// <summary>
        /// Called for each block-specific option read from the block's options list.
        /// </summary>
        /// <param name="code">The option code.</param>
        /// <param name="value">The option value without padding.</param>
        protected virtual void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            throw new NotImplementedException("The OnReadOptionsCode must be overriden if ReadOption is called.");
        }

        /// <summary>
        /// Reads the trailing total length field and checks that it matches the leading one.
        /// </summary>
        /// <param name="reader">The reader positioned at the end of this block.</param>
        protected void ReadClosingField(BinaryReader reader)
        {
            // The block can have unused space after its options, so read the field at the end of the block.
            reader.BaseStream.Position = this.PreReadPosition + this.TotalLength - BlockTotalLengthLength;
            UInt32 readLength = reader.ReadUInt32();
            if (readLength != this.TotalLength)
            {
                throw new IOException(
                    "Closing field (Block Total Length) doesn't match opening field (readLength = " + readLength +
                        ", this.TotalLength = " + this.TotalLength + ").");
            }
        }

        private void ReadOptions(BinaryReader reader)
        {
            // The end of options marker is optional when the options run to the end of the block.
            long optionsEnd = this.PreReadPosition + this.TotalLength - BlockTotalLengthLength;
            while (reader.BaseStream.Position < optionsEnd)
            {
                UInt16 code = reader.ReadUInt16();
                UInt16 valueLength = reader.ReadUInt16();
                if (code == EndOptionCode)
                {
                    break;
                }
                byte[] value = reader.ReadBytes(valueLength);
                int remainderLength = valueLength % OptionValueAlignmentBoundary;
                if (remainderLength > 0)
                {
                    _ = reader.ReadBytes(OptionValueAlignmentBoundary - remainderLength); // Read fill bytes to boundary.
                }
                if (code == CommentOptionCode)
                {
                    this.Comment = Encoding.UTF8.GetString(value);
                }
                else
                {
                    this.OnReadOptionsCode(code, value);
                }
            }
        }

        /// <summary>
        /// Reads the block's options list if the block has one.
        /// </summary>
        /// <param name="reader">The reader positioned at the start of the options.</param>
        protected void TryReadOptions(BinaryReader reader)
        {
            var totalExceptOptionLength = this.GetRemainingLength(reader);
            if (this.TotalLength != totalExceptOptionLength)
            {
                this.ReadOptions(reader);
            }
        }
    }
}