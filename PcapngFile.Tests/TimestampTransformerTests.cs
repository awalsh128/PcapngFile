namespace PcapngFile.Tests
{
    using System;

    using NUnit.Framework;

    [TestFixture]
    public class TimestampTransformerTests
    {
        [Test]
        [TestCase(6, 1434416300606680, 635700131006066800)]
        [TestCase(6, 1434416300625354, 635700131006253540)]
        [TestCase(6, 1434416301126820, 635700131011268200)]
        [TestCase(6, 1434416308690049, 635700131086900490)]
        public void ToDateTimeWithExplicitResolution(byte resolution, long value, long expectedTicks)
        {
            var transformer = new TimestampTransformer(resolution);
            var actualTimestamp = transformer.ToDateTime(value);
            var actualTicks = actualTimestamp.Ticks;
            Assert.That(actualTicks, Is.EqualTo(expectedTicks));
        }

        [Test]
        [TestCase(1434416300606680, 635700131006066800)]
        [TestCase(1434416300625354, 635700131006253540)]
        [TestCase(1434416301126820, 635700131011268200)]
        [TestCase(1434416308690049, 635700131086900490)]
        public void ToDateTimeWithImplicitResolution(long value, long expectedTicks)
        {
            var transformer = new TimestampTransformer();
            var actualTimestamp = transformer.ToDateTime(value);
            var actualTicks = actualTimestamp.Ticks;
            Assert.That(actualTicks, Is.EqualTo(expectedTicks));
        }

        [Test]
        [TestCase((byte)0x80, 1L, TimeSpan.TicksPerSecond, false)] // 2^0: seconds.
        [TestCase((byte)0x8A, 1024L, TimeSpan.TicksPerSecond, false)] // 2^-10: 1/1024 seconds.
        [TestCase((byte)0x8A, 512L, TimeSpan.TicksPerSecond / 2, false)]
        [TestCase((byte)0x8A, 1L, 9765L, true)] // 1/1024 s is 9765.625 ticks.
        [TestCase((byte)0x87, 1L, 78125L, false)] // 2^-7 s is exactly 78125 ticks.
        public void ToDateTimeWithBaseTwoResolution(byte resolution, long value, long expectedTicksSinceEpoch, bool expectedPrecisionLoss)
        {
            var transformer = new TimestampTransformer(resolution);
            var actual = transformer.ToDateTime(value, out bool precisionLoss);
            Assert.That(actual.Ticks - DateTime.UnixEpoch.Ticks, Is.EqualTo(expectedTicksSinceEpoch));
            Assert.That(precisionLoss, Is.EqualTo(expectedPrecisionLoss));
        }

        [Test]
        [TestCase((byte)6, 1_000_001L, 10_000_010L, false)]
        [TestCase((byte)9, 1_000_000_100L, 10_000_001L, false)] // Nanoseconds that are whole ticks.
        [TestCase((byte)9, 1_000_000_123L, 10_000_001L, true)] // 23 ns is truncated.
        [TestCase((byte)0, 2L, 2 * TimeSpan.TicksPerSecond, false)]
        public void ReportsPrecisionLossPerTimestamp(byte resolution, long value, long expectedTicksSinceEpoch, bool expectedPrecisionLoss)
        {
            var transformer = new TimestampTransformer(resolution);
            var actual = transformer.ToDateTime(value, out bool precisionLoss);
            Assert.That(actual.Ticks - DateTime.UnixEpoch.Ticks, Is.EqualTo(expectedTicksSinceEpoch));
            Assert.That(precisionLoss, Is.EqualTo(expectedPrecisionLoss));
        }

        [Test]
        [TestCase(3600L, 0L, 3600L)]
        [TestCase(-1L, 1_000_000L, 0L)]
        [TestCase(1_434_416_300L, 606_680L, 1_434_416_300L)] // Timestamps relative to an absolute offset.
        public void ToDateTimeAppliesOffset(long offsetSeconds, long value, long expectedSecondsSinceEpoch)
        {
            var transformer = new TimestampTransformer(6, offsetSeconds);
            var actual = transformer.ToDateTime(value);
            Assert.That(actual, Is.EqualTo(DateTime.UnixEpoch.AddSeconds(expectedSecondsSinceEpoch).AddTicks(value % 1_000_000 * 10)));
        }

        [Test]
        public void ToDateTimeReturnsUtc()
        {
            Assert.That(new TimestampTransformer().ToDateTime(0).Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        [TestCase((byte)100, long.MaxValue)] // 10^-100 s: every timestamp is under one tick.
        [TestCase((byte)0xFF, -1L)] // 2^-127 s with the largest unsigned timestamp.
        public void ToDateTimeWithExtremeResolutions(byte resolution, long value)
        {
            var actual = new TimestampTransformer(resolution).ToDateTime(value, out bool precisionLoss);
            Assert.That(actual, Is.EqualTo(DateTime.UnixEpoch));
            Assert.That(precisionLoss, Is.True);
        }

        [Test]
        [TestCase((byte)6, -1L, 0L)] // Largest unsigned timestamp: about 584,942 years of microseconds.
        [TestCase((byte)0, long.MaxValue, 0L)]
        [TestCase((byte)6, 0L, -100_000_000_000L)] // Offset before year 1.
        [TestCase((byte)6, 0L, long.MaxValue)]
        [TestCase((byte)6, 0L, long.MinValue)]
        public void ToDateTimeOutOfRangeThrows(byte resolution, long value, long offsetSeconds)
        {
            var transformer = new TimestampTransformer(resolution, offsetSeconds);
            Assert.Throws<ArgumentOutOfRangeException>(() => transformer.ToDateTime(value));
        }
    }
}