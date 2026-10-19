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
    /// The kinds of component that can record a verdict on a packet.
    /// </summary>
    public enum PacketVerdictType : byte
    {
        /// <summary>
        /// A hardware verdict with variable length data.
        /// </summary>
        Hardware = 0,

        /// <summary>
        /// A Linux eBPF traffic control verdict (a TC_ACT_* value).
        /// </summary>
        LinuxEbpfTrafficControl = 1,

        /// <summary>
        /// A Linux eBPF XDP verdict (an xdp_action value).
        /// </summary>
        LinuxEbpfXdp = 2,
    }

    /// <summary>
    /// A verdict on what would be done with a packet (epb_verdict), for example by a firewall.
    /// </summary>
    public class PacketVerdict
    {
        internal PacketVerdict(PacketVerdictType type, byte[] data, UInt64? value)
        {
            this.Type = type;
            this.Data = data;
            this.Value = value;
        }

        /// <summary>
        /// Gets the component that recorded the verdict. Types this library doesn't know keep their numeric value.
        /// </summary>
        public PacketVerdictType Type { get; }

        /// <summary>
        /// Gets the raw verdict data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Gets the verdict as an integer for the Linux eBPF types, or <c>null</c> for other types.
        /// </summary>
        public UInt64? Value { get; }
    }
}