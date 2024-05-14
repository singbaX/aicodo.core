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
    public interface IMappingService
    {
        string GetPropertyName(string columnName);
    }

    class DefaultMapping : IMappingService
    {
        public string GetPropertyName(string columnName)
        {
            return columnName;
        }
    }

    public static class SqlHelper
    {
        private static Dictionary<DbType, object> _DefaultValues = new Dictionary<DbType, object>();

        static IMappingService _MappingService = new DefaultMapping();

        public static string MySql_GetTableSchema { get; set; } =
@"select C.TABLE_NAME,
        C.COLUMN_NAME,
        C.ORDINAL_POSITION,
        C.COLUMN_TYPE AS COLUMN_TYPE,
        C.DATA_TYPE AS DATA_TYPE,
        IFNULL(C.CHARACTER_MAXIMUM_LENGTH,'') AS DATA_LENGTH,
        IFNULL(C.COLUMN_DEFAULT, '') AS COLUMN_DEFAULT,
        IFNULL(C.COLUMN_COMMENT, '') AS COLUMN_COMMENT,
         CASE C.EXTRA WHEN 'auto_increment' THEN 'Y' ELSE '' END AS IS_IDENTITY,
        0 AS IDENTITY_SEED,
        1 AS IDENTITY_INCR,
        CASE C.IS_NULLABLE WHEN 'YES' THEN 'Y' ELSE '' END AS IS_NULLABLE,
        CASE PK.CONSTRAINT_NAME WHEN 'PRIMARY' THEN 'Y' ELSE '' END AS IS_PRIMARY_KEY,
        IFNULL(PK.CONSTRAINT_NAME,'') AS PK_NAME,
        CASE IFNULL(FK.CONSTRAINT_NAME,'') WHEN '' THEN '' ELSE 'Y' END AS IS_FOREIGN_KEY,
        IFNULL(FK.CONSTRAINT_NAME,'') AS FK_NAME,
        IFNULL(FK.REFERENCED_COLUMN_NAME, '') AS FOREIGN_KEY,
         IFNULL(FK.REFERENCED_TABLE_NAME, '') AS FOREIGN_TABLE
        from information_schema.columns C
        LEFT OUTER JOIN
        (SELECT* FROM information_schema.key_column_usage WHERE CONSTRAINT_NAME = 'PRIMARY' AND TABLE_SCHEMA = @DBName AND TABLE_NAME = @TableName) PK
         ON PK.TABLE_NAME= C.TABLE_NAME AND PK.COLUMN_NAME = C.COLUMN_NAME
         LEFT OUTER JOIN
         (SELECT* FROM information_schema.key_column_usage WHERE CONSTRAINT_NAME<> 'PRIMARY' AND TABLE_SCHEMA = @DBName AND TABLE_NAME = @TableName) FK
         ON FK.TABLE_NAME= C.TABLE_NAME AND FK.COLUMN_NAME = C.COLUMN_NAME
         where c.Table_schema=@DBName AND c.TABLE_NAME = @TableName";

        static SqlHelper()
        {
            _DefaultValues.Add(DbType.AnsiString, string.Empty);
            _DefaultValues.Add(DbType.AnsiStringFixedLength, string.Empty);
            _DefaultValues.Add(DbType.Binary, null);
            _DefaultValues.Add(DbType.Boolean, false);
            _DefaultValues.Add(DbType.Byte, 0);
            _DefaultValues.Add(DbType.Currency, 0);
            _DefaultValues.Add(DbType.Date, DateHelper.MinDate);
            _DefaultValues.Add(DbType.DateTime, DateHelper.MinDate);
            _DefaultValues.Add(DbType.DateTime2, DateHelper.MinDate);
            _DefaultValues.Add(DbType.DateTimeOffset, DateHelper.MinDate);
            _DefaultValues.Add(DbType.Decimal, 0);
            _DefaultValues.Add(DbType.Guid, Guid.Empty);
            _DefaultValues.Add(DbType.Int16, 0);
            _DefaultValues.Add(DbType.Int32, 0);
            _DefaultValues.Add(DbType.Int64, 0);
            _DefaultValues.Add(DbType.Object, null);
            _DefaultValues.Add(DbType.SByte, 0);
            _DefaultValues.Add(DbType.Single, 0);
            _DefaultValues.Add(DbType.String, string.Empty);
            _DefaultValues.Add(DbType.StringFixedLength, string.Empty);
            _DefaultValues.Add(DbType.Time, DateHelper.MinDate);
            _DefaultValues.Add(DbType.UInt16, 0);
            _DefaultValues.Add(DbType.UInt32, 0);
            _DefaultValues.Add(DbType.UInt64, 0);
            _DefaultValues.Add(DbType.VarNumeric, 0);
            _DefaultValues.Add(DbType.Xml, string.Empty);
        }

        static DbConnection Open(this string connName)
        {
            var connection = SqlData.Current.Connections[connName];
            if (connection == null)
            {
                throw new Exception("数据库连接不存在");
            }
            return connection.Open();
        }

        static DbDataAdapter CreateAdapter(this string connName)
        {
            var connection = SqlData.Current.Connections[connName];
            if (connection == null)
            {
                throw new Exception("数据库连接不存在");
            }
            return connection.CreateAdapter();
        }

        public static void UseMappingService(IMappingService service)
        {
            _MappingService = service;
        }

        public static void CloseConnection(this DbConnection conn)
        {
            try
            {
                conn.Close();
            }
            catch
            {
            }
        }

        public static object GetDBValue(DbType dbtype, object value)
        {
            if (value == null || value.ToString().Length == 0)
            {
                return _DefaultValues[dbtype];
            }
            return value;
        }

        #region with connection
        public static object ExecuteScalar(this DbConnection db, SqlItem sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                var result = cmd.ExecuteScalar();
                return result;
            }
        }

        public static object ExecuteScalar(this DbConnection db, string sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                var result = cmd.ExecuteScalar();
                return result;
            }
        }

        public static int ExecuteNoneQuery(this DbConnection db, SqlItem sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                var count = cmd.ExecuteNonQuery();
                return count;
            }
        }

        public static int ExecuteNoneQuery(this DbConnection db, string sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                var count = cmd.ExecuteNonQuery();
                return count;
            }
        }

        public static IEnumerable<T> ExecuteQuery<T>(this DbConnection db, SqlItem sql, params object[] nameParas) where T : IEntity, new()
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                var reader = cmd.ExecuteReader();
                var items = reader.ToItems<T>(sql.ResultConverters).ToList();
                reader.Close();
                reader.Dispose();
                return items;
            }
        }

        public static IEnumerable<T> ExecuteQuery<T>(this DbConnection db, string sql, params object[] nameParas) where T : IEntity, new()
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                var reader = cmd.ExecuteReader();
                var items = reader.ToItems<T>().ToList();
                reader.Close();
                reader.Dispose();
                return items;
            }
        }

        public static IEnumerable<T> ExecuteQuery<T>(this DbConnection db, string sql,
            IEnumerable<QueryResultConverter> converters, params object[] nameParas) where T : IEntity, new()
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                var reader = cmd.ExecuteReader();
                var items = reader.ToItems<T>(converters).ToList();
                reader.Close();
                reader.Dispose();
                return items;
            }
        }
        #endregion

        #region with connection transaction
        public static object ExecuteScalar(this DbConnection db, DbTransaction trans, SqlItem sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                cmd.Transaction = trans;
                var result = cmd.ExecuteScalar();
                return result;
            }
        }

        public static object ExecuteScalar(this DbConnection db, DbTransaction trans, string sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                cmd.Transaction = trans;
                var result = cmd.ExecuteScalar();
                return result;
            }
        }

        public static int ExecuteNoneQuery(this DbConnection db, DbTransaction trans, SqlItem sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                cmd.Transaction = trans;
                var count = cmd.ExecuteNonQuery();
                return count;
            }
        }

        public static int ExecuteNoneQuery(this DbConnection db, DbTransaction trans, string sql, params object[] nameParas)
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                cmd.Transaction = trans;
                var count = cmd.ExecuteNonQuery();
                return count;
            }
        }

        public static IEnumerable<T> ExecuteQuery<T>(this DbConnection db, DbTransaction trans, SqlItem sql, params object[] nameParas) where T : IEntity, new()
        {
            lock (db)
            {
                var cmd = db.CreateDBCommand(sql, nameParas);
                cmd.Transaction = trans;
                var reader = cmd.ExecuteReader();
                var items = reader.ToItems<T>(sql.ResultConverters).ToList();
                reader.Close();
                reader.Dispose();
                return items;
            }
        }
        #endregion

        public static object ExecuteScalar(this SqlItem sql, params object[] nameParas)
        {
            return sql.ExecuteCommand<object>((cmd) =>
            {
                return cmd.ExecuteScalar();
            }, nameParas);
        }

        public static int ExecuteNoneQuery(this SqlItem sql, params object[] nameParas)
        {
            return sql.ExecuteCommand<int>((cmd) =>
            {
                return cmd.ExecuteNonQuery();
            }, nameParas);
        }

        public static IEnumerable<T> ExecuteQuery<T>(this SqlItem sql, params object[] nameParas) where T : IEntity, new()
        {
            return sql.ExecuteCommand<IEnumerable<T>>((cmd) =>
            {
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.ToItems<T>(sql.ResultConverters).ToList();
                }
            }, nameParas);
        }

        public static string ExecuteJson(this SqlItem sql, params object[] nameParas)
        {
            using (var db = sql.ConnectionName.Open())
            {
                var cmd = CreateDBCommand(db, sql, nameParas);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.CreateJson();
                }
            }
        }

        public static T ExecuteCommand<T>(this SqlItem sql, Func<DbCommand, T> funcResult, params object[] nameParas)
        {
            if (sql == null)
            {
                throw new Exception("SQL 命令不存在");
            }

            try
            {
                using (var db = sql.ConnectionName.Open())
                {
                    var cmd = CreateDBCommand(db, sql, nameParas);
                    return funcResult(cmd);
                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"{sql.Name}执行错误:{ex.Message}");
                sb.AppendLine($"{sql.CommandText}");
                if (nameParas != null && nameParas.Length > 0)
                {
                    for (int i = 0; i < nameParas.Length - 1; i += 2)
                    {
                        sb.Append($"@{nameParas[i]}={nameParas[i + 1]},");
                    }
                    sb.ToString(0, sb.Length - 1).WriteErrorLog();
                }
                else
                {
                    sb.ToString().WriteErrorLog();
                }
                throw ex;
            }
        }

        private static DbCommand CreateDBCommand(this DbConnection db, SqlItem sql, object[] nameParas)
        {
            string result = string.Empty;
            var cmd = db.CreateCommand();
            var args = nameParas.ToDictionary();
            AddParameters(cmd, args);
            ResetCommandPageText(sql, cmd, args);
            return cmd;
        }

        private static DbCommand CreateDBCommand(this DbConnection db, string sql, object[] nameParas)
        {
            string result = string.Empty;
            var args = nameParas.ToDictionary();
            var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            AddParameters(cmd, nameParas);
            return cmd;
        }

        private static void ResetCommandPageText(SqlItem sql, DbCommand cmd, Dictionary<string, object> args)
        {
            var text = sql.GetCommandText(args);
            if (sql.CanUsePage)
            {
                var pIndex = cmd.Parameters[sql.PageIndexName];
                var pSize = cmd.Parameters[sql.PageSizeName];
                if (pIndex == null || pSize == null)
                {
                    return;
                }

                var size = Convert.ToInt32(pSize.Value);
                var index = Convert.ToInt32(pIndex.Value);

                var limit = string.Format(" LIMIT {0},{1};", index * size, size);
                cmd.CommandText = text.TrimEnd(' ', '\r', '\n', ';') + limit;
            }
            else
            {
                cmd.CommandText = text;
            }
        }

        private static void AddFirstUpperParameters(DbCommand cmd, object[] nameParas)
        {
            if (nameParas != null && nameParas.Length > 1)
            {
                for (int i = 0; i < nameParas.Length - 1; i += 2)
                {
                    string name = nameParas[i].ToString();
                    if (!char.IsUpper(name[0]))
                    {
                        continue;
                    }
                    DbParameter para = cmd.CreateParameter();
                    para.ParameterName = nameParas[i].ToString();
                    para.Value = nameParas[i + 1];
                    cmd.Parameters.Add(para);
                }
            }
        }

        private static void AddParameters(DbCommand cmd, object[] nameParas)
        {
            AddParameters(cmd, nameParas.ToDictionary());
        }

        private static void AddParameters(DbCommand cmd, Dictionary<string, object> args)
        {
            if (args != null && args.Count > 0)
            {
                foreach (var arg in args)
                {
                    DbParameter para = cmd.CreateParameter();
                    para.ParameterName = arg.Key;

                    object value = arg.Value;
                    if (value is DynamicEntity doc)
                    {
                        value = doc.ToJson();
                    }
                    para.Value = value;
                    cmd.Parameters.Add(para);

                }
            }
        }

        private static IEnumerable<T> ToItems<T>(this IDataReader reader,
            IEnumerable<QueryResultConverter> converters = null) where T : IEntity, new()
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
                    var pname = _MappingService.GetPropertyName(name);
                    newItem.SetValue(pname, vv);
                }
                yield return newItem;
            }
        }

        public static IEnumerable<string> GetParameters(this string commandText,
            string prameterPrefix = "@", bool distinct = true)
        {
            List<string> result = new List<string>();
            var p = prameterPrefix;
            //Regex paramReg = new Regex(@"(?<!@)[^\w$#@]@(?!@)[\w$#@]+");
            Regex paramReg = new Regex($"(?<!{p})[^\\w$#{p}]{p}(?!{p})[\\w$#{p}]+");
            MatchCollection matches = paramReg.Matches(commandText);
            foreach (Match m in matches)
            {
                var name = m.Groups[0].Value.Substring(m.Groups[0].Value.IndexOf(prameterPrefix) + 1);
                if (distinct)
                {
                    if (result.FirstOrDefault(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        result.Add(name);
                    }
                }
                else
                {
                    result.Add(name);
                }
            }
            return result;
        }

        #region Schema
        public static string ToFormatedName(this string name, bool skipPrefix = false)
        {
            if (name.Length > 1)
            {
                if (name.IndexOf('_') < 0)
                {
                    return name.ToFirstUpper();
                }

                StringBuilder sb = new StringBuilder();
                var i = skipPrefix ? 1 : 0;
                var names = name.Split('_');
                for (; i < names.Length; i++)
                {
                    sb.Append(names[i].ToFirstUpper());
                }
                return sb.ToString();
            }
            return name.ToFirstUpper();
        }

        internal static string ToFirstUpper(this string name)
        {
            if (name.IsNullOrEmpty())
            {
                return name;
            }
            if (char.IsUpper(name[0]))
            {
                return name;
            }
            if (name.Length > 1)
            {
                return name.Substring(0, 1).ToUpper() + name.Substring(1);
            }
            return name.ToUpper();
        }

        internal static string ToFirstLower(this string name)
        {
            if (name.IsNullOrEmpty())
            {
                return name;
            }
            if (char.IsLower(name[0]))
            {
                return name;
            }
            if (name.Length > 1)
            {
                return name.Substring(0, 1).ToLower() + name.Substring(1);
            }
            return name.ToLower();
        }

        internal static string ToFormatedName(this string name)
        {
            if (name.Length > 1)
            {
                if (name.IndexOf('_') < 0)
                {
                    return name.ToFirstUpper();
                }

                StringBuilder sb = new StringBuilder();
                foreach (var s in name.Split('_'))
                {
                    sb.Append(s.ToFirstUpper());
                }
                return sb.ToString();
            }
            return name.ToFirstUpper();
        }
        #endregion
    }

}
