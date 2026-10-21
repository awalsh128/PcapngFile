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
    using System.Buffers.Binary;
    using System.IO;

    /// <summary>
    /// Reads the fields of one block in the byte order of the section it belongs to.
    /// </summary>
    internal sealed class EndianBinaryReader : BinaryReader
    {
        internal EndianBinaryReader(byte[] block, bool isBigEndian)
            : base(new MemoryStream(block, writable: false))
        {
            this.IsBigEndian = isBigEndian;
        }

        internal bool IsBigEndian { get; }

        public override short ReadInt16() => this.IsBigEndian ? BinaryPrimitives.ReverseEndianness(base.ReadInt16()) : base.ReadInt16();

        public override ushort ReadUInt16() => this.IsBigEndian ? BinaryPrimitives.ReverseEndianness(base.ReadUInt16()) : base.ReadUInt16();

        public override int ReadInt32() => this.IsBigEndian ? BinaryPrimitives.ReverseEndianness(base.ReadInt32()) : base.ReadInt32();

        public override uint ReadUInt32() => this.IsBigEndian ? BinaryPrimitives.ReverseEndianness(base.ReadUInt32()) : base.ReadUInt32();

        public override long ReadInt64() => this.IsBigEndian ? BinaryPrimitives.ReverseEndianness(base.ReadInt64()) : base.ReadInt64();

        public override ulong ReadUInt64() => this.IsBigEndian ? BinaryPrimitives.ReverseEndianness(base.ReadUInt64()) : base.ReadUInt64();

        public override byte[] ReadBytes(int count)
        {
            // Unlike BinaryReader, a block that ends early is malformed rather than the end of the file.
            var bytes = base.ReadBytes(count);
            if (bytes.Length != count)
            {
                throw new EndOfStreamException("Field extends past the end of the block.");
            }
            return bytes;
        }
    }
}
