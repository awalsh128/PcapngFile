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
    using System.Text;

    /// <summary>
    /// Describes a network interface that packets were captured on.
    /// </summary>
    public class InterfaceDescriptionBlock : BlockBase
    {
        internal static readonly TimestampTransformer DefaultTimestampTransformer = new TimestampTransformer();

        private const UInt16 DescriptionOptionCode = 3;
        private const UInt16 EuiAddressOptionCode = 7;
        private const UInt16 FilterOptionCode = 11;
        private const UInt16 FrameCheckSequenceOptionCode = 13;
        private const UInt16 IPv4AddressOptionCode = 4;
        private const UInt16 IPv6AddressOptionCode = 5;
        private const UInt16 MacAddressOptionCode = 6;
        private const UInt16 NameOptionCode = 2;
        private const UInt16 OperatingSystemOptionCode = 12;
        private const UInt16 SpeedOptionCode = 8;
        private const UInt16 TimeOffsetSecondsOptionCode = 14;
        private const UInt16 TimeZoneOptionCode = 10;
        private const UInt16 TimestampResolutionOptionCode = 9;

        private TimestampTransformer timestampTransformer;

        /// <summary>
        /// Gets the interface description (if_description), or <c>null</c> if there is none.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the interface's EUI-64 address (if_EUIaddr), or <c>null</c> if there is none.
        /// </summary>
        public byte[] EuiAddress { get; private set; }

        /// <summary>
        /// Gets the raw capture filter (if_filter), or <c>null</c> if there is none. The first byte is the filter type.
        /// </summary>
        public byte[] Filter { get; private set; }

        /// <summary>
        /// Gets the length in bits of the frame check sequence for this interface (if_fcslen).
        /// </summary>
        public int FrameCheckSequence { get; private set; }

        /// <summary>
        /// Gets the interface's IPv4 address and netmask (if_IPv4addr), or <c>null</c> if there are none.
        /// </summary>
        public byte[] IPv4Address { get; private set; }

        /// <summary>
        /// Gets the interface's IPv6 address and prefix length (if_IPv6addr), or <c>null</c> if there are none.
        /// </summary>
        public byte[] IPv6Address { get; private set; }

        /// <summary>
        /// Gets the link layer type of the interface.
        /// </summary>
        public LinkType LinkType { get; private set; }

        /// <summary>
        /// Gets the interface's MAC address (if_MACaddr), or <c>null</c> if there is none.
        /// </summary>
        public byte[] MacAddress { get; private set; }

        /// <summary>
        /// Gets the interface name (if_name), or <c>null</c> if there is none.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the operating system of the capturing machine (if_os), or <c>null</c> if there is none.
        /// </summary>
        public string OperatingSystem { get; private set; }

        /// <summary>
        /// Gets the maximum number of bytes captured from each packet, or 0 if there is no limit.
        /// </summary>
        public int SnapLength { get; private set; }

        /// <summary>
        /// Gets the interface speed in bits per second (if_speed).
        /// </summary>
        public long Speed { get; private set; }

        /// <summary>
        /// Gets the offset in seconds added to packet timestamps to get absolute time (if_tsoffset).
        /// </summary>
        public long TimeOffsetSeconds { get; private set; }

        /// <summary>
        /// Gets the time zone offset from GMT used by packet timestamps (if_tzone).
        /// </summary>
        public int TimeZone { get; private set; }

        /// <summary>
        /// Gets the resolution of packet timestamps (if_tsresol), or 6 (microseconds) if the option is absent.
        /// </summary>
        /// <remarks>
        /// If the most significant bit is 0, the remaining bits are a negative power of 10; if it is 1, a negative power of 2.
        /// </remarks>
        public byte TimestampResolution { get; private set; }

        internal InterfaceDescriptionBlock(BinaryReader reader)
            : base(reader)
        {
            this.LinkType = (LinkType)reader.ReadUInt16();
            _ = reader.ReadUInt16(); // Reserved field.
            this.SnapLength = reader.ReadInt32();
            this.TimestampResolution = TimestampTransformer.DefaultResolution; // Used when if_tsresol is absent.
            this.TryReadOptions(reader);
            this.ReadClosingField(reader);
            this.timestampTransformer = null;
        }

        internal TimestampTransformer GetTimestampTransformer()
        {
            return this.timestampTransformer ??= new TimestampTransformer(this.TimestampResolution, this.TimeOffsetSeconds);
        }

        /// <inheritdoc/>
        protected override void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            switch (code)
            {
                case DescriptionOptionCode:
                    this.Description = Encoding.UTF8.GetString(value);
                    break;
                case EuiAddressOptionCode:
                    this.EuiAddress = value;
                    break;
                case FilterOptionCode:
                    this.Filter = value; // TODO Dig into spec and get actual data type.
                    break;
                case FrameCheckSequenceOptionCode:
                    this.FrameCheckSequence = value[0];
                    break;
                case IPv4AddressOptionCode:
                    this.IPv4Address = value;
                    break;
                case IPv6AddressOptionCode:
                    this.IPv6Address = value;
                    break;
                case MacAddressOptionCode:
                    this.MacAddress = value;
                    break;
                case NameOptionCode:
                    this.Name = Encoding.UTF8.GetString(value);
                    break;
                case OperatingSystemOptionCode:
                    this.OperatingSystem = Encoding.UTF8.GetString(value);
                    break;
                case SpeedOptionCode:
                    this.Speed = this.ToInt64(value);
                    break;
                case TimeOffsetSecondsOptionCode:
                    this.TimeOffsetSeconds = this.ToInt64(value);
                    break;
                case TimeZoneOptionCode:
                    this.TimeZone = this.ToInt32(value); // GMT offset
                    break;
                case TimestampResolutionOptionCode:
                    this.TimestampResolution = value[0]; // TODO Dig into spec and get actual data type.
                    break;
            }
        }
    }
}