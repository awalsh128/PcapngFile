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
    /// Converts raw timestamps to <see cref="DateTime"/> using an interface's if_tsresol and if_tsoffset options.
    /// </summary>
    internal class TimestampTransformer
    {
        internal const byte DefaultResolution = 6;

        private const int BaseTwoMask = 1 << 7;
        private const int ExponentMask = ~BaseTwoMask;

        // Above this power of 10, every 64-bit timestamp is less than one tick.
        private const int MaxBaseTenExponent = 38;

        private static readonly long UnixEpochTicks = DateTime.UnixEpoch.Ticks;

        private readonly UInt128 unitsPerSecond;
        private readonly long offsetTicks;

        /// <summary>
        /// Creates a transformer for a timestamp resolution and offset.
        /// </summary>
        /// <param name="resolution">
        /// The if_tsresol value. If the most significant bit is 0, the remaining bits are a negative power of 10 (6 means
        /// microseconds); if it is 1, they are a negative power of 2 (10 means 1/1024 of a second).
        /// </param>
        /// <param name="offsetSeconds">The if_tsoffset value, in seconds, added to every timestamp.</param>
        public TimestampTransformer(byte resolution = DefaultResolution, long offsetSeconds = 0)
        {
            int exponent = resolution & ExponentMask;
            if ((resolution & BaseTwoMask) == 0)
            {
                this.unitsPerSecond = exponent <= MaxBaseTenExponent ? UInt128Pow10(exponent) : UInt128.MaxValue;
            }
            else
            {
                this.unitsPerSecond = UInt128.One << exponent;
            }

            // Offsets beyond the DateTime range can't produce a valid timestamp; clamp them so ToDateTime reports the overflow.
            const long maxOffsetSeconds = long.MaxValue / TimeSpan.TicksPerSecond;
            this.offsetTicks = Math.Clamp(offsetSeconds, -maxOffsetSeconds, maxOffsetSeconds) * TimeSpan.TicksPerSecond;
        }

        /// <summary>
        /// Converts a raw timestamp to a UTC <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The raw timestamp, an unsigned 64-bit count of resolution units stored in a <see cref="long"/>.</param>
        /// <returns>The timestamp as a UTC <see cref="DateTime"/>, truncated to whole ticks (100 ns).</returns>
        /// <exception cref="ArgumentOutOfRangeException">The timestamp is outside the range of <see cref="DateTime"/>.</exception>
        internal DateTime ToDateTime(long value)
        {
            return this.ToDateTime(value, out _);
        }

        /// <summary>
        /// Converts a raw timestamp to a UTC <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The raw timestamp, an unsigned 64-bit count of resolution units stored in a <see cref="long"/>.</param>
        /// <param name="precisionLoss"><c>true</c> if the timestamp was finer than a tick (100 ns) and was truncated.</param>
        /// <returns>The timestamp as a UTC <see cref="DateTime"/>, truncated to whole ticks (100 ns).</returns>
        /// <exception cref="ArgumentOutOfRangeException">The timestamp is outside the range of <see cref="DateTime"/>.</exception>
        internal DateTime ToDateTime(long value, out bool precisionLoss)
        {
            // The product fits in 128 bits: (2^64 - 1) * 10^7 < 2^88.
            UInt128 scaled = (UInt128)(ulong)value * (ulong)TimeSpan.TicksPerSecond;
            (UInt128 ticks, UInt128 remainder) = UInt128.DivRem(scaled, this.unitsPerSecond);
            precisionLoss = remainder != UInt128.Zero;

            Int128 totalTicks = (Int128)UnixEpochTicks + (Int128)ticks + this.offsetTicks;
            if (totalTicks < DateTime.MinValue.Ticks || totalTicks > DateTime.MaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Timestamp is outside the range of DateTime (value = " + (ulong)value + ", units per second = " + this.unitsPerSecond +
                        ", offset ticks = " + this.offsetTicks + ").");
            }
            return new DateTime((long)totalTicks, DateTimeKind.Utc);
        }

        private static UInt128 UInt128Pow10(int exponent)
        {
            UInt128 result = UInt128.One;
            for (int i = 0; i < exponent; i++)
            {
                result *= 10;
            }
            return result;
        }
    }
}