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

  internal class TimestampTransformer
  {
    private const int exponentSignMask = 1 << 7;
    private const int exponentMask = ~exponentSignMask;

    private static readonly long UnixEpochTicks = new DateTime(1970, 1, 1).Ticks;

    private readonly long operand;
    private readonly bool operandIsMultiplier;

    internal bool PrecisionLoss => !this.operandIsMultiplier;

    public TimestampTransformer(byte resolution = 6)
    {
      // TODO Support GMT offset.
      int exponent = resolution & exponentMask;
      if ((resolution & exponentSignMask) == 0)
      {
        // Base 10				
        if (exponent <= 7)
        {
          this.operandIsMultiplier = true;
          this.operand = (long)Math.Pow(10.0, 7 - exponent);
        }
        else
        {
          this.operandIsMultiplier = false;
          this.operand = (long)Math.Pow(10.0, exponent - 7);
        }
      }
      else
      {
        // Base 2				
        throw new NotImplementedException("Transformation of timestamps in base 2 resolution is not supported.");
      }
    }

    internal DateTime ToDateTime(long value)
    {
      if (this.operandIsMultiplier)
      {
        return new DateTime(UnixEpochTicks + (this.operand * value));
      }
      return new DateTime(UnixEpochTicks + (value / this.operand));
    }
  }
}