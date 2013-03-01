using System;
using System.Collections.ObjectModel;
using System.IO;

namespace PcapngFile
{
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
			this.ReadOptions(reader);
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
