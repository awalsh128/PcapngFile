using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PcapngFile
{
	public class NameResolutionRecord
	{
		private const UInt16 EndRecordType = 0;
		private const UInt16 Ip4RecordType = 1;
		private const UInt16 Ip6RecordType = 2;

		private const int Ip4Length = 4;
		private const int Ip6Length = 8;
		private const int ValueAlignmentBoundary = 4;

		public bool IsIpVersion6 { get; private set; }
		public byte[] IpAddress { get; private set; }
		public ReadOnlyCollection<string> DnsEntries { get; private set; }

		internal NameResolutionRecord(BinaryReader reader)
		{
			int entriesLength;
			int remainderLength;
			UInt16 type = reader.ReadUInt16();
			int valueLength = reader.ReadUInt16();

			if (type != EndRecordType)
			{
				entriesLength = valueLength;

				if (type == Ip6RecordType)
				{
					this.IsIpVersion6 = true;
					this.IpAddress = reader.ReadBytes(Ip6Length);
					entriesLength -= Ip6Length;
				}
				else
				{
					this.IsIpVersion6 = false;
					this.IpAddress = reader.ReadBytes(Ip4Length);
					entriesLength -= Ip4Length;
				}
				this.DnsEntries = this.ReadDnsEntries(reader, entriesLength);

				remainderLength = valueLength % ValueAlignmentBoundary;
				if (remainderLength > 0)
				{
					reader.ReadBytes(ValueAlignmentBoundary - remainderLength);		// Read fill bytes to boundary.
				}
			}
		}

		private ReadOnlyCollection<string> ReadDnsEntries(BinaryReader reader, int entriesLength)
		{
			var entries = new List<string>();
			byte[] entriesBuffer = reader.ReadBytes(entriesLength);
			int i = 0, j = 0;
			while (j < entriesLength)
			{
				if (entriesBuffer[j] == 0)
				{
					entries.Add(UTF8Encoding.UTF8.GetString(entriesBuffer, i, j - i));
					i = ++j;
				}
				else
				{
					j++;
				}
			}
			return new ReadOnlyCollection<string>(entries);
		}
	}
}
