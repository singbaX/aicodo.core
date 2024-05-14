// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace AiCodo
{
    public static class ByteHelper
    {
        public static int GetInt32(this byte[] bytes, int index, int length)
        {
            int value = 0;
            for (int i = 0; i < length; i++)
            {
                value = (value << 8) + bytes[index + i];
            }
            return value;
        }

        public static Guid GetGuid(this byte[] bytes, int index)
        {
            if (bytes == null || bytes.Length < index + 16)
            {
                return Guid.Empty;
            }

            byte[] gbytes = new byte[16];
            Array.Copy(bytes, index, gbytes, 0, 16);
            return new Guid(gbytes);
        }

        public static long GetInt64(this byte[] bytes, int index)
        {
            long value = 0;
            for (int i = 0; i < 8; i++)
            {
                value = (value << 8) + bytes[index + i];
            }
            return value;
        }

        public static int GetNetInt32(this byte[] bytes, int index, int length)
        {
            int value = 0;
            for (int i = 0; i < length; i++)
            {
                value = value + (bytes[index + i] << (8 * i));
            }
            return value;
        }

        public static byte[] GetBytes(this byte[] bytes, int index, int length)
        {
            var newBytes = new byte[length];
            Array.Copy(bytes, index, newBytes, 0, length);
            return newBytes;
        }

        public static byte[] GetNetBytes(this int value, int length)
        {
            byte[] bytes = new byte[length];
            if (length > 0)
            {
                bytes[0] = (byte)(value & 0xFF);
            }
            if (length > 1)
            {
                bytes[1] = (byte)((value & 0xFF00) >> 8);
            }
            if (length > 2)
            {
                bytes[2] = (byte)((value & 0xFF0000) >> 16);
            }
            if (length > 3)
            {
                bytes[3] = (byte)((value & 0xFF000000) >> 24);
            }
            return bytes;
        }

        public static byte[] GetNetBytes(this long value)
        {
            int length = 8;
            byte[] bytes = new byte[length];

            long ff = 0xff;
            var pos = 0;
            for (int i = 0; i < 8; i++)
            {
                bytes[i] = (byte)((value & ff) >> pos);
                pos += 8;
                ff = ff << 8;
            }
            return bytes;
        }

        public static byte[] GetBytes(this long value)
        {
            int length = 8;
            byte[] bytes = new byte[length];

            long ff = 0xff;
            var pos = 0;
            for (int i = 0; i < 8; i++)
            {
                bytes[7 - i] = (byte)((value & ff) >> pos);
                pos += 8;
                ff = ff << 8;
            }
            return bytes;
        }

        public static byte[] GetBytes(this int value, int length = 4)
        {
            byte[] bytes = GetNetBytes(value, length);
            for (int i = 0; i < (bytes.Length / 2); i++)
            {
                var b = bytes[i];
                bytes[i] = bytes[bytes.Length - i - 1];
                bytes[bytes.Length - i - 1] = b;
            }
            return bytes;
        }

        public static byte[] GetHexBytes(this string hex)
        {
            if ((hex.Length % 2) == 1)
            {
                hex = "0" + hex;
            }
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static int FindBytes(this byte[] data, byte[] find,int index=0)
        {
            if (find == null || find.Length == 0)
            {
                return -1;
            } 
            var flag = true;
            int i = 0;
            for(; index < (data.Length - find.Length + 1); index++)
            {
                flag = true;
                for (i = 0; i < find.Length; i++)
                {
                    if (data[index + i] != find[i])
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    return index;
                }
            }

            return -1;
        }

        public static string GetString(this byte[] data)
        {
            return System.Text.Encoding.UTF8.GetString(data);
        }

        public static string GetString(this byte[] data, int index, int count)
        {
            return System.Text.Encoding.UTF8.GetString(data, index, count);
        }

        public static byte[] FixedLength(this byte[] bytes, int length)
        {
            if (bytes.Length == length)
            {
                return bytes;
            }
            var data = new byte[length];
            Array.Copy(bytes, data, bytes.Length > length ? length : bytes.Length);
            return data;
        }

        public static byte[] GetBytes(this string source)
        {
            Encoding encoding = Encoding.UTF8;
            return encoding.GetBytes(source);
        }

        public static byte[] GetBytes(this string source, string encodingName)
        {
            Encoding encoding = string.IsNullOrEmpty(encodingName) ? Encoding.Default : Encoding.GetEncoding(encodingName);

            return encoding.GetBytes(source);
        }

        public static byte GetAddChceck(this byte[] sourceBytes, int index, int length)
        {
            byte value = 0;
            for (int i = index; i < index + length; i++)
            {
                value += sourceBytes[i];
            }
            return value;
        }

        public static byte GetXorChceck(this byte[] sourceBytes, int index, int length)
        {
            byte value = 0;
            for (int i = index; i < index + length; i++)
            {
                value = (byte)(value ^ sourceBytes[i]);
            }
            return value;
        }

        public static byte[] Merge(this byte[] source, byte[] append)
        {
            var bytes = new byte[source.Length + append.Length];
            Array.Copy(source, 0, bytes, 0, source.Length);
            Array.Copy(append, 0, bytes, source.Length, append.Length);
            return bytes;
        }

        public static byte[] CopyBytes(this byte[] sourceBytes, int sourceFromIndex, byte[] bytesAdd)
        {
            Array.Copy(bytesAdd, 0, sourceBytes, sourceFromIndex, bytesAdd.Length);
            return sourceBytes;
        }

        public static void CopyBits(this byte[] sourceBytes, int sourceBitFrom, byte[] bytesAdd, int bitAddFrom, int length)
        {
            int bitIndex = 0;
            int byteIndex = Math.DivRem(sourceBitFrom, 8, out bitIndex);

            int endBitIndex = 0;
            int byteCount = Math.DivRem((bitIndex + length), 8, out endBitIndex);
            if (endBitIndex > 0)
            {
                byteCount++;
            }

            byte[] oldBytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                oldBytes[i] = sourceBytes[byteIndex + i];
            }

            BitArray oldBits = new BitArray(oldBytes);
            BitArray addBits = new BitArray(bytesAdd);

            Func<int, int> getBitArrayIndex = (index) =>
            {
                int b = 0;
                int B = Math.DivRem(index, 8, out b);
                return B * 8 + (7 - b);
            };

            for (int i = 0; i < length; i++)
            {
                oldBits[getBitArrayIndex(bitIndex + i)] = addBits[getBitArrayIndex(bitAddFrom + i)];
            }
            byte[] newBytes = new byte[byteCount];
            oldBits.CopyTo(newBytes, 0);

            Array.Copy(newBytes, 0, sourceBytes, byteIndex, byteCount);
        }

        public static string ToHexString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "");
        }
        public static string ToHexString(this byte[] data, int index, int length)
        {
            return BitConverter.ToString(data, index, length).Replace("-", "");
        }

        public static string ToBase64String(this byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static byte[] FromBase64String(this string data)
        {
            return Convert.FromBase64String(data);
        }
    }
}
