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
    using System.Text;

    /// <summary>
    /// A vendor-specific option (opt_custom) identified by an IANA Private Enterprise Number.
    /// </summary>
    public class CustomOption
    {
        private const UInt16 StringCopyableCode = 2988;
        private const UInt16 BinaryCopyableCode = 2989;
        private const UInt16 StringNoCopyCode = 19372;

        internal CustomOption(UInt16 code, uint privateEnterpriseNumber, byte[] data)
        {
            this.Code = code;
            this.PrivateEnterpriseNumber = privateEnterpriseNumber;
            this.Data = data;
        }

        /// <summary>
        /// Gets the option code: 2988 or 19372 for a UTF-8 string, 2989 or 19373 for binary data.
        /// </summary>
        public UInt16 Code { get; }

        /// <summary>
        /// Gets the IANA Private Enterprise Number of the organization that defined the option.
        /// </summary>
        public uint PrivateEnterpriseNumber { get; }

        /// <summary>
        /// Gets the custom data, without the Private Enterprise Number or padding.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Gets whether <see cref="Data"/> is a UTF-8 string rather than binary data.
        /// </summary>
        public bool IsString => this.Code == StringCopyableCode || this.Code == StringNoCopyCode;

        /// <summary>
        /// Gets whether the option may be copied into a new file when the file is rewritten.
        /// </summary>
        public bool IsCopyable => this.Code == StringCopyableCode || this.Code == BinaryCopyableCode;

        /// <summary>
        /// Gets <see cref="Data"/> as a string if <see cref="IsString"/> is <c>true</c>.
        /// </summary>
        /// <returns>The data decoded as UTF-8, or <c>null</c> for binary data.</returns>
        public string GetString()
        {
            return this.IsString ? Encoding.UTF8.GetString(this.Data) : null;
        }
    }
}