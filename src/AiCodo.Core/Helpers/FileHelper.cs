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
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    public static class FileHelper
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr _lopen(string lpPathName, int iReadWrite);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
        const int OF_READWRITE = 2;
        const int OF_SHARE_DENY_NONE = 0x40;
        static IntPtr HFILE_ERROR = new IntPtr(-1);

        /// <summary>
        /// 取文件扩展名
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetExt(this string fileName)
        {
            fileName = fileName.Trim();
            var indexOfExt = fileName.LastIndexOf('.');
            return (indexOfExt >= 0 && indexOfExt < fileName.Length - 1) ?
                fileName.Substring(indexOfExt + 1).Trim() : "";
        }

        public static bool CanOpen(this string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new Exception("文件不存在");
            }
            IntPtr vHandle = _lopen(fileName, OF_READWRITE | OF_SHARE_DENY_NONE);
            if (vHandle == HFILE_ERROR)
            {
                return false;
            }
            CloseHandle(vHandle);
            return true;
        }

        /// <summary>
        /// 取文件或文件夹名称，
        /// </summary>
        /// <param name="path">D:\singba\aa</param>
        /// <returns>aa</returns>
        public static string GetName(this string path)
        {
            string name = path.TrimEnd('\\');
            int index = name.LastIndexOf("\\");
            return index > 0 ? name.Substring(index + 1) : name;
        }

        public static long GetFileLength(this string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            return fileInfo == null ? 0 : fileInfo.Length;
        }

        public static bool IsNameLike(this string sourceName, string likeName)
        {
            if (sourceName.Contains('\\'))
            {
                sourceName = sourceName.Substring(sourceName.LastIndexOf('\\') + 1);
            }
            var index = likeName.IndexOf('*');
            if (index < 0)
            {
                return sourceName.Equals(likeName, StringComparison.OrdinalIgnoreCase);
            }
            //支持一个*

            var start = index > 0 ? likeName.Substring(0, index) : "";
            var end = index == (likeName.Length - 1) ? "" : likeName.Substring(index + 1);

            return (string.IsNullOrEmpty(start) || sourceName.StartsWith(start, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(end) || sourceName.EndsWith(end, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 取相对路径，只实现子路径
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static string GetRelativePath(this string fullPath, string root)
        {
            var fixRoot = root.FixPath();
            var index = fullPath.IndexOf(fixRoot);
            if (index < 0)
            {
                throw new Exception(string.Format("{0}不是{1}的子目录", fullPath, root));
            }
            return fullPath.Substring(index + 1);
        }

        /// <summary>
        /// 取文件或文件夹所在的目录
        /// </summary>
        /// <param name="path">文件或文件夹名称</param>
        /// <returns></returns>
        public static string GetParentPath(this string path)
        {
            var newPath = path.TrimEnd('\\');
            var index = newPath.LastIndexOf('\\');
            return index < 0 ? "" : newPath.Substring(0, index + 1);
        }

        /// <summary>
        /// 如果路径最后不是"\"，则补一个，如果是空路径，则直接返回
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string FixPath(this string path)
        {
            return string.IsNullOrEmpty(path) ? "" :
                path.EndsWith("\\") ? path : (path + "\\");
        }

        public static void OpenFileProcess(this string fileName)
        {
            System.Diagnostics.Process.Start(fileName);
        }

        public static bool IsFileExists(this string fileName)
        {
            return File.Exists(fileName);
        }

        public static bool IsFileNotExists(this string fileName)
        {
            return !File.Exists(fileName);
        }

        public static string FixedPath(this string path)
        {
            return path.EndsWith("\\") ? path : (path + "\\");
        }

        public static void DeleteFile(this string fileName)
        {
            File.Delete(fileName);
        }

        public static void CopyFile(this string fileName, string targetName, bool overwrite = true, bool forceCopy = true)
        {
            var dir = Path.GetDirectoryName(targetName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            try
            {
                File.Copy(fileName, targetName, overwrite);
            }
            catch (Exception ex)
            {
                CopyStream(fileName, targetName);
                //if (ex is UnauthorizedAccessException)
                //{
                //可以加入是否只读的判断
                //}
                //else
                //{
                throw;
                //}
            }
        }

        public static FileAttributes MergeFileAttributes(this FileAttributes source, bool add, FileAttributes attr)
        {
            if (add)
            {
                return source | attr;
            }
            else if ((source & attr) == attr)
            {
                return (FileAttributes)((int)source - (int)attr);
            }
            return source;
        }

        public static void CopyStream(this string fileName, string targetName)
        {
            using (var sourceStream = fileName.OpenShare())
            {
                StreamWriter sw = new StreamWriter(targetName);
                sourceStream.CopyTo(sw.BaseStream);
                sw.Flush();
                sw.Close();
            }
        }

        public static MemoryStream ReadToMemory(this string fileName)
        {
            using (var fs = File.OpenRead(fileName))
            {
                int length = (int)fs.Length;
                byte[] data = new byte[length];
                fs.Position = 0;
                fs.Read(data, 0, length);
                MemoryStream ms = new MemoryStream(data);
                return ms;
            }
        }

        public static FileStream OpenRead(this string fileName)
        {
            return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static FileStream OpenShare(this string fileName)
        {
            return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        #region 文件md5
        public static bool IsSameMd5(this string sourceFile, string targetFile)
        {
            string arg_0E_0 = sourceFile.GetMD5HashFromFile();
            string mD5HashFromFile = targetFile.GetMD5HashFromFile();
            return arg_0E_0.Equals(mD5HashFromFile);
        }

        public static bool IsSameBytes(this string sourceFile, string targetFile)
        {
            FileStream sourceStream = sourceFile.OpenShare();
            FileStream targetStream = targetFile.OpenShare();
            bool isSame = true;
            if (sourceStream.Length != targetStream.Length)
            {
                isSame = false;
            }
            else
            {
                while (sourceStream.CanRead)
                {
                    if (!targetStream.CanRead)
                    {
                        isSame = false;
                        break;
                    }
                    var sb = sourceStream.ReadByte();
                    var tb = targetStream.ReadByte();
                    if (sb != tb)
                    {
                        isSame = false;
                        break;
                    }
                    if (sb == -1 && tb == -1)
                    {
                        break;
                    }
                }
            }
            sourceStream.Close();
            targetStream.Close();
            return isSame;
        }

        public static string GetMD5HashFromFile(this string fileName, bool appendLength = false)
        {
            string result;
            try
            {
                long length;
                byte[] array;
                GetMD5(fileName, out length, out array);
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < array.Length; i++)
                {
                    stringBuilder.Append(array[i].ToString("x2"));
                }
                if (appendLength)
                {
                    stringBuilder.AppendFormat("{0:x2}", length);
                }
                result = stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail, error:" + ex.Message);
            }
            return result;
        }

        public static byte[] GetMD5Bytes(this string fileName)
        {
            try
            {
                long length;
                byte[] array;
                GetMD5(fileName, out length, out array);
                return array;
            }
            catch (Exception ex)
            {
                throw new Exception("Get MD5 fail, error:" + ex.Message);
            }
        }

        private static void GetMD5(string fileName, out long length, out byte[] array)
        {
            FileStream fileStream = fileName.OpenShare();
            length = fileStream.Length;
            array = new MD5CryptoServiceProvider().ComputeHash(fileStream);
            fileStream.Close();
        }

        public static byte[] GetMD5Bytes(this Stream stream)
        {
            return new MD5CryptoServiceProvider().ComputeHash(stream);
        }
        #endregion

        public static string ReadFileText(this string fileName)
        {
            using (var sr = System.IO.File.OpenText(fileName))
            {
                return sr.ReadToEnd();
            }
        }

        public static byte[] ReadFileBytes(this string fileName)
        {
            return File.ReadAllBytes(fileName);
        }

        public static void WriteToFile(this byte[] content, string fileName)
        {
            string dir = fileName.Substring(0, fileName.LastIndexOf('\\'));
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            using (var sw = System.IO.File.OpenWrite(fileName))
            {
                sw.Write(content, 0, content.Length);
                sw.Flush();
                sw.Close();
            }
        }


        static object _WriteLock = new object();
        public static void WriteTo(this string content, string fileName, bool append = false)
        {
            string dir = fileName.Substring(0, fileName.LastIndexOf('\\'));
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            lock (_WriteLock)
            {
                try
                {
                    __Write(content, fileName, append);
                }
                catch
                {
                    Threads.StartNew(() =>
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            __Write(content, fileName, append);
                        }
                        catch (Exception ex)
                        {
                        }
                    });
                }
            }
        }

        static void AppendToFile(string fileName, string line)
        {
            string dir = fileName.Substring(0, fileName.LastIndexOf('\\'));
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            try
            {
                using (var sw = System.IO.File.AppendText(fileName))
                {
                    sw.Write(line);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void __Write(string content, string fileName, bool append)
        {
            using (var sw = append ? System.IO.File.AppendText(fileName) : System.IO.File.CreateText(fileName))
            {
                sw.Write(content);
                sw.Close();
            }
        }

        public static string ConcatPath(this IEnumerable<string> paths)
        {
            string split = "\\";
            StringBuilder sb = new StringBuilder();
            foreach (var p in paths)
            {
                if (string.IsNullOrEmpty(p))
                {
                    continue;
                }
                sb.Append(p.EndsWith(split) ? p : (p + split));
            }
            return sb.ToString();
        }

        public static bool IsDirectoryExists(this string path)
        {
            return System.IO.Directory.Exists(path);
        }

        public static IEnumerable<string> LoadAllFolders(this string path)
        {
            if (!IsDirectoryExists(path))
            {
                yield break;
            }
            foreach (var folder in System.IO.Directory.GetDirectories(path))
            {
                yield return folder;
            }
        }

        public static IEnumerable<string> LoadAllFiles(this string path)
        {
            if (!IsDirectoryExists(path))
            {
                yield break;
            }
            foreach (var file in System.IO.Directory.GetFiles(path))
            {
                yield return file;
            }
        }

        public static IEnumerable<string> LoadAllFolderNames(this string path)
        {
            if (!IsDirectoryExists(path))
            {
                yield break;
            }
            foreach (var folder in System.IO.Directory.GetDirectories(path))
            {
                yield return folder.GetName();
            }
        }

        public static IEnumerable<string> LoadAllFileNames(this string path)
        {
            if (!IsDirectoryExists(path))
            {
                yield break;
            }
            foreach (var file in System.IO.Directory.GetFiles(path))
            {
                yield return file.GetName();
            }
        }

        public static void FileMove(this string fileName, string newName)
        {
            if (newName.IndexOf(':') > 0)
            {
                System.IO.File.Move(fileName, newName);
            }
            else
            {
                var path = fileName.GetParentPath() + newName;
                System.IO.File.Move(fileName, path);
            }
        }

        public static void FolderMove(this string folderName, string newName)
        {
            if (newName.IndexOf(':') > 0)
            {
                System.IO.Directory.Move(folderName, newName);
            }
            else
            {
                var path = folderName.GetParentPath() + newName;
                System.IO.Directory.Move(folderName, path);
            }
        }

        public static bool DeleteFolder(this string folder, bool recursive = false)
        {
            if (IsDirectoryExists(folder))
            {
                System.IO.Directory.Delete(folder, recursive);
                return true;
            }
            return false;
        }

        public static void CopyToDir(this string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir))
                return;

            if (!Directory.Exists(toDir))
            {
                Directory.CreateDirectory(toDir);
            }

            string[] files = Directory.GetFiles(fromDir);
            foreach (string formFileName in files)
            {
                string fileName = Path.GetFileName(formFileName);
                string toFileName = Path.Combine(toDir, fileName);
                try
                {
                    File.Copy(formFileName, toFileName, false);
                }
                catch
                {
                    continue;
                }
            }
            string[] fromDirs = Directory.GetDirectories(fromDir);
            foreach (string fromDirName in fromDirs)
            {
                string dirName = Path.GetFileName(fromDirName);
                string toDirName = Path.Combine(toDir, dirName);
                CopyToDir(fromDirName, toDirName);
            }
        }

        public static void MoveToDir(this string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir))
                return;

            CopyToDir(fromDir, toDir);
            Directory.Delete(fromDir, true);
        }

        public static bool CreateFolderIfNotExists(this string folder)
        {
            if (IsDirectoryExists(folder))
            {
                return true;
            }
            System.IO.Directory.CreateDirectory(folder);
            return true;
        }

        public static bool CreateFileFolder(this string file)
        {
            var folder = file.Substring(0, file.LastIndexOf("\\"));
            return CreateFolderIfNotExists(folder);
        }

        public static string CreateNewFileNameIfExists(this string fileName)
        {
            var dir = fileName.GetParentPath();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                return fileName;
            }
            if (!File.Exists(fileName))
            {
                return fileName;
            }
            var name = fileName.GetName();
            var ext = fileName.GetExt();
            var nameWithOutExt = ext.Length > 0 ? name.Substring(0, name.Length - ext.Length - 1) : name;
            int index = 1;
            while (true)
            {
                var newName = Path.Combine(dir, string.Format("{0}({1}).{2}", nameWithOutExt, index, ext));
                if (!File.Exists(newName))
                {
                    return newName;
                }
                index++;
            }
        }

        public static bool IsFileLocked(this string filename)
        {
            bool Locked = false;
            try
            {
                FileStream fs =
                    File.Open(filename, FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);
                fs.Close();
            }
            catch (IOException ex)
            {
                Locked = true;
            }
            return Locked;
        }

    }
}
