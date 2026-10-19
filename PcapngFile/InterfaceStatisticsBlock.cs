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
    /// Capture statistics for an interface.
    /// </summary>
    public class InterfaceStatisticsBlock : BlockBase
    {
        private const UInt16 EndTimeOptionCode = 3;
        private const UInt16 PacketsDroppedOptionCode = 5;
        private const UInt16 PacketsFilterAcceptOptionCode = 6;
        private const UInt16 PacketsOsDroppedOptionCode = 7;
        private const UInt16 PacketsReceivedOptionCode = 4;
        private const UInt16 StartTimeOptionCode = 2;
        private const UInt16 TotalPacketsSentOptionCode = 8;

        /// <summary>
        /// Gets the time the capture ended (isb_endtime).
        /// </summary>
        public long EndTime { get; private set; }

        /// <summary>
        /// Gets the zero-based index of the <see cref="InterfaceDescriptionBlock"/> in this section that the statistics refer to.
        /// </summary>
        public int InterfaceID { get; private set; }

        /// <summary>
        /// Gets the number of packets dropped by the interface (isb_ifdrop).
        /// </summary>
        public long PacketsDropped { get; private set; }

        /// <summary>
        /// Gets the number of packets accepted by the capture filter (isb_filteraccept).
        /// </summary>
        public long PacketsFilterAccept { get; private set; }

        /// <summary>
        /// Gets the number of packets dropped by the operating system (isb_osdrop).
        /// </summary>
        public long PacketsOsDropped { get; private set; }

        /// <summary>
        /// Gets the number of packets received by the interface (isb_ifrecv).
        /// </summary>
        public long PacketsReceived { get; private set; }

        /// <summary>
        /// Gets the time the capture started (isb_starttime).
        /// </summary>
        public long StartTime { get; private set; }

        /// <summary>
        /// Gets the raw timestamp of when the statistics were taken.
        /// </summary>
        public long Timestamp { get; private set; }

        /// <summary>
        /// Gets the number of packets delivered to the user (isb_usrdeliv).
        /// </summary>
        public long TotalPacketsSent { get; private set; }

        internal InterfaceStatisticsBlock(BinaryReader reader)
            : base(reader)
        {
            this.InterfaceID = reader.ReadInt32();
            this.Timestamp = ReadTimestamp(reader);
            this.TryReadOptions(reader);
            this.ReadClosingField(reader);
        }

        /// <inheritdoc/>
        protected override void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            switch (code)
            {
                case EndTimeOptionCode:
                    this.EndTime = this.ToTimestamp(value);
                    break;
                case PacketsDroppedOptionCode:
                    this.PacketsDropped = this.ToInt64(value);
                    break;
                case PacketsFilterAcceptOptionCode:
                    this.PacketsFilterAccept = this.ToInt64(value);
                    break;
                case PacketsOsDroppedOptionCode:
                    this.PacketsOsDropped = this.ToInt64(value);
                    break;
                case PacketsReceivedOptionCode:
                    this.PacketsReceived = this.ToInt64(value);
                    break;
                case StartTimeOptionCode:
                    this.StartTime = this.ToTimestamp(value);
                    break;
                case TotalPacketsSentOptionCode:
                    this.TotalPacketsSent = this.ToInt64(value);
                    break;
            }
        }
    }
}