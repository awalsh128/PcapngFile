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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Maps one network address to one or more host names.
    /// </summary>
    public class NameResolutionRecord
    {
        private const UInt16 EndRecordType = 0;
        private const UInt16 Ip4RecordType = 1;
        private const UInt16 Ip6RecordType = 2;

        private const int Ip4Length = 4;
        private const int Ip6Length = 16;
        private const int ValueAlignmentBoundary = 4;

        /// <summary>
        /// Gets whether <see cref="IpAddress"/> is an IPv6 address rather than IPv4.
        /// </summary>
        public bool IsIpVersion6 { get; private set; }

        /// <summary>
        /// Gets the network address.
        /// </summary>
        public byte[] IpAddress { get; private set; }

        /// <summary>
        /// Gets the host names for <see cref="IpAddress"/>.
        /// </summary>
        public ReadOnlyCollection<string> DnsEntries { get; private set; }

        internal bool IsEndRecord { get; }

        // False for record types this library does not support, which are skipped.
        internal bool IsSupported => this.IpAddress != null;

        internal NameResolutionRecord(BinaryReader reader)
        {
            UInt16 type = reader.ReadUInt16();
            int valueLength = reader.ReadUInt16();
            byte[] value = reader.ReadBytes(valueLength);

            int remainderLength = valueLength % ValueAlignmentBoundary;
            if (remainderLength > 0)
            {
                _ = reader.ReadBytes(ValueAlignmentBoundary - remainderLength);     // Read fill bytes to boundary.
            }

            int addressLength;
            switch (type)
            {
                case EndRecordType:
                    this.IsEndRecord = true;
                    return;
                case Ip4RecordType:
                    addressLength = Ip4Length;
                    break;
                case Ip6RecordType:
                    this.IsIpVersion6 = true;
                    addressLength = Ip6Length;
                    break;
                default:
                    return;
            }

            if (valueLength < addressLength)
            {
                throw new IOException("Name resolution record is shorter than its address (type = " + type + ", length = " + valueLength + ").");
            }
            this.IpAddress = value[..addressLength];
            this.DnsEntries = ReadDnsEntries(value[addressLength..]);
        }

        private static ReadOnlyCollection<string> ReadDnsEntries(byte[] entriesBuffer)
        {
            var entries = new List<string>();
            int entriesLength = entriesBuffer.Length;
            int i = 0, j = 0;
            while (j < entriesLength)
            {
                if (entriesBuffer[j] == 0)
                {
                    entries.Add(UTF8Encoding.UTF8.GetString(entriesBuffer, i, j - i));
                    i = ++j;
                }
                else
                {
                    j++;
                }
            }
            return new ReadOnlyCollection<string>(entries);
        }
    }
}