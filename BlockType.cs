using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcapngFile
{
	public enum BlockType : uint
	{
		None = 0x00000000,
		EnhancedPacket = 0x00000006,
		InterfaceDescription = 0x00000001,
		InterfaceStatistics = 0x00000005,
		NameResolution = 0x00000004,
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Marked as obsolete.</remarks>
		Packet = 0x00000002,
		SectionHeader = 0x0A0D0D0A,
		SimplePacket = 0x00000003
	}
}
