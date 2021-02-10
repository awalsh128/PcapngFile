/*
Copyright (c) 2021, Andrew Walsh
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

  public class PacketOptionFlags
  {
    private const uint InboundMask = 0x00000001;
    private const uint OutboundMask = 0x00000002;
    private const uint UnicastMask = 0x00000004;
    private const uint MulticastMask = 0x00000008;
    private const uint BroadcastMask = 0x0000000C;
    private const uint PromiscuousMask = 0x00000010;
    private const uint FcsLengthMask = 0x000001E0;
    private const int FcsLengthOffset = 5;
    private const uint LinkLayerSymbolErrorMask = 0x80000000;
    private const uint LinkLayerPreambleErrorMask = 0x40000000;
    private const uint LinkLayerStartFrameDelimiterErrorMask = 0x40000000;
    private const uint LinkLayerUnalignedFrameErrorMask = 0x20000000;
    private const uint LinkLayerWrongInterframeGapErrorMask = 0x10000000;
    private const uint LinkLayerPacketTooShortErrorMask = 0x08000000;
    private const uint LinkLayerPacketTooLongErrorMask = 0x04000000;
    private const uint LinkLayerCrcErrorMask = 0x02000000;

    private readonly uint optionValue;

    internal PacketOptionFlags(byte[] optionValue)
    {
      this.optionValue = BitConverter.ToUInt32(optionValue, 0);
    }

    public bool ContainsLinkLayerCrcError()
    {
      return (this.optionValue & LinkLayerCrcErrorMask) > 0;
    }
    public bool ContainsLinkLayerPacketTooLongError()
    {
      return (this.optionValue & LinkLayerPacketTooLongErrorMask) > 0;
    }
    public bool ContainsLinkLayerPacketTooShortError()
    {
      return (this.optionValue & LinkLayerPacketTooShortErrorMask) > 0;
    }
    public bool ContainsLinkLayerPreambleError()
    {
      return (this.optionValue & LinkLayerPreambleErrorMask) > 0;
    }
    public bool ContainsLinkLayerStartFrameDelimiterError()
    {
      return (this.optionValue & LinkLayerStartFrameDelimiterErrorMask) > 0;
    }
    public bool ContainsLinkLayerSymbolError()
    {
      return (this.optionValue & LinkLayerSymbolErrorMask) > 0;
    }
    public bool ContainsLinkLayerUnalignedFrameError()
    {
      return (this.optionValue & LinkLayerUnalignedFrameErrorMask) > 0;
    }
    public bool ContainsLinkLayerWrongInterframeGapError()
    {
      return (this.optionValue & LinkLayerWrongInterframeGapErrorMask) > 0;
    }
    public uint GetFcsLength()
    {
      return (this.optionValue & FcsLengthMask) >> FcsLengthOffset;
    }
    internal byte[] GetOptionValue()
    {
      return BitConverter.GetBytes(this.optionValue);
    }
    public bool IsBroadcast()
    {
      return (this.optionValue & BroadcastMask) == BroadcastMask;
    }
    public bool IsInbound()
    {
      return (this.optionValue & InboundMask) > 0;
    }
    public bool IsOutbound()
    {
      return (this.optionValue & OutboundMask) > 0;
    }
    public bool IsMulticast()
    {
      return !this.IsBroadcast() && ((this.optionValue & MulticastMask) > 0);
    }
    public bool IsPromiscuous()
    {
      return (this.optionValue & PromiscuousMask) > 0;
    }
    public bool IsUnicast()
    {
      return !this.IsBroadcast() && ((this.optionValue & UnicastMask) > 0);
    }
  }
}