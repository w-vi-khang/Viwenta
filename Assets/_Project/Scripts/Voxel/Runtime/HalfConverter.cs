using System;

namespace Viwenta.Voxel
{
    /// <summary>float to IEEE 754 half (binary16) bits, round to nearest. Overflow becomes infinity, NaN is not preserved.</summary>
    public static class HalfConverter
    {
        public static ushort ToHalf(float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            int sign = (bits >> 16) & 0x8000;
            int exponent = ((bits >> 23) & 0xFF) - 127 + 15;
            int mantissa = bits & 0x7FFFFF;

            if (exponent >= 31)
                return (ushort)(sign | 0x7C00);

            if (exponent <= 0)
            {
                if (exponent < -10)
                    return (ushort)sign;
                mantissa |= 0x800000;
                int shift = 14 - exponent;
                int subnormal = mantissa >> shift;
                if (((mantissa >> (shift - 1)) & 1) != 0)
                    subnormal++;
                return (ushort)(sign | subnormal);
            }

            int half = sign | (exponent << 10) | (mantissa >> 13);
            if ((mantissa & 0x1000) != 0)
                half++; // a carry out of the mantissa correctly bumps the exponent
            return (ushort)half;
        }
    }
}
