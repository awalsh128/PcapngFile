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
    using System.IO;

    /// <summary>
    /// A captured packet together with the interface it was captured on, its timestamp and options.
    /// </summary>
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

        /// <summary>
        /// Gets the number of packet bytes captured, which can be less than <see cref="DataLength"/> if the packet was truncated.
        /// </summary>
        public int CapturedLength { get; private set; }

        /// <summary>
        /// Gets the captured packet data.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the original length of the packet on the wire.
        /// </summary>
        public int DataLength { get; private set; }

        /// <summary>
        /// Gets the number of packets lost between this packet and the preceding one (epb_dropcount).
        /// </summary>
        public long DropCount { get; private set; }

        /// <summary>
        /// Gets the packet hash (epb_hash), or <c>null</c> if there is none.
        /// </summary>
        public byte[] Hash { get; private set; }

        /// <summary>
        /// Gets the algorithm used to compute <see cref="Hash"/>.
        /// </summary>
        public HashAlgorithm HashAlgorithm { get; private set; }

        /// <summary>
        /// Gets the zero-based index of the <see cref="InterfaceDescriptionBlock"/> in this section that the packet was captured on.
        /// </summary>
        public int InterfaceID { get; private set; }

        /// <summary>
        /// Gets the packet flags (epb_flags), or <c>null</c> if there are none.
        /// </summary>
        public PacketOptionFlags Flags { get; private set; }

        /// <summary>
        /// Gets the raw timestamp in units of the interface's timestamp resolution. Use <see cref="GetTimestamp()"/> to convert it.
        /// </summary>
        public long Timestamp { get; private set; }

        internal EnhancedPacketBlock(BinaryReader reader)
            : base(reader)
        {
            this.InterfaceID = reader.ReadInt32();
            this.Timestamp = ReadTimestamp(reader);
            this.CapturedLength = reader.ReadInt32();
            this.DataLength = reader.ReadInt32();
            this.Data = reader.ReadBytes(this.CapturedLength);

            int remainderLength = this.CapturedLength % DataAlignmentBoundary;
            if (remainderLength > 0)
            {
                int paddingLength = DataAlignmentBoundary - remainderLength;
                _ = reader.ReadBytes(paddingLength);
            }

            this.TryReadOptions(reader);
            this.ReadClosingField(reader);
        }

        /// <summary>
        /// Gets the UTC <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/> assuming the default resolution
        /// (microseconds) and no offset.
        /// </summary>
        /// <returns>The UTC <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The timestamp is outside the range of <see cref="DateTime"/>.</exception>
        public DateTime GetTimestamp()
        {
            return InterfaceDescriptionBlock.DefaultTimestampTransformer.ToDateTime(this.Timestamp);
        }

        /// <summary>
        /// Gets the UTC <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/>, applying the interface's
        /// timestamp resolution (if_tsresol) and offset (if_tsoffset).
        /// </summary>
        /// <param name="block">The <see cref="InterfaceDescriptionBlock"/> the packet was captured on.</param>
        /// <param name="precisionLoss">
        /// True if the timestamp was finer than a <see cref="DateTime"/> tick (100 ns) and was truncated.
        /// </param>
        /// <returns>The UTC <see cref="DateTime"/> equivalent of the <see cref="Timestamp"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The timestamp is outside the range of <see cref="DateTime"/>.</exception>
        public DateTime GetTimestamp(InterfaceDescriptionBlock block, out bool precisionLoss)
        {
            ArgumentNullException.ThrowIfNull(block);
            return block.GetTimestampTransformer().ToDateTime(this.Timestamp, out precisionLoss);
        }

        /// <inheritdoc/>
        protected override void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            switch (code)
            {
                case DropCountOptionCode:
                    this.DropCount = this.ToInt64(value);
                    break;
                case FlagsOptionCode:
                    this.Flags = new PacketOptionFlags(this.ToUInt32(value));
                    break;
                case HashOptionCode:
                    this.HashAlgorithm = GetHashAlgorithm(value[0]);
                    this.Hash = value[1..];
                    break;
            }
        }
    }
}