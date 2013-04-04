using System;
using System.IO;
using System.Text;

namespace PcapngFile
{
	public abstract class BlockBase
	{
		private const byte Crc32HashAlgorithm = 2;
		private const byte MD5HashAlgorithm = 3;
		private const byte Sha1HashAlgorithm = 4;
		private const byte TwosComplementHashAlgorithm = 0;
		private const byte XorHashAlgorithm = 1;
		
		private const UInt16 CommentOptionCode = 1;
		private const UInt32 PacketFlagsOption = 2;

		protected const UInt16 EndOptionCode = 0;
		protected const UInt32 EndOptionField = 0;
		protected const int OptionValueAlignmentBoundary = 4;
		protected long PreReadPosition;

		internal const int BlockTotalLengthLength = 4;
		internal const int BlockTypeLength = 4;

		public string Comment { get; private set; }
		public BlockType StoreType { get; private set; }
		public UInt32 TotalLength { get; private set; }

		internal BlockBase(BinaryReader reader)
		{
			this.PreReadPosition = reader.BaseStream.Position;
			this.StoreType = (BlockType)reader.ReadUInt32();
			this.TotalLength = reader.ReadUInt32();
		}

		protected HashAlgorithm GetHashAlgorithm(byte value)
		{
			switch (value)
			{
				case Crc32HashAlgorithm:
					return HashAlgorithm.Crc32;					
				case MD5HashAlgorithm:
					return HashAlgorithm.MD5;					
				case Sha1HashAlgorithm:
					return HashAlgorithm.Sha1;					
				case TwosComplementHashAlgorithm:
					return HashAlgorithm.TwosComplement;
				case XorHashAlgorithm:
					return HashAlgorithm.Xor;					
				default:
					throw new ArgumentOutOfRangeException("Value not recognized as a valid constant (value = " + value.ToString() + ").");
			}
		}
		protected byte GetHashAlgorithmCode(HashAlgorithm value)
		{
			switch (value)
			{
				case HashAlgorithm.Crc32:
					return Crc32HashAlgorithm;
				case HashAlgorithm.MD5:
					return MD5HashAlgorithm;
				case HashAlgorithm.Sha1:
					return Sha1HashAlgorithm;
				case HashAlgorithm.TwosComplement:
					return TwosComplementHashAlgorithm;					
				case HashAlgorithm.Xor:
					return XorHashAlgorithm;
				default:
					throw new ArgumentOutOfRangeException("Value not recognized as a valid constant (value = " + value.ToString() + ").");
			}
		}

		protected virtual void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			throw new NotImplementedException("The OnReadOptionsCode must be overriden if ReadOption is called.");
		}

		//protected abstract void OnSerialize(StreamWriter writer);

		protected void ReadOptions(BinaryReader reader)
		{
			UInt16 code;
			byte[] value;
			UInt16 valueLength;
			int remainderLength;

			while (true)
			{
				code = reader.ReadUInt16();
				valueLength = reader.ReadUInt16();
				if (valueLength > 0)
				{
					value = reader.ReadBytes(valueLength);
					remainderLength = (int)valueLength % OptionValueAlignmentBoundary;
					if (remainderLength > 0)
					{
						reader.ReadBytes(OptionValueAlignmentBoundary - remainderLength);		// Read fill bytes to boundary.
					}
				}
				else
				{
					break;
				}
				if (code == CommentOptionCode)
				{
					this.Comment = UTF8Encoding.UTF8.GetString(value);
				}
				else
				{
					this.OnReadOptionsCode(code, value);
				}
			}
		}

		protected void ReadClosingField(BinaryReader reader)
		{
			UInt32 readLength = reader.ReadUInt32();
			if (readLength != this.TotalLength)
			{
				throw new IOException("Closing field (Block Total Length) doesn't match opening field (readLength = " + readLength + ", this.TotalLength = " + this.TotalLength + ").");
			}
		}

		public void Serialize(StreamWriter writer)
		{
			writer.Write((UInt32)this.StoreType);
			writer.Write((UInt32)this.TotalLength);
			//this.OnSerialize(writer);
			writer.Write((UInt32)this.TotalLength);
		}
	}
}
