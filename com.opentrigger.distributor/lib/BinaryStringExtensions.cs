using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace com.opentrigger.distributord
{
    public enum Endian
    {
        LittleEndian = 2,
        /// <summary>
        /// MSB First
        /// </summary>
        BigEndian = 4,
        NativeEndian = 8,
    }

    public static class Extensions
    {

        public static string ToHexString(this byte[] bytes, string space = "")
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            return BitConverter.ToString(bytes).Replace("-", space);
        }

        /// <summary>
        /// Converts a HexString "EEFF" to a byte array {0xEE, 0xFF}
        /// </summary>
        /// <param name="hexString"></param>
        /// <param name="cleanupFirst"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this string hexString, bool cleanupFirst = false)
        {
            if (cleanupFirst) hexString = Regex.Replace(hexString, "[^0-F]", string.Empty, RegexOptions.IgnoreCase);
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


        public static Endian GetNativeEndian() => BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian;


        public static byte[] ConvertEndianness(this byte[] values, Endian from, Endian to = Endian.NativeEndian)
        {
            if (from == Endian.NativeEndian || to == Endian.NativeEndian)
            {
                var nativeEndian = GetNativeEndian();
                if (from == Endian.NativeEndian) from = nativeEndian;
                if (to == Endian.NativeEndian) to = nativeEndian;
            }
            if (from != to) return values.Reverse().ToArray();
            return values;
        }

        public static bool EndOfStream(this Stream ms) => ms.Position == ms.Length;
        public static bool EndOfStream(this BinaryReader br) => br.BaseStream.EndOfStream();

        public static int ReadInt16MsbFirst(this BinaryReader br)
        {
            var bytes = br.ReadBytes(2).ConvertEndianness(Endian.BigEndian);
            return BitConverter.ToInt16(bytes, 0);
        }

        public static int ReadInt24MsbFirst(this BinaryReader br)
        {
            // inflate to 32bit integer (BigEndian)
            var bytes = new byte[]{ 0,0,0,0 };
            br.Read(bytes, 1, 3);

            return BitConverter.ToInt32(bytes.ConvertEndianness(Endian.BigEndian),0);
        }

        public static int ReadInt8(this BinaryReader br) => br.ReadByte();
        public static uint ReadUint8(this BinaryReader br) => br.ReadByte();

        public static bool ReadTokenCubeBool(this BinaryReader br)
        {
            return br.ReadInt8() == 0x01;
        }

    }
}
