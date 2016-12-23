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

    public class EnhancedPacketBlock : BlockBase
    {
        private const int DataAlignmentBoundary = 4;
        private const UInt16 DropCountOptionCode = 4;
        private const UInt32 DropCountOptionHeader = (DropCountOptionCode << 16) | 8;
        private const UInt16 FlagsOptionCode = 2;
        private const UInt32 FlagsOptionHeader = (FlagsOptionCode << 16) | 4;
        private const UInt16 HashOptionCode = 3;
        //private const UInt32 HashOptionHeader = (HashOptionCode << 16) | <variable depending on code>
        private const int HeaderAndFooterLength = (7 * 4) + BlockTotalLengthLength; // 7 4-byte header fields + 1 4-byte footer field

        public int CapturedLength { get; private set; }

        public byte[] Data { get; private set; }

        public int DataLength { get; private set; }

        public long DropCount { get; private set; }

        public byte[] Hash { get; private set; }

        public HashAlgorithm HashAlgorithm { get; private set; }

        public int InterfaceID { get; private set; }

        public PacketOptionFlags Flags { get; private set; }

        public long Timestamp { get; private set; }

        internal EnhancedPacketBlock(BinaryReader reader)
            : base(reader)
        {
            this.InterfaceID = reader.ReadInt32();
            this.Timestamp = GetTimestamp(reader);
            this.CapturedLength = reader.ReadInt32();
            this.DataLength = reader.ReadInt32();
            this.Data = reader.ReadBytes(this.CapturedLength);

            int remainderLength = this.CapturedLength % DataAlignmentBoundary;
            if (remainderLength > 0)
            {
                int paddingLength = DataAlignmentBoundary - remainderLength;
                reader.ReadBytes(paddingLength);
            }

            this.TryReadOptions(reader);
            this.ReadClosingField(reader);
        }

        private static long GetTimestamp(BinaryReader reader)
        {
            if (BitConverter.IsLittleEndian)
            {
                var high = reader.ReadBytes(4);
                var low = reader.ReadBytes(4);
                var ordered = new byte[8];

                Buffer.BlockCopy(low, 0, ordered, 0, 4);
                Buffer.BlockCopy(high, 0, ordered, 4, 4);
                return BitConverter.ToInt64(ordered, 0);
            }
            return reader.ReadInt64();
        }

        /// <summary>
        /// Gets the <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/> assuming default resolution.
        /// </summary>
        /// <returns>The <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/> assuming default resolution.</returns>
        public DateTime GetTimestamp()
        {
            return InterfaceDescriptionBlock.DefaultTimestampTransformer.ToDateTime(this.Timestamp);
        }

        /// <summary>
        /// Gets the <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/>.
        /// </summary>
        /// <param name="block">The <see cref="InterfaceDescriptionBlock"/> used to determine the resolution of the <see cref="Timestamp"/>.</param>
        /// <param name="precisionLoss">True if there was precision loss during the transformation.</param>
        /// <returns>The <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/>.</returns>
        public DateTime GetTimestamp(InterfaceDescriptionBlock block, out bool precisionLoss)
        {
            TimestampTransformer transformer;
            try
            {
                transformer = block.GetTimestampTransformer();
            }
            catch (NotImplementedException ex)
            {
                throw new NotSupportedException(ex.Message, ex);
            }
            precisionLoss = transformer.PrecisionLoss;
            return transformer.ToDateTime(this.Timestamp);
        }

        protected override void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            switch (code)
            {
                case DropCountOptionCode:
                    this.DropCount = BitConverter.ToInt64(value, 0);
                    break;
                case FlagsOptionCode:
                    this.Flags = new PacketOptionFlags(value);
                    break;
                case HashOptionCode:
                    this.HashAlgorithm = this.GetHashAlgorithm(value[0]);
                    Array.Copy(value, 1, this.Hash, 0, value.Length - 1);
                    break;
            }
        }
    }
}