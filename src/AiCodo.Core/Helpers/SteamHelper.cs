// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    public static class SteamHelper
    {
        #region 扩展方法
        public static string ReadToEnd(this Stream stream)
        {
            if (stream == null)
            {
                return string.Empty;
            }
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("utf-8")))
            {
                return reader.ReadToEndAsync().Result;
            }
        }

        public static Stream ToStream(this string content)
        {
            MemoryStream stream = new MemoryStream();
            var sw = new StreamWriter(stream);
            sw.Write(content);
            sw.Flush();

            stream.Position = 0;
            return stream;
        }
        #endregion 

        public static string GetMd5(this Stream inputStream)
        {
            try
            {
                if (inputStream.Position != 0)
                {
                    if (inputStream.CanSeek)
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);
                    }
                    else
                    {
                        throw new Exception("计算md5必须从0开始");
                    }
                }
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(inputStream);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("计算Md5失败，错误:" + ex.Message);
            }
        }
    }
}
