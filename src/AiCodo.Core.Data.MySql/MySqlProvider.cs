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

namespace AiCodo.Data
{
    #region MySql
    public class MySqlProvider : DbProvider, IAlterTable, IColumnDefaultValue
    {
        static Dictionary<string, string> _ColumnDefaultValues = new Dictionary<string, string>
        {
            {"int","'0'"},
            {"float","'0'"},
            {"double","'0'"},
            {"varchar","''"},
            {"char","''"},
            {"text","''"},
            {"mediumtext","''"},
            {"longtext","''"},
            {"datetime","'1900-01-01'"},
        };

        #region sql

        const string MySql_CheckTable = @"SELECT EXISTS(SELECT f.* FROM information_schema.`TABLES` f
WHERE f.TABLE_SCHEMA='{0}' AND f.TABLE_NAME='{1}')";

        public static string MySql_GetTables { get; set; } =
 @"select T.TABLE_NAME,T.`ENGINE` from information_schema.TABLES T
WHERE T.TABLE_SCHEMA=@DBName AND T.TABLE_TYPE= 'BASE TABLE'";

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
        #endregion

        private static MySqlProvider _Instance = new MySqlProvider();
        public static MySqlProvider Instance
        {
            get
            {
                return _Instance;
            }
        }

        public override string CreateConnectionString(DynamicEntity args)
        {
            var server = args.GetString("server", "localhost");
            var port = args.GetInt32("port", 3306);
            var uid = args.GetString("uid", "root");
            var pwd = args.GetString("pwd", "sa123456");
            var database = args.GetString("database", "");
            var charset = args.GetString("charset", "utf8");
            string connectionString = CreateMySqlConnectionString(server, port, uid, pwd, database, charset);
            return connectionString;
        }

        public static string CreateMySqlConnectionString(string server, int port, string uid, string pwd, string database,
            string charset = "utf8", string sslMode = "none")
        {
            return $"Server={server};Port={port};Database={database};Uid={uid};Pwd={pwd};CharSet={charset};SslMode={sslMode};";
        }

        protected override DbProviderFactory GetFactory()
        {
            return MySqlConnector.MySqlConnectorFactory.Instance;
        }

        public override string GetLastIdentity(string tableName = "", string colName = "")
        {
            return "LAST_INSERT_ID()";
        }

        public override string GetName(string columnName)
        {
            return $"`{columnName}`";
        }

        public override string GetParameter(string columnName)
        {
            return $"@{columnName}";
        }

        public override TableSchema GetTableSchema(DbConnection db, string tableName)
        {
            var sql = MySql_GetTableSchema;
            var items = db.ExecuteQuery<DynamicEntity>(sql, "DBName", db.Database, "TableName", tableName)
                .Select(q =>
                {
                    var col = new Column
                    {
                        Name = q.GetString("COLUMN_NAME"),
                        ColumnOrdinal = q.GetInt32("ORDINAL_POSITION", 0),
                        ColumnType = q.GetString("COLUMN_TYPE"),
                        DataType = q.GetString("DATA_TYPE", ""),
                        Length = q.GetInt64("DATA_LENGTH", 0),
                        IsAutoIncrement = q.GetString("IS_IDENTITY", "").Equals("Y"),
                        DefaultValue = q.GetString("COLUMN_DEFAULT", ""),
                        NullAble = q.GetString("IS_NULLABLE", "").Equals("Y"),
                        IsKey = q.GetString("IS_PRIMARY_KEY", "").Equals("Y"),
                        IsReadOnly = q.GetString("IS_IDENTITY", "").Equals("Y")
                    };
                    col.PropertyName = col.Name.ToFormatedName();
                    col.ResetComment(q.GetString("COLUMN_COMMENT"));
                    return col;
                }).ToList();

            var schema = new TableSchema
            {
                Name = tableName,
                Schema = db.Database,
                CodeName = tableName.ToFormatedName(true),
                Key = items.Where(c => c.IsKey).Select(c => c.Name).AggregateStrings()
            };
            items.AddToCollection(schema.Columns);
            return schema;
        }

        public override IEnumerable<string> GetTables(DbConnection db)
        {
            var sql = MySql_GetTables;
            var items = db.ExecuteQuery<DynamicEntity>(sql, "DBName", db.Database);
            return items.Select(s => s.GetString("TABLE_NAME"));
        }

        public override string ExistsTable(string dbName, string tableName)
        {
            return string.Format(MySql_CheckTable, dbName, tableName);
        }

        public override string CreateView(string name, string select, bool replace = true)
        {
            return $"CREATE{(replace ? " OR REPLACE" : "")} VIEW {name} AS \r\n{select}";
        }


