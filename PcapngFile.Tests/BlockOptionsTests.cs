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

            public override void Write(ulong value) => base.Write(this.isBigEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

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

        private static void WriteSectionHeader(SectionWriter writer, ushort majorVersion = 1, ushort minorVersion = 0, string userApplication = null)
        {
            WriteBlock(writer, (uint)BlockType.SectionHeader, body =>
            {
                body.Write(0x1A2B3C4Du);
                body.Write(majorVersion);
                body.Write(minorVersion);
                body.Write(-1L);
                if (userApplication != null)
                {
                    WriteOption(body, 4, Encoding.UTF8.GetBytes(userApplication)); // shb_userappl
                    WriteEndOfOptions(body);
                }
            });
        }

        private static void WriteInterface(SectionWriter writer, int snapLength, Action<SectionWriter> writeOptions = null)
        {
            WriteBlock(writer, (uint)BlockType.InterfaceDescription, body =>
            {
                body.Write((ushort)LinkType.Ethernet);
                body.Write((ushort)0);
                body.Write(snapLength);
                if (writeOptions != null)
                {
                    writeOptions(body);
                    WriteEndOfOptions(body);
                }
            });
        }

        private static Reader CreateReader(bool isBigEndian, Action<SectionWriter> write)
        {
            var stream = new MemoryStream();
            using (var writer = new SectionWriter(stream, isBigEndian))
            {
                write(writer);
            }
            stream.Position = 0;
            return new Reader(stream);
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

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsRepeatedNewAndCustomInterfaceOptions(bool isBigEndian)
        {
            var ipv6 = Ip6Address.Append((byte)64).ToArray();
            using var reader = CreateReader(isBigEndian, writer =>
            {
                WriteSectionHeader(writer);
                WriteInterface(writer, 0, body =>
                {
                    WriteOption(body, 1, Encoding.UTF8.GetBytes("first")); // opt_comment
                    WriteOption(body, 1, Encoding.UTF8.GetBytes("second"));
                    WriteOption(body, 2, Encoding.UTF8.GetBytes("eth0\0ignored")); // if_name, ends at the zero byte
                    WriteOption(body, 4, new byte[] { 10, 0, 0, 1, 255, 0, 0, 0 }); // if_IPv4addr
                    WriteOption(body, 4, new byte[] { 10, 0, 0, 2, 255, 0, 0, 0 });
                    WriteOption(body, 5, ipv6); // if_IPv6addr
                    WriteOption(body, 6, new byte[] { 0 }); // if_MACaddr with an invalid length
                    WriteOption(body, 15, Encoding.UTF8.GetBytes("NIC 9000")); // if_hardware
                    WriteOption(body, 16, writer.Bytes(w => w.Write(1_000_000UL))); // if_txspeed
                    WriteOption(body, 17, writer.Bytes(w => w.Write(2_000_000UL))); // if_rxspeed
                    WriteOption(body, 18, Encoding.UTF8.GetBytes("Europe/Berlin")); // if_iana_tzname
                    WriteOption(body, 2988, writer.Bytes(w => { w.Write(32473u); w.Write(Encoding.UTF8.GetBytes("text")); }));
                    WriteOption(body, 19373, writer.Bytes(w => { w.Write(32473u); w.Write(new byte[] { 1, 2 }); }));
                });
            });

            var block = reader.InterfaceDescriptionBlocks.Single();
            Assert.That(block.Comment, Is.EqualTo("first"));
            Assert.That(block.Comments, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(block.Name, Is.EqualTo("eth0"));
            Assert.That(block.IPv4Address, Is.EqualTo(new byte[] { 10, 0, 0, 1, 255, 0, 0, 0 }));
            Assert.That(block.IPv4Addresses.Select(a => a[3]), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(block.IPv6Addresses.Single(), Is.EqualTo(ipv6));
            Assert.That(block.MacAddress, Is.Null);
            Assert.That(block.InvalidOptionCodes, Is.EqualTo(new ushort[] { 6 }));
            Assert.That(block.Hardware, Is.EqualTo("NIC 9000"));
            Assert.That(block.TransmitSpeed, Is.EqualTo(1_000_000UL));
            Assert.That(block.ReceiveSpeed, Is.EqualTo(2_000_000UL));
            Assert.That(block.IanaTimeZoneName, Is.EqualTo("Europe/Berlin"));

            Assert.That(block.CustomOptions, Has.Count.EqualTo(2));
            Assert.That(block.CustomOptions[0].PrivateEnterpriseNumber, Is.EqualTo(32473));
            Assert.That(block.CustomOptions[0].IsString, Is.True);
            Assert.That(block.CustomOptions[0].IsCopyable, Is.True);
            Assert.That(block.CustomOptions[0].GetString(), Is.EqualTo("text"));
            Assert.That(block.CustomOptions[1].IsString, Is.False);
            Assert.That(block.CustomOptions[1].IsCopyable, Is.False);
            Assert.That(block.CustomOptions[1].Data, Is.EqualTo(new byte[] { 1, 2 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsRepeatedAndNewPacketOptions(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian, writer =>
            {
                WriteSectionHeader(writer);
                WriteInterface(writer, 0);
                WriteBlock(writer, (uint)BlockType.EnhancedPacket, body =>
                {
                    body.Write(0);
                    WriteTimestamp(body, 1);
                    body.Write(0);
                    body.Write(0);
                    WriteOption(body, 2, writer.Bytes(w => w.Write(0x0E00u))); // epb_flags bits 9, 10 and 11
                    WriteOption(body, 3, new byte[] { 5, 1, 2, 3, 4 }); // epb_hash, Toeplitz
                    WriteOption(body, 3, new byte[] { 99, 7 }); // epb_hash, unknown algorithm
                    WriteOption(body, 5, writer.Bytes(w => w.Write(42UL))); // epb_packetid
                    WriteOption(body, 6, writer.Bytes(w => w.Write(3u))); // epb_queue
                    WriteOption(body, 7, writer.Bytes(w => { w.Write((byte)2); w.Write(2UL); })); // epb_verdict, XDP_PASS
                    WriteOption(body, 7, new byte[] { 0, 0xAA, 0xBB }); // epb_verdict, hardware
                    WriteOption(body, 8, writer.Bytes(w => { w.Write(1234u); w.Write(0u); })); // epb_processid_threadid
                    WriteOption(body, 8, writer.Bytes(w => { w.Write(5678u); w.Write(9u); }));
                    WriteEndOfOptions(body);
                });
            });

            var block = reader.EnhancedPacketBlocks.Single();
            Assert.That(block.HashAlgorithm, Is.EqualTo(HashAlgorithm.Toeplitz));
            Assert.That(block.Hash, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(block.Hashes.Select(h => h.Algorithm), Is.EqualTo(new[] { HashAlgorithm.Toeplitz, (HashAlgorithm)99 }));
            Assert.That(block.PacketId, Is.EqualTo(42UL));
            Assert.That(block.Queue, Is.EqualTo(3u));
            Assert.That(block.Verdicts, Has.Count.EqualTo(2));
            Assert.That(block.Verdicts[0].Type, Is.EqualTo(PacketVerdictType.LinuxEbpfXdp));
            Assert.That(block.Verdicts[0].Value, Is.EqualTo(2UL));
            Assert.That(block.Verdicts[1].Type, Is.EqualTo(PacketVerdictType.Hardware));
            Assert.That(block.Verdicts[1].Data, Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(block.Verdicts[1].Value, Is.Null);
            Assert.That(block.ProcessThreadIds.Select(p => (p.ProcessId, p.ThreadId)), Is.EqualTo(new[] { (1234u, 0u), (5678u, 9u) }));
            Assert.That(block.Flags.IsChecksumNotReady(), Is.True);
            Assert.That(block.Flags.IsChecksumValid(), Is.True);
            Assert.That(block.Flags.IsTcpSegmentationOffloaded(), Is.True);
            Assert.That(block.InvalidOptionCodes, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsEuiNameResolutionRecords(bool isBigEndian)
        {
            var eui48 = new byte[] { 0x02, 0xCA, 0xFF, 0xEE, 0xF0, 0x0D };
            var eui64 = new byte[] { 0x02, 0x34, 0x56, 0xFF, 0xFE, 0x78, 0x9A, 0xBC };
            using var reader = CreateReader(isBigEndian, writer =>
            {
                WriteSectionHeader(writer);
                WriteBlock(writer, (uint)BlockType.NameResolution, body =>
                {
                    WriteRecord(body, 3, eui48, "teapot");
                    WriteRecord(body, 4, eui64, "kettle");
                    body.Write(0u); // nrb_record_end
                });
            });

            var records = reader.NameResolutionBlocks.Single().Records;
            Assert.That(records.Select(r => r.RecordType), Is.EqualTo(new[] { NameResolutionRecordType.Eui48, NameResolutionRecordType.Eui64 }));
            Assert.That(records[0].Address, Is.EqualTo(eui48));
            Assert.That(records[0].IpAddress, Is.Null);
            Assert.That(records[0].DnsEntries, Is.EqualTo(new[] { "teapot" }));
            Assert.That(records[1].Address, Is.EqualTo(eui64));
            Assert.That(records[1].DnsEntries, Is.EqualTo(new[] { "kettle" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SimplePacketDataExcludesPaddingAndHonorsSnapLength(bool isBigEndian)
        {
            static void WriteSimplePacket(SectionWriter writer, uint originalLength, byte[] paddedData)
            {
                WriteBlock(writer, (uint)BlockType.SimplePacket, body =>
                {
                    body.Write(originalLength);
                    body.Write(paddedData);
                });
            }

            using var reader = CreateReader(isBigEndian, writer =>
            {
                // No snap length: the data is the original length, without padding.
                WriteSectionHeader(writer);
                WriteInterface(writer, 0);
                WriteSimplePacket(writer, 5, new byte[] { 1, 2, 3, 4, 5, 0, 0, 0 });
                WriteSimplePacket(writer, 100, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

                // The first interface's snap length applies, not a later interface's.
                WriteSectionHeader(writer);
                WriteInterface(writer, 3);
                WriteInterface(writer, 0);
                WriteSimplePacket(writer, 6, new byte[] { 1, 2, 3, 0 });
            });

            var packets = reader.SimplePacketBlocks.ToArray();
            Assert.That(packets.Select(p => p.Data.Length), Is.EqualTo(new[] { 5, 8, 3 }));
            Assert.That(packets.Select(p => p.DataLength), Is.EqualTo(new uint[] { 5, 100, 6 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SkipsSectionsWithUnsupportedMajorVersion(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian, writer =>
            {
                WriteSectionHeader(writer, majorVersion: 2, userApplication: "future");
                WriteInterface(writer, 0);
                WriteSectionHeader(writer, majorVersion: 1, minorVersion: 2, userApplication: "minor 2");
                WriteInterface(writer, 96);
            });

            var blocks = reader.AllBlocks.ToArray();
            Assert.That(blocks.Select(b => b.StoreType), Is.EqualTo(new[] { BlockType.SectionHeader, BlockType.InterfaceDescription }));
            Assert.That(((SectionHeaderBlock)blocks[0]).UserApplication, Is.EqualTo("minor 2"));
            Assert.That(((InterfaceDescriptionBlock)blocks[1]).SnapLength, Is.EqualTo(96));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadsPacketBlockTimestampAndHashes(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian, writer =>
            {
                WriteSectionHeader(writer);
                WriteInterface(writer, 0);
                WriteBlock(writer, (uint)BlockType.Packet, body =>
                {
                    body.Write((ushort)0);
                    body.Write((ushort)7);
                    WriteTimestamp(body, PacketTimestamp);
                    body.Write(3u);
                    body.Write(3u);
                    body.Write(new byte[] { 1, 2, 3, 0 });
                    WriteOption(body, 3, new byte[] { 2, 0xDE, 0xAD, 0xBE, 0xEF }); // pack_hash
                    WriteOption(body, 3, new byte[] { 4, 1 });
                    WriteEndOfOptions(body);
                });
            });

            var blocks = reader.AllBlocks.ToArray();
            var iface = blocks.OfType<InterfaceDescriptionBlock>().Single();
            var packet = blocks.OfType<PacketBlock>().Single();
            Assert.That(packet.Timestamp, Is.EqualTo(PacketTimestamp));
            Assert.That(packet.GetTimestamp(iface, out _), Is.EqualTo(DateTime.UnixEpoch.AddTicks(PacketTimestamp * 10)));
            Assert.That(packet.DropsCount, Is.EqualTo(7));
            Assert.That(packet.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(packet.Hashes.Select(h => h.Algorithm), Is.EqualTo(new[] { HashAlgorithm.Crc32, HashAlgorithm.Sha1 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConvertsInterfaceStatisticsTimestamps(bool isBigEndian)
        {
            using var reader = CreateReader(isBigEndian);
            var blocks = reader.AllBlocks.ToArray();
            var iface = blocks.OfType<InterfaceDescriptionBlock>().Single();
            var statistics = blocks.OfType<InterfaceStatisticsBlock>().Single();

            // The interface uses nanoseconds (if_tsresol 9) and an if_tsoffset of -5 seconds.
            static DateTime Expected(long nanoseconds) => DateTime.UnixEpoch.AddTicks(nanoseconds / 100).AddSeconds(-5);
            Assert.That(statistics.GetTimestamp(iface, out _), Is.EqualTo(Expected(PacketTimestamp)));
            Assert.That(statistics.GetStartTime(iface), Is.EqualTo(Expected(StatisticsStartTime)));
            Assert.That(statistics.GetEndTime(iface), Is.Null);
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
