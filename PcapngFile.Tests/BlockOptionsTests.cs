namespace PcapngFile.Tests
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Linq;
    using System.Text;

    using NUnit.Framework;

    [TestFixture]
    public class BlockOptionsTests
    {
        private const long PacketTimestamp = 0x0005_1234_89AB_CDEF;
        private const long StatisticsStartTime = 0x0005_1234_0000_0001;
        private const uint PacketFlags = 0x01 /* inbound */ | (2 << 2) /* multicast */ | (4 << 5) /* FCS length */ | 0x0100_0000 /* CRC error */;
        private const uint CustomBlockType = 0x00000BAD;

        private static readonly byte[] Ip6Address = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        // Writes the multi-byte fields of a section in its byte order.
        private sealed class SectionWriter : BinaryWriter
        {
            private readonly bool isBigEndian;

            public SectionWriter(Stream stream, bool isBigEndian)
                : base(stream, Encoding.UTF8, leaveOpen: true)
            {
                this.isBigEndian = isBigEndian;
            }

            public override void Write(ushort value) => base.Write(this.isBigEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

            public override void Write(int value) => base.Write(this.isBigEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

            public override void Write(uint value) => base.Write(this.isBigEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

            public override void Write(long value) => base.Write(this.isBigEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

            public SectionWriter CreateBodyWriter(Stream stream) => new SectionWriter(stream, this.isBigEndian);

            public byte[] Bytes(Action<SectionWriter> write)
            {
                var stream = new MemoryStream();
                using (var writer = this.CreateBodyWriter(stream))
                {
                    write(writer);
                }
                return stream.ToArray();
            }
        }

        // Writes one section with one of each block type, each carrying options, and a custom block to be skipped.
        private static void WriteSection(Stream stream, bool isBigEndian, string userApplication = "test app")
        {
            using var writer = new SectionWriter(stream, isBigEndian);

            WriteBlock(writer, (uint)BlockType.SectionHeader, body =>
            {
                body.Write(0x1A2B3C4Du);
                body.Write((ushort)1);
                body.Write((ushort)0);
                body.Write(-1L);
                WriteOption(body, 4, Encoding.UTF8.GetBytes(userApplication)); // shb_userappl
                WriteEndOfOptions(body);
            });

            WriteBlock(writer, (uint)BlockType.InterfaceDescription, body =>
            {
                body.Write((ushort)LinkType.Ethernet);
                body.Write((ushort)0);
                body.Write(65535);
                WriteOption(body, 2, Encoding.UTF8.GetBytes("eth0")); // if_name
                WriteOption(body, 9, new byte[] { 9 }); // if_tsresol
                WriteOption(body, 14, writer.Bytes(w => w.Write(-5L))); // if_tsoffset
                WriteEndOfOptions(body);
            });

            WriteBlock(writer, CustomBlockType, body =>
            {
                body.Write(32473u); // Private Enterprise Number reserved for documentation.
                body.Write(new byte[] { 1, 2, 3, 4 });
            });

            WriteBlock(writer, (uint)BlockType.EnhancedPacket, body =>
            {
                body.Write(0);
                WriteTimestamp(body, PacketTimestamp);
                body.Write(5);
                body.Write(60);
                body.Write(new byte[] { 1, 2, 3, 4, 5, 0, 0, 0 }); // Data padded to 32 bits.
                WriteOption(body, 1, Encoding.UTF8.GetBytes("a comment")); // opt_comment
                WriteOption(body, 2, writer.Bytes(w => w.Write(PacketFlags))); // epb_flags
                WriteOption(body, 3, new byte[] { 2, 0xDE, 0xAD, 0xBE, 0xEF }); // epb_hash (CRC32)
                WriteOption(body, 4, writer.Bytes(w => w.Write(7L))); // epb_dropcount
                WriteEndOfOptions(body);
            });

            WriteBlock(writer, (uint)BlockType.NameResolution, body =>
            {
                WriteRecord(body, 1, new byte[] { 192, 168, 0, 1 }, "host4");
                WriteOption(body, 0x0123, new byte[] { 1, 2, 3 }); // Unknown record type, skipped.
                WriteRecord(body, 2, Ip6Address, "host6");
                body.Write((ushort)0); // nrb_record_end
                body.Write((ushort)0);
                WriteOption(body, 2, Encoding.UTF8.GetBytes("dns")); // ns_dnsname
                WriteEndOfOptions(body);
            });

            WriteBlock(writer, (uint)BlockType.InterfaceStatistics, body =>
            {
                body.Write(0);
                WriteTimestamp(body, PacketTimestamp);
                WriteOption(body, 2, writer.Bytes(w => WriteTimestamp(w, StatisticsStartTime))); // isb_starttime
                WriteOption(body, 4, writer.Bytes(w => w.Write(100L))); // isb_ifrecv
                // No end of options marker; the options run to the end of the block.
            });
        }

        private static Reader CreateReader(bool isBigEndian)
        {
            var stream = new MemoryStream();
            WriteSection(stream, isBigEndian);
            stream.Position = 0;
            return new Reader(stream);
        }

        private static void WriteBlock(SectionWriter writer, uint type, Action<SectionWriter> writeBody)
        {
            var bodyStream = new MemoryStream();
            using (var bodyWriter = writer.CreateBodyWriter(bodyStream))
            {
                writeBody(bodyWriter);
            }
            var body = bodyStream.ToArray();
            Assert.That(body.Length % 4, Is.Zero, "Block body must be padded to 32 bits.");

            uint totalLength = (uint)(body.Length + 12);
            writer.Write(type);
            writer.Write(totalLength);
            writer.Write(body);
            writer.Write(totalLength);
        }

        private static void WriteOption(SectionWriter writer, ushort code, byte[] value)
        {
            writer.Write(code);
            writer.Write((ushort)value.Length);
            writer.Write(value);
            writer.Write(new byte[(4 - (value.Length % 4)) % 4]);
        }

        private static void WriteEndOfOptions(SectionWriter writer)
        {
            writer.Write(0u);
        }

        private static void WriteRecord(SectionWriter writer, ushort type, byte[] address, string name)
        {
            var value = address.Concat(Encoding.UTF8.GetBytes(name)).Append((byte)0).ToArray();
            WriteOption(writer, type, value);
        }

        private static void WriteTimestamp(SectionWriter writer, long timestamp)
        {
            writer.Write((uint)(timestamp >> 32));
            writer.Write((uint)timestamp);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsSectionHeaderOptions(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var block = reader.SectionHeaderBlocks.Single();
            Assert.That(block.ByteOrderMagic, Is.EqualTo(0x1A2B3C4D));
            Assert.That(block.MajorVersion, Is.EqualTo(1));
            Assert.That(block.UserApplication, Is.EqualTo("test app"));
            Assert.That(block.SectionLength, Is.EqualTo(-1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsInterfaceDescriptionOptions(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var block = reader.InterfaceDescriptionBlocks.Single();
            Assert.That(block.LinkType, Is.EqualTo(LinkType.Ethernet));
            Assert.That(block.Name, Is.EqualTo("eth0"));
            Assert.That(block.TimestampResolution, Is.EqualTo(9));
            Assert.That(block.TimeOffsetSeconds, Is.EqualTo(-5));
            Assert.That(block.SnapLength, Is.EqualTo(65535));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsEnhancedPacketOptions(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var block = reader.EnhancedPacketBlocks.Single();
            Assert.That(block.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(block.CapturedLength, Is.EqualTo(5));
            Assert.That(block.DataLength, Is.EqualTo(60));
            Assert.That(block.Timestamp, Is.EqualTo(PacketTimestamp));
            Assert.That(block.Comment, Is.EqualTo("a comment"));
            Assert.That(block.DropCount, Is.EqualTo(7));
            Assert.That(block.HashAlgorithm, Is.EqualTo(HashAlgorithm.Crc32));
            Assert.That(block.Hash, Is.EqualTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));
            Assert.That(block.Flags, Is.Not.Null);
            Assert.That(block.Flags.IsInbound(), Is.True);
            Assert.That(block.Flags.IsMulticast(), Is.True);
            Assert.That(block.Flags.GetFcsLength(), Is.EqualTo(4));
            Assert.That(block.Flags.ContainsLinkLayerCrcError(), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GetTimestampAppliesInterfaceResolutionAndOffset(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var blocks = reader.AllBlocks.ToArray();
            var iface = blocks.OfType<InterfaceDescriptionBlock>().Single();
            var packet = blocks.OfType<EnhancedPacketBlock>().Single();

            // The interface uses nanoseconds (if_tsresol 9) and an if_tsoffset of -5 seconds.
            var timestamp = packet.GetTimestamp(iface, out bool precisionLoss);
            Assert.That(timestamp, Is.EqualTo(DateTime.UnixEpoch.AddTicks(PacketTimestamp / 100).AddSeconds(-5)));
            Assert.That(timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(precisionLoss, Is.EqualTo(PacketTimestamp % 100 != 0));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsNameResolutionRecordsAndOptions(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var block = reader.NameResolutionBlocks.Single();
            Assert.That(block.NameServerName, Is.EqualTo("dns"));
            Assert.That(block.Records, Has.Count.EqualTo(2));

            Assert.That(block.Records[0].IsIpVersion6, Is.False);
            Assert.That(block.Records[0].IpAddress, Is.EqualTo(new byte[] { 192, 168, 0, 1 }));
            Assert.That(block.Records[0].DnsEntries, Is.EqualTo(new[] { "host4" }));

            Assert.That(block.Records[1].IsIpVersion6, Is.True);
            Assert.That(block.Records[1].IpAddress, Is.EqualTo(Ip6Address));
            Assert.That(block.Records[1].DnsEntries, Is.EqualTo(new[] { "host6" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsInterfaceStatisticsOptionsWithoutEndMarker(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var block = reader.InterfaceStatisticsBlocks.Single();
            Assert.That(block.Timestamp, Is.EqualTo(PacketTimestamp));
            Assert.That(block.StartTime, Is.EqualTo(StatisticsStartTime));
            Assert.That(block.PacketsReceived, Is.EqualTo(100));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsAllBlocksInOrderSkippingUnsupportedBlocks(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var types = reader.AllBlocks.Select(b => b.StoreType).ToArray();
            Assert.That(types, Is.EqualTo(new[]
            {
                BlockType.SectionHeader,
                BlockType.InterfaceDescription,
                BlockType.EnhancedPacket,
                BlockType.NameResolution,
                BlockType.InterfaceStatistics,
            }));
        }

        [Test]
        public void ReadsSectionsOfDifferentByteOrder()
        {
            var stream = new MemoryStream();
            WriteSection(stream, isBigEndian: false, userApplication: "little");
            WriteSection(stream, isBigEndian: true, userApplication: "big");
            WriteSection(stream, isBigEndian: false, userApplication: "little again");
            stream.Position = 0;

            using var reader = new Reader(stream);
            Assert.That(reader.SectionHeaderBlocks.Select(b => b.UserApplication), Is.EqualTo(new[] { "little", "big", "little again" }));
            Assert.That(reader.EnhancedPacketBlocks.Select(b => b.Timestamp), Is.All.EqualTo(PacketTimestamp));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsUnseekableStream(bool isBigEndian)
        {
            var stream = new MemoryStream();
            WriteSection(stream, isBigEndian);
            using var reader = new Reader(new UnseekableStream(stream.ToArray()));

            Assert.That(reader.PeekStoreType(), Is.EqualTo(BlockType.SectionHeader));
            Assert.That(reader.ReadBlock(), Is.InstanceOf<SectionHeaderBlock>());
            Assert.That(reader.PeekClrType(), Is.EqualTo(typeof(InterfaceDescriptionBlock)));
            Assert.That(reader.EnhancedPacketBlocks.Single().Data, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(reader.ReadBlock(), Is.Null);
            Assert.That(reader.BaseStreamPosition, Is.Null);
            Assert.Throws<NotSupportedException>(() => reader.Reset());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsBlockWithUnusedSpaceAfterOptions(bool isBigEndian)
        {
            var stream = new MemoryStream();
            using (var writer = new SectionWriter(stream, isBigEndian))
            {
                WriteBlock(writer, (uint)BlockType.SectionHeader, body =>
                {
                    body.Write(0x1A2B3C4Du);
                    body.Write((ushort)1);
                    body.Write((ushort)0);
                    body.Write(-1L);
                    WriteEndOfOptions(body);
                    body.Write(new byte[8]); // Unused space before the closing length field.
                });
                WriteBlock(writer, (uint)BlockType.InterfaceDescription, body =>
                {
                    body.Write((ushort)LinkType.Ethernet);
                    body.Write((ushort)0);
                    body.Write(96);
                });
            }
            stream.Position = 0;

            using var reader = new Reader(stream);
            var blocks = reader.AllBlocks.ToArray();
            Assert.That(blocks.Select(b => b.StoreType), Is.EqualTo(new[] { BlockType.SectionHeader, BlockType.InterfaceDescription }));
            Assert.That(((InterfaceDescriptionBlock)blocks[1]).SnapLength, Is.EqualTo(96));
            Assert.That(((InterfaceDescriptionBlock)blocks[1]).TimestampResolution, Is.EqualTo(6), "Default if_tsresol.");
        }

        [Test]
        public void ThrowsOnUnrecognizedByteOrderMagic()
        {
            var stream = new MemoryStream();
            WriteSection(stream, isBigEndian: false);
            var bytes = stream.ToArray();
            bytes[8] = 0xFF; // First byte of the byte order magic.

            using var reader = new Reader(new MemoryStream(bytes));
            Assert.Throws<IOException>(() => reader.ReadBlock());
        }

        [Test]
        public void ReadsReadOnlyFile()
        {
            var path = Path.GetTempFileName();
            try
            {
                using (var file = File.Create(path))
                {
                    WriteSection(file, isBigEndian: false);
                }
                File.SetAttributes(path, FileAttributes.ReadOnly);

                using var reader = new Reader(path);
                Assert.That(reader.AllBlocks.Count(), Is.EqualTo(5));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        private sealed class UnseekableStream : MemoryStream
        {
            public UnseekableStream(byte[] buffer)
                : base(buffer, writable: false)
            {
            }

            public override bool CanSeek => false;

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override long Length => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
        }
    }
}
