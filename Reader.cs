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

		public IEnumerable<BlockBase> AllBlocks
		{
			get { return GetAllBlockIterator(); }
		}
		public IEnumerable<EnhancedPacketBlock> EnhancedPacketBlocks
		{
			get { return GetEnhancedPacketBlockIterator(); }
		}
		public string Filename { get; private set; }
		public IEnumerable<InterfaceDescriptionBlock> InterfaceDescriptionBlocks
		{
			get { return GetInterfaceDescriptionBlockIterator(); }
		}
		public IEnumerable<InterfaceStatisticsBlock> InterfaceStatisticBlocks
		{
			get { return GetInterfaceStatisticsBlockIterator(); }
		}
		public IEnumerable<NameResolutionBlock> NameResolutionBlocks
		{
			get { return GetNameResolutionBlockIterator(); }
		}
		public IEnumerable<PacketBlock> PacketBlocks
		{
			get { return GetPacketBlockIterator(); }
		}
		public IEnumerable<SectionHeaderBlock> SectionHeaderBlocks
		{
			get { return GetSectionHeaderBlockIterator(); }
		}
		public IEnumerable<SimplePacketBlock> SimplePacketBlocks
		{
			get { return GetSimplePacketBlockIterator(); }
		}

		public Reader(string filename)
		{
			this.Filename = filename;
			this.stream = new FileStream(filename, FileMode.Open);
			this.reader = new BinaryReader(this.stream, Encoding.UTF8);			
		}

		private IEnumerable<BlockBase> GetAllBlockIterator()
		{
			BlockType type = BlockType.None;

			do
			{
				type = this.PeekStoreType();
				switch (type)
				{
					case BlockType.EnhancedPacket:
						yield return new EnhancedPacketBlock(this.reader);
						break;
					case BlockType.InterfaceDescription:
						yield return new InterfaceDescriptionBlock(this.reader);
						break;
					case BlockType.InterfaceStatistics:
						yield return new InterfaceStatisticsBlock(this.reader);
						break;
					case BlockType.NameResolution:
						yield return new NameResolutionBlock(this.reader);
						break;
					case BlockType.None:
						// EOF
						break;
					case BlockType.Packet:
						yield return new PacketBlock(this.reader);
						break;
					case BlockType.SectionHeader:
						yield return new SectionHeaderBlock(this.reader);
						break;
					case BlockType.SimplePacket:
						yield return new SimplePacketBlock(this.reader);
						break;
					default:
						throw new IOException("Unable to determine the block type (type = " + type + ").");
				}
			}
			while (type != BlockType.None);

			this.Reset();
		}

		private IEnumerable<EnhancedPacketBlock> GetEnhancedPacketBlockIterator()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.EnhancedPacket)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return new EnhancedPacketBlock(this.reader);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		private IEnumerable<InterfaceDescriptionBlock> GetInterfaceDescriptionBlockIterator()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.InterfaceDescription)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return new InterfaceDescriptionBlock(this.reader);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		private IEnumerable<InterfaceStatisticsBlock> GetInterfaceStatisticsBlockIterator()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.InterfaceStatistics)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return new InterfaceStatisticsBlock(this.reader);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		private IEnumerable<NameResolutionBlock> GetNameResolutionBlockIterator()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.NameResolution)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return new NameResolutionBlock(this.reader);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		private IEnumerable<PacketBlock> GetPacketBlockIterator()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.Packet)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return new PacketBlock(this.reader);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		private IEnumerable<SectionHeaderBlock> GetSectionHeaderBlockIterator()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.SectionHeader)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return new SectionHeaderBlock(this.reader);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		private IEnumerable<SimplePacketBlock> GetSimplePacketBlockIterator()
		{
			BlockType readType = BlockType.None;
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == BlockType.SimplePacket)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return new SimplePacketBlock(this.reader);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		private IEnumerable<T> GetByTypeIterator<T>() where T : BlockBase
		{			
			System.Reflection.BindingFlags activatorFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
			BlockType requestType = GetStoreType<T>();
			BlockType readType = BlockType.None;
			Type clrType = typeof(T);
			UInt32 blockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
			UInt32 totalBlockLength = 0;

			do
			{
				readType = (BlockType)this.reader.ReadUInt32();
				if (readType == requestType)
				{
					this.stream.Seek(-BlockBase.BlockTypeLength, SeekOrigin.Current);
					yield return (T)Activator.CreateInstance(clrType, activatorFlags, null, new object[] { this.reader }, null);
				}
				else
				{
					totalBlockLength = this.reader.ReadUInt32();
					this.stream.Seek(totalBlockLength - blockHeaderLength, SeekOrigin.Current);
				}
			}
			while (this.reader.PeekChar() > 0);

			this.Reset();
		}

		public Type GetClrType(BlockType value)
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

		public BlockType GetStoreType<T>() where T : BlockBase
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

		public Type PeekClrType()
		{
			return GetClrType(PeekStoreType());
		}

		public BlockType PeekStoreType()
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

		public BlockBase ReadBlock()
		{			
			BlockBase block;
			BlockType type = this.PeekStoreType();

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
				case BlockType.None:
					block = null;
					break;
				default:
					throw new IOException("Unable to determine the block type (type = " + type + ").");
			}

			return block;
		}

		public T ReadBlock<T>() where T : BlockBase
		{
			return (T)ReadBlock();
		}

		public void Reset()
		{
			this.reader.BaseStream.Seek(0, SeekOrigin.Begin);
		}
	}
}
