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
    using System.Threading;
    public class TextFile
    {
        static Dictionary<string, TextFile> _Files = new Dictionary<string, TextFile>();
        static object _FileLock = new object();

        public static void AddLine(string fileName, string line)
        {
            lock (_FileLock)
            {
                var key = fileName.ToLower();
                if (!_Files.TryGetValue(key, out var file))
                {
                    file = new TextFile(fileName);
                    _Files[key] = file;
                }
                file.AddLine(line);
            }
        }

        Queue<string> _Lines = new Queue<string>();
        string _FileName = "";
        object _QueueLock = new object();
        bool _IsStart = false;

        private TextFile(string fileName)
        {
            _FileName = fileName.FixedAppBasePath();
            var root = _FileName.GetParentPath();
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }

        void AddLine(string line, bool addTime = true, string timeFormat = "yyyy-MM-dd HH:mm:ss.fff")
        {
            if (addTime)
            {
                line = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] {line}";
            }
            lock (_QueueLock)
            {
                _Lines.Enqueue(line);
            }
            CheckWriteStart();
        }

        private void CheckWriteStart()
        {
            if (_IsStart)
            {
                return;
            }
            lock (_QueueLock)
            {
                if (_IsStart)
                {
                    return;
                }
                _IsStart = true;
                Threads.StartNew(() =>
                {
                    var sw = System.IO.File.AppendText(_FileName);
                    var count = 0;
                    while (true)
                    {
                        if (_Lines.Count > 0)
                        {
                            var line = "";
                            lock (_QueueLock)
                            {
                                line = _Lines.Dequeue();
                            }
                            sw.WriteLine(line);
                            count++;
                            if (count > 100)
                            {
                                count = 0;
                                sw.Flush();
                            }
                            continue;
                        }
                        sw.Flush();
                        count = 0;
                        while (_Lines.Count == 0 && count < 10)
                        {
                            Thread.Sleep(1000);
                            count++;
                        }
                        if (_Lines.Count == 0)
                        {
                            lock (_QueueLock)
                            {
                                if (_Lines.Count == 0)
                                {
                                    sw.Close();
                                    _IsStart = false;
                                    break;
                                }
                            }
                        }
                    }
                });
            }
        }
    }
}
