namespace PcapngFile.Tests
{    
    using NUnit.Framework;    
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
		public void ReadSpecificBlocksWithoutException(string manifestResourceTestFileName)
		{
			var reader = new Reader(GetManifestResource(manifestResourceTestFileName));			
			// Force evaluation with ToArray
			// ReSharper disable ReturnValueOfPureMethodIsNotUsed
			reader.EnhancedPacketBlocks.ToArray();
			reader.InterfaceDescriptionBlocks.ToArray();
			reader.InterfaceStatisticsBlocks.ToArray();
			reader.NameResolutionBlocks.ToArray();
			reader.PacketBlocks.ToArray();
			reader.SectionHeaderBlocks.ToArray();
			reader.SimplePacketBlocks.ToArray();
			// ReSharper restore ReturnValueOfPureMethodIsNotUsed
		}
	}
}