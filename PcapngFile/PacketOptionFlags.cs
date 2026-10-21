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
    using System;

    /// <summary>
    /// The flags (epb_flags) describing a packet's direction, reception type and link layer errors.
    /// </summary>
    public class PacketOptionFlags
    {
        private const uint DirectionMask = 0x00000003;
        private const uint InboundDirection = 1;
        private const uint OutboundDirection = 2;
        private const uint ReceptionTypeMask = 0x0000001C;
        private const int ReceptionTypeOffset = 2;
        private const uint UnicastReceptionType = 1;
        private const uint MulticastReceptionType = 2;
        private const uint BroadcastReceptionType = 3;
        private const uint PromiscuousReceptionType = 4;
        private const uint FcsLengthMask = 0x000001E0;
        private const int FcsLengthOffset = 5;
        private const uint ChecksumNotReadyMask = 0x00000200;
        private const uint ChecksumValidMask = 0x00000400;
        private const uint TcpSegmentationOffloadedMask = 0x00000800;
        private const uint LinkLayerSymbolErrorMask = 0x80000000;
        private const uint LinkLayerPreambleErrorMask = 0x40000000;
        private const uint LinkLayerStartFrameDelimiterErrorMask = 0x20000000;
        private const uint LinkLayerUnalignedFrameErrorMask = 0x10000000;
        private const uint LinkLayerWrongInterframeGapErrorMask = 0x08000000;
        private const uint LinkLayerPacketTooShortErrorMask = 0x04000000;
        private const uint LinkLayerPacketTooLongErrorMask = 0x02000000;
        private const uint LinkLayerCrcErrorMask = 0x01000000;

        private readonly uint optionValue;

        internal PacketOptionFlags(byte[] optionValue)
            : this(BitConverter.ToUInt32(optionValue, 0))
        {
        }

        internal PacketOptionFlags(uint optionValue)
        {
            this.optionValue = optionValue;
        }

        /// <summary>
        /// Gets whether the packet had a CRC error.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerCrcError()
        {
            return (this.optionValue & LinkLayerCrcErrorMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet was too long.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerPacketTooLongError()
        {
            return (this.optionValue & LinkLayerPacketTooLongErrorMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet was too short.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerPacketTooShortError()
        {
            return (this.optionValue & LinkLayerPacketTooShortErrorMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet had a preamble error.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerPreambleError()
        {
            return (this.optionValue & LinkLayerPreambleErrorMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet had a start frame delimiter error.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerStartFrameDelimiterError()
        {
            return (this.optionValue & LinkLayerStartFrameDelimiterErrorMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet had a symbol error.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerSymbolError()
        {
            return (this.optionValue & LinkLayerSymbolErrorMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet had an unaligned frame error.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerUnalignedFrameError()
        {
            return (this.optionValue & LinkLayerUnalignedFrameErrorMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet had a wrong inter-frame gap error.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool ContainsLinkLayerWrongInterframeGapError()
        {
            return (this.optionValue & LinkLayerWrongInterframeGapErrorMask) > 0;
        }

        /// <summary>
        /// Gets the length of the frame check sequence in bytes.
        /// </summary>
        /// <returns>The FCS length, or 0 if it is not available.</returns>
        public uint GetFcsLength()
        {
            return (this.optionValue & FcsLengthMask) >> FcsLengthOffset;
        }
        /// <summary>
        /// Gets whether the packet's checksum wasn't computed yet because of checksum offloading.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool IsChecksumNotReady()
        {
            return (this.optionValue & ChecksumNotReadyMask) > 0;
        }

        /// <summary>
        /// Gets whether the packet's checksum was already checked before capture, so it doesn't need checking again.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool IsChecksumValid()
        {
            return (this.optionValue & ChecksumValidMask) > 0;
        }

        /// <summary>
        /// Gets whether TCP segmentation was offloaded, so the packet covers several link layer packets.
        /// </summary>
        /// <returns><c>true</c> if the flag is set.</returns>
        public bool IsTcpSegmentationOffloaded()
        {
            return (this.optionValue & TcpSegmentationOffloadedMask) > 0;
        }

        internal byte[] GetOptionValue()
        {
            return BitConverter.GetBytes(this.optionValue);
        }

        /// <summary>
        /// Gets whether the packet was received as broadcast.
        /// </summary>
        /// <returns><c>true</c> if the packet was broadcast.</returns>
        public bool IsBroadcast()
        {
            return this.GetReceptionType() == BroadcastReceptionType;
        }

        /// <summary>
        /// Gets whether the packet was inbound.
        /// </summary>
        /// <returns><c>true</c> if the packet was inbound.</returns>
        public bool IsInbound()
        {
            return (this.optionValue & DirectionMask) == InboundDirection;
        }

        /// <summary>
        /// Gets whether the packet was outbound.
        /// </summary>
        /// <returns><c>true</c> if the packet was outbound.</returns>
        public bool IsOutbound()
        {
            return (this.optionValue & DirectionMask) == OutboundDirection;
        }

        /// <summary>
        /// Gets whether the packet was received as multicast.
        /// </summary>
        /// <returns><c>true</c> if the packet was multicast.</returns>
        public bool IsMulticast()
        {
            return this.GetReceptionType() == MulticastReceptionType;
        }

        /// <summary>
        /// Gets whether the packet was received in promiscuous mode.
        /// </summary>
        /// <returns><c>true</c> if the packet was received promiscuously.</returns>
        public bool IsPromiscuous()
        {
            return this.GetReceptionType() == PromiscuousReceptionType;
        }

        /// <summary>
        /// Gets whether the packet was received as unicast.
        /// </summary>
        /// <returns><c>true</c> if the packet was unicast.</returns>
        public bool IsUnicast()
        {
            return this.GetReceptionType() == UnicastReceptionType;
        }

        private uint GetReceptionType()
        {
            return (this.optionValue & ReceptionTypeMask) >> ReceptionTypeOffset;
        }
    }
}