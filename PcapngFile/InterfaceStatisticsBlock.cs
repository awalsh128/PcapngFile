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

    public class InterfaceStatisticsBlock : BlockBase
    {
        private const UInt16 EndTimeOptionCode = 3;
        private const UInt16 PacketsDroppedOptionCode = 5;
        private const UInt16 PacketsFilterAcceptOptionCode = 6;
        private const UInt16 PacketsOsDroppedOptionCode = 7;
        private const UInt16 PacketsReceivedOptionCode = 4;
        private const UInt16 StartTimeOptionCode = 2;
        private const UInt16 TotalPacketsSentOptionCode = 8;

        public long EndTime { get; private set; }
        public int InterfaceID { get; private set; }
        public long PacketsDropped { get; private set; }
        public long PacketsFilterAccept { get; private set; }
        public long PacketsOsDropped { get; private set; }
        public long PacketsReceived { get; private set; }
        public long StartTime { get; private set; }
        public long Timestamp { get; private set; }
        public long TotalPacketsSent { get; private set; }

        internal InterfaceStatisticsBlock(BinaryReader reader)
            : base(reader)
        {
            this.InterfaceID = reader.ReadInt32();
            this.Timestamp = reader.ReadInt64();
            this.TryReadOptions(reader);
            this.ReadClosingField(reader);
        }

        override protected void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            switch (code)
            {
                case EndTimeOptionCode:
                    this.EndTime = BitConverter.ToInt64(value, 0);
                    break;
                case PacketsDroppedOptionCode:
                    this.PacketsDropped = BitConverter.ToInt64(value, 0);
                    break;
                case PacketsFilterAcceptOptionCode:
                    this.PacketsFilterAccept = BitConverter.ToInt64(value, 0);
                    break;
                case PacketsOsDroppedOptionCode:
                    this.PacketsOsDropped = BitConverter.ToInt64(value, 0);
                    break;
                case PacketsReceivedOptionCode:
                    this.PacketsReceived = BitConverter.ToInt64(value, 0);
                    break;
                case StartTimeOptionCode:
                    this.StartTime = BitConverter.ToInt64(value, 0);
                    break;
                case TotalPacketsSentOptionCode:
                    this.TotalPacketsSent = BitConverter.ToInt64(value, 0);
                    break;
            }
        }
    }
}
