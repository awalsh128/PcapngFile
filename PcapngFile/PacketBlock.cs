/*
Copyright (c) 2013, Andrew Walsh
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. All advertising materials mentioning features or use of this software
   must display the following acknowledgement:
   This product includes software developed by the <organization>.
4. Neither the name of the <organization> nor the
   names of its contributors may be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY <COPYRIGHT HOLDER> ''AS IS'' AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
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

	public class PacketBlock : BlockBase
	{
		private const UInt16 FlagsOptionCode = 2;
		private const UInt16 HashOptionCode = 3;		
	
		public UInt32 CapturedLength { get; private set; }
		public byte[] Data { get; private set; }
		public UInt32 DataLength { get; private set; }		
		public UInt16 DropsCount { get; private set; }
		public PacketOptionFlags Flags { get; private set; }
		public byte[] Hash { get; private set; }
		public HashAlgorithm HashAlgorithm { get; private set; }
		public UInt16 InterfaceID { get; private set; }		
		public DateTime Timestamp { get; private set; }

		internal PacketBlock(BinaryReader reader)
			: base(reader)
		{
			this.InterfaceID = reader.ReadUInt16();
			this.DropsCount = reader.ReadUInt16();
			this.Timestamp = DateTime.FromBinary(reader.ReadInt64());
			this.CapturedLength = reader.ReadUInt32();
			this.DataLength = reader.ReadUInt32();
			this.Data = reader.ReadBytes((int)this.CapturedLength);
			this.ReadOptions(reader);
			this.ReadClosingField(reader);
		}

		override protected void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			switch (code)
			{
				case FlagsOptionCode:
					this.Flags = new PacketOptionFlags(value);
					break;
				case HashOptionCode:
					this.HashAlgorithm = (HashAlgorithm)value[0];
					Array.Copy(value, 1, this.Hash, 0, value.Length - 1);
					break;
			}
		}
	}
}
