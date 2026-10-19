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
    /// A captured packet in the obsolete Packet Block format. New files use <see cref="EnhancedPacketBlock"/> instead.
    /// </summary>
    public class PacketBlock : BlockBase
    {
        private const int DataAlignmentBoundary = 4;
        private const UInt16 FlagsOptionCode = 2;
        private const UInt16 HashOptionCode = 3;

        /// <summary>
        /// Gets the number of packet bytes captured, which can be less than <see cref="DataLength"/> if the packet was truncated.
        /// </summary>
        public UInt32 CapturedLength { get; private set; }

        /// <summary>
        /// Gets the captured packet data.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the original length of the packet on the wire.
        /// </summary>
        public UInt32 DataLength { get; private set; }

        /// <summary>
        /// Gets the number of packets lost between this packet and the preceding one.
        /// </summary>
        public UInt16 DropsCount { get; private set; }

        /// <summary>
        /// Gets the packet flags (pack_flags), or <c>null</c> if there are none.
        /// </summary>
        public PacketOptionFlags Flags { get; private set; }

        /// <summary>
        /// Gets the packet hash (pack_hash), or <c>null</c> if there is none.
        /// </summary>
        public byte[] Hash { get; private set; }

        /// <summary>
        /// Gets the algorithm used to compute <see cref="Hash"/>.
        /// </summary>
        public HashAlgorithm HashAlgorithm { get; private set; }

        /// <summary>
        /// Gets the zero-based index of the <see cref="InterfaceDescriptionBlock"/> in this section that the packet was captured on.
        /// </summary>
        public UInt16 InterfaceID { get; private set; }

        /// <summary>
        /// Gets the time the packet was captured.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        internal PacketBlock(BinaryReader reader)
            : base(reader)
        {
            this.InterfaceID = reader.ReadUInt16();
            this.DropsCount = reader.ReadUInt16();
            this.Timestamp = DateTime.FromBinary(reader.ReadInt64());
            this.CapturedLength = reader.ReadUInt32();
            this.DataLength = reader.ReadUInt32();
            this.Data = reader.ReadBytes((int)this.CapturedLength);

            int remainderLength = (int)(this.CapturedLength % DataAlignmentBoundary);
            if (remainderLength > 0)
            {
                _ = reader.ReadBytes(DataAlignmentBoundary - remainderLength);
            }

            this.TryReadOptions(reader);
            this.ReadClosingField(reader);
        }

        /// <inheritdoc/>
        protected override void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            switch (code)
            {
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