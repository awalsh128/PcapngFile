namespace PcapngFile.Tests
{
	using System.Linq;
	using NUnit.Framework;

	[TestFixture]
	public class ReaderTests
	{
		[Test]
		[TestCase("TestFile1.pcapng", 5179)]
		public void ReadAllBlocksAndVerifyBlockCount(string filename, int expectedBlockCount)
		{
			var reader = new Reader(filename);
			var blocks = reader.AllBlocks.ToArray();
			Assert.IsNotEmpty(blocks);
			Assert.AreEqual(expectedBlockCount, blocks.Length);
		}

		[Test]
		[TestCase("TestFile1.pcapng")]
		public void ReadSpecificBlocksWithoutException(string filename)
		{
			var reader = new Reader(filename);			
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