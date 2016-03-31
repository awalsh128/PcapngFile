/*
Copyright (c) 2016, Andrew Walsh
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
   This product includes software developed by the PcapngFile.
4. Neither the name of the PcapngFile nor the
   names of its contributors may be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY Andrew Walsh ''AS IS'' AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL Andrew Walsh BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

namespace PcapngFile
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Text;

	public class NameResolutionBlock : BlockBase
	{
		private const UInt16 NameServerIp4AddressOptionCode = 3;
		private const UInt16 NameServerIp6AddressOptionCode = 4;
		private const UInt16 NameServerNameOptionCode = 2;

		public bool IsIpVersion6
		{
			get { return this.NameServerIp4Address == null; }
		}
		public byte[] NameServerIp4Address { get; private set; }
		public byte[] NameServerIp6Address { get; private set; }
		public string NameServerName { get; private set; }		
		public ReadOnlyCollection<NameResolutionRecord> Records { get; private set; }

		internal NameResolutionBlock(BinaryReader reader)
			: base(reader)
		{
			this.Records = this.ReadRecords(reader);
			this.ReadOptions(reader);
			this.ReadClosingField(reader);
		}

		override protected void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			switch (code)
			{
				case NameServerIp4AddressOptionCode:
					this.NameServerIp4Address = value;
					break;
				case NameServerIp6AddressOptionCode:
					this.NameServerIp6Address = value;
					break;
				case NameServerNameOptionCode:
					this.NameServerName = UTF8Encoding.UTF8.GetString(value);
					break;
			}
		}

		private ReadOnlyCollection<NameResolutionRecord> ReadRecords(BinaryReader reader)
		{
			var records = new List<NameResolutionRecord>();
			NameResolutionRecord record;
			do
			{
				record = new NameResolutionRecord(reader);
				records.Add(record);
			} 
			while (record.IpAddress != null);

			return new ReadOnlyCollection<NameResolutionRecord>(records);
		}
	}
}
