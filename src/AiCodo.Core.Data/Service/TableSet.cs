// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiCodo.Data
{
    public enum UpdateMode
    {
        Update,
        CreateNew
    }

    public class TableSet
    {
        static Dictionary<string, TableSchema> _TableSchemas = new Dictionary<string, TableSchema>();

        Dictionary<string, string> _SqlItems = new Dictionary<string, string>();

        public string TableName { get; private set; }
        public string ConnName { get; private set; }

        public string LogicKeys { get; set; } = "";

        public string LogicRemoveColumnName { get; set; } = "";

        public UpdateMode UpdateMode { get; set; } = UpdateMode.Update;

        public bool EnableLogicRemove { get; set; } = true;

        private SqlConnection _ConnItem = null;
        private TableSchema _Schema = null;
        private IDbProvider _Provider = null;

        private bool _HasAutoKey = false;
        private string _AutoKeyName = "";

        private bool _UseConfigSchema = false;

        public TableSet(string connName, string tableName)
        {
            TableName = tableName;
            ConnName = connName;
            InitItems();
        }

        #region 初始化对象
        private void InitItems()
        {
            var conn = SqlData.Current.Connections[ConnName];
            if (conn == null)
            {
                throw new Exception($"连接[{ConnName}]不存在");
            }
            _ConnItem = conn;
            _Provider = DbProviderFactories.GetProvider(conn.ProviderName);

            var name = $"{ConnName}.{TableName}";
            if (!_TableSchemas.TryGetValue(name.ToLower(), out var schema))
            {
                lock (_TableSchemas)
                {
                    if (!_TableSchemas.TryGetValue(name.ToLower(), out schema))
                    {
                        schema = conn.GetTable(TableName);
                        if (schema == null)
                        {
                            throw new Exception($"表[{name}]不存在");
                        }
                        _TableSchemas.Add(name.ToLower(), schema);
                    }
                }
            }
            _Schema = schema;
            if (_Schema != null)
            {
                var autoColumn = schema.GetAutoIncrementColumn();
                if (autoColumn == null)
                {
                    _HasAutoKey = false;
                }
                else
                {
                    _HasAutoKey = true;
                    _AutoKeyName = autoColumn.Name;
                }
            }
        }
        #endregion

        #region CreateTableIfNotExists
        public TableSet CreateTableIfNotExists()
        {
            using (var db = _ConnItem.Open())
            {
                if (!_Provider.CheckTable(db, _Schema, out string error))
                {
                    throw new Exception(error);
                }
            }
            return this;
        }
        #endregion

        #region 创建SQL语句
        public string GetOrCreateSql(string sqlName)
        {
            if (_SqlItems.TryGetValue(sqlName, out var sql))
            {
                return sql;
            }
            lock (_SqlItems)
            {
                if (_SqlItems.TryGetValue(sqlName, out sql))
                {
                    return sql;
                }

                switch (sqlName)
                {
                    case "Insert":
                        if (_HasAutoKey)
                        {
                            sql = _Provider.CreateInsert(_Schema, true, true);
                        }
                        else
                        {
                            sql = _Provider.CreateInsert(_Schema, false);
                        }
                        _SqlItems.Add(sqlName, sql);
                        break;
                    case "Update":
                        sql = _Provider.CreateUpdate(_Schema);
                        _SqlItems.Add(sqlName, sql);
                        break;
                    case "Delete":
                        sql = _Provider.CreateDelete(_Schema);
                        _SqlItems.Add(sqlName, sql);
                        break;
                    case "Select":
                        sql = _Provider.CreateSelect(_Schema);
                        _SqlItems.Add(sqlName, sql);
                        break;
                    case "SelectByKeys":
                        sql = _Provider.CreateSelectByKeys(_Schema);
                        _SqlItems.Add(sqlName, sql);
                        break;
                    case "SelectByLogicKeys":
                        sql = CreateSelectByLogicKeys(_Schema);
                        _SqlItems.Add(sqlName, sql);
                        break;
                    default:
                        break;
                }
                return sql;
            }
        }

        public virtual string CreateSelectByLogicKeys(TableSchema table)
        {
            if (LogicKeys.IsNullOrEmpty())
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            table.Columns.ForEachWithFirst((c) => { sb.AppendFormat("{0}", _Provider.GetName(c.Name)); }, (c) => { sb.AppendFormat(",{0}", _Provider.GetName(c.Name)); });
            sb.AppendFormat("\r\nFROM {0} ", _Provider.GetName(table.Name));
            sb.Append("\r\nWHERE ");

            var names = LogicKeys.Split(',');
            var columns = names.Select(name => table.Columns.FirstOrDefault(c => name.Equals(c.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            columns.ForEachWithFirst((c) => { sb.AppendFormat("{0}={1}", _Provider.GetName(c.Name), _Provider.GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(" AND {0}={1}", _Provider.GetName(c.Name), _Provider.GetParameter(c.Name)); });
            return sb.ToString();
        }
        #endregion

        #region Insert
        public bool Insert<T>(T entity) where T : IEntity
        {
            using (var conn = _ConnItem.Open())
            {
                var sql = GetOrCreateSql("Insert");
                if (sql.IsNullOrEmpty())
                {
                    return false;
                }
                if (_HasAutoKey)
                {
                    var id = conn.ExecuteScalar(sql, entity.GetNameValues());
                    entity.SetValue(_AutoKeyName, id);
                    return true;
                }
                else
                {
                    return conn.ExecuteNoneQuery(sql, entity.GetNameValues()) > 0;
                }
            }
        }

        public bool Insert<T>(DbConnection conn, DbTransaction trans, T entity) where T : IEntity
        {
            var sql = GetOrCreateSql("Insert");
            if (sql.IsNullOrEmpty())
            {
                return false;
            }
            if (_HasAutoKey)
            {
                var id = conn.ExecuteScalar(trans, sql, entity.GetNameValues());
                entity.SetValue(_AutoKeyName, id);
                return true;
            }
            else
            {
                return conn.ExecuteNoneQuery(trans, sql, entity.GetNameValues()) > 0;
            }
        }
        #endregion

        #region Update
        public bool Update<T>(T entity) where T : IEntity
        {
            using (var conn = _ConnItem.Open())
            {
                var sql = GetOrCreateSql("Update");
                if (sql.IsNullOrEmpty())
                {
                    return false;
                }
                return conn.ExecuteNoneQuery(sql, entity.GetNameValues()) > 0;
            }
        }
        public bool Update<T>(DbConnection conn, DbTransaction trans, T entity) where T : IEntity
        {
            var sql = GetOrCreateSql("Update");
            if (sql.IsNullOrEmpty())
            {
                return false;
            }
            return conn.ExecuteNoneQuery(trans, sql, entity.GetNameValues()) > 0;
        }
        #endregion

        #region UpdateFields
        public bool UpdateFields<T>(T entity) where T : IEntity
        {
            using (var conn = _ConnItem.Open())
            {
                var sql = GetUpdate(entity.GetFieldNames().ToArray());
                return conn.ExecuteNoneQuery(sql, entity.GetNameValues()) > 0;
            }
        }
        public bool UpdateFields<T>(DbConnection conn, DbTransaction trans, T entity) where T : IEntity
        {
            var sql = GetUpdate(entity.GetFieldNames().ToArray());
            return conn.ExecuteNoneQuery(trans, sql, entity.GetNameValues()) > 0;
        }

        public string GetUpdate(params string[] names)
        {
            var table = _Schema;
            var columns = table.Columns.Where(c => !c.IsKey && names.FirstOrDefault(f => f.Equals(c.Name, StringComparison.OrdinalIgnoreCase)) != null);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} SET \r\n", _Provider.GetName(table.Name));
            columns.Where(c => !c.IsKey).ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}\r\n", _Provider.GetName(c.Name), _Provider.GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(",{0}={1}\r\n", _Provider.GetName(c.Name), _Provider.GetParameter(c.Name)); });
            sb.Append(" WHERE ");
            table.Columns.Where(c => c.IsKey).ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}", _Provider.GetName(c.Name), _Provider.GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(" AND {0}={1}", _Provider.GetName(c.Name), _Provider.GetParameter(c.Name)); });
            return sb.ToString();
        }
        #endregion

        #region Delete
        public bool Delete(params object[] key)
        {
            using (var conn = _ConnItem.Open())
            {
                if (TryGetDeleteKeyArgs(key, out var sql, out var args))
                {
                    return conn.ExecuteNoneQuery(sql, args) > 0;
                }
                return false;
            }
        }

        public bool Delete(DbConnection conn, DbTransaction trans, params object[] key)
        {
            if (TryGetDeleteKeyArgs(key, out var sql, out var args))
            {
                return conn.ExecuteNoneQuery(trans, sql, args) > 0;
            }
            return false;
        }

        private bool TryGetDeleteKeyArgs(object[] key, out string sql, out object[] nameValues)
        {
            nameValues = null;
            sql = GetOrCreateSql("Delete");
            if (sql.IsNullOrEmpty())
            {
                return false;
            }
            if (_Schema.Key.IsNullOrEmpty())
            {
                return false;
            }

            var names = _Schema.Key.Split(',');
            if (names.Length != key.Length)
            {
                return false;
            }
            nameValues = GetNameValues(names, key);
            return true;
        }

        private static object[] GetNameValues(string[] names, object[] values)
        {
            object[] nameValues;
            var list = new List<object>();
            for (int i = 0; i < names.Length; i++)
            {
                list.Add(names[i]);
                list.Add(values[i]);
            }
            nameValues = list.ToArray();
            return nameValues;
        }
        #endregion

        #region 保存
        public bool Save<T>(DbConnection conn, DbTransaction trans, T data,
            Func<T, IEntity> onCreate = null, Func<T, IEntity, IEntity> onUpdate = null) where T : IEntity
        {
            if (LogicKeys.IsNullOrEmpty())
            {
                throw new Exception("没有设置逻辑主键");
            }
            var logicNames = LogicKeys.Split(',');
            var keys = logicNames.Select(name => data.GetValue(name)).ToArray();
            var item = GetLogic<DynamicEntity>(keys);
            if (item == null)
            {
                IEntity dataItem = data;
                if (onCreate != null)
                {
                    dataItem = onCreate(data);
                }
                return Insert(conn, trans, dataItem);
            }
            else
            {
                IEntity dataItem = data;
                if (onUpdate != null)
                {
                    dataItem = onUpdate(data, item);
                }
                return Update(conn, trans, dataItem);
            }
        }
        #endregion

        public T Get<T>(params object[] key) where T : IEntity, new()
        {
            using (var conn = _ConnItem.Open())
            {
                var sql = GetOrCreateSql("SelectByKeys");
                if (sql.IsNullOrEmpty())
                {
                    return default;
                }

                var names = _Schema.Key.Split(',');
                if (names.Length != key.Length)
                {
                    return default;
                }
                return conn.ExecuteQuery<T>(sql, GetNameValues(names, key)).FirstOrDefault();
            }
        }

        public T GetLogic<T>(params object[] key) where T : IEntity, new()
        {
            using (var conn = _ConnItem.Open())
            {
                var sql = GetOrCreateSql("SelectByLogicKeys");
                if (sql.IsNullOrEmpty())
                {
                    return default;
                }

                var names = LogicKeys.Split(',');
                if (names.Length != key.Length)
                {
                    return default;
                }
                return conn.ExecuteQuery<T>(sql, GetNameValues(names, key)).FirstOrDefault();
            }
        }

        public List<T> Query<T>(string where, params object[] nameValues) where T : IEntity, new()
        {
            using (var conn = _ConnItem.Open())
            {
                var sql = _Provider.CreateSelect(_Schema, where);
                if (sql.IsNullOrEmpty())
                {
                    return default;
                }

                return conn.ExecuteQuery<T>(sql, nameValues).ToList();
            }
        }
    }
}
