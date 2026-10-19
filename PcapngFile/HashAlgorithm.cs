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
    /// The algorithms that can be used to hash a packet.
    /// </summary>
    public enum HashAlgorithm
    {
        /// <summary>
        /// 32-bit cyclic redundancy check.
        /// </summary>
        Crc32 = 2,

        /// <summary>
        /// MD5 digest.
        /// </summary>
        MD5 = 3,

        /// <summary>
        /// SHA-1 digest.
        /// </summary>
        Sha1 = 4,

        /// <summary>
        /// Two's complement checksum.
        /// </summary>
        TwosComplement = 0,

        /// <summary>
        /// XOR checksum.
        /// </summary>
        Xor = 1,

        /// <summary>
        /// Toeplitz hash, as used for receive side scaling.
        /// </summary>
        Toeplitz = 5,
    }
}