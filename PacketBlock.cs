using System;
using System.Collections.ObjectModel;
using System.IO;

namespace PcapngFile
{
	public class PacketBlock : BlockBase
	{
		private const UInt16 FlagsOptionCode = 2;
		private const UInt16 HashOptionCode = 3;		
	
		public UInt32 CapturedLength { get; private set; }
		public byte[] Data { get; private set; }
		public UInt32 DataLength { get; private set; }		
		public UInt16 DropsCount { get; private set; }
		public PacketOptionFlags Flags { get; private set; }
		public byte[] Hash { get; private set; }
		public HashAlgorithm HashAlgorithm { get; private set; }
		public UInt16 InterfaceID { get; private set; }		
		public DateTime Timestamp { get; private set; }

		internal PacketBlock(BinaryReader reader)
			: base(reader)
		{
			this.InterfaceID = reader.ReadUInt16();
			this.DropsCount = reader.ReadUInt16();
			this.Timestamp = DateTime.FromBinary(reader.ReadInt64());
			this.CapturedLength = reader.ReadUInt32();
			this.DataLength = reader.ReadUInt32();
			this.Data = reader.ReadBytes((int)this.CapturedLength);
			this.ReadOptions(reader);
			this.ReadClosingField(reader);
		}

		override protected void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			switch (code)
			{
				case FlagsOptionCode:
					this.Flags = new PacketOptionFlags(value);
					break;
				case HashOptionCode:
					this.HashAlgorithm = (HashAlgorithm)value[0];
					Array.Copy(value, 1, this.Hash, 0, value.Length - 1);
					break;
			}
		}
	}
}
