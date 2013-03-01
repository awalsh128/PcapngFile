using System;
using System.Collections.ObjectModel;
using System.IO;

namespace PcapngFile
{
	public class SimplePacketBlock : BlockBase
	{						
		public byte[] Data { get; private set; }
		public UInt32 DataLength { get; private set; }

		internal SimplePacketBlock(BinaryReader reader)
			: base(reader)
		{			
			this.DataLength = reader.ReadUInt32();
			this.Data = reader.ReadBytes((int)this.TotalLength - 16);	// 4 fields x 4 bytes			
			this.ReadClosingField(reader);
		}		
	}
}
