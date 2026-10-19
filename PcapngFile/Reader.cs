/*
Copyright (c) 2013-2026, Andrew Walsh
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

namespace PcapngFile
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Reads PCAP Next Generation files and generates CLR objects from its data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implemented according to the draft specification at https://datatracker.ietf.org/doc/draft-ietf-opsawg-pcapng/.
    /// </para>
    /// <para>
    /// Blocks are read sequentially, so the stream does not need to support seeking unless <see cref="Reset"/> is used.
    /// Sections of either byte order are supported, and blocks of types this library does not support are skipped.
    /// </para>
    /// </remarks>
    public class Reader : IDisposable
    {
        private const int BlockHeaderLength = BlockBase.BlockTypeLength + BlockBase.BlockTotalLengthLength;
        private const int ByteOrderMagicLength = 4;
        private const uint ByteOrderMagic = 0x1A2B3C4D;
        private const uint SwappedByteOrderMagic = 0x4D3C2B1A;
        private const int MinimumBlockLength = BlockHeaderLength + BlockBase.BlockTotalLengthLength;

        private readonly bool leaveOpen;
        private readonly Stream stream;

        // The byte order of the current section, set by its Section Header Block.
        private bool isBigEndian;

        // The next supported block, read ahead by a peek but not yet returned.
        private RawBlock pending;

        /// <summary>
        /// Get all blocks of all types upcast to the base class.
        /// </summary>
        public IEnumerable<BlockBase> AllBlocks => this.GetAllBlockIterator();

        /// <summary>
        /// Gets the underlying stream length.
        /// </summary>
        public long? BaseStreamLength => this.stream.CanSeek ? this.stream.Length : (long?)null;

        /// <summary>
        /// Gets the underlying stream position.
        /// </summary>
        /// <remarks>After a peek, this is past the peeked block.</remarks>
        public long? BaseStreamPosition => this.stream.CanSeek ? this.stream.Position : (long?)null;

        /// <summary>
        /// Get all enhanced packet blocks.
        /// </summary>
        public IEnumerable<EnhancedPacketBlock> EnhancedPacketBlocks => this.GetIterator<EnhancedPacketBlock>();

        /// <summary>
        /// Get all interface description blocks.
        /// </summary>
        public IEnumerable<InterfaceDescriptionBlock> InterfaceDescriptionBlocks => this.GetIterator<InterfaceDescriptionBlock>();

        /// <summary>
        /// Get all interface statistics blocks.
        /// </summary>
        public IEnumerable<InterfaceStatisticsBlock> InterfaceStatisticsBlocks => this.GetIterator<InterfaceStatisticsBlock>();

        /// <summary>
        /// Get all name resolution blocks.
        /// </summary>
        public IEnumerable<NameResolutionBlock> NameResolutionBlocks => this.GetIterator<NameResolutionBlock>();

        /// <summary>
        /// Get all packet blocks.
        /// </summary>
        public IEnumerable<PacketBlock> PacketBlocks => this.GetIterator<PacketBlock>();

        /// <summary>
        /// Get all section header blocks.
        /// </summary>
        public IEnumerable<SectionHeaderBlock> SectionHeaderBlocks => this.GetIterator<SectionHeaderBlock>();

        /// <summary>
        /// Get all simple packet blocks.
        /// </summary>
        public IEnumerable<SimplePacketBlock> SimplePacketBlocks => this.GetIterator<SimplePacketBlock>();

        /// <summary>
        /// Construct a PCAP-NG file reader from a given stream.
        /// </summary>
        /// <param name="stream">The stream of the PCAP-NG file being read. It does not need to support seeking.</param>
        /// <param name="leaveOpen">Leave stream open on reader <see cref="Dispose()"/>.</param>
        public Reader(Stream stream, bool leaveOpen = false)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Construct a PCAP-NG file reader from a given file name.
        /// </summary>
        /// <param name="filename">The filename of the PCAP-NG file being read.</param>
        public Reader(string filename)
                : this(File.OpenRead(filename), false)
        { }

        /// <summary>
        /// Releases the reader and, unless it was created with <c>leaveOpen</c>, the underlying stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the reader's resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.leaveOpen)
            {
                this.stream.Dispose();
            }
        }

        private IEnumerable<BlockBase> GetAllBlockIterator()
        {
            BlockBase block;
            while ((block = this.ReadBlock()) != null)
            {
                yield return block;
            }

            this.ResetIfSeekable();
        }

        private IEnumerable<T> GetIterator<T>() where T : BlockBase
        {
            var iteratorBlockType = GetStoreType<T>();
            RawBlock raw;
            while ((raw = this.PeekRawBlock()) != null)
            {
                this.pending = null;
                if (raw.Type == iteratorBlockType)
                {
                    yield return (T)CreateBlock(raw);
                }
            }

            this.ResetIfSeekable();
        }

        /// <summary>
        /// Gets the CLR class type from the store type enumeration.
        /// </summary>
        /// <param name="value">The store type enumeration.</param>
        /// <returns>The CLR class type from the store type enumeration.</returns>
        public static Type GetClrType(BlockType value)
        {
            return value switch
            {
                BlockType.EnhancedPacket => typeof(EnhancedPacketBlock),
                BlockType.InterfaceDescription => typeof(InterfaceDescriptionBlock),
                BlockType.InterfaceStatistics => typeof(InterfaceStatisticsBlock),
                BlockType.NameResolution => typeof(NameResolutionBlock),
                BlockType.None => null,
                BlockType.Packet => typeof(PacketBlock),
                BlockType.SectionHeader => typeof(SectionHeaderBlock),
                BlockType.SimplePacket => typeof(SimplePacketBlock),
                _ => throw new ArgumentOutOfRangeException("Unable to determine the block type (value = " + value + ")."),
            };
        }

        /// <summary>
        /// Gets the store type enumeration from the CLR class type.
        /// </summary>
        /// <typeparam name="T">The CLR type parameter.</typeparam>
        /// <returns>The store type enumeration from the CLR class type.</returns>
        public static BlockType GetStoreType<T>() where T : BlockBase
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
        /// Peek the CLR class type of the next block without consuming it.
        /// </summary>
        /// <returns>The CLR class type value or null if EOF is reached.</returns>
        public Type PeekClrType()
        {
            return GetClrType(PeekStoreType());
        }

        /// <summary>
        /// Peek the store type of the next block without consuming it.
        /// </summary>
        /// <returns>The store type or <see cref="BlockType.None"/> if EOF is reached.</returns>
        public BlockType PeekStoreType()
        {
            return this.PeekRawBlock()?.Type ?? BlockType.None;
        }

        /// <summary>
        /// Read the next block upcast to the base class.
        /// </summary>
        /// <returns>The next block upcast to the base class, or <c>null</c> if EOF is reached.</returns>
        /// <remarks>The return type will be the base class (<see cref="BlockBase"/>) but the actual type will be different.</remarks>
        public BlockBase ReadBlock()
        {
            var raw = this.PeekRawBlock();
            if (raw == null)
            {
                return null;
            }
            this.pending = null;
            return CreateBlock(raw);
        }

        /// <summary>
        /// Read the next block cast to the given class.
        /// </summary>
        /// <returns>The next block cast to the given class.</returns>
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
        /// <exception cref="NotSupportedException">The underlying stream does not support seeking.</exception>
        public void Reset()
        {
            if (!this.stream.CanSeek)
            {
                throw new NotSupportedException("Reset requires a stream that supports seeking.");
            }
            _ = this.stream.Seek(0, SeekOrigin.Begin);
            this.pending = null;
            this.isBigEndian = false;
        }

        private void ResetIfSeekable()
        {
            if (this.stream.CanSeek)
            {
                this.Reset();
            }
        }

        private static BlockBase CreateBlock(RawBlock raw)
        {
            using var reader = new EndianBinaryReader(raw.Data, raw.IsBigEndian);
            return raw.Type switch
            {
                BlockType.EnhancedPacket => new EnhancedPacketBlock(reader),
                BlockType.InterfaceDescription => new InterfaceDescriptionBlock(reader),
                BlockType.InterfaceStatistics => new InterfaceStatisticsBlock(reader),
                BlockType.NameResolution => new NameResolutionBlock(reader),
                BlockType.Packet => new PacketBlock(reader),
                BlockType.SectionHeader => new SectionHeaderBlock(reader),
                BlockType.SimplePacket => new SimplePacketBlock(reader),
                _ => throw new InvalidOperationException("Unsupported block type (type = " + raw.Type + ")."),
            };
        }

        private static bool IsSupported(BlockType type)
        {
            return type switch
            {
                BlockType.EnhancedPacket or
                BlockType.InterfaceDescription or
                BlockType.InterfaceStatistics or
                BlockType.NameResolution or
                BlockType.Packet or
                BlockType.SectionHeader or
                BlockType.SimplePacket => true,
                _ => false,
            };
        }

        // Returns the next supported block without consuming it, skipping unsupported ones, or null at EOF.
        private RawBlock PeekRawBlock()
        {
            while (this.pending == null)
            {
                var raw = this.ReadRawBlock();
                if (raw == null)
                {
                    return null;
                }
                if (IsSupported(raw.Type))
                {
                    this.pending = raw;
                }
            }
            return this.pending;
        }

        // Reads the next whole block from the stream, or returns null at EOF.
        private RawBlock ReadRawBlock()
        {
            var header = new byte[BlockHeaderLength + ByteOrderMagicLength];
            int headerLength = this.stream.ReadAtLeast(header.AsSpan(0, BlockHeaderLength), BlockHeaderLength, throwOnEndOfStream: false);
            if (headerLength == 0)
            {
                return null;
            }
            if (headerLength < BlockHeaderLength)
            {
                throw new EndOfStreamException("File ends in the middle of a block header.");
            }

            // The Section Header Block type reads the same in either byte order; its byte order magic sets the section's.
            if (BinaryPrimitives.ReadUInt32LittleEndian(header) == (uint)BlockType.SectionHeader)
            {
                headerLength += ByteOrderMagicLength;
                this.stream.ReadExactly(header, BlockHeaderLength, ByteOrderMagicLength);
                this.isBigEndian = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(BlockHeaderLength)) switch
                {
                    ByteOrderMagic => false,
                    SwappedByteOrderMagic => true,
                    var magic => throw new IOException("Section header byte order magic not recognized (magic = 0x" + magic.ToString("X8") + ")."),
                };
            }

            var type = (BlockType)this.ReadUInt32(header, 0);
            uint totalLength = this.ReadUInt32(header, BlockBase.BlockTypeLength);
            if (totalLength < MinimumBlockLength || totalLength % 4 != 0 || totalLength > int.MaxValue)
            {
                throw new IOException("Block total length is invalid (type = " + type + ", length = " + totalLength + ").");
            }

            var data = new byte[totalLength];
            header.AsSpan(0, headerLength).CopyTo(data);
            this.stream.ReadExactly(data, headerLength, data.Length - headerLength);
            return new RawBlock(type, data, this.isBigEndian);
        }

        private uint ReadUInt32(byte[] buffer, int offset)
        {
            var span = buffer.AsSpan(offset);
            return this.isBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(span) : BinaryPrimitives.ReadUInt32LittleEndian(span);
        }

        private sealed record RawBlock(BlockType Type, byte[] Data, bool IsBigEndian);
    }
}