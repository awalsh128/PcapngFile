namespace PcapngFile.Tests
{
  using NUnit.Framework;
  using PcapngFile;
  using System.IO;
  using System.Linq;
  using System.Reflection;

  [TestFixture]
  public class ReaderTests
  {
    private static Stream GetManifestResource(string name)
    {
      return Assembly.GetExecutingAssembly().GetManifestResourceStream("PcapngFile.Tests." + name);
    }

    [Test]
    [TestCase("TestFile1.pcapng", 5179)]
    public void ReadAllBlocksAndVerifyBlockCount(string manifestResourceTestFileName, int expectedBlockCount)
    {
      var reader = new Reader(GetManifestResource(manifestResourceTestFileName));
      var blocks = reader.AllBlocks.ToArray();
      Assert.IsNotEmpty(blocks);
      Assert.AreEqual(expectedBlockCount, blocks.Length);
    }

    [Test]
    [TestCase("TestFile1.pcapng")]
    [TestCase("TestFile2.pcapng")]
    public void ReadSpecificBlocksWithoutException(string manifestResourceTestFileName)
    {
      var reader = new Reader(GetManifestResource(manifestResourceTestFileName));
      // Force evaluation with ToArray      
      _ = reader.EnhancedPacketBlocks.ToArray();
      _ = reader.InterfaceDescriptionBlocks.ToArray();
      _ = reader.InterfaceStatisticsBlocks.ToArray();
      _ = reader.NameResolutionBlocks.ToArray();
      _ = reader.PacketBlocks.ToArray();
      _ = reader.SectionHeaderBlocks.ToArray();
      _ = reader.SimplePacketBlocks.ToArray();
    }
  }
}