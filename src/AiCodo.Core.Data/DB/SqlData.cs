// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace AiCodo.Data
{
    [XmlRoot("SqlData")]
    public class SqlData : EntityBase
    {
        public static DynamicEntity ConnectionStrings { get; } = new DynamicEntity();

        static string _ConfigFileName = "sql.xml".FixedAppConfigPath();

        private static object _LoadLock = new object();

        private Dictionary<string, SqlTableGroup> _Tables = new Dictionary<string, SqlTableGroup>();

        private string _CurrentFileName;

        #region 属性 AutoGenerateItems
        private bool _AutoGenerateItems = false;
        [XmlAttribute("AutoGenerateItems"), DefaultValue(false)]
        public bool AutoGenerateItems
        {
            get
            {
                return _AutoGenerateItems;
            }
            set
            {
                _AutoGenerateItems = value;
                RaisePropertyChanged(() => AutoGenerateItems);
            }
        }
        #endregion

        #region 属性 GenerateMapper
        private bool _GenerateMapper = false;
        [XmlAttribute("GenerateMapper"), DefaultValue(false)]
        public bool GenerateMapper
        {
            get
            {
                return _GenerateMapper;
            }
            set
            {
                if (_GenerateMapper == value)
                {
                    return;
                }
                _GenerateMapper = value;
                RaisePropertyChanged("GenerateMapper");
            }
        }
        #endregion

        #region 属性 Imports
        private CollectionBase<ImportConfigItem> _Imports = null;
        [XmlElement("Import", typeof(ImportConfigItem))]
        public CollectionBase<ImportConfigItem> Imports
        {
            get
            {
                if (_Imports == null)
                {
                    _Imports = new CollectionBase<ImportConfigItem>();
                }
                return _Imports;
            }
            set
            {
                _Imports = value;
                RaisePropertyChanged(() => Imports);
            }
        }
        #endregion

        #region 属性 Connections
        private SqlConnectionCollection _Connections = null;
        [XmlArray("Connections")]
        [XmlArrayItem("Connection", typeof(SqlConnection))]
        public SqlConnectionCollection Connections
        {
            get
            {
                if (_Connections == null)
                {
                    _Connections = new SqlConnectionCollection();
                }
                return _Connections;
            }
            set
            {
                _Connections = value;
                RaisePropertyChanged(() => Connections);
            }
        }
        #endregion

        #region 属性 Groups
        private SqlGroupCollection _Groups = null;
        [XmlArray("Groups")]
        [XmlArrayItem("Group", typeof(SqlGroup))]
        public SqlGroupCollection Groups
        {
            get
            {
                if (_Groups == null)
                {
                    _Groups = new SqlGroupCollection();
                }
                return _Groups;
            }
            set
            {
                _Groups = value;
                RaisePropertyChanged(() => Groups);
            }
        }
        #endregion

        #region 属性 Current
        private static SqlData _Current = null;
        public static SqlData Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_LoadLock)
                    {
                        if (_Current == null)
                        {
                            ReloadCurrent();
                        }
                    }
                }
                return _Current;
            }
        }
        #endregion

        public static void ReloadCurrent()
        {
            _Current = Load(_ConfigFileName);
            ReplaceSqlDataConnectionStrings();
            _Current.ImportConfigs();
        }

        private static void ReplaceSqlDataConnectionStrings()
        {
            if (!AppSetting.ApplicationSetting.ContainsKey("SqlData"))
            {
                return;
            }
            var config = AppSetting.ApplicationSetting.GetValue("SqlData");
            if (config != null && config is DynamicEntity data)
            {
                var connectionStrings = data.GetJsonDataValue<DynamicEntity>("ConnectionStrings");
                if (connectionStrings != null)
                {
                    foreach (var item in connectionStrings)
                    {
                        ConnectionStrings[item.Key] = item.Value;
                    }
                }
            }
        }

        public static SqlData Load(string file)
        {
            var fileName = file.FixedAppDataPath();
            var sqldata = fileName.IsFileExists() ? fileName.LoadXDoc<SqlData>() : new SqlData();
            if (sqldata != null)
            {
                sqldata.Connections.ForEach(c =>
                {
                    c.ReloadTables();
                });
                if (sqldata.AutoGenerateItems)
                {
                    sqldata.GenerateItems();
                    sqldata.Save();
                }
                else
                {
                    sqldata.ResetTables();
                }
                sqldata._CurrentFileName = fileName;
            }
            return sqldata;
        }
        public string CheckUpdate()
        {
            var sbsql = new StringBuilder();
            this.Connections.ForEach(c =>
            {
                c.CheckUpdate(sbsql);
            });
            return sbsql.ToString();
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_CurrentFileName))
            {
                _CurrentFileName = _ConfigFileName;
            }
            this.SaveXDoc(_CurrentFileName);
        }

        public void ReloadTables()
        {
            this.Connections.ForEach(c =>
            {
                var group = this.Groups[c.Name];
                if (group == null)
                {
                    group = new SqlGroup { Name = c.Name };
                    this.Groups.Add(group);
                }

                c.ReloadTables();
                foreach (var table in c.Tables)
                {
                    var t = group.Items[table.Name];
                    if (t == null)
                    {
                        t = new SqlTableGroup { Name = table.Name, TableName = table.Name, ConnectionName = c.Name };
                        group.Items.Add(t);
                    }
                }
            });

        }

        public void ReloadTableItems()
        {
            Connections.ForEach(con =>
            {
                foreach(var table in con.Tables)
                {
                    var item = DataItems.Current.GetItem(table.CodeName);
                    if (item == null)
                    {
                        item = new DataItem
                        {
                            Name=table.CodeName,
                            DisplayName = table.DisplayName, 
                        };
                        table.Columns.Select(c => new DataColumn
                        {
                            Name = c.PropertyName,
                            DisplayName = c.DisplayName,
                            FieldName = c.Name
                        }).AddToCollection(item.Columns);
                        DataItems.Current.AddItem(item);
                    }
                    else
                    {
                        table.Columns.ForEach(c =>
                        {
                            var dc=item.Columns.FirstOrDefault(f=>f.FieldName == c.Name);
                            if (dc == null)
                            {
                                dc = new DataColumn
                                {
                                    Name = c.PropertyName,
                                    DisplayName = c.DisplayName,
                                    FieldName = c.Name
                                };
                                item.Columns.Add(dc);
                            }
                        });
                    }
                }
            });
        }

        #region 导入其它配置
        private void ImportConfigs()
        {
            if (Imports != null && Imports.Count > 0)
            {
                foreach (var item in Imports)
                {
                    if (string.IsNullOrEmpty(item.ConfigName))
                    {
                        continue;
                    }
                    this.Log($"正在导入Sql配置{item.ConfigName}");

                    var fileName = System.IO.Path.Combine(_CurrentFileName.GetParentPath(), item.ConfigName);
                    var config = Load(fileName);

                    var groups = string.IsNullOrEmpty(item.GroupName) ? config.Groups.ToList() :
                        item.GroupName.Split(',').Select(s => config.Groups.FirstOrDefault(g => g.Name.Equals(s, StringComparison.OrdinalIgnoreCase)))
                            .Where(g => g != null).ToList();
                    if (groups.Count > 0)
                    {
                        foreach (var g in groups)
                        {
                            var tables = string.IsNullOrEmpty(item.TableName) ?
                                g.Items.ToList() :
                                item.TableName.Split(',').Select(s => g.Items[s]).Where(f => f != null).ToList();
                            if (tables.Count == 0)
                            {
                                continue;
                            }
                            var group = Groups.FirstOrDefault(f => f.Name.Equals(g.Name, StringComparison.OrdinalIgnoreCase));
                            if (group == null)
                            {
                                group = new SqlGroup
                                {
                                    Name = g.Name
                                };
                                tables.AddToCollection(group.Items);
                                Groups.Add(group);
                            }
                            else
                            {
                                tables.ForEach(t =>
                                {
                                    var oldTable = group.Items[t.Name];
                                    if (oldTable != null)
                                    {
                                        group.Items.Remove(oldTable);
                                    }
                                    group.Items.Add(t);
                                });
                            }
                        }
                    }
                }
            }
        }
        #endregion

        private void ResetTables()
        {
            _Tables.Clear();
            Groups.ForEach(g =>
            {
                foreach (var t in g.Items)
                {
                    var key = t.TableName.ToLower();
                    if (_Tables.ContainsKey(key))
                    {
                        throw new Exception(string.Format("有重复表[{0}-{1}]", g.Name, t.TableName));
                    }
                    _Tables.Add(t.TableName.ToLower(), t);
                }
            });
        }

        public IEnumerable<TableSchema> GetTables()
        {
            foreach (var conn in Connections)
            {
                foreach (var table in conn.Tables)
                {
                    yield return table;
                }
            }
        }

        public TableSchema GetTable(string tableName)
        {
            foreach (var conn in Connections)
            {
                var table = conn.GetTable(tableName);
                if (table != null)
                {
                    return table;
                }
            }
            return null;
        }

        public IEnumerable<SqlTableGroup> GetSqlTables()
        {
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    yield return item;
                }
            }
        }

        public SqlTableGroup GetSqlTable(string tableName, string connName = "")
        {
            if (connName.IsNotEmpty())
            {
                var group = Groups[connName];
                if (group == null)
                {
                    return null;
                }
                var table = group.Items[tableName];
                if (table != null)
                {
                    return table;
                }
            }

            foreach (var group in Groups)
            {
                var table = group.Items[tableName];
                if (table != null)
                {
                    return table;
                }
            }
            return null;
        }

        public SqlItem GetSqlItem(string fullName)
        {
            var names = fullName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToArray();
            if (names.Length == 3)
            {
                return GetSqlItem(names[0], names[1], names[2]);
            }
            if (names.Length == 2)
            {
                return GetSqlItem("", names[0], names[1]);
            }
            return null;
        }

        public SqlItem GetSqlItem(string groupName, string tableName, string commandName)
        {
            var group = groupName.IsNullOrEmpty() ? Groups.FirstOrDefault() : Groups[groupName];
            if (group == null)
            {
                this.Log($"SqlGroup:{groupName}没有配置", Category.Exception);
                return null;
            }

            var table = group.Items[tableName];
            if (table == null)
            {
                this.Log($"SqlTable:{groupName}-{tableName}没有配置", Category.Exception);
                return null;
            }
            var item = table.Items[commandName];
            if (item == null)
            {
                this.Log($"Sql:{groupName}-{tableName}-{commandName}没有配置", Category.Exception);
            }
            return item;
        }

        public void GenerateItems()
        {
            if (!AutoGenerateItems)
            {
                return;
            }

            foreach (var c in Connections)
            {
                var provider = c.GetProvider();
                if (provider == null)
                {
                    $"Provider [{c.Name}-{c.ProviderName}] Not Found".WriteErrorLog();
                    continue;
                }

                var tables = c.Tables;
                foreach (var t in tables)
                {
                    //先判断表是否存在
                    var table = _Tables.GetDictionaryValue(t.Name.ToLower(), null);
                    //如果不存在就创建
                    if (table == null)
                    {
                        //先判断有没有分组
                        var groupName = c.Name;
                        var group = Groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

                        //如果没有创建分组
                        if (group == null)
                        {
                            group = new SqlGroup { Name = groupName.Trim() };
                            Groups.Add(group);
                        }

                        table = group.Items[t.Name];
                        if (table == null)
                        {
                            //创建SQL分组（按照表分组）
                            table = new SqlTableGroup
                            {
                                Name = t.Name,
                                TableName = t.Name,
                                ConnectionName = c.Name
                            };
                            group.Items.Add(table);
                        }
                        _Tables.Add(t.Name.ToLower(), table);
                    }

                    //生成常用的SQL
                    if (GenerateMapper && provider is ICreateMapper p)
                    {
                        p.CreateMapperItems(table, t);
                        continue;
                    }

                    if (t.HasAutoIncrementColumn())
                    {
                        CheckSqlItem(table, "Insert", SqlType.Scalar, "新增", () => provider.CreateInsert(t, true, true));
                    }
                    else
                    {
                        CheckSqlItem(table, "Insert", SqlType.Execute, "新增", () => provider.CreateInsert(t));
                    }

                    CheckSqlItem(table, "Delete", SqlType.Execute, "删除", () => provider.CreateDelete(t));
                    CheckSqlItem(table, "Update", SqlType.Execute, "更新", () => provider.CreateUpdate(t));
                    CheckSqlItem(table, "SelectAll", SqlType.Query, "全选", () => provider.CreateSelect(t));
                    CheckSqlItem(table, "SelectByKeys", SqlType.QueryOne, "主键选择", () => provider.CreateSelectByKeys(t));

                    if (t.Columns.FirstOrDefault(f => f.Name.Equals("IsValid")) != null)
                    {
                        CheckSqlItem(table, "SetValid", SqlType.Execute, "有效", () => CreateValid(provider, t));
                    }
                    CheckSqlItem(table, "Count", SqlType.Scalar, "记录数", () => provider.CreateCount(t));
                }
            }
        }

        private string CreateValid(IDbProvider provider, TableSchema t)
        {
            return provider.CreateUpdate(t, new string[] { "IsValid" });
        }

        //private static string CreateInvalid(TableSchema table)

        private static void CheckSqlItem(SqlTableGroup table, string sqlname, SqlType sqlType, string description, Func<string> create)
        {
            var sql = table.Items.FirstOrDefault(s => s.Name.EqualsOrdinalIgnoreCase(sqlname));
            if (sql == null)
            {
                sql = new SqlItem
                {
                    Name = sqlname,
                    Description = description,
                    SqlType = sqlType,
                    IsGenerate = true,
                    ConnectionName = table.ConnectionName,
                    CommandText = "\r\n" + create() + "\r\n"
                };
                table.Items.Add(sql);
            }
            else if (sql.IsGenerate)
            {
                sql.SqlType = sqlType;
                sql.ConnectionName = table.ConnectionName;
                sql.CommandText = "\r\n" + create() + "\r\n";
            }
        }

        public System.Data.Common.DbConnection OpenConnection(string connName)
        {
            var connItem = connName.IsNullOrEmpty() ?
                Connections.FirstOrDefault() :
                Connections[connName];

            if (connItem == null)
            {
                throw new Exception($"Connection[{connName}] not found.");
            }
            var p = Data.DbProviderFactories.GetFactory(connItem.ProviderName);
            if (p == null)
            {
                throw new Exception($"Provider not found ({connItem.ProviderName})");
            }
            var conn = p.CreateConnection();
            conn.ConnectionString = connItem.GetConnectionString();
            conn.Open();
            return conn;
        }
    }

    public class ImportConfigItem : EntityBase
    {
        #region 属性 ConfigName
        private string _ConfigName = string.Empty;
        [XmlAttribute("ConfigName")]
        public string ConfigName
        {
            get
            {
                return _ConfigName;
            }
            set
            {
                _ConfigName = value;
                RaisePropertyChanged(() => ConfigName);
            }
        }
        #endregion

        #region 属性 GroupName
        private string _GroupName = string.Empty;
        [XmlAttribute("GroupName"), DefaultValue("")]
        public string GroupName
        {
            get
            {
                return _GroupName;
            }
            set
            {
                _GroupName = value;
                RaisePropertyChanged(() => GroupName);
            }
        }
        #endregion

        #region 属性 TableName
        private string _TableName = string.Empty;
        [XmlAttribute("TableName"), DefaultValue("")]
        public string TableName
        {
            get
            {
                return _TableName;
            }
            set
            {
                _TableName = value;
                RaisePropertyChanged(() => TableName);
            }
        }
        #endregion
    }

    public partial class SqlConnection : EntityBase
    {
        public SqlConnection()
        {
        }

        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        #endregion

        #region 属性 ProviderName
        private string _ProviderName = string.Empty;
        [XmlAttribute("ProviderName"), DefaultValue("")]
        public string ProviderName
        {
            get
            {
                return _ProviderName;
            }
            set
            {
                _ProviderName = value;
                RaisePropertyChanged(() => ProviderName);
            }
        }
        #endregion

        #region 属性 ConnectionString
        private string _ConnectionString = string.Empty;
        [XmlAttribute("ConnectionString"), DefaultValue("")]
        public string ConnectionString
        {
            get
            {
                return _ConnectionString;
            }
            set
            {
                _ConnectionString = value;
                RaisePropertyChanged(() => ConnectionString);
            }
        }
        #endregion

        #region 属性 TimeOut
        private int _TimeOut = 30;
        [XmlAttribute("TimeOut"), DefaultValue(30)]
        public int TimeOut
        {
            get
            {
                return _TimeOut;
            }
            set
            {
                _TimeOut = value;
                RaisePropertyChanged(() => TimeOut);
            }
        }
        #endregion

        #region 属性 ConnectionCount
        private int _ConnectionCount = 8;
        [XmlAttribute("ConnectionCount"), DefaultValue(8)]
        public int ConnectionCount
        {
            get
            {
                return _ConnectionCount;
            }
            set
            {
                _ConnectionCount = value;
                RaisePropertyChanged(() => ConnectionCount);
            }
        }
        #endregion

        #region 属性 Tables
        private TableSchemaCollection _Tables = null;
        [XmlElement("Table")]
        public TableSchemaCollection Tables
        {
            get
            {
                if (_Tables == null)
                {
                    _Tables = new TableSchemaCollection();
                    _Tables.CollectionChanged += Tables_CollectionChanged;
                }
                return _Tables;
            }
            set
            {
                if (_Tables != null)
                {
                    _Tables.CollectionChanged -= Tables_CollectionChanged;
                    OnTablesRemoved(_Tables);
                }
                _Tables = value;
                RaisePropertyChanged(() => Tables);
                if (_Tables != null)
                {
                    _Tables.CollectionChanged += Tables_CollectionChanged;
                    OnTablesAdded(_Tables);
                }
            }
        }

        private void Tables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnTablesAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnTablesRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnTablesAdded(IList newItems)
        {
            foreach (TableSchema item in newItems)
            {
                item.Connection = this;
            }
        }

        protected virtual void OnTablesRemoved(IList oldItems)
        {
            foreach (TableSchema item in oldItems)
            {
                item.Connection = null;
            }
        }
        #endregion 

        #region GetProvider
        public IDbProvider GetProvider()
        {
            return DbProviderFactories.GetProvider(ProviderName);
        }
        #endregion

        public string GetConnectionString()
        {
            if (SqlData.ConnectionStrings.TryGetValue(Name, out var connString))
            {
                return connString.ToString();
            }
            return ConnectionString;
        }

        public TableSchema GetTable(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            var table = Tables[name];
            if (table == null)
            {
                ReloadTables();
            }

            return Tables[name];
        }

        public void ReloadTables()
        {
            try
            {
                lock (this)
                {
                    if (Tables.Count == 0)
                    {
                        this.LoadTables().OrderBy(t => t.Name).AddToCollection(Tables);
                    }
                    else
                    {
                        var oldTables = Tables.ToList();
                        var tables = this.LoadTables().OrderBy(t => t.Name).ToList();
                        Tables.Clear();
                        tables.ForEach(t =>
                        {
                            var oldTable = oldTables.FirstOrDefault(f => f.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));
                            if (oldTable != null)
                            {
                                if (!(string.IsNullOrEmpty(oldTable.CodeName) || oldTable.CodeName.Equals(t.CodeName)))
                                {
                                    t.CodeName = oldTable.CodeName;
                                }

                                if (!(string.IsNullOrEmpty(oldTable.DisplayName) || oldTable.DisplayName.Equals(t.DisplayName)))
                                {
                                    t.DisplayName = oldTable.DisplayName;
                                }
                                t.Columns.ForEach(c =>
                                {
                                    var col = oldTable.Columns[c.Name];
                                    if (col != null)
                                    {
                                        foreach (var name in col.GetFieldNames().Where(d => !Column.IsDBFields(d)))
                                        {
                                            c.SetValue(name, col.GetValue(name));
                                        }
                                    }
                                });
                            }
                            Tables.Add(t);
                        });

                        oldTables.Where(t => tables.FirstOrDefault(f => f.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase)) == null)
                            .ForEach(t =>
                            {
                                t.IsValid = false;
                                Tables.Add(t);
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void CheckUpdate(StringBuilder sbsql)
        {
            try
            {
                lock (this)
                {
                    if (Tables.Count == 0)
                    {
                        this.LoadTables()
                            .ForEach(t =>
                            {
                                sbsql.AppendLine($"----Table {t.Name} 配置未加载");
                            });
                    }
                    else
                    {
                        var provider = GetProvider();
                        var oldTables = Tables.ToList();
                        var tables = this.LoadTables().OrderBy(t => t.Name).ToList();

                        foreach (var t in tables.Where(t => oldTables.FirstOrDefault(f => f.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase)) == null).ToList())
                        {
                            sbsql.AppendLine($"----Table {t.Name} 配置未加载");
                            tables.Remove(t);
                        }

                        foreach (var t in oldTables)
                        {
                            var table = tables.FirstOrDefault(f => f.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));
                            if (table == null)
                            {
                                //新增表
                                sbsql.AppendLine($"---- Create Table {t.Name}");
                                sbsql.AppendLine(provider.CreateTable(t));
                                sbsql.AppendLine($"--- End Create Table {t.Name}");
                                continue;
                            }

                            var name = "";
                            if (provider is IAlterTable alterTable)
                            {
                                foreach (var c in t.Columns)
                                {
                                    var column = table.Columns.FirstOrDefault(f => f.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                                    if (column == null)
                                    {
                                        sbsql.AppendLine(alterTable.CreateAddColumn(t.Name, c, name));
                                    }
                                    else
                                    {
                                        //比较不同
                                        if (c.DataType != column.DataType || c.Length != column.Length
                                            || c.NullAble != column.NullAble || c.IsAutoIncrement != column.IsAutoIncrement)
                                        {
                                            sbsql.AppendLine(alterTable.CreateChangeColumn(t.Name, c, name));
                                        }
                                    }
                                    name = c.Name;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class SqlConnectionCollection : CollectionBase<SqlConnection>
    {
        public SqlConnection this[string name]
        {
            get
            {
                return Items.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    public partial class SqlTableGroup : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        #endregion

        #region 属性 ConnectionName
        private string _ConnectionName = string.Empty;
        [XmlAttribute("ConnectionName"), DefaultValue("")]
        public string ConnectionName
        {
            get
            {
                return _ConnectionName;
            }
            set
            {
                _ConnectionName = value;
                RaisePropertyChanged(() => ConnectionName);
            }
        }
        #endregion

        #region 属性 TableName
        private string _TableName = string.Empty;
        [XmlAttribute("TableName"), DefaultValue("")]
        public string TableName
        {
            get
            {
                return _TableName;
            }
            set
            {
                _TableName = value;
                RaisePropertyChanged(() => TableName);
                RaisePropertyChanged("Table");
            }
        }
        #endregion

        #region 属性 EntityName
        private string _EntityName = string.Empty;
        [XmlAttribute("EntityName"), DefaultValue("")]
        public string EntityName
        {
            get
            {
                return _EntityName;
            }
            set
            {
                _EntityName = value;
                RaisePropertyChanged(() => EntityName);
            }
        }
        #endregion

        #region 属性 Mappers
        private CollectionBase<SqlMapper> _Mappers = null;
        [XmlElement("Map", typeof(SqlMapper))]
        public CollectionBase<SqlMapper> Mappers
        {
            get
            {
                if (_Mappers == null)
                {
                    _Mappers = new CollectionBase<SqlMapper>();
                }
                return _Mappers;
            }
            set
            {
                _Mappers = value;
                RaisePropertyChanged("Mappers");
            }
        }
        #endregion

        #region 属性 Items
        private SqlItemCollection _Items = null;
        [XmlElement("Sql", typeof(SqlItem))]
        public SqlItemCollection Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new SqlItemCollection();
                    _Items.CollectionChanged += _Items_CollectionChanged;
                }
                return _Items;
            }
            set
            {
                if (_Items != null)
                {
                    _Items.CollectionChanged -= _Items_CollectionChanged;
                }
                _Items = value;
                if (_Items != null)
                {
                    _Items.CollectionChanged += _Items_CollectionChanged;
                    foreach (var item in _Items)
                    {
                        item.Group = this;
                    }
                }
                RaisePropertyChanged(() => Items);
            }
        }

        void _Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (SqlItem item in e.NewItems)
                    {
                        item.Group = this;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (SqlItem item in e.OldItems)
                    {
                        item.Group = null;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region 属性 Table
        [XmlIgnore()]
        public TableSchema Table
        {
            get
            {
                var conn = SqlData.Current.Connections[ConnectionName];
                if (conn == null)
                {
                    return null;
                }
                return conn.GetTable(TableName);
            }
        }
        #endregion

        #region 属性 Group
        private SqlGroup _Group = null;
        [XmlIgnore()]
        public SqlGroup Group
        {
            get
            {
                return _Group;
            }
            set
            {
                _Group = value;
                RaisePropertyChanged(() => Group);
            }
        }
        #endregion
    }

    public class SqlGroup : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        #endregion

        #region 属性 Items
        private SqlTableGroupCollection _Items = null;
        [XmlElement("Table", typeof(SqlTableGroup))]
        public SqlTableGroupCollection Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new SqlTableGroupCollection();
                    _Items.CollectionChanged += _Items_CollectionChanged;
                }
                return _Items;
            }
            set
            {
                if (_Items != null)
                {
                    _Items.CollectionChanged -= _Items_CollectionChanged;
                }
                _Items = value;
                if (_Items != null)
                {
                    _Items.CollectionChanged += _Items_CollectionChanged;
                    foreach (var item in _Items)
                    {
                        item.Group = this;
                    }
                }
                RaisePropertyChanged(() => Items);
            }
        }

        void _Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (SqlTableGroup item in e.NewItems)
                    {
                        item.Group = this;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (SqlTableGroup item in e.OldItems)
                    {
                        item.Group = null;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
        }
        #endregion
    }

    public class SqlTableGroupCollection : CollectionBase<SqlTableGroup>
    {
        public SqlTableGroup this[string name]
        {
            get
            {
                return Items.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    public class SqlGroupCollection : CollectionBase<SqlGroup>
    {
        public SqlGroup this[string name]
        {
            get
            {
                return Items.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    public partial class SqlItem : SqlItemBase
    {
        #region 属性 CommandText
        private string _CommandText = string.Empty;
        [XmlElement("CommandText")]
        public string CommandText
        {
            get
            {
                return _CommandText;
            }
            set
            {
                _CommandText = value;
                RaisePropertyChanged(() => CommandText);
            }
        }
        #endregion 

        #region 属性 Mapper
        private SqlMapper _Mapper = null;
        [XmlElement("Mapper"), DefaultValue(null)]
        public SqlMapper Mapper
        {
            get
            {
                return _Mapper;
            }
            set
            {
                if (_Mapper != null)
                {
                    _Mapper.SqlItem = null;
                }
                _Mapper = value;
                RaisePropertyChanged("Mapper");
                if (_Mapper != null)
                {
                    _Mapper.SqlItem = this;
                }
            }
        }
        #endregion

        public override string GetCommandText(Dictionary<string, object> args)
        {
            if (Mapper != null)
            {
                return Mapper.GetCommandText(args);
            }
            return CommandText;
        }

        public IEnumerable<string> GetParameters(string prefix = "@")
        {
            if (Mapper != null)
            {
                return Mapper.GetParameters(prefix).Distinct();
            }
            return CommandText.GetParameters(prefix).Distinct();
        }
    }

    #region Mapper 

    public partial class SqlMapper : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (_Name == value)
                {
                    return;
                }
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        #endregion

        #region 属性 Items
        private CollectionBase<SqlMapperItemBase> _Items = null;
        [XmlElement("Ref", typeof(SqlRefItem))]
        [XmlElement("Text", typeof(SqlTextItem))]
        public CollectionBase<SqlMapperItemBase> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new CollectionBase<SqlMapperItemBase>();
                    _Items.CollectionChanged += Items_CollectionChanged;
                }
                return _Items;
            }
            set
            {
                if (_Items != null)
                {
                    _Items.CollectionChanged -= Items_CollectionChanged;
                    OnItemsRemoved(_Items);
                }
                _Items = value;
                RaisePropertyChanged("Items");
                if (_Items != null)
                {
                    _Items.CollectionChanged += Items_CollectionChanged;
                    OnItemsAdded(_Items);
                }
            }
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnItemsAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnItemsRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnItemsAdded(IList newItems)
        {
            foreach (SqlMapperItemBase item in newItems)
            {
                item.Parent = this;
            }
        }

        protected virtual void OnItemsRemoved(IList oldItems)
        {
            foreach (SqlMapperItemBase item in oldItems)
            {
                item.Parent = null;
            }
        }
        #endregion

        #region 属性 SqlItem
        private SqlItem _SqlItem = null;
        [XmlIgnore, JsonIgnore]
        public SqlItem SqlItem
        {
            get
            {
                return _SqlItem;
            }
            set
            {
                _SqlItem = value;
                RaisePropertyChanged("SqlItem");
            }
        }
        #endregion

        public SqlMapper AddText(string text, string condition = "")
        {
            Items.Add(new SqlTextItem { Text = text, Condition = condition });
            return this;
        }
        public SqlMapper AddRef(string name, string condition = "")
        {
            Items.Add(new SqlRefItem { RefName = name, Condition = condition });
            return this;
        }

        public string GetCommandText(Dictionary<string, object> args)
        {
            var sb = new StringBuilder();
            foreach (var item in Items)
            {
                sb.Append(item.GetCommandText(args));
            }
            return sb.ToString();
        }

        public IEnumerable<string> GetParameters(string prefix = "@")
        {
            foreach(var item in Items)
            {
                foreach (var name in item.GetParameters(prefix))
                {
                    yield return name;
                }
            }
        }
    }

    public partial class SqlRefItem : SqlMapperItemBase
    {
        #region 属性 RefName
        private string _RefName = string.Empty;
        [XmlAttribute("RefName"), DefaultValue("")]
        public string RefName
        {
            get
            {
                return _RefName;
            }
            set
            {
                if (_RefName == value)
                {
                    return;
                }
                _RefName = value;
                RaisePropertyChanged("RefName");
            }
        }
        #endregion

        #region 属性 Condition
        private string _Condition = string.Empty;
        [XmlAttribute("Condition"), DefaultValue("")]
        public string Condition
        {
            get
            {
                return _Condition;
            }
            set
            {
                if (_Condition == value)
                {
                    return;
                }
                _Condition = value;
                RaisePropertyChanged("Condition");
            }
        }
        #endregion

        public override string GetCommandText(Dictionary<string, object> args)
        {
            if (RefName.IsNullOrEmpty() || Parent == null ||
                Parent.SqlItem == null || Parent.SqlItem.Group == null)
            {
                return "";
            }

            if (Condition.IsNotEmpty() && Condition.Eval(args).ToBoolean() == false)
            {
                return "";
            }
            var group = Parent.SqlItem.Group;
            var item = group.Mappers.FirstOrDefault(f => f.Name.Equals(RefName));
            if (item == null)
            {
                return "";
            }
            return item.GetCommandText(args);
        }

        public override IEnumerable<string> GetParameters(string prefix = "@")
        {
            if (Condition.IsNotNullOrEmpty())
            {
                var names = Condition.GetParameters(prefix);
                foreach (var name in names)
                {
                    yield return name;
                }
            }
            if (RefName.IsNullOrEmpty() || Parent == null ||
                Parent.SqlItem == null || Parent.SqlItem.Group == null)
            {
                yield break;
            }

            var group = Parent.SqlItem.Group;
            var item = group.Mappers.FirstOrDefault(f => f.Name.Equals(RefName));
            if (item != null)
            {
                foreach (var name in item.GetParameters(prefix))
                {
                    yield return name;
                }
            }
        }
    }

    public partial class SqlTextItem : SqlMapperItemBase
    {
        #region 属性 Condition
        private string _Condition = string.Empty;
        [XmlAttribute("Condition"), DefaultValue("")]
        public string Condition
        {
            get
            {
                return _Condition;
            }
            set
            {
                if (_Condition == value)
                {
                    return;
                }
                _Condition = value;
                RaisePropertyChanged("Condition");
            }
        }
        #endregion

        #region 属性 Text
        private string _Text = string.Empty;
        [XmlText, DefaultValue("")]
        public string Text
        {
            get
            {
                return _Text;
            }
            set
            {
                if (_Text == value)
                {
                    return;
                }
                _Text = value;
                RaisePropertyChanged("Text");
            }
        }
        #endregion

        public override string GetCommandText(Dictionary<string, object> args)
        {
            if (Condition.IsNullOrEmpty() || Text.IsNullOrEmpty())
            {
                return "";
            }
            var ok = Condition.Eval(args).ToBoolean();
            return ok ? Text : "";
        }

        public override IEnumerable<string> GetParameters(string prefix = "@")
        {
            if (Condition.IsNotNullOrEmpty())
            {
                var names=Condition.GetParameters(prefix);
                foreach(var name in names)
                {
                    yield return name;
                }
            }
            if (Text.IsNotNullOrEmpty())
            {
                var names= Text.GetParameters(prefix);
                foreach(var name in names)
                {
                    yield return name;
                }
            }
        }
    }

    public class SqlMapperItemBase : EntityBase
    {
        #region 属性 Parent
        private SqlMapper _Parent = null;
        [XmlIgnore, JsonIgnore]
        public SqlMapper Parent
        {
            get
            {
                return _Parent;
            }
            set
            {
                _Parent = value;
                RaisePropertyChanged("Parent");
            }
        }
        #endregion

        public virtual string GetCommandText(Dictionary<string, object> args)
        {
            return "";
        }
        public virtual IEnumerable<string> GetParameters(string prefix = "@")
        {
            yield break;
        }
    }
    #endregion

    public partial class SqlItemBase : TagItemBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        #endregion

        #region 属性 DisplayName
        private string _DisplayName = string.Empty;
        [XmlAttribute("DisplayName"), DefaultValue("")]
        public string DisplayName
        {
            get
            {
                return _DisplayName;
            }
            set
            {
                if (_DisplayName == value)
                {
                    return;
                }
                _DisplayName = value;
                RaisePropertyChanged("DisplayName");
            }
        }
        #endregion

        #region 属性 DataName
        private string _DataName = string.Empty;
        [XmlAttribute("DataName"), DefaultValue("")]
        public string DataName
        {
            get
            {
                return _DataName;
            }
            set
            {
                if (_DataName == value)
                {
                    return;
                }
                _DataName = value;
                RaisePropertyChanged("DataName");
            }
        }
        #endregion

        #region 属性 IsShared
        [XmlIgnore()]
        public bool IsShared
        {
            get
            {
                return !string.IsNullOrEmpty(SharedName);
            }
        }
        #endregion

        #region 属性 SharedName
        private string _SharedName = "";
        [XmlAttribute("SharedName"), DefaultValue("")]
        public string SharedName
        {
            get
            {
                return _SharedName;
            }
            set
            {
                _SharedName = value;
                RaisePropertyChanged(() => SharedName);
                RaisePropertyChanged(() => IsShared);
            }
        }
        #endregion

        #region 属性 AuthPage
        private string _AuthPage = "";
        [XmlAttribute("AuthPage"), DefaultValue("")]
        public string AuthPage
        {
            get
            {
                return _AuthPage;
            }
            set
            {
                _AuthPage = value;
                RaisePropertyChanged(() => AuthPage);
            }
        }
        #endregion

        #region 属性 AuthValue
        private int _AuthValue = 0;
        [XmlAttribute("AuthValue"), DefaultValue(0)]
        public int AuthValue
        {
            get
            {
                return _AuthValue;
            }
            set
            {
                _AuthValue = value;
                RaisePropertyChanged(() => AuthValue);
            }
        }
        #endregion

        #region 属性 ConnectionName
        private string _ConnectionName = string.Empty;
        [XmlAttribute("ConnectionName"), DefaultValue("")]
        public string ConnectionName
        {
            get
            {
                return _ConnectionName;
            }
            set
            {
                _ConnectionName = value;
                RaisePropertyChanged(() => ConnectionName);
            }
        }
        #endregion

        #region 属性 SqlType
        private SqlType _SqlType = SqlType.Query;
        [XmlAttribute("SqlType"), DefaultValue(typeof(SqlType), "Query")]
        public SqlType SqlType
        {
            get
            {
                return _SqlType;
            }
            set
            {
                _SqlType = value;
                RaisePropertyChanged(() => SqlType);
            }
        }
        #endregion

        #region 属性 EntityType
        private string _EntityType = string.Empty;
        [XmlAttribute("EntityType"), DefaultValue("")]
        public string EntityType
        {
            get
            {
                return _EntityType;
            }
            set
            {
                _EntityType = value;
                RaisePropertyChanged(() => EntityType);
            }
        }
        #endregion

        #region 属性 Description
        private string _Description = "";
        [XmlAttribute("Description"), DefaultValue("")]
        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(_Description))
                {
                    return Name;
                }
                return _Description;
            }
            set
            {
                _Description = value;
                RaisePropertyChanged(() => Description);
            }
        }
        #endregion

        #region 属性 IsDynamic
        private bool _IsDynamic = false;
        [XmlAttribute("IsDynamic"), DefaultValue(false)]
        public bool IsDynamic
        {
            get
            {
                return _IsDynamic;
            }
            set
            {
                _IsDynamic = value;
                RaisePropertyChanged(() => IsDynamic);
            }
        }
        #endregion

        #region 属性 CanUsePage
        private bool _CanUsePage = false;
        [XmlAttribute("CanUsePage"), DefaultValue(false)]
        public bool CanUsePage
        {
            get
            {
                return _CanUsePage;
            }
            set
            {
                _CanUsePage = value;
                RaisePropertyChanged(() => CanUsePage);
            }
        }
        #endregion

        #region 属性 PageIndexName
        private string _PageIndexName = "PageIndex";
        [XmlAttribute("PageIndexName"), DefaultValue("PageIndex")]
        public string PageIndexName
        {
            get
            {
                return _PageIndexName;
            }
            set
            {
                _PageIndexName = value;
                RaisePropertyChanged(() => PageIndexName);
            }
        }
        #endregion

        #region 属性 PageSizeName
        private string _PageSizeName = "PageSize";
        [XmlAttribute("PageSizeName"), DefaultValue("PageSize")]
        public string PageSizeName
        {
            get
            {
                return _PageSizeName;
            }
            set
            {
                _PageSizeName = value;
                RaisePropertyChanged(() => PageSizeName);
            }
        }
        #endregion

        #region 属性 IsGenerate
        private bool _IsGenerate = false;
        [XmlAttribute("IsGenerate"), DefaultValue(false)]
        public bool IsGenerate
        {
            get
            {
                return _IsGenerate;
            }
            set
            {
                _IsGenerate = value;
                RaisePropertyChanged(() => IsGenerate);
            }
        }
        #endregion

        #region 属性 IsQueryOnly
        private bool _IsQueryOnly = true;
        [XmlAttribute("IsQueryOnly"), DefaultValue(true)]
        public bool IsQueryOnly
        {
            get
            {
                return _IsQueryOnly;
            }
            set
            {
                if (_IsQueryOnly == value)
                {
                    return;
                }
                _IsQueryOnly = value;
                RaisePropertyChanged("IsQueryOnly");
            }
        }
        #endregion

        #region 属性 FunctionType
        private FunctionType _FunctionType = FunctionType.All;
        [XmlAttribute("FunctionType"), DefaultValue(typeof(FunctionType), "All")]
        public FunctionType FunctionType
        {
            get
            {
                return _FunctionType;
            }
            set
            {
                _FunctionType = value;
                RaisePropertyChanged(() => FunctionType);
            }
        }
        #endregion

        #region 属性 AccessLimit
        private AccessLimit _AccessLimit = AccessLimit.None;
        [XmlAttribute("AccessLimit"), DefaultValue(typeof(AccessLimit), "None")]
        public AccessLimit AccessLimit
        {
            get
            {
                return _AccessLimit;
            }
            set
            {
                _AccessLimit = value;
                RaisePropertyChanged("AccessLimit");
            }
        }
        #endregion

        #region 属性 Parameters
        private CollectionBase<SqlParameter> _Parameters = null;
        [XmlArray("Parameters"), XmlArrayItem("Parameter", typeof(SqlParameter))]
        public CollectionBase<SqlParameter> Parameters
        {
            get
            {
                if (_Parameters == null)
                {
                    _Parameters = new CollectionBase<SqlParameter>();
                }
                return _Parameters;
            }
            set
            {
                _Parameters = value;
                RaisePropertyChanged("Parameters");
            }
        }
        #endregion

        #region 属性 ResultConverters
        private CollectionBase<QueryResultConverter> _ResultConverters = null;
        [XmlArray("ResultConverters"), DefaultValue(null)]
        [XmlArrayItem("Item", typeof(QueryResultConverter))]
        public CollectionBase<QueryResultConverter> ResultConverters
        {
            get
            {
                return _ResultConverters;
            }
            set
            {
                _ResultConverters = value;
                RaisePropertyChanged(() => ResultConverters);
            }
        }
        #endregion

        #region 属性 Group
        private SqlTableGroup _Group = null;
        [XmlIgnore()]
        public SqlTableGroup Group
        {
            get
            {
                return _Group;
            }
            internal set
            {
                _Group = value;
                RaisePropertyChanged(() => Group);
            }
        }
        #endregion        

        public virtual string GetCommandText(Dictionary<string, object> args)
        {
            return "";
        }
    }

    [Flags]
    public enum AccessLimit
    {
        None,
        LocalOnly = 1,
    }

    public class SqlParameter : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (_Name == value)
                {
                    return;
                }
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        #endregion

        #region 属性 ColumnName
        private string _ColumnName = string.Empty;
        [XmlAttribute("ColumnName"), DefaultValue("")]
        public string ColumnName
        {
            get
            {
                return _ColumnName;
            }
            set
            {
                if (_ColumnName == value)
                {
                    return;
                }
                _ColumnName = value;
                RaisePropertyChanged("ColumnName");
            }
        }
        #endregion

        #region 属性 DataType
        private string _DataType = string.Empty;
        [XmlAttribute("DataType"), DefaultValue("")]
        public string DataType
        {
            get
            {
                return _DataType;
            }
            set
            {
                if (_DataType == value)
                {
                    return;
                }
                _DataType = value;
                RaisePropertyChanged("DataType");
            }
        }
        #endregion

        #region 属性 Description
        private string _Description = string.Empty;
        [XmlAttribute("Description"), DefaultValue("")]
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                if (_Description == value)
                {
                    return;
                }
                _Description = value;
                RaisePropertyChanged("Description");
            }
        }
        #endregion
    }

    public partial class QueryResultConverter : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (_Name == value)
                {
                    return;
                }
                _Name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        #endregion

        #region 属性 Args
        private string _Args = string.Empty;
        [XmlAttribute("Args"), DefaultValue("")]
        public string Args
        {
            get
            {
                return _Args;
            }
            set
            {
                _Args = value;
                RaisePropertyChanged(() => Args);
            }
        }
        #endregion

        #region 属性 Converter
        private string _Converter = string.Empty;
        [XmlElement("Converter"), DefaultValue("")]
        public string Converter
        {
            get
            {
                return _Converter;
            }
            set
            {
                if (_Converter == value)
                {
                    return;
                }
                _Converter = value;
                RaisePropertyChanged(() => Converter);
            }
        }
        #endregion
    }

    public class SqlItemCollection : CollectionBase<SqlItem>
    {
        public SqlItem this[string name]
        {
            get
            {
                return Items.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }
    }


    [Flags]
    public enum FunctionType
    {
        None = 0,
        Params = 1,
        ParamsWithTrans = 2,
        Item = 4,
        ItemWithTrans = 8,
        JsonResult = 16,
        ParamsOnly = 3,
        ItemOnly = 12,
        All = 15,
        JsonQuery = 17
    }

    public enum SqlType
    {
        Query,
        QueryOne,
        Execute,
        Scalar
    }
}
