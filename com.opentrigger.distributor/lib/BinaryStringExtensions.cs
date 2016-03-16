using System;

namespace com.opentrigger.distributord
{
    public static class Extensions
    {

        public static string ToHexString(this byte[] bytes, string space = "")
        {
            return BitConverter.ToString(bytes).Replace("-", space);
        }

        public static byte[] ToBytes(this string hexString)
        {
            if (hexString.Length % 2 == 1) throw new Exception("odd number of digits");

            byte[] bytes = new byte[hexString.Length >> 1];
            for (int i = 0; i < hexString.Length >> 1; ++i) bytes[i] = (byte)((_nibbles(hexString[i << 1]) << 4) + (_nibbles(hexString[(i << 1) + 1])));
            return bytes;
        }

        private static int _nibbles(char hex)
        {
            var val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
