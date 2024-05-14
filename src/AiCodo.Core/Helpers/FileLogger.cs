// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Concurrent;
    public static class FileLogger
    {
        private static string AppPath = ApplicationConfig.LocalDataFolder;

        static object ErrorLock = new object();

        private static ConcurrentBag<string> _Files = new ConcurrentBag<string>();

        public static void WriteErrorLog(this Exception ex)
        {
            lock (ErrorLock)
            {
                ex.ToString().AppendFileLog(string.Format("log\\error{0}.log", DateTime.Now.Date.ToString("yyyyMMdd")));
            }
        }

        public static void WriteErrorLog(this string message)
        {
            lock (ErrorLock)
            {
                message.AppendFileLog(string.Format("log\\error{0}.log", DateTime.Now.Date.ToString("yyyyMMdd")));
            }
        }

        public static void WriteFileLog(this string text, string filename)
        {
            try
            {
                string path = System.IO.Path.Combine(AppPath, filename.Replace("/", "\\"));
                string dir = path.Substring(0, path.LastIndexOf('\\'));
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                lock (path)
                {
                    using (var sw = System.IO.File.Exists(path) ? System.IO.File.AppendText(path) : System.IO.File.CreateText(path))
                    {
                        sw.Write(text);
                        sw.Close();
                    }
                }
            }
            catch
            {
            }
        }

        public static void AppendFileLog(this string text, string filename, string dateFormat = "yyyy-MM-dd HH:mm:ss")
        {
            try
            {
                string path = System.IO.Path.Combine(AppPath, filename.Replace("/", "\\"));
                string dir = path.Substring(0, path.LastIndexOf('\\'));
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                using (var sw = System.IO.File.Exists(path) ? System.IO.File.AppendText(path) : System.IO.File.CreateText(path))
                {
                    sw.WriteLine("[{0}]{1}", DateTime.Now.ToString(dateFormat), text);
                    sw.Close();
                }
            }
            catch
            {
            }
        }
    }
}
