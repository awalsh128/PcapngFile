using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PcapngFile
{
	public class EnhancedPacketBlock : BlockBase
	{		
		private const int DataAlignmentBoundary = 4;
		private const UInt16 DropCountOptionCode = 4;
		private const UInt32 DropCountOptionHeader = (DropCountOptionCode << 16) | 8;
		private const UInt16 FlagsOptionCode = 2;
		private const UInt32 FlagsOptionHeader = (FlagsOptionCode << 16) | 4;
		private const UInt16 HashOptionCode = 3;
		//private const UInt32 HashOptionHeader = (HashOptionCode << 16) | <variable depending on code>
		private const int HeaderAndFooterLength = (7 * 4) + BlockTotalLengthLength;	// 7 4-byte header fields + 1 4-byte footer field

		public int CapturedLength { get; private set; }		
		public byte[] Data { get; private set; }
		public int DataLength { get; private set; }
		public long DropCount { get; private set; }		
		public byte[] Hash { get; private set; }
		public HashAlgorithm HashAlgorithm { get; private set; }
		public int InterfaceID { get; private set; }
		public PacketOptionFlags Flags { get; private set; }
		public long Timestamp { get; private set; }

		internal EnhancedPacketBlock(BinaryReader reader)
			: base(reader)
		{
			int paddingLength;
			int remainderLength;
			int totalExceptOptionLength;

			this.InterfaceID = reader.ReadInt32();
			this.Timestamp = reader.ReadInt64();
			this.CapturedLength = reader.ReadInt32();
			this.DataLength = reader.ReadInt32();
			this.Data = reader.ReadBytes((int)this.CapturedLength);

			remainderLength = (int)this.DataLength % DataAlignmentBoundary;
			if (remainderLength > 0)
			{
				paddingLength = DataAlignmentBoundary - remainderLength;				
				reader.ReadBytes(paddingLength);				
				totalExceptOptionLength = HeaderAndFooterLength + this.CapturedLength + paddingLength;
			}
			else
			{
				totalExceptOptionLength = HeaderAndFooterLength + this.CapturedLength;
			}

			if (this.TotalLength != totalExceptOptionLength)
			{
				this.ReadOptions(reader);
			}
			this.ReadClosingField(reader);
		}

		public DateTime GetTimestamp(InterfaceDescriptionBlock block)
		{
			return DateTime.FromBinary(this.Timestamp / block.GetTimePrecisionDivisor());
		}

		protected override void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			switch (code)
			{				
				case DropCountOptionCode:
					this.DropCount = BitConverter.ToInt64(value, 0);
					break;
				case FlagsOptionCode:
					this.Flags = new PacketOptionFlags(value);
					break;
				case HashOptionCode:
					this.HashAlgorithm = this.GetHashAlgorithm(value[0]);
					Array.Copy(value, 1, this.Hash, 0, value.Length - 1);
					break;
			}
		}

		//protected override void OnSerialize(StreamWriter writer)
		//{
		//	int paddingLength;
		//	bool optionWritten;
		//	int remainderLength;

		//	writer.Write(this.InterfaceID);
		//	writer.Write(this.Timestamp);
		//	writer.Write(this.CapturedLength);
		//	writer.Write(this.DataLength);
		//	writer.Write(this.Data);
			
		//	remainderLength = (int)this.DataLength % DataAlignmentBoundary;
		//	if (remainderLength > 0)
		//	{
		//		paddingLength = DataAlignmentBoundary - remainderLength;
		//		writer.Write(new byte[paddingLength]);				
		//	}

		//	optionWritten = false;
		//	if (this.Flags != null)
		//	{
		//		writer.Write(FlagsOptionHeader);				
		//		writer.Write(this.Flags.GetOptionValue());
		//		optionWritten = true;
		//	}
		//	if (this.HashAlgorithm != PcapngFile.HashAlgorithm.None)
		//	{				
		//		writer.Write(this.GetHashAlgorithmCode(this.HashAlgorithm));
		//		optionWritten = true;
		//	}
		//	if (this.DropCount > 0)
		//	{
		//		writer.Write(this.DropCount);
		//		optionWritten = true;
		//	}
		//	if (optionWritten)
		//	{
		//		writer.Write(EndOptionField);
		//	}
		//}
	}
}
