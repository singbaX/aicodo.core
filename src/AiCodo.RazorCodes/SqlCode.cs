// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AiCodo.Codes
{
    public class SqlCode : EntityBase
    {
        public static SqlCode Current { get; } = new SqlCode();

        public event EventHandler ConfigFileChanged;

        FileSystemWatcher _ConfigWatcher = null;

        string _FileName = "";

        public SqlCode()
        {

        }

        public SqlCode(string fileName)
        {
            Load(fileName);
        }

        SqlData _Config = null;
        public SqlData Config
        {
            get
            { 
                if(_Config == null)
                {
                    return SqlData.Current;
                }
                return _Config;
            }
            private set
            {
                _Config = value;
                RaisePropertyChanged("Config");
            }
        }

        public void Load(string fileName)
        {
            try
            {
                Config = SqlData.Load(fileName);
                _FileName = fileName;
            }
            catch (Exception ex)
            {
                this.Log(ex.Message);
            }
        }

        public void Reload()
        {
            if (_FileName.IsNotNullOrEmpty())
            {
                Load(_FileName);
            }
        }

        public bool Watch()
        {
            if (_ConfigWatcher != null)
            {
                return false;
            }

            var fileName = System.IO.Path.GetFullPath(_FileName);
            var root = System.IO.Path.GetDirectoryName(fileName);
            var filename = System.IO.Path.GetFileName(fileName);

            FileSystemWatcher watcher = new FileSystemWatcher(root);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = filename;
            watcher.Changed += ConfigFile_Changed;
            watcher.EnableRaisingEvents = true;
            this.Log($"启动文件{filename}监控{root}");
            return true;
        }

        private void ConfigFile_Changed(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("文件已修改，准备加载");
            Load(_FileName);
            ConfigFileChanged?.Invoke(this, new EventArgs());
        }

        public void Merge(string fileName)
        {
            var config = SqlData.Load(fileName);

            config.Connections.ForEach(conn =>
            {
                var oldConn = Config.Connections[conn.Name];
                if (oldConn == null)
                {
                    Config.Connections.Add(conn);
                }
                else
                {
                    conn.Tables.ForEach(t =>
                    {
                        var oldTable = oldConn.Tables[t.Name];
                        if (oldTable == null)
                        {
                            oldConn.Tables.Add(t);
                        }
                        else
                        {

                        }
                    });
                }
            });

            config.Groups.ForEach(g =>
            {
                var oldGroup = Config.Groups[g.Name];
                if (oldGroup == null)
                {
                    Config.Groups.Add(g);
                }
                else
                {
                    g.Items.ForEach(t =>
                    {
                        var oldTable = oldGroup.Items[t.Name];
                        if (oldTable == null)
                        {
                            oldGroup.Items.Add(oldTable);
                        }
                        else
                        {
                            t.Items.ForEach(s =>
                            {
                                var oldSql = oldTable.Items[s.Name];
                                if (oldSql == null)
                                {
                                    oldTable.Items.Add(s);
                                }
                                else
                                {

                                }
                            });
                        }
                    });
                }
            });
            Config.Save();
        }

        public void CreateFileWithTemplate<T>(T model, string templateName, string codeName)
        {
            CodeService.CreateFileWithTemplate(model, templateName, codeName);
        }

        public void CreateCodeWithTemplate<T>(string code, string codeFile)
        {
            code.WriteTo(codeFile);
        }

        public string CreateCodeWithTemplate<T>(T model, string templateName)
        {
            return CodeService.CreateCodeWithTemplate(model, templateName);
        }

        public IEnumerable<TableSchema> GetTables()
        {
            return Config?.GetTables();
        }

        public IEnumerable<SqlTableGroup> GetSqlTables()
        {
            return Config?.GetSqlTables();
        }
        public SqlConnection GetConn(string name)
        {
            return Config?.Connections[name];
        }

        public TableSchema GetTable(string tableName)
        {
            return Config?.GetTable(tableName);
        }

        public SqlTableGroup GetSqlTable(string tableName)
        {
            return Config?.GetSqlTable(tableName);
        }

        //public string GetCodeType(string dataType)
        //{
        //    return CodeSetting.Current.GetCodeType(dataType, providerName);
        //}

        //public string GetDefaultValue(string dataType)
        //{
        //    if (_DefaultValues.TryGetValue(dataType.ToLower(), out string t))
        //    {
        //        return t;
        //    }
        //    return "string";
        //}

        public IDbProvider GetProvider(string tableName)
        {
            var table = GetTable(tableName.ToLower());
            if (table == null)
            {
                return null;
            }
            return table.GetProvider();
        }

        public string GetColumnType(string tableName, string columnName)
        {
            var defaultTypeName = "string";

            var table = GetTable(tableName.ToLower());
            if (table == null)
            {
                return defaultTypeName;
            }

            var column = table.Columns.FirstOrDefault(c => c.Name.EqualsOrdinalIgnoreCase(columnName));
            if (column == null)
            {
                //取全局参数
                var paramterType = CodeSetting.Current.GetParameterCodeType(columnName);
                if (paramterType.IsNotEmpty())
                {
                    return paramterType;
                }

                foreach (var t in GetTables())
                {
                    column = table.Columns.FirstOrDefault(c => c.Name.EqualsOrdinalIgnoreCase(columnName));
                    if (column != null)
                    {
                        break;
                    }
                }

                if (column == null)
                {
                    return defaultTypeName;
                }
            }
            var typeName = CodeSetting.Current.GetCodeType(column.DataType, table.Connection.ProviderName);
            return typeName;
        }
    }
}
