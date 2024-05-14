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
    public static class ApplicationConfig
    {
        #region 程序运行根目录
        static string _BaseDirectory = null;
        /// <summary>
        /// 如果是Web项目返回的是bin目录，如果是win项目，返回是BaseDirectory,文本最后有\\
        /// </summary>
        public static string BaseDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_BaseDirectory))
                {
                    _BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                }
                return _BaseDirectory;
            }
            set
            {
                _BaseDirectory = value;
            }
        }
        #endregion

        #region 属性 LocalDataFolder
        private static string _LocalDataFolder = string.Empty;
        public static string LocalDataFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_LocalDataFolder))
                {
                    return "data".FixedAppBasePath();
                }
                return _LocalDataFolder;
            }
            set
            {
                _LocalDataFolder = value;
                if (string.IsNullOrEmpty(_LocalDataFolder))
                {
                    return;
                }
                if (!System.IO.Directory.Exists(_LocalDataFolder))
                {
                    System.IO.Directory.CreateDirectory(_LocalDataFolder);
                }
            }
        }
        #endregion

        #region 属性 LocalConfigFolder
        private static string _LocalConfigFolder = string.Empty;
        public static string LocalConfigFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_LocalConfigFolder))
                {
                    return "configs".FixedAppBasePath();
                }
                return _LocalConfigFolder;
            }
            set
            {
                _LocalConfigFolder = value;
                if (string.IsNullOrEmpty(_LocalConfigFolder))
                {
                    return;
                }
                if (!System.IO.Directory.Exists(_LocalConfigFolder))
                {
                    System.IO.Directory.CreateDirectory(_LocalConfigFolder);
                }
            }
        }
        #endregion

        #region 属性 LocalUserFolder
        private static string _LocalUserFolder = string.Empty;
        public static string LocalUserFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_LocalUserFolder))
                {
                    return LocalDataFolder;
                }
                return _LocalUserFolder;
            }
            set
            {
                _LocalUserFolder = value;
            }
        }
        #endregion

        #region 属性 StartTime
        private static DateTime _StartTime = DateTime.Now;
        public static DateTime StartTime
        {
            get
            {
                return _StartTime;
            }
            set
            {
                _StartTime = value;
            }
        }
        #endregion

        public static string FixedAppBasePath(this string path)
        {
            if (path.IndexOf(':') > 0) //绝对路径
            {
                return path;
            }
            return Path.GetFullPath(Path.Combine(BaseDirectory, path));
        }

        public static string FixedAppDataPath(this string path)
        {
            if (path.IndexOf(':') > 0) //绝对路径
            {
                return path;
            }
            return Path.GetFullPath(Path.Combine(LocalDataFolder, path));
        }

        public static string FixedAppConfigPath(this string path)
        {
            if (path.IndexOf(':') > 0) //绝对路径
            {
                return path;
            }
            return Path.GetFullPath(Path.Combine(LocalConfigFolder, path));
        }
    }
}
