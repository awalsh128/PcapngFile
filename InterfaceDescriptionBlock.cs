/*
Copyright (c) 2013, Andrew Walsh
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. All advertising materials mentioning features or use of this software
   must display the following acknowledgement:
   This product includes software developed by the <organization>.
4. Neither the name of the <organization> nor the
   names of its contributors may be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY <COPYRIGHT HOLDER> ''AS IS'' AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;

namespace PcapngFile
{
	public class InterfaceDescriptionBlock : BlockBase
	{
		private const int TimestampResolutionSignMask = 0x80;

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
			reader.ReadUInt16();	// Reserved field.
			this.SnapLength = reader.ReadInt32();
			this.ReadOptions(reader);
			this.ReadClosingField(reader);
		}

		public int GetTimePrecisionDivisor()
		{
			int value = this.TimestampResolution ^ TimestampResolutionSignMask;
			if ((this.TimestampResolution & TimestampResolutionSignMask) > 0)
			{
				return (int)Math.Pow(10, value);
			}
			else
			{
				if (value == 0)
				{
					return 1000000;	// 10^6 - microsecond
				}
				else
				{
					return 1 << value;
				}
			}
		}

		override protected void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			switch (code)
			{
				case DescriptionOptionCode:
					this.Description = UTF8Encoding.UTF8.GetString(value);
					break;
				case EuiAddressOptionCode:
					this.EuiAddress = value;
					break;
				case FilterOptionCode:
					this.Filter = value;	// TODO Dig into spec and get actual data type.
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
					this.Name = UTF8Encoding.UTF8.GetString(value);
					break;
				case OperatingSystemOptionCode:
					this.OperatingSystem = UTF8Encoding.UTF8.GetString(value);
					break;
				case SpeedOptionCode:
					this.Speed = BitConverter.ToInt64(value, 0);
					break;
				case TimeOffsetSecondsOptionCode:
					this.TimeOffsetSeconds = BitConverter.ToInt64(value, 0);
					break;
				case TimeZoneOptionCode:					
					this.TimeZone = BitConverter.ToInt32(value, 0);	// GMT offset						
					break;
				case TimestampResolutionOptionCode:
					this.TimestampResolution = value[0];	// TODO Dig into spec and get actual data type.
					break;
			}
		}
	}
}
