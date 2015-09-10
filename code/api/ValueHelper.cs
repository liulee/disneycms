using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DisneyCMS
{
    public class ValueHelper
    {
        public const float PRECISIONF = 0.000001f;
        public const double PRECISIOND = 0.000000000000001;
        private const ushort CRC_SEED = 0xFFFF;
        private const ushort CRC_POLY16 = 0x1021;
        #region CRC8校验表

        private static readonly byte[] CRC8Table =
        {
            0, 94, 188, 226, 97, 63, 221, 131, 194, 156, 126, 32, 163, 253, 31, 65,
            157, 195, 33, 127, 252, 162, 64, 30, 95, 1, 227, 189, 62, 96, 130, 220,
            35, 125, 159, 193, 66, 28, 254, 160, 225, 191, 93, 3, 128, 222, 60, 98,
            190, 224, 2, 92, 223, 129, 99, 61, 124, 34, 192, 158, 29, 67, 161, 255,
            70, 24, 250, 164, 39, 121, 155, 197, 132, 218, 56, 102, 229, 187, 89, 7,
            219, 133, 103, 57, 186, 228, 6, 88, 25, 71, 165, 251, 120, 38, 196, 154,
            101, 59, 217, 135, 4, 90, 184, 230, 167, 249, 27, 69, 198, 152, 122, 36,
            248, 166, 68, 26, 153, 199, 37, 123, 58, 100, 134, 216, 91, 5, 231, 185,
            140, 210, 48, 110, 237, 179, 81, 15, 78, 16, 242, 172, 47, 113, 147, 205,
            17, 79, 173, 243, 112, 46, 204, 146, 211, 141, 111, 49, 178, 236, 14, 80,
            175, 241, 19, 77, 206, 144, 114, 44, 109, 51, 209, 143, 12, 82, 176, 238,
            50, 108, 142, 208, 83, 13, 239, 177, 240, 174, 76, 18, 145, 207, 45, 115,
            202, 148, 118, 40, 171, 245, 23, 73, 8, 86, 180, 234, 105, 55, 213, 139,
            87, 9, 235, 181, 54, 104, 138, 212, 149, 203, 41, 119, 244, 170, 72, 22,
            233, 183, 85, 11, 136, 214, 52, 106, 43, 117, 151, 201, 74, 20, 246, 168,
            116, 42, 200, 150, 21, 75, 169, 247, 182, 232, 10, 84, 215, 137, 107, 53
        };

        #endregion CRC8校验表

        #region CRC16校验表

        private static readonly byte[] CrcTableH =
        {
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40
        };

        private static readonly byte[] CrcTableL =
        {
            0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04,
            0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09, 0x08, 0xC8,
            0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC,
            0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3, 0x11, 0xD1, 0xD0, 0x10,
            0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4,
            0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A, 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38,
            0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C,
            0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26, 0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0,
            0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4,
            0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F, 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68,
            0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C,
            0xB4, 0x74, 0x75, 0xB5, 0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0,
            0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54,
            0x9C, 0x5C, 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98,
            0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
            0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80, 0x40
        };

        #endregion

        public static byte CheckCRC8(byte[] val, int start, int end)
        {
            if (val == null)
                throw new ArgumentNullException("val");
            byte c = 0;
            for (int i = start; i <= end; i++)
            {
                byte b = val[i];
                c = CRC8Table[c ^ b];
            }
            return c;
        }

        public static ushort CheckCRC16(byte[] buffer, int start, int end, out byte crcHi, out byte crcLo)
        {
            crcHi = 0xff; // high crc byte initialized
            crcLo = 0xff; // low crc byte initialized

            for (int i = start; i < end; i++)
            {
                int crcIndex = crcLo ^ buffer[i]; // calculate the crc lookup index
                crcLo = (byte)(crcHi ^ CrcTableH[crcIndex]);
                crcHi = CrcTableL[crcIndex];
            }
            return (ushort)(crcHi << 8 | crcLo);
        }

        public static ushort CheckSum2(byte[] buff, int start, int length)
        {
            ushort shift = 0, data = 0, val = 0;
            int i;
            shift = CRC_SEED;
            for (i = start; i < length; i++)
            {
                if ((i % 2) == 0)
                {
                    byte[] by = { buff[i], buff[i + 1] };
                    data = BitConverter.ToUInt16(by, 0);
                }
                val = (ushort)(shift ^ data);
                shift = (ushort)(shift << 1);
                data = (ushort)(data << 1);
                if ((val & 0x8000) != 0)
                    shift = (ushort)(shift ^ CRC_POLY16);
            }
            return shift;
        }

        public static UInt16 CheckSum(byte[] buffer, int start, int length)
        {

            UInt32 cksum = 0;
            int index = start;
            int size = length;

            while (size > 1)
            {
                byte[] by = { buffer[index], buffer[index + 1] };
                Array.Reverse(by);
                cksum += BitConverter.ToUInt16(by, 0);
                index += 2;
                size -= 2;
            }
            if (size == 1)
            {
                cksum += buffer[index];
            }
            cksum = (cksum >> 16) + (cksum & 0xffff);
            cksum += (cksum >> 16);
            return (ushort)(~cksum);
        }

        public static UInt16 CheckSum(ushort[] buff, int start, int len)
        {
            uint cksum = 0;
            for (int i = start; i < start + len; i++)
            {
                cksum += buff[i];
            }
            cksum = (cksum >> 16) + (cksum & 0xffff);
            cksum += (cksum >> 16);
            return (ushort)(~cksum);
        }

        public static ushort[] Bytes2Ushorts(byte[] data)
        {
            int i = 0;
            var result = new List<ushort>();
            while (data.Length - i > 2)
            {
                result.Add(BitConverter.ToUInt16(data, i));
                i += 2;
            }

            return result.ToArray();
        }


        /// <summary>
        /// 异或校验
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public static byte CheckXor(byte[] data, int start, int end)
        {
            byte result = 0;
            for (int i = start; i < end; i++)
            {
                result = (byte)((((int)result) ^ ((int)data[i])) & 0x000000ff);
            }
            return result;
        }

        public static byte ICMPCheckSum(byte[] data, int start, int end, out byte check)
        {
            byte checksum = 0;
            for (int i = start; i < end; i++)
            {
                checksum += data[i];
            }
            checksum = (byte)((checksum >> 8) + (checksum & 0xff));
            checksum += (byte)(checksum >> 8);
            check = (byte)(~checksum + 1);
            return check;
        }

        /// <summary>
        /// 加和校验
        /// </summary>
        /// <param name="packsge"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        private static byte CheckPlusSum(byte[] packsge, int startIndex, int endIndex, out byte check)
        {
            int sum = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                sum = (sum + packsge[i]) % 0xffff;
            }
            byte result = (byte)(sum & 0xff);
            check = result;
            return result;
        }

        /// <summary>
        /// 加总异或
        /// </summary>
        /// <param name="packsge"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        private static byte CheckXorSum(byte[] packsge, int startIndex, int endIndex, out byte check)
        {
            byte result = (byte)(packsge[startIndex] ^ packsge[startIndex + 1]);
            for (int i = startIndex; i < endIndex + 1; i++)
            {
                check = (byte)(result ^ packsge[i]);
            }
            check = result;
            return check;
        }

        public static bool IsEqualZero(double value)
        {
            return Math.Abs(value) < PRECISIOND;
        }

        public static bool IsEqualZero(float value)
        {
            return Math.Abs(value) < PRECISIONF;
        }

        /// 小字序
        public static short GetShort(byte[] data, int index)
        {
            var by = new byte[2];
            Array.Copy(data, index, by, 0, 2);
            return BitConverter.ToInt16(by, 0);
        }

        public static short GetShort_BE(byte[] data, int index)
        {
            var by = new byte[2];
            Array.Copy(data, index, by, 0, 2);
            Array.Reverse(by);
            return BitConverter.ToInt16(by, 0);
        }

        public static ushort GetUshort_BE(byte[] data, int index)
        {
            var by = new byte[2];
            Array.Copy(data, index, by, 0, 2);
            Array.Reverse(by);
            return BitConverter.ToUInt16(by, 0);
        }
        /// 小字序
        public static ushort GetUShort(byte[] data, int index)
        {
            var by = new byte[2];
            Array.Copy(data, index, by, 0, 2);
            return BitConverter.ToUInt16(by, 0);
        }

        public static ushort GetUShort_BE(byte[] data, int index)
        {
            var by = new byte[2];
            Array.Copy(data, index, by, 0, 2);
            Array.Reverse(by);
            return BitConverter.ToUInt16(by, 0);
        }


        /// 小字序
        public static float GetFloat(byte[] data, int index)
        {
            var by = new byte[4];
            Array.Copy(data, index, by, 0, 4);
            return BitConverter.ToSingle(by, 0);
        }

        /// 大字序
        public static float GetFloat_BE(byte[] data, int index)
        {
            var by = new byte[4];
            Array.Copy(data, index, by, 0, 4);
            Array.Reverse(by);
            return BitConverter.ToSingle(by, 0);
        }

        /// 小字序
        public static double GetDouble(byte[] data, int index)
        {
            var by = new byte[8];
            Array.Copy(data, index, by, 0, 8);
            return BitConverter.ToDouble(by, 0);
        }

        /// 大字序
        public static double GetDouble_BE(byte[] data, int index)
        {
            var by = new byte[8];
            Array.Copy(data, index, by, 0, 8);
            Array.Reverse(by);
            return BitConverter.ToSingle(by, 0);
        }

        /// 小字序
        public static int GetInt(byte[] data, int index)
        {
            var by = new byte[4];
            Array.Copy(data, index, by, 0, 4);
            return BitConverter.ToInt32(by, 0);
        }

        /// 大字序
        public static int GetInt_BE(byte[] data, int index)
        {
            var by = new byte[4];
            Array.Copy(data, index, by, 0, 4);
            Array.Reverse(by);
            return BitConverter.ToInt32(by, 0);
        }

        /// 小字序
        public static uint GetUInt(byte[] data, int index)
        {
            var by = new byte[4];
            Array.Copy(data, index, by, 0, 4);
            return BitConverter.ToUInt32(by, 0);
        }

        /// 大字序
        public static uint GetUInt_BE(byte[] data, int index)
        {
            var by = new byte[4];
            Array.Copy(data, index, by, 0, 4);
            Array.Reverse(by);
            return BitConverter.ToUInt32(by, 0);
        }


        /// 小字序
        public static long GetLong(byte[] data, int index)
        {
            var by = new byte[8];
            Array.Copy(data, index, by, 0, 8);
            return BitConverter.ToInt64(by, 0);
        }

        /// 大字序
        public static long GetLong_BE(byte[] data, int index)
        {
            var by = new byte[8];
            Array.Copy(data, index, by, 0, 8);
            Array.Reverse(by);
            return BitConverter.ToInt64(by, 0);
        }

        /// 小字序
        public static ulong GetULong(byte[] data, int index)
        {
            var by = new byte[8];
            Array.Copy(data, index, by, 0, 8);
            return BitConverter.ToUInt64(by, 0);
        }

        /// 大字序
        public static ulong GetULong_BE(byte[] data, int index)
        {
            var by = new byte[8];
            Array.Copy(data, index, by, 0, 8);
            Array.Reverse(by);
            return BitConverter.ToUInt64(by, 0);
        }

        // FD010022 ... 16进制文本转 byte数组
        public static byte[] ToBytes(string str)
        {
            int len = str.Length / 2;
            byte[] buff = new byte[len];
            for (int i = 0; i < len; i++)
            {
                buff[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
            return buff;
        }

        public static byte[] StrToToHexByte(string hexString)
        {
            string str = hexString;
            str = str.Replace(" ", "").Replace("\n", "").Replace("\r", "");
            return ToBytes(str);
        }

        public static string BytesToHexStr(byte[] da, int len = 0, string separator = " ")
        {
            if (da == null) return "";
            if (len == 0)
            {
                len = da.Length;
            }
            else
            {
                len = Math.Min(len, da.Length);
            }
            return BytesToHexStr(da, 0, len, separator);
        }

        public static string BytesToHexStr(byte[] da, int start, int length, string separator = " ")
        {
            if (da == null || da.Length == 0)
            {
                return "";
            }
            if (start < 0) start = 0;
            length = Math.Min(length, da.Length);
            string str = "";
            if (separator == null)
            {
                separator = " ";
            }
            for (int i = start; i < (start + length); i++)
            {
                // str = str + Convert.ToString(da[i], 0x10).PadLeft(2, '0') + separator;
                str += string.Format("{0:X2}", da[i]) + separator;
            }
            if (length > 0 && separator.Length > 0)
            {
                str = str.Remove(str.Length - separator.Length);
            }
            return str;
        }

        public static void WriteShort(byte[] buff, int offset, short value)
        {
            byte[] b2 = BitConverter.GetBytes(value);
            Array.Copy(b2, 0, buff, offset, 2);
        }

        public static void WriteShort_BE(byte[] buff, int offset, short value)
        {
            byte[] b2 = BitConverter.GetBytes(value);
            Array.Reverse(b2);
            Array.Copy(b2, 0, buff, offset, 2);
        }

        public static void WriteShort_BE(byte[] buff, int offset, ushort value)
        {
            byte[] b2 = BitConverter.GetBytes(value);
            Array.Reverse(b2);
            Array.Copy(b2, 0, buff, offset, 2);
        }

        public static void WriteUShort(byte[] buff, int offset, ushort value)
        {
            byte[] b2 = BitConverter.GetBytes(value);
            Array.Copy(b2, 0, buff, offset, 2);
        }

        public static void WriteUShort_BE(byte[] buff, int offset, ushort value)
        {
            byte[] b2 = BitConverter.GetBytes(value);
            Array.Reverse(b2);
            Array.Copy(b2, 0, buff, offset, 2);
        }

        public static void WriteInt(byte[] buff, int offset, int value)
        {
            byte[] b4 = BitConverter.GetBytes(value);
            Array.Copy(b4, 0, buff, offset, 4);
        }

        public static void WriteInt_BE(byte[] buff, int offset, int value)
        {
            byte[] b4 = BitConverter.GetBytes(value);
            Array.Reverse(b4);
            Array.Copy(b4, 0, buff, offset, 4);
        }

        public static void WriteUInt(byte[] buff, int offset, uint value)
        {
            byte[] b4 = BitConverter.GetBytes(value);
            Array.Copy(b4, 0, buff, offset, 4);
        }

        public static void WriteUInt_BE(byte[] buff, int offset, uint value)
        {
            byte[] b4 = BitConverter.GetBytes(value);
            Array.Reverse(b4);
            Array.Copy(b4, 0, buff, offset, 4);
        }

        public static void WriteLong(byte[] buff, int offset, long value)
        {
            byte[] b8 = BitConverter.GetBytes(value);
            Array.Copy(b8, 0, buff, offset, 8);
        }

        public static void WriteLong_BE(byte[] buff, int offset, long value)
        {
            byte[] b8 = BitConverter.GetBytes(value);
            Array.Reverse(b8);
            Array.Copy(b8, 0, buff, offset, 8);
        }

        public static void WriteULong(byte[] buff, int offset, ulong value)
        {
            byte[] b8 = BitConverter.GetBytes(value);
            Array.Copy(b8, 0, buff, offset, 8);
        }

        public static void WriteULong_BE(byte[] buff, int offset, ulong value)
        {
            byte[] b8 = BitConverter.GetBytes(value);
            Array.Reverse(b8);
            Array.Copy(b8, 0, buff, offset, 8);
        }


        public static void WriteFloat(byte[] buff, int offset, float value)
        {
            byte[] b4 = BitConverter.GetBytes(value);
            Array.Copy(b4, 0, buff, offset, 4);
        }

        public static void WriteFloat_BE(byte[] buff, int offset, float value)
        {
            byte[] b4 = BitConverter.GetBytes(value);
            Array.Reverse(b4);
            Array.Copy(b4, 0, buff, offset, 4);
        }

        public static void WriteDouble(byte[] buff, int offset, double value)
        {
            byte[] b8 = BitConverter.GetBytes(value);
            Array.Copy(b8, 0, buff, offset, 8);
        }

        public static void WriteDouble_BE(byte[] buff, int offset, float value)
        {
            byte[] b8 = BitConverter.GetBytes(value);
            Array.Reverse(b8);
            Array.Copy(b8, 0, buff, offset, 8);
        }

        public static void WriteAscii(byte[] buff, int offset, int value)
        {
            string s = string.Format("{0:00000000}", value); //8 bytes
            byte[] ss = Encoding.ASCII.GetBytes(s);
            Array.Copy(ss, 0, buff, offset, ss.Length);
        }

        public static void WriteAscii(byte[] buff, int offset, string str)
        {
            if (string.IsNullOrEmpty(str)) return;
            byte[] abs = Encoding.ASCII.GetBytes(str);
            Array.Copy(abs, 0, buff, offset, abs.Length);
        }

        // 将 20120049 写成 0x49, 0x00 0x12 0x20
        public static void WriteBCD(byte[] buff, int offset, int value)
        {
            string s = string.Format("{0:00000000}", value);
            byte[] ss = Encoding.ASCII.GetBytes(s);
            buff[offset] = (byte)(((ss[6] - 0x30) << 4) + (ss[7] - 0x30));
            buff[offset + 1] = (byte)(((ss[4] - 0x30) << 4) + (ss[5] - 0x30));
            buff[offset + 2] = (byte)(((ss[2] - 0x30) << 4) + (ss[3] - 0x30));
            buff[offset + 3] = (byte)(((ss[0] - 0x30) << 4) + (ss[1] - 0x30));
        }

        /// 写入IP: 小字序
        // 将 "192.168.1.12 写成小序 [12,1,168,192]
        public static void WriteIP(byte[] buff, int offset, string ip)
        {
            string[] cs = ip.Split('.');
            if (cs.Length == 4)
            {
                buff[offset] = Convert.ToByte(cs[0]);
                buff[offset + 1] = Convert.ToByte(cs[1]);
                buff[offset + 2] = Convert.ToByte(cs[2]);
                buff[offset + 3] = Convert.ToByte(cs[3]);
            }
        }

        /// <summary>
        ///  逗号分隔的数字串,分解为数字数组.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<uint> ToIntArray(string p, char separator = ',')
        {
            if (string.IsNullOrEmpty(p))
            {
                return null;
            }
            List<uint> numbers = new List<uint>();
            string[] ps = p.Split(separator);
            foreach (string pi in ps)
            {
                if (string.IsNullOrEmpty(pi))
                {
                    continue;
                }
                numbers.Add(Convert.ToUInt32(pi));
            }
            return numbers;
        }

        // [1,2,3] => "1,2,3"
        public static string ToStr(IList<uint> list, char separator = ',')
        {
            if (list != null && list.Count > 0)
            {
                string x = "";
                foreach (uint i in list)
                {
                    x += Convert.ToString(i) + separator;
                }
                return x.Substring(0, x.Length - 1);
            }
            else
            {
                return "";
            }
        }

        public static double ValueAt(double[] values, uint idx, double defValue)
        {
            if (idx >= 0 && idx < values.Length)
            {
                return values[idx];
            }
            else
            {
                return defValue;
            }
        }

        // millseconds from 2001-1-1
        public static long MillSeconds(DateTime time)
        {
            DateTime centuryBegin = new DateTime(2001, 1, 1);
            long elapsedTicks = time.Ticks - centuryBegin.Ticks;
            return elapsedTicks / 10000; // ms
        }

        public static string GetString(byte[] data, int offset, int len)
        {
            return Encoding.ASCII.GetString(data, offset, len);
        }

        public static string GetMd5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

        public static string GetSha1HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.SHA1 sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                byte[] retVal = sha1.ComputeHash(file);
                file.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

        public static string RemoveWrap(string cmd)
        {
            return cmd.Replace("\r\n", "");
        }

        // bit[8] -> byte.
        public static byte ToByte(byte[] bs, int offset)
        {
            byte v = 0;
            for (byte i = 0; i < 8; i++)
            {
                if (bs[8 + offset - i - 1] == 1)
                {
                    v |= (byte)(0x01 << i);
                }
            }
            return v;
        }

        public static byte Sum(byte[] buff, uint start, uint len)
        {
            byte v = 0;
            uint pos = start;
            while (pos < start + len)
            {
                v += buff[pos++];
            }
            return v;
        }

        public static string GetIpLastAddr(string ip)
        {
            int pos = ip.LastIndexOf(".");
            if (pos != -1)
            {
                return ip.Substring(pos + 1);
            }
            return ip;
        }
    }
}
