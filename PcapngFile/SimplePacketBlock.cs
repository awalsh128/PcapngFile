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
    /// A captured packet with minimal metadata. It has no timestamp and belongs to the first interface of its section.
    /// </summary>
    public class SimplePacketBlock : BlockBase
    {
        private const int HeaderAndFooterLength = 16; // Type, length, original length and trailing length fields.

        /// <summary>
        /// Gets the captured packet data, without padding.
        /// </summary>
        /// <remarks>
        /// Its length is the smallest of <see cref="DataLength"/> and the snap length of the section's first interface.
        /// </remarks>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the original length of the packet on the wire.
        /// </summary>
        public UInt32 DataLength { get; private set; }

        // The snap length is that of the first interface in the section, or 0 for no limit.
        internal SimplePacketBlock(BinaryReader reader, uint snapLength)
            : base(reader)
        {
            this.DataLength = reader.ReadUInt32();
            long length = Math.Min(this.DataLength, this.TotalLength - HeaderAndFooterLength);
            if (snapLength > 0)
            {
                length = Math.Min(length, snapLength);
            }
            this.Data = reader.ReadBytes((int)length);
            this.ReadClosingField(reader);
        }
    }
}