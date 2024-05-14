// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Text;

    public class SqlContext
    {
        DbConnection _Connection = null;
        DbTransaction _Transaction = null;

        Sort[] _Sorts = null;
        ISqlFilter _Filter = null;

        int _PageIndex = 0;
        int _PageSize = 0;

        SqlItem _SqlItem = null;
        SqlConnection _SqlConn = null;
        IDbProvider _Provider = null;

        #region SqlContext
        public SqlContext(string sqlName)
        {
            var sqlItem = SqlData.Current.GetSqlItem(sqlName);
            if (sqlItem == null)
            {
                throw new Exception($"SqlItem [{sqlName}] Not Found");
            }
            _SqlItem = sqlItem;
            Sql = _SqlItem.CommandText;
            _SqlConn = GetSqlConnection();
            if (_SqlConn == null)
            {
                throw new Exception($"SqlItem [{sqlName}] Connection Not Found");
            }
            _Provider = DbProviderFactories.GetProvider(_SqlConn.ProviderName);
        }

        public SqlContext(string sql, string providerName)
        {
            Sql = sql;
            _Provider = DbProviderFactories.GetProvider(providerName);
        }

        public SqlContext(SqlItem sqlItem)
        {
            _SqlItem = sqlItem;
            Sql = _SqlItem.CommandText;
            _SqlConn = GetSqlConnection();
            if (_SqlConn == null)
            {
                throw new Exception($"SqlItem [{sqlItem.Name}] Connection Not Found");
            }
            _Provider = DbProviderFactories.GetProvider(_SqlConn.ProviderName);
        }

        private SqlConnection GetSqlConnection()
        {
            var sqlItem = _SqlItem;
            var connName = sqlItem.ConnectionName.IsNullOrEmpty() ? sqlItem.Group.ConnectionName : sqlItem.ConnectionName;
            var connItem = connName.IsNullOrEmpty() ? SqlData.Current.Connections.FirstOrDefault() :
                SqlData.Current.Connections[connName];
            return connItem;
        }

        public void Dispose()
        {
            if (_Connection == null) return;
            try
            {
                _Connection.Close();
                _Connection.Dispose();
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region 属性 Sql
        private string _Sql = string.Empty;
        public string Sql
        {
            get
            {
                return _Sql;
            }
            private set
            {
                _Sql = value;
            }
        }
        #endregion

        #region 属性 Type
        private SqlType _Type = SqlType.Query;
        public SqlType Type
        {
            get
            {
                return _Type;
            }
            private set
            {
                _Type = value;
            }
        }
        #endregion

        public SqlContext ResetSql(Dictionary<string, object> args)
        {
            if (_SqlItem != null)
            {
                Sql = _SqlItem.GetCommandText(args);
            }
            return this;
        }

        #region 连接处理
        public SqlContext UseConnection(DbConnection conn)
        {
            _Connection = conn;
            return this;
        }

        public SqlContext UseTransaction(DbTransaction transaction)
        {
            _Transaction = transaction;
            return this;
        }

        public DbConnection Connect()
        {
            return _SqlConn.Open();
        }
        #endregion

        #region 过滤
        public SqlContext SetFilter(ISqlFilter filter)
        {
            _Filter = filter;
            return this;
        }
        #endregion

        #region 排序
        public SqlContext SetSorts(params Sort[] sorts)
        {
            _Sorts = sorts;
            return this;
        }
        #endregion

        #region 分页
        public SqlContext SetPage(int pageIndex, int pageSize)
        {
            _PageIndex = pageIndex;
            _PageSize = pageSize;
            return this;
        }
        #endregion

        #region 执行方法
        public int GetTotalCount(params object[] nameValues)
        {
            var sql = Sql;
            nameValues = _ResetFilter(nameValues, ref sql);
            sql = _Provider.ResetQueryTotal(sql);

            return ExecuteSql<object>(sql, (cmd) => cmd.ExecuteScalar(), nameValues)
                .ToInt32();
        }

        public IEnumerable<T> ExecuteQuery<T>(params object[] nameValues)
            where T : IEntity, new()
        {
            var sql = Sql;
            nameValues = _ResetFilter(nameValues, ref sql);
            if (_Sorts != null)
            {
                sql += _Provider.CreateOrderBy(_Sorts);
            }
            if (_PageSize > 0)
            {
                var from = _PageIndex > 0 ? (_PageIndex - 1) * _PageSize : 0;
                sql = _Provider.ResetQueryLimit(sql, from, _PageSize);
            }

            return ExecuteSql<IEnumerable<T>>(sql, (cmd) => _Provider.ToItems<T>(cmd.ExecuteReader()), nameValues);
        }

        public T ExecuteQueryOne<T>(params object[] nameValues)
            where T : IEntity, new()
        {
            var sql = Sql;
            nameValues = _ResetFilter(nameValues, ref sql);
            if (_Sorts != null)
            {
                sql += _Provider.CreateOrderBy(_Sorts);
            }
            if (_PageSize > 0)
            {
                var from = _PageIndex > 0 ? (_PageIndex - 1) * _PageSize : 0;
                sql = _Provider.ResetQueryLimit(sql, from, _PageSize);
            }
            return ExecuteSql<IEnumerable<T>>(sql, (cmd) => _Provider.ToItems<T>(cmd.ExecuteReader()), nameValues).FirstOrDefault();
        }

        public int ExecuteNoneQuery(params object[] nameValues)
        {
            var sql = Sql;
            nameValues = _ResetFilter(nameValues, ref sql);
            return ExecuteSql<int>(sql, (cmd) => cmd.ExecuteNonQuery(), nameValues);
        }

        public object ExecuteScalar(params object[] nameValues)
        {
            var sql = Sql;
            nameValues = _ResetFilter(nameValues, ref sql);
            return ExecuteSql<object>(sql, (cmd) => cmd.ExecuteScalar(), nameValues);
        }

        private T ExecuteSql<T>(string sql, Func<DbCommand, T> func, params object[] nameValues)
        {
            var db = _Connection == null ? Connect() : _Connection;
            var cmd = _Provider.CreateCommand(db, sql, nameValues);
            if (_Transaction != null)
            {
                cmd.Transaction = _Transaction;
            }
            return func(cmd);
        }
        #endregion

        private object[] _ResetFilter(object[] nameValues, ref string sql)
        {
            if (_Filter == null)
            {
                return nameValues;
            }

            DynamicEntity filterParameters = new DynamicEntity();
            sql = AppendFilter(sql, filterParameters, _Filter);
            if (nameValues != null)
            {
                for (int i = 0; i < nameValues.Length - 1; i += 2)
                {
                    var name = nameValues[i].ToString();
                    if (filterParameters.ContainsKey(name))
                    {
                        throw new Exception($"参数名称与自动生成过滤名称重复[{name}]");
                    }
                    filterParameters[name] = nameValues[i + 1];
                }
            }
            return filterParameters.GetNameValues();
        }

        private string AppendFilter(string sql, IEntity parameter, ISqlFilter filter)
        {
            var pindex = 0;
            var where = _Provider.CreateFilter(filter, parameter, "P", ref pindex);
            if (where.IsNotNullOrEmpty())
            {
                if (sql.IndexOf("where", StringComparison.OrdinalIgnoreCase) > 0 && Type == SqlType.Query)
                {
                    sql = $"SELECT * FROM ({sql}) N WHERE " + where;
                }
                else
                {
                    sql = sql + "\r\n WHERE " + where;
                }
            }
            return sql;
        }
    }
}
