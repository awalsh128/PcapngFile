using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PcapngFile
{
	public class SectionHeaderBlock : BlockBase
	{		
		private const UInt16 HardwareOptionCode = 2;
		private const UInt16 OperatingSystemOptionCode = 3;
		private const UInt16 UserApplicationOptionCode = 4;

		public UInt32 ByteOrderMagic { get; private set; }		
		public string Hardware { get; private set; }
		public UInt16 MajorVersion { get; private set; }
		public UInt16 MinorVersion { get; private set; }
		public string OperatingSystem { get; private set; }
		public Int64 SectionLength { get; private set; }
		public string UserApplication { get; private set; }

		internal SectionHeaderBlock(BinaryReader reader)
			: base(reader)
		{
			this.ByteOrderMagic = reader.ReadUInt32();
			this.MajorVersion = reader.ReadUInt16();
			this.MinorVersion = reader.ReadUInt16();
			this.SectionLength = reader.ReadInt64();
			this.ReadOptions(reader);			
			this.ReadClosingField(reader);
		}

		override protected void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			switch (code)
			{				
				case HardwareOptionCode:
					this.Hardware = UTF8Encoding.UTF8.GetString(value);
					break;
				case OperatingSystemOptionCode:
					this.OperatingSystem = UTF8Encoding.UTF8.GetString(value);
					break;
				case UserApplicationOptionCode:
					this.UserApplication = UTF8Encoding.UTF8.GetString(value);
					break;
			}
		}
	}
}
