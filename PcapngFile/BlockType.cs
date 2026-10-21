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
    /// <summary>
    /// The type codes of the blocks in a PCAP-NG file.
    /// </summary>
    public enum BlockType : uint
    {
        /// <summary>
        /// No block; returned when the end of the file is reached.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// An <see cref="EnhancedPacketBlock"/>.
        /// </summary>
        EnhancedPacket = 0x00000006,

        /// <summary>
        /// An <see cref="InterfaceDescriptionBlock"/>.
        /// </summary>
        InterfaceDescription = 0x00000001,

        /// <summary>
        /// An <see cref="InterfaceStatisticsBlock"/>.
        /// </summary>
        InterfaceStatistics = 0x00000005,

        /// <summary>
        /// A <see cref="NameResolutionBlock"/>.
        /// </summary>
        NameResolution = 0x00000004,

        /// <summary>
        /// A <see cref="PacketBlock"/>.
        /// </summary>
        /// <remarks>Marked as obsolete.</remarks>
        Packet = 0x00000002,

        /// <summary>
        /// A <see cref="SectionHeaderBlock"/>.
        /// </summary>
        SectionHeader = 0x0A0D0D0A,

        /// <summary>
        /// A <see cref="SimplePacketBlock"/>.
        /// </summary>
        SimplePacket = 0x00000003
    }
}