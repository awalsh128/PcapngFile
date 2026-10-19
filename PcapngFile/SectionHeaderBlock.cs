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
    using System.IO;
    using System.Text;

    /// <summary>
    /// Starts a section of the file. Every other block belongs to the section before it.
    /// </summary>
    public class SectionHeaderBlock : BlockBase
    {
        private const UInt16 HardwareOptionCode = 2;
        private const UInt16 OperatingSystemOptionCode = 3;
        private const UInt16 UserApplicationOptionCode = 4;

        /// <summary>
        /// Gets the byte order magic number (0x1A2B3C4D) used to detect the section's byte order.
        /// </summary>
        public UInt32 ByteOrderMagic { get; private set; }

        /// <summary>
        /// Gets the hardware used to create the section (shb_hardware), or <c>null</c> if there is none.
        /// </summary>
        public string Hardware { get; private set; }

        /// <summary>
        /// Gets the major version of the file format.
        /// </summary>
        public UInt16 MajorVersion { get; private set; }

        /// <summary>
        /// Gets the minor version of the file format.
        /// </summary>
        public UInt16 MinorVersion { get; private set; }

        /// <summary>
        /// Gets the operating system used to create the section (shb_os), or <c>null</c> if there is none.
        /// </summary>
        public string OperatingSystem { get; private set; }

        /// <summary>
        /// Gets the length of the section in bytes, or -1 if it is not specified.
        /// </summary>
        public Int64 SectionLength { get; private set; }

        /// <summary>
        /// Gets the application used to create the section (shb_userappl), or <c>null</c> if there is none.
        /// </summary>
        public string UserApplication { get; private set; }

        internal SectionHeaderBlock(BinaryReader reader)
            : base(reader)
        {
            this.ByteOrderMagic = reader.ReadUInt32();
            this.MajorVersion = reader.ReadUInt16();
            this.MinorVersion = reader.ReadUInt16();
            this.SectionLength = reader.ReadInt64();
            this.TryReadOptions(reader);
            this.ReadClosingField(reader);
        }

        /// <inheritdoc/>
        protected override void OnReadOptionsCode(UInt16 code, byte[] value)
        {
            switch (code)
            {
                case HardwareOptionCode:
                    this.Hardware = Encoding.UTF8.GetString(value);
                    break;
                case OperatingSystemOptionCode:
                    this.OperatingSystem = Encoding.UTF8.GetString(value);
                    break;
                case UserApplicationOptionCode:
                    this.UserApplication = Encoding.UTF8.GetString(value);
                    break;
            }
        }
    }
}