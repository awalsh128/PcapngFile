using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PcapngFile
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// http://www.winpcap.org/ntar/draft/PCAP-DumpFileFormat.html
	/// </remarks>
	public class Reader
	{
		private BinaryReader reader;
		private FileStream stream;

		public string Filename { get; private set; }

		public Reader(string filename)
		{
			this.Filename = filename;
			this.stream = new FileStream(filename, FileMode.Open);
			this.reader = new BinaryReader(this.stream, Encoding.UTF8);
		}

		private Type GetClrType(BlockType value)
		{
			switch (value)
			{
				case BlockType.EnhancedPacket:
					return typeof(EnhancedPacketBlock);
				case BlockType.InterfaceDescription:
					return typeof(InterfaceDescriptionBlock);
				case BlockType.InterfaceStatistics:
					return typeof(InterfaceStatisticsBlock);
				case BlockType.NameResolution:
					return typeof(NameResolutionBlock);
				case BlockType.None:
					return null;
				case BlockType.Packet:
					return typeof(PacketBlock);
				case BlockType.SectionHeader:
					return typeof(SectionHeaderBlock);
				case BlockType.SimplePacket:
					return typeof(SimplePacketBlock);
				default:
					throw new ArgumentOutOfRangeException("Unable to determine the block type (value = " + value + ").");
			}
		}

		private BlockType GetStoreType<T>() where T : BlockBase
		{
			Type clrType = typeof(T);
			if (clrType == typeof(EnhancedPacketBlock))
			{
				return BlockType.EnhancedPacket;
			}
			if (clrType == typeof(InterfaceDescriptionBlock))
			{
				return BlockType.InterfaceDescription;
			}
			if (clrType == typeof(InterfaceStatisticsBlock))
			{
				return BlockType.InterfaceStatistics;
			}
			if (clrType == typeof(NameResolutionBlock))
			{
				return BlockType.NameResolution;
			}
			if (clrType == typeof(PacketBlock))
			{
				return BlockType.Packet;
			}
			if (clrType == typeof(SectionHeaderBlock))
			{
				return BlockType.SectionHeader;
			}
			if (clrType == typeof(SimplePacketBlock))
			{
				return BlockType.SimplePacket;
			}
			throw new ArgumentException("CLR type not mapped to a valid BlockType (typeof(T) = " + typeof(T) + ").");
		}

		public BlockType PeekType()
		{
			if (reader.PeekChar() > 0)
			{
				BlockType type = (BlockType)this.reader.ReadUInt32();
				this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
				return type;
			}
			else
			{
				return BlockType.None;
			}
		}

		public List<BlockBase> ReadAllBlocks()
		{
			BlockType type = BlockType.None;
			var result = new List<BlockBase>();

			do
			{
				type = this.PeekType();
				switch (type)
				{
					case BlockType.EnhancedPacket:
						result.Add(new EnhancedPacketBlock(this.reader));
						break;
					case BlockType.InterfaceDescription:
						result.Add(new InterfaceDescriptionBlock(this.reader));
						break;
					case BlockType.InterfaceStatistics:
						result.Add(new InterfaceStatisticsBlock(this.reader));
						break;
					case BlockType.NameResolution:
						result.Add(new NameResolutionBlock(this.reader));
						break;
					case BlockType.None:
						// EOF
						break;
					case BlockType.Packet:
						result.Add(new PacketBlock(this.reader));
						break;
					case BlockType.SectionHeader:
						result.Add(new SectionHeaderBlock(this.reader));
						break;
					case BlockType.SimplePacket:
						result.Add(new SimplePacketBlock(this.reader));
						break;
					default:
						throw new IOException("Unable to determine the block type (type = " + type + ").");
				}
			}
			while (type != BlockType.None);

			return result;
		}

		public List<EnhancedPacketBlock> ReadAllEnhancedPacketBlocks()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;
			var result = new List<EnhancedPacketBlock>();

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.EnhancedPacket)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					result.Add(new EnhancedPacketBlock(this.reader));
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			return result;
		}

		public T ReadBlock<T>() where T : BlockBase
		{
			BlockBase block;
			BlockType type = this.PeekType();

			switch (type)
			{
				case BlockType.EnhancedPacket:
					block = new EnhancedPacketBlock(this.reader);
					break;
				case BlockType.InterfaceDescription:
					block = new InterfaceDescriptionBlock(this.reader);
					break;
				case BlockType.InterfaceStatistics:
					block = new InterfaceStatisticsBlock(this.reader);
					break;
				case BlockType.NameResolution:
					block = new NameResolutionBlock(this.reader);
					break;
				case BlockType.Packet:
					block = new PacketBlock(this.reader);
					break;
				case BlockType.SectionHeader:
					block = new SectionHeaderBlock(this.reader);
					break;
				case BlockType.SimplePacket:
					block = new SimplePacketBlock(this.reader);
					break;
				default:
					throw new IOException("Unable to determine the block type (type = " + type + ").");
			}

			return (T)block;
		}

		//public List<T> ReadBlocks<T>() where T : BlockBase
		//{
		//	BlockType readType = BlockType.None;
		//	BlockType requestedType = this.GetStoreType<T>();
		//	Func<BinaryReader, T> ctor;
		//	var result = new List<T>();

		//	switch (readType)
		//	{
		//		case BlockType.EnhancedPacket:
		//			ctor = (reader) => (T)new EnhancedPacketBlock(reader);
		//			break;
		//		case BlockType.InterfaceDescription:
		//			result.Add(new InterfaceDescriptionBlock(this.reader));
		//			break;
		//		case BlockType.InterfaceStatistics:
		//			result.Add(new InterfaceStatisticsBlock(this.reader));
		//			break;
		//		case BlockType.NameResolution:
		//			result.Add(new NameResolutionBlock(this.reader));
		//			break;
		//		case BlockType.Packet:
		//			result.Add(new PacketBlock(this.reader));
		//			break;
		//		case BlockType.SectionHeader:
		//			result.Add(new SectionHeaderBlock(this.reader));
		//			break;
		//		case BlockType.SimplePacket:
		//			result.Add(new SimplePacketBlock(this.reader));
		//			break;
		//		default:
		//			throw new IOException("Unable to determine the block type (type = " + readType + ").");
		//	}

		//	do
		//	{
		//		readType = this.PeekType();
		//		if (readType == requestedType)
		//		{
		//			result.Add(ctor(reader));
		//		}				
		//	}
		//	while (readType != BlockType.None);

		//	return result;
		//}
	}
}
