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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
        private const byte ToeplitzHashAlgorithm = 5;

        private const UInt16 CommentOptionCode = 1;
        private const UInt16 CustomStringOptionCode = 2988;
        private const UInt16 CustomBinaryOptionCode = 2989;
        private const UInt16 CustomStringNoCopyOptionCode = 19372;
        private const UInt16 CustomBinaryNoCopyOptionCode = 19373;
        private const int PrivateEnterpriseNumberLength = 4;
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
        private readonly List<string> comments = new List<string>();
        private readonly List<CustomOption> customOptions = new List<CustomOption>();
        private readonly List<UInt16> invalidOptionCodes = new List<UInt16>();

        /// <summary>
        /// Gets the first comment (opt_comment) attached to the block, or <c>null</c> if there is none.
        /// </summary>
        public string Comment => this.comments.Count > 0 ? this.comments[0] : null;

        /// <summary>
        /// Gets all comments (opt_comment) attached to the block, in file order.
        /// </summary>
        public IReadOnlyList<string> Comments { get; }

        /// <summary>
        /// Gets the vendor-specific options (opt_custom) attached to the block, in file order.
        /// </summary>
        public IReadOnlyList<CustomOption> CustomOptions { get; }

        /// <summary>
        /// Gets the codes of options that were ignored because their length is invalid for their type.
        /// </summary>
        public IReadOnlyList<UInt16> InvalidOptionCodes { get; }

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
            this.Comments = new ReadOnlyCollection<string>(this.comments);
            this.CustomOptions = new ReadOnlyCollection<CustomOption>(this.customOptions);
            this.InvalidOptionCodes = new ReadOnlyCollection<UInt16>(this.invalidOptionCodes);
            this.StoreType = (BlockType)reader.ReadUInt32();
            this.TotalLength = reader.ReadUInt32();
        }

        /// <summary>
        /// Gets the hash algorithm for a stored algorithm code.
        /// </summary>
        /// <param name="value">The algorithm code stored in the file.</param>
        /// <returns>The matching <see cref="HashAlgorithm"/>, or the code cast to it if the algorithm isn't known.</returns>
        protected static HashAlgorithm GetHashAlgorithm(byte value)
        {
            return value switch
            {
                Crc32HashAlgorithm => HashAlgorithm.Crc32,
                MD5HashAlgorithm => HashAlgorithm.MD5,
                Sha1HashAlgorithm => HashAlgorithm.Sha1,
                TwosComplementHashAlgorithm => HashAlgorithm.TwosComplement,
                XorHashAlgorithm => HashAlgorithm.Xor,
                ToeplitzHashAlgorithm => HashAlgorithm.Toeplitz,
                _ => (HashAlgorithm)value,
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
                HashAlgorithm.Toeplitz => ToeplitzHashAlgorithm,
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

        // The option value helpers below throw InvalidOptionLengthException when a value has the wrong length for its
        // option; ReadOptions then ignores the option and records its code in InvalidOptionCodes.

        private protected static void RequireLength(byte[] value, int length)
        {
            if (value.Length != length)
            {
                throw new InvalidOptionLengthException();
            }
        }

        private protected static void RequireMinimumLength(byte[] value, int length)
        {
            if (value.Length < length)
            {
                throw new InvalidOptionLengthException();
            }
        }

        private protected static byte ToByte(byte[] value)
        {
            RequireLength(value, 1);
            return value[0];
        }

        // Option strings are not zero-terminated, but a zero byte still ends the string.
        private protected static string ToUtf8String(byte[] value)
        {
            int end = Array.IndexOf(value, (byte)0);
            return Encoding.UTF8.GetString(value, 0, end < 0 ? value.Length : end);
        }

        // Gets a timestamp from an option value stored as its high 32 bits followed by its low 32 bits.
        private protected long ToTimestamp(byte[] value)
        {
            RequireLength(value, 8);
            long high = this.ToUInt32(value[..4]);
            long low = this.ToUInt32(value[4..]);
            return (high << 32) | low;
        }

        // Numeric option values are stored in the byte order of the section.
        private protected int ToInt32(byte[] value)
        {
            RequireLength(value, 4);
            return this.isBigEndian ? BinaryPrimitives.ReadInt32BigEndian(value) : BinaryPrimitives.ReadInt32LittleEndian(value);
        }

        private protected uint ToUInt32(byte[] value)
        {
            RequireLength(value, 4);
            return this.isBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(value) : BinaryPrimitives.ReadUInt32LittleEndian(value);
        }

        private protected long ToInt64(byte[] value)
        {
            RequireLength(value, 8);
            return this.isBigEndian ? BinaryPrimitives.ReadInt64BigEndian(value) : BinaryPrimitives.ReadInt64LittleEndian(value);
        }

        private protected ulong ToUInt64(byte[] value)
        {
            RequireLength(value, 8);
            return this.isBigEndian ? BinaryPrimitives.ReadUInt64BigEndian(value) : BinaryPrimitives.ReadUInt64LittleEndian(value);
        }

        // Hash options store the algorithm code in the first byte and the hash after it.
        private protected static PacketHash ToPacketHash(byte[] value)
        {
            RequireMinimumLength(value, 1);
            return new PacketHash(GetHashAlgorithm(value[0]), value[1..]);
        }

        // Converts a raw timestamp using the resolution and offset of the interface the block refers to.
        private protected static DateTime ToDateTime(InterfaceDescriptionBlock block, long timestamp, out bool precisionLoss)
        {
            ArgumentNullException.ThrowIfNull(block);
            return block.GetTimestampTransformer().ToDateTime(timestamp, out precisionLoss);
        }

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
                try
                {
                    switch (code)
                    {
                        case CommentOptionCode:
                            this.comments.Add(ToUtf8String(value));
                            break;
                        case CustomStringOptionCode:
                        case CustomBinaryOptionCode:
                        case CustomStringNoCopyOptionCode:
                        case CustomBinaryNoCopyOptionCode:
                            RequireMinimumLength(value, PrivateEnterpriseNumberLength);
                            this.customOptions.Add(new CustomOption(
                                code, this.ToUInt32(value[..PrivateEnterpriseNumberLength]), value[PrivateEnterpriseNumberLength..]));
                            break;
                        default:
                            this.OnReadOptionsCode(code, value);
                            break;
                    }
                }
                catch (InvalidOptionLengthException)
                {
                    // The spec allows readers to ignore options with an invalid length and keep reading.
                    this.invalidOptionCodes.Add(code);
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

        private sealed class InvalidOptionLengthException : Exception
        {
        }
    }
}