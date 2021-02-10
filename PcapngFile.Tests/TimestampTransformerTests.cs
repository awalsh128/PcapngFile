namespace PcapngFile.Tests
{
  using NUnit.Framework;
  using System;

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
      Assert.AreEqual(expectedTicks, actualTicks);
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
      Assert.AreEqual(expectedTicks, actualTicks);
    }

    [Test]
    public void BaseTwoResolutionNotSupported()
    {
      try
      {
        // ReSharper disable ObjectCreationAsStatement
        _ = new TimestampTransformer(0x80);
        // ReSharper restore ObjectCreationAsStatement
        Assert.Fail("Expected construction to throw a NotImplementedException.");
      }
      catch (NotImplementedException)
      {
      }
    }
  }
}