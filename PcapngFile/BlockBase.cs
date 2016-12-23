/*
Copyright (c) 2016, Andrew Walsh
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
    using System.IO;
    using System.Text;

    public abstract class BlockBase
    {
        private const byte Crc32HashAlgorithm = 2;
        private const byte MD5HashAlgorithm = 3;
        private const byte Sha1HashAlgorithm = 4;
        private const byte TwosComplementHashAlgorithm = 0;
        private const byte XorHashAlgorithm = 1;

        private const UInt16 CommentOptionCode = 1;
        private const UInt32 PacketFlagsOption = 2;

        protected const UInt16 EndOptionCode = 0;
        protected const UInt32 EndOptionField = 0;
        protected const int OptionValueAlignmentBoundary = 4;

        internal const int BlockTotalLengthLength = 4;
        internal const int BlockTypeLength = 4;
        protected long PreReadPosition;

        public string Comment { get; private set; }

        public BlockType StoreType { get; private set; }

        public UInt32 TotalLength { get; private set; }

        internal BlockBase(BinaryReader reader)
        {
            this.PreReadPosition = reader.BaseStream.Position;
            this.StoreType = (BlockType)reader.ReadUInt32();
            this.TotalLength = reader.ReadUInt32();
        }

        protected HashAlgorithm GetHashAlgorithm(byte value)
        {
            switch (value)
            {
                case Crc32HashAlgorithm:
                    return HashAlgorithm.Crc32;
                case MD5HashAlgorithm:
                    return HashAlgorithm.MD5;
                case Sha1HashAlgorithm:
                    return HashAlgorithm.Sha1;
                case TwosComplementHashAlgorithm:
                    return HashAlgorithm.TwosComplement;
                case XorHashAlgorithm:
                    return HashAlgorithm.Xor;
                default:
                    throw new ArgumentOutOfRangeException("Value not recognized as a valid constant (value = " + value + ").");
            }
        }

        protected byte GetHashAlgorithmCode(HashAlgorithm value)
        {
            switch (value)
            {
                case HashAlgorithm.Crc32:
                    return Crc32HashAlgorithm;
                case HashAlgorithm.MD5:
                    return MD5HashAlgorithm;
                case HashAlgorithm.Sha1:
                    return Sha1HashAlgorithm;
                case HashAlgorithm.TwosComplement:
                    return TwosComplementHashAlgorithm;
                case HashAlgorithm.Xor:
                    return XorHashAlgorithm;
                default:
                    throw new ArgumentOutOfRangeException("Value not recognized as a valid constant (value = " + value + ").");
            }
        }

        protected long GetRemainingLength(BinaryReader reader)
        {
            var readLength = reader.BaseStream.Position - this.PreReadPosition;
            return readLength + BlockTotalLengthLength;
        }

        protected virtual void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            throw new NotImplementedException("The OnReadOptionsCode must be overriden if ReadOption is called.");
        }

        protected void ReadClosingField(BinaryReader reader)
        {
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
            while (true)
            {
                UInt16 code = reader.ReadUInt16();
                UInt16 valueLength = reader.ReadUInt16();
                byte[] value;
                if (valueLength > 0)
                {
                    value = reader.ReadBytes(valueLength);
                    int remainderLength = valueLength % OptionValueAlignmentBoundary;
                    if (remainderLength > 0)
                    {
                        reader.ReadBytes(OptionValueAlignmentBoundary - remainderLength); // Read fill bytes to boundary.
                    }
                }
                else
                {
                    break;
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