// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AiCodo.Data
{
    #region DbProvider
    public abstract class DbProvider : IDbProvider
    {
        protected abstract DbProviderFactory GetFactory();

        public DbProviderFactory Factory
        {
            get
            {
                return GetFactory();
            }
        }

        #region 表结构操作方法
        public bool CheckTable(DbConnection conn, TableSchema table, out string error)
        {
            error = "";
            var sql = ExistsTable(conn.Database, table.Name);
            if (sql.IsNullOrEmpty())
            {
                error = "[ExistsTable] not implement";
                return false;
            }
            var exists = conn.ExecuteScalar(sql).ToBoolean();
            if (exists)
            {
                return true;
            }
            var createSql = CreateTable(table);
            if (createSql.IsNullOrEmpty())
            {
                error = "[CreateTable] not implement";
                return false;
            }
            var ok = conn.ExecuteNoneQuery(createSql);
            return true;
        }
        #endregion

        public abstract string CreateConnectionString(DynamicEntity args);

        public virtual string ResetQueryLimit(string sql, int from, int count)
        {
            if (count == 0)
            {
                return sql;
            }
            var limit = string.Format(" LIMIT {0},{1}", from, count);
            return sql.TrimEnd(';', ' ') + limit;
        }

        public virtual string ResetQueryTotal(string sql)
        {
            return $"SELECT COUNT(*) FROM ({sql}) X";
        }


        public abstract string GetLastIdentity(string tableName = "", string fieldName = "");

        public abstract string GetName(string columnName);

        public abstract string GetParameter(string columnName);

        public virtual IEnumerable<string> GetParameters(string commandText)
        {
            return commandText.GetParameters("@");
        }

        protected void AppendNames(StringBuilder sb, IEnumerable<string> names)
        {
            names.ForEachWithFirst((c) => { sb.AppendFormat("{0}", GetName(c)); },
                (c) => { sb.AppendFormat(",{0}", GetName(c)); });
        }
        protected void AppendParameters(StringBuilder sb, IEnumerable<string> names)
        {
            names.ForEachWithFirst((c) => { sb.AppendFormat("{0}", GetParameter(c)); },
                (c) => { sb.AppendFormat(",{0}", GetParameter(c)); });
        }
        protected void AppendNameParameters(StringBuilder sb, IEnumerable<string> names)
        {
            names.ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}", GetName(c), GetParameter(c)); },
                (c) => { sb.AppendFormat(",{0}={1}", GetName(c), GetParameter(c)); });
        }

        public virtual TableSchema GetTableSchema(DbConnection db, string tableName)
        {
            return null;
        }

        public virtual IEnumerable<string> GetTables(DbConnection db)
        {
            yield break;
        }

        #region Command
        public virtual DbCommand CreateCommand(DbConnection db, string sql, params object[] nameValues)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            SetParameters(cmd, nameValues);
            return cmd;
        }

        public virtual IEnumerable<T> ToItems<T>(IDataReader reader) where T : IEntity, new()
        {
            while (reader.Read())
            {
                T newItem = new T();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var vv = reader.GetValue(i);
                    if (vv is DBNull)
                    {
                        continue;
                    }
                    var name = reader.GetName(i);
                    newItem.SetValue(name, vv);
                }
                yield return newItem;
            }
        }

        protected virtual void SetParameters(DbCommand cmd, params object[] nameValues)
        {
            if (nameValues != null && nameValues.Length > 1)
            {
                for (int i = 0; i < nameValues.Length - 1; i += 2)
                {
                    DbParameter para = cmd.CreateParameter();
                    para.ParameterName = nameValues[i].ToString();

                    object value = nameValues[i + 1];
                    if (value is DynamicEntity doc)
                    {
                        value = doc.ToJson();
                    }
                    para.Value = value;
                    cmd.Parameters.Add(para);
                }
            }
        }
        #endregion

        #region Create Filter
        public virtual string CreateFilter(ISqlFilter filter, IEntity parameters, string prefix, ref int pIndex)
        {
            if (filter is ILogicFilter logicFilter)
            {
                return CreateLogicFilter(logicFilter, parameters, prefix, ref pIndex);
            }
            if (filter is ICompareFilter compareFilter)
            {
                return CreateCompareFilter(compareFilter, parameters, prefix, ref pIndex);
            }
            return filter.ToString();
        }

        public virtual string CreateLogicFilter(ILogicFilter filter, IEntity parameters, string prefix, ref int pIndex)
        {
            var items = filter.Items;
            var logicType = filter.Type;
            if (items == null || items.Count == 0)
            {
                return "";
            }

            if (logicType == LogicJoinType.Not)
            {
                return $"NOT ({CreateFilter(items[0], parameters, prefix, ref pIndex)})";
            }

            if (items.Count == 1)
            {
                return $"({CreateFilter(items[0], parameters, prefix, ref pIndex)})";
            }

            List<string> filterItems = new List<string>();
            foreach (var item in items)
            {
                var s = CreateFilter(item, parameters, prefix, ref pIndex);
                filterItems.Add(s);
            }
            var type = logicType == LogicJoinType.Or ? " OR " : " AND ";
            return filterItems.Select(s => $"({s})").AggregateSplitStrings(type);
        }

        protected virtual string CreateCompareFilter(ICompareFilter filter, IEntity parameters, string prefix, ref int pIndex)
        {
            pIndex++;
            var pname = $"{prefix}{pIndex}";
            parameters.SetValue(pname, filter.Value);
            var type = filter.Type;
            var name = filter.Name;

            if (type == LogicCompareType.Contains)
            {
                return $"{name} LIKE CONCAT('%',@{pname},'%')";
            }

            if (type == LogicCompareType.In)
            {
                var args = filter.Value.ToString().Split(',');
                var argNames = new string[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    pIndex++;
                    argNames[i] = $"{prefix}{pIndex}";
                    parameters.SetValue(argNames[i], args[i]);
                }

                return $"{name} IN ({argNames.Select(s => $"@{s}").AggregateSplitStrings()})";
            }
            return $"{name} {GetCompareType(type)} @{pname}";
        }

        static string GetCompareType(LogicCompareType type)
        {
            if (CompareFilter.Types.TryGetValue(type, out string t))
            {
                return t;
            }
            return "=";
        }
        #endregion

        public virtual string CreateOrderBy(params Sort[] sort)
        {
            var orderby = sort.Select(s => s.ToString()).AggregateStrings();
            return orderby.IsNullOrEmpty() ? "" : $"\r\nORDER BY {orderby}";
        }

        public virtual IEnumerable<TableSchema> LoadTables(DbConnection db)
        {
            return GetTables(db).Select(name => GetTableSchema(db, name));
        }

        public virtual string ExistsTable(string dbName, string name)
        {
            return "";
        }

        public virtual string CreateView(string name, string select, bool replace = true)
        {
            return "";
        }

        public virtual string CreateTable(TableSchema table)
        {
            return "";
        }

        public virtual string CreateAlterTable(TableSchema table, TableSchema oldTable)
        {
            return "";
        }

        public virtual string CreateInsert(TableSchema table,
            bool reload = false, bool reloadAutoColumnOnly = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}\r\n(", GetName(table.Name));
            var insertColumns = table.Columns.Where(c => !c.IsAutoIncrement).Select(s => s.Name);
            AppendNames(sb, insertColumns);
            sb.Append(")");
            sb.Append("\r\nVALUES(");
            AppendParameters(sb, insertColumns);
            sb.Append(");");

            if (reload)
            {
                sb.Append("\r\n");
                //如果有自增列且自增列是主键（Mysql肯定是主键），直接根据最后新增自增列来取
                var autoColumn = table.GetAutoIncrementColumn();
                if (autoColumn != null)
                {
                    if (reloadAutoColumnOnly)
                    {
                        string select = string.Format("SELECT {0} AS {1};", GetLastIdentity(table.Name, autoColumn.Name), GetName(autoColumn.Name));
                        sb.Append(select);
                    }
                    else
                    {
                        string where = string.Format("{0}={1}", GetName(autoColumn.Name), GetLastIdentity(table.Name, autoColumn.Name));
                        sb.Append(CreateSelect(table, where));
                    }
                }
                else
                {
                    sb.Append(CreateSelectByKeys(table));
                }
            }
            return sb.ToString();
        }

        public virtual string CreateUpdate(TableSchema table)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} SET \r\n", GetName(table.Name));
            table.Columns.Where(c => !c.IsKey).ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}\r\n", GetName(c.Name), GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(",{0}={1}\r\n", GetName(c.Name), GetParameter(c.Name)); });
            sb.Append(" WHERE ");
            table.Columns.Where(c => c.IsKey).ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}", GetName(c.Name), GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(" AND {0}={1}", GetName(c.Name), GetParameter(c.Name)); });
            return sb.ToString();
        }

        public virtual string CreateDelete(TableSchema table)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("DELETE FROM {0} \r\n", GetName(table.Name));
            sb.Append(" WHERE ");
            table.Columns.Where(c => c.IsKey).ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}", GetName(c.Name), GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(" AND {0}={1}", GetName(c.Name), GetParameter(c.Name)); });
            return sb.ToString();
        }

        public virtual string CreateCount(TableSchema table)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT count(*)");
            sb.AppendFormat(" FROM {0} ", GetName(table.Name));
            return sb.ToString();
        }

        public virtual string CreateSelect(TableSchema table)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            table.Columns.ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}", GetName(c.Name)); },
                (c) => { sb.AppendFormat(",{0}", GetName(c.Name)); });
            sb.AppendFormat("\r\nFROM {0} ", GetName(table.Name));
            return sb.ToString();
        }

        public virtual string CreateSelect(TableSchema table, string where)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            table.Columns.ForEachWithFirst((c) => { sb.AppendFormat("{0}", GetName(c.Name)); }, (c) => { sb.AppendFormat(",{0}", GetName(c.Name)); });
            sb.AppendFormat("\r\nFROM {0} ", GetName(table.Name));
            sb.Append("\r\nWHERE ");
            sb.Append(where);

            return sb.ToString();
        }

        public virtual string CreateSelectByKeys(TableSchema table)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            table.Columns.ForEachWithFirst((c) => { sb.AppendFormat("{0}", GetName(c.Name)); }, (c) => { sb.AppendFormat(",{0}", GetName(c.Name)); });
            sb.AppendFormat("\r\nFROM {0} ", GetName(table.Name));
            sb.Append("\r\nWHERE ");
            table.Columns.Where(c => c.IsKey).ForEachWithFirst((c) => { sb.AppendFormat("{0}={1}", GetName(c.Name), GetParameter(c.Name)); }, (c) => { sb.AppendFormat(" AND {0}={1}", GetName(c.Name), GetParameter(c.Name)); });
            return sb.ToString();
        }

        public virtual string CreateInsert(TableSchema table, string[] columns)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}\r\n(", GetName(table.Name));
            var insertColumns = table.Columns.Where(c => !c.IsAutoIncrement
                && columns.FirstOrDefault(f => f.Equals(c.Name, StringComparison.OrdinalIgnoreCase)) != null)
                .Select(s => s.Name);
            AppendNames(sb, insertColumns);
            sb.Append(")");
            sb.Append("\r\nVALUES(");
            AppendParameters(sb, insertColumns);
            sb.Append(");");
             
            //如果有自增列且自增列是主键（Mysql肯定是主键），直接根据最后新增自增列来取
            var autoColumn = table.GetAutoIncrementColumn();
            if (autoColumn != null)
            { 
                string select = string.Format("SELECT {0} AS {1};", GetLastIdentity(table.Name, autoColumn.Name), GetName(autoColumn.Name));
                sb.Append(select); 
            } 
            return sb.ToString();
        }

        public virtual string CreateUpdate(TableSchema table, string[] columns)
        {
            var updateColumns = table.Columns.Where(c => c.IsKey == false
                && columns.FirstOrDefault(f => f.Equals(c.Name, StringComparison.OrdinalIgnoreCase)) != null);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} SET \r\n", GetName(table.Name));
            updateColumns.Where(c => !c.IsKey).ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}\r\n", GetName(c.Name), GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(",{0}={1}\r\n", GetName(c.Name), GetParameter(c.Name)); });
            sb.Append(" WHERE ");
            table.Columns.Where(c => c.IsKey).ForEachWithFirst(
                (c) => { sb.AppendFormat("{0}={1}", GetName(c.Name), GetParameter(c.Name)); },
                (c) => { sb.AppendFormat(" AND {0}={1}", GetName(c.Name), GetParameter(c.Name)); });
            return sb.ToString();
        }

        public virtual string CreateSelect(TableSchema table, string[] columns, string where = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            table.Columns
                .Where(c=>columns.FirstOrDefault(f=>f.Equals(c.Name, StringComparison.OrdinalIgnoreCase))!=null)
                .ForEachWithFirst((c) => { sb.AppendFormat("{0}", GetName(c.Name)); }, (c) => { sb.AppendFormat(",{0}", GetName(c.Name)); });
            sb.AppendFormat("\r\nFROM {0} ", GetName(table.Name));
            if (where.IsNotEmpty())
            {
                where = where.Trim();
                if (where.StartsWith("where ", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append($"\r\n{where}");
                }
                else
                {
                    sb.Append($"\r\nWHERE {where}");
                }
            }
            return sb.ToString();
        }

    }
    #endregion

}
