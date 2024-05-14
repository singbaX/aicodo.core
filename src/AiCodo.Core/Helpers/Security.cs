// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AiCodo
{
    public static class SecurityHelper
    {
        #region DES加密
        public static string EncryptDES(this string encryptString, string key = "")
        {
            DESCode des = new DESCode();
            return (string.IsNullOrEmpty(key) || key.Length < 8) ?
                des.EncryptDES(encryptString) :
                des.EncryptDES(encryptString, key);
        }

        public static string DecryptDES(this string decryptString, string key = "")
        {
            DESCode des = new DESCode();
            return (string.IsNullOrEmpty(key) || key.Length < 8) ?
                des.DecryptDES(decryptString) :
                des.DecryptDES(decryptString, key);
        }

        public static byte[] EncryptDES(this byte[] bytes, string key)
        {
            DESCode des = new DESCode();
            return des.EncryptDES(bytes, key);
        }

        public static byte[] DecryptDES(this byte[] bytes, string key = "")
        {
            DESCode des = new DESCode();
            return des.DecryptDES(bytes, key);
        }
        #endregion

        #region MD5加密 
        public static string EncryptMd5_16(this string sourceString)
        {
            return MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(sourceString)).
                Skip(4).Take(8).
                Select(b => b.ToString("x2")).AggregateStrings("");
            //return BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(sourceString),4,8));
        }

        public static string EncryptMd5_32(this string sourceString)
        {
            return MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(sourceString))
                .Select(b => b.ToString("x2")).AggregateStrings("");
        }
        #endregion

        #region RSA 加密解密

        //密钥对
        private const string PublicRsaKey = @"<RSAKeyValue><Modulus>x</Modulus><Exponent>e</Exponent></RSAKeyValue>";
        private const string PrivateRsaKey = @"<RSAKeyValue><Modulus>x</Modulus><Exponent>e</Exponent><P>p</P><Q>q</Q><DP>dp</DP><DQ>dq</DQ><InverseQ>iq</InverseQ><D>d</D></RSAKeyValue>";

        /// <summary>
        /// RSA 加密
        /// </summary>
        /// <param name="source">待加密字段</param>
        /// <returns></returns>
        public static string EncryptRSA(this string source)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(PublicRsaKey);
            var cipherbytes = rsa.Encrypt(Encoding.UTF8.GetBytes(source), true);
            return Convert.ToBase64String(cipherbytes);
        }

        /// <summary>
        /// RSA解密
        /// </summary>
        /// <param name="source">待解密字段</param>
        /// <returns></returns>
        public static string DecryptRSA(this string source)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(PrivateRsaKey);
            var cipherbytes = rsa.Decrypt(Convert.FromBase64String(source), true);
            return Encoding.UTF8.GetString(cipherbytes);
        }

        public static string EncryptRSA(this string source, string publicXmlKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicXmlKey);
            var cipherbytes = rsa.Encrypt(Encoding.UTF8.GetBytes(source), true);
            return Convert.ToBase64String(cipherbytes);
        }

        public static string DecryptRSA(this string source, string privateXmlKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateXmlKey);
            var cipherbytes = rsa.Decrypt(Convert.FromBase64String(source), true);
            return Encoding.UTF8.GetString(cipherbytes);
        }

        public static string CreateRSAKey(bool includePrivate)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            return rsa.ToXmlString(includePrivate);
        }

        #endregion
    }

    class DESCode
    {
        //默认密匙
        private string CodeKey = "abihonet";

        /// <summary>
        /// DES加密
        /// </summary>
        public string EncryptDES(string encryptString)
        {
            return EncryptDES(encryptString, CodeKey);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        public string DecryptDES(string decryptString)
        {
            return DecryptDES(decryptString, CodeKey);
        }

        //默认密钥向量
        private byte[] Keys = { 0xEF, 0xAB, 0x56, 0x78, 0x90, 0x34, 0xCD, 0x12 };

        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="encryptString">待加密的字符串</param>
        /// <param name="encryptKey">加密密钥,要求为8位</param>
        /// <returns>加密成功返回加密后的字符串，失败返回源串</returns>
        public string EncryptDES(string encryptString, string encryptKey)
        {
            try
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                string str = Convert.ToBase64String(EncryptDES(inputByteArray, encryptKey));
                return str;
            }
            catch
            {
                return encryptString;
            }
        }

        public byte[] EncryptDES(byte[] inputByteArray, string encryptKey)
        {
            byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
            byte[] rgbIV = Keys;
            DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
            MemoryStream mStream = new MemoryStream();
            CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
            cStream.Write(inputByteArray, 0, inputByteArray.Length);
            cStream.FlushFinalBlock();

            var bytes = mStream.ToArray();
            return bytes;
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="decryptString">待解密的字符串(BASE64)</param>
        /// <param name="decryptKey">解密密钥,要求为8位,和加密密钥相同</param>
        /// <returns>解密成功返回解密后的字符串，失败返源串</returns>
        public string DecryptDES(string decryptString, string decryptKey)
        {
            try
            {
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                var bytes = DecryptDES(inputByteArray, decryptKey);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return decryptString;
            }
        }

        public byte[] DecryptDES(byte[] inputByteArray, string decryptKey)
        {
            byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));
            byte[] rgbIV = Keys;
            DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
            MemoryStream mStream = new MemoryStream();
            CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
            cStream.Write(inputByteArray, 0, inputByteArray.Length);
            cStream.FlushFinalBlock();
            var bytes = mStream.ToArray();
            return bytes;
        }
    }
}
