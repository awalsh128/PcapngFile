using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PcapngFile
{
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