        public override string CreateTable(TableSchema t)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE `{t.Name}` (");
            foreach (var c in t.Columns)
            {
                sb.AppendFormat("  `{0}` {1} {2}{3},\r\n", c.Name, GetFullType(c), c.NullAble ? "NULL" : "NOT NULL", c.IsAutoIncrement ? " AUTO_INCREMENT" : "");
            }

            sb.Append($" PRIMARY KEY({t.Key})");
            sb.AppendLine(")");
            sb.AppendLine("COLLATE = 'utf8_general_ci'");
            sb.AppendLine("ENGINE = InnoDB");
            sb.AppendLine(";");
            sb.AppendLine("");
            return sb.ToString();
        }

        public string CreateChangeColumn(string tableName, Column c, string afterName = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ALTER TABLE `{tableName}`");
            var after = afterName.IsNullOrEmpty() ? " FIRST" : $" AFTER `{afterName}`";
            var comment = c.Comment.IsNullOrEmpty() ? "" : $" COMMENT '{c.Comment}' ";
            string defaultValue = GetDefaultValue(c);

            sb.AppendFormat("CHANGE COLUMN `{0}` `{0}` {1} {2}{5}{6}{3}{4};", c.Name,
                GetFullType(c),
                c.NullAble ? "NULL" : "NOT NULL",
                c.IsAutoIncrement ? " AUTO_INCREMENT" : "",
                after,
                comment,
                defaultValue);
            return sb.ToString();
        }


        private string GetDefaultValue(Column c)
        {
            var defaultValue = c.DefaultValue;
            if (defaultValue.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                defaultValue = "";
            }

            if (defaultValue.IsNullOrEmpty())
            {
                if (!c.IsAutoIncrement)
                {
                    defaultValue = GetDefaultValue(c.DataType);
                }
            }
            if (defaultValue.IsNotNullOrEmpty())
            {
                defaultValue = $" DEFAULT {defaultValue}";
            }

            return defaultValue;
        }

        public string CreateAddColumn(string tableName, Column c, string afterName = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ALTER TABLE `{tableName}`");
            var after = afterName.IsNullOrEmpty() ? " FIRST" : $" AFTER `{afterName}`";
            var comment = c.Comment.IsNullOrEmpty() ? "" : $" COMMENT '{c.Comment}' ";
            string defaultValue = GetDefaultValue(c);

            sb.AppendFormat("ADD COLUMN `{0}` {1} {2}{5}{6}{3}{4};", c.Name,
                GetFullType(c),
                c.NullAble ? "NULL" : "NOT NULL", c.IsAutoIncrement ? " AUTO_INCREMENT" : "",
                after,
                comment,
                defaultValue);
            return sb.ToString();
        }

        public override string CreateAlterTable(TableSchema t, TableSchema old)
        {
            var columns = new List<Column>();
            foreach (var c in t.Columns)
            {
                var oldColumn = old.Columns.FirstOrDefault(f => f.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                if (oldColumn == null)
                {
                    columns.Add(c);
                    continue;
                }
                if (c.ColumnType.Equals(oldColumn.ColumnType, StringComparison.OrdinalIgnoreCase)
                    && (c.Length == 0 || c.Length == oldColumn.Length))
                {
                    continue;
                }
                columns.Add(c);
            }

            if (columns.Count == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ALTER TABLE `{t.Name}`\r\n");
            var first = true;
            foreach (var c in columns)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(",\r\n");
                }
                var i = t.Columns.IndexOf(c);
                var after = "";
                if (i == 0)
                {
                    after = " FIRST";
                }
                else
                {
                    after = $" AFTER `{t.Columns[i - 1].Name}`";
                }

                var modify = "";
                if (old.Columns.FirstOrDefault(f => f.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase)) != null)
                {
                    modify = $"CHANGE COLUMN `{c.Name}` `{c.Name}`";
                }
                else
                {
                    modify = $"ADD COLUMN `{c.Name}`";
                }

                var nullAble = c.NullAble ? "NULL" : "NOT NULL";
                var auto = c.IsAutoIncrement ? " AUTO_INCREMENT" : "";

                sb.Append($"{modify} {GetFullType(c)} {nullAble} {auto} {after}");
            }
            sb.Append(";");
            sb.AppendLine("");
            return sb.ToString();
        }

        private static string GetFullType(Column c)
        {
            var type = c.ColumnType.ToUpper();
            switch (c.ColumnType.ToLower())
            {
                case "int":
                    return $"{type}(11)";
                case "bigint":
                    return $"{type}(20)";
                case "varchar":
                case "char":
                case "binary":
                case "varbinary":
                    return $"{type}({c.Length})";
                default:
                    break;
            }
            return type;
        }

        public void SetDefaultValue(string dataType, string defaultValue)
        {
            _ColumnDefaultValues[dataType.ToLower()] = defaultValue;
        }

        public string GetDefaultValue(string dataType)
        {
            if (_ColumnDefaultValues.TryGetValue(dataType.ToLower(), out string value))
            {
                return value;
            }
            return "";
        }
    }
    #endregion
}
