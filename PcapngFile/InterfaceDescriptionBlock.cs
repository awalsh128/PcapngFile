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

		public string Description { get; private set; }

		public byte[] EuiAddress { get; private set; }

		public byte[] Filter { get; private set; }

		public int FrameCheckSequence { get; private set; }

		public byte[] IPv4Address { get; private set; }

		public byte[] IPv6Address { get; private set; }

		public LinkType LinkType { get; private set; }

		public byte[] MacAddress { get; private set; }

		public string Name { get; private set; }

		public string OperatingSystem { get; private set; }

		public int SnapLength { get; private set; }

		public long Speed { get; private set; }

		public long TimeOffsetSeconds { get; private set; }

		public int TimeZone { get; private set; }

		public byte TimestampResolution { get; private set; }		

		internal InterfaceDescriptionBlock(BinaryReader reader)
			: base(reader)
		{
			this.LinkType = (LinkType)reader.ReadUInt16();
			reader.ReadUInt16(); // Reserved field.
			this.SnapLength = reader.ReadInt32();
			this.ReadOptions(reader);
			this.ReadClosingField(reader);
			this.timestampTransformer = null;
		}

		internal TimestampTransformer GetTimestampTransformer()
		{
			return this.timestampTransformer ?? (this.timestampTransformer = new TimestampTransformer(this.TimestampResolution));
		}		

		override protected void OnReadOptionsCode(UInt16 code, byte[] value)
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
					this.Speed = BitConverter.ToInt64(value, 0);
					break;
				case TimeOffsetSecondsOptionCode:
					this.TimeOffsetSeconds = BitConverter.ToInt64(value, 0);
					break;
				case TimeZoneOptionCode:
					this.TimeZone = BitConverter.ToInt32(value, 0); // GMT offset						
					break;
				case TimestampResolutionOptionCode:
					this.TimestampResolution = value[0]; // TODO Dig into spec and get actual data type.
					break;
			}
		}
	}
}