/*
Copyright (c) 2013, Andrew Walsh
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
   This product includes software developed by the <organization>.
4. Neither the name of the <organization> nor the
   names of its contributors may be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY <COPYRIGHT HOLDER> ''AS IS'' AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

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
	/// Reads PCAP Next Generation files and generates CLR objects from its data.
	/// </summary>
	/// <remarks>
	/// Implemented according to the draft specification at http://www.winpcap.org/ntar/draft/PCAP-DumpFileFormat.html.
	/// </remarks>
	public class Reader
	{
		private BinaryReader reader;
		private FileStream stream;

		/// <summary>
		/// Get all blocks of all types upcast to the base class.
		/// </summary>		
		public IEnumerable<BlockBase> AllBlocks
		{
			get { return GetAllBlockIterator(); }
		}
		/// <summary>
		/// Get all enhanced packet blocks.
		/// </summary>
		public IEnumerable<EnhancedPacketBlock> EnhancedPacketBlocks
		{
			get { return GetIterator<EnhancedPacketBlock>((r) => new EnhancedPacketBlock(r)); }
		}
		/// <summary>
		/// Get the filename of the PCAP-NG file being read.
		/// </summary>
		public string Filename { get; private set; }
		/// <summary>
		/// Get all interface description blocks.
		/// </summary>
		public IEnumerable<InterfaceDescriptionBlock> InterfaceDescriptionBlocks
		{
			get { return GetIterator<InterfaceDescriptionBlock>((r) => new InterfaceDescriptionBlock(r)); }
		}
		/// <summary>
		/// Get all interface statistics blocks.
		/// </summary>
		public IEnumerable<InterfaceStatisticsBlock> InterfaceStatisticsBlocks
		{
			get { return GetIterator<InterfaceStatisticsBlock>((r) => new InterfaceStatisticsBlock(r)); }
		}
		/// <summary>
		/// Get all name resolution blocks.
		/// </summary>
		public IEnumerable<NameResolutionBlock> NameResolutionBlocks
		{
			get { return GetIterator<NameResolutionBlock>((r) => new NameResolutionBlock(r)); }
		}
		/// <summary>
		/// Get all packet blocks.
		/// </summary>
		public IEnumerable<PacketBlock> PacketBlocks
		{
			get { return GetIterator<PacketBlock>((r) => new PacketBlock(r)); }
		}
		/// <summary>
		/// Get all section header blocks.
		/// </summary>
		public IEnumerable<SectionHeaderBlock> SectionHeaderBlocks
		{
			get { return GetIterator<SectionHeaderBlock>((r) => new SectionHeaderBlock(r)); }
		}
		/// <summary>
		/// Get all simple packet blocks.
		/// </summary>
		public IEnumerable<SimplePacketBlock> SimplePacketBlocks
		{
			get { return GetIterator<SimplePacketBlock>((r) => new SimplePacketBlock(r)); }
		}

		/// <summary>
		/// Construct a PCAP-NG file reader.
		/// </summary>
		/// <param name="filename">The filename of the PCAP-NG file being read.</param>
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

		private IEnumerable<T> GetIterator<T>(Func<BinaryReader,T> blockGenerator)
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
					yield return blockGenerator(this.reader);
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

		/// <summary>
		/// Gets the CLR class type from the store type enumeration.
		/// </summary>
		/// <param name="value">The store type enumeration.</param>
		/// <returns>The CLR class type from the store type enumeration.</returns>
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

		/// <summary>
		/// Gets the store type enumeration from the CLR class type.
		/// </summary>
		/// <typeparam name="T">The CLR type parameter.</typeparam>
		/// <returns>The store type enumeration from the CLR class type.</returns>
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

		/// <summary>
		/// Peek the CLR class type value without moving the file cursor.
		/// </summary>
		/// <returns>The CLR class type value or null if EOF is reached.</returns>		
		public Type PeekClrType()
		{
			return GetClrType(PeekStoreType());
		}

		/// <summary>
		/// Peek the CLR class type value without moving the file cursor.
		/// </summary>
		/// <returns>The CLR class type value or <see cref="BlockType.None"/> if EOF is reached.</returns>	
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

		/// <summary>
		/// Read the next block from the file cursor upcast to the base class.
		/// </summary>
		/// <returns>The next block from the file cursor upcast to the base class.</returns>
		/// <remarks>The return type will be the base class (<see cref="BlockBaser"/>) but the actual type will be different.</remarks>
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

		/// <summary>
		/// Read the next block from the file cursor cast to the given class.
		/// </summary>
		/// <returns>The next block from the file cursor cast to the given class.</returns>
		/// <typeparam name="T">The expected class type to be read.</typeparam>
		/// <remarks>
		/// Make sure to <see cref="PeekClrType"/> first since an incorrect type parameter will result in an <see cref="InvalidCastException"/>.
		/// </remarks>
		public T ReadBlock<T>() where T : BlockBase
		{
			return (T)ReadBlock();
		}

		/// <summary>
		/// Reset the file cursor to the beginning.
		/// </summary>
		public void Reset()
		{
			this.reader.BaseStream.Seek(0, SeekOrigin.Begin);
		}
	}
}
