namespace PcapngFile.Tests
{
    using System;

    using NUnit.Framework;

    [TestFixture]
    public class PacketOptionFlagsTests
    {
        private static PacketOptionFlags Create(uint value)
        {
            return new PacketOptionFlags(BitConverter.GetBytes(value));
        }

        [Test]
        [TestCase(0u, false, false)]
        [TestCase(1u, true, false)]
        [TestCase(2u, false, true)]
        public void Direction(uint value, bool inbound, bool outbound)
        {
            var flags = Create(value);
            Assert.That(flags.IsInbound(), Is.EqualTo(inbound));
            Assert.That(flags.IsOutbound(), Is.EqualTo(outbound));
        }

        [Test]
        [TestCase(0u, false, false, false, false)]
        [TestCase(1u, true, false, false, false)]
        [TestCase(2u, false, true, false, false)]
        [TestCase(3u, false, false, true, false)]
        [TestCase(4u, false, false, false, true)]
        public void ReceptionType(uint receptionType, bool unicast, bool multicast, bool broadcast, bool promiscuous)
        {
            var flags = Create(receptionType << 2);
            Assert.That(flags.IsUnicast(), Is.EqualTo(unicast));
            Assert.That(flags.IsMulticast(), Is.EqualTo(multicast));
            Assert.That(flags.IsBroadcast(), Is.EqualTo(broadcast));
            Assert.That(flags.IsPromiscuous(), Is.EqualTo(promiscuous));
        }

        [Test]
        public void FcsLength()
        {
            Assert.That(Create(0xFu << 5).GetFcsLength(), Is.EqualTo(15));
        }

        [Test]
        [TestCase(31, "Symbol")]
        [TestCase(30, "Preamble")]
        [TestCase(29, "StartFrameDelimiter")]
        [TestCase(28, "UnalignedFrame")]
        [TestCase(27, "WrongInterframeGap")]
        [TestCase(26, "PacketTooShort")]
        [TestCase(25, "PacketTooLong")]
        [TestCase(24, "Crc")]
        public void LinkLayerErrorsUseOneBitEach(int bit, string expectedError)
        {
            var flags = Create(1u << bit);
            var errors = new (string Name, bool Set)[]
            {
        ("Symbol", flags.ContainsLinkLayerSymbolError()),
        ("Preamble", flags.ContainsLinkLayerPreambleError()),
        ("StartFrameDelimiter", flags.ContainsLinkLayerStartFrameDelimiterError()),
        ("UnalignedFrame", flags.ContainsLinkLayerUnalignedFrameError()),
        ("WrongInterframeGap", flags.ContainsLinkLayerWrongInterframeGapError()),
        ("PacketTooShort", flags.ContainsLinkLayerPacketTooShortError()),
        ("PacketTooLong", flags.ContainsLinkLayerPacketTooLongError()),
        ("Crc", flags.ContainsLinkLayerCrcError()),
            };
            foreach (var (name, set) in errors)
            {
                Assert.That(set, Is.EqualTo(name == expectedError), name);
            }
        }
    }
}