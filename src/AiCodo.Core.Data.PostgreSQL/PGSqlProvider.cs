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
    #region PGSql
    public class PGSqlProvider : DbProvider, IAlterTable, IColumnDefaultValue, ICreateMapper
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

        const string PGSql_CheckTable = @"SELECT EXISTS(SELECT f.* FROM information_schema.tables f
WHERE f.table_catalog='{0}' AND f.table_name='{1}')";

        public static string PGSql_GetTables { get; set; } =
 @"select t.table_name from information_schema.tables T
WHERE T.table_catalog =@DBName and T.table_schema = 'public'";

        public static string PGSql_GetTableSchema { get; set; } =
 @"select C.TABLE_NAME,
        C.COLUMN_NAME,
        C.ORDINAL_POSITION,
        C.udt_name AS COLUMN_TYPE,
        C.DATA_TYPE AS DATA_TYPE,
        COALESCE(C.CHARACTER_MAXIMUM_LENGTH,0) AS DATA_LENGTH,
        COALESCE(C.COLUMN_DEFAULT, '') AS COLUMN_DEFAULT,
        COALESCE(d.description, '') AS COLUMN_COMMENT,
        C.is_identity AS IS_IDENTITY,
        COALESCE(c.identity_start ,'0') AS IDENTITY_SEED,
        COALESCE(c.identity_increment ,'0') AS IDENTITY_INCR,
        CASE C.IS_NULLABLE WHEN 'YES' THEN 'Y' ELSE '' END AS IS_NULLABLE,
        CASE COALESCE(PK.CONSTRAINT_NAME,'') WHEN '' THEN '' ELSE 'Y' END AS IS_PRIMARY_KEY,
        COALESCE(PK.CONSTRAINT_NAME,'') AS PK_NAME,
        CASE COALESCE(FK.CONSTRAINT_NAME,'') WHEN '' THEN '' ELSE 'Y' END AS IS_FOREIGN_KEY,
        COALESCE(FK.CONSTRAINT_NAME,'') AS FK_NAME,
        COALESCE(FK.foreign_column_name, '') AS FOREIGN_KEY,
         COALESCE(FK.foreign_table_name, '') AS FOREIGN_TABLE
        from information_schema.columns C
        JOIN pg_class s ON s.relname = c.table_name
        LEFT JOIN pg_description d ON d.objoid = s.oid AND d.objsubid = c.ordinal_position
        LEFT OUTER JOIN
        (SELECT* FROM information_schema.key_column_usage WHERE constraint_catalog =@DBName and table_schema ='public' and constraint_name like '%_pk' AND TABLE_NAME = @TableName) PK
         ON PK.TABLE_NAME= C.TABLE_NAME AND PK.COLUMN_NAME = C.COLUMN_NAME
         LEFT OUTER JOIN
         (SELECT
     tc.constraint_name, tc.table_name, kcu.column_name, 
     ccu.table_name AS foreign_table_name,
     ccu.column_name AS foreign_column_name,
     tc.is_deferrable,tc.initially_deferred
 FROM 
     information_schema.table_constraints AS tc 
     JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name
     JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name
 WHERE constraint_type = 'FOREIGN KEY' and kcu.constraint_catalog =@DBName and tc.table_name =  @TableName) FK
     ON FK.TABLE_NAME= C.TABLE_NAME AND FK.COLUMN_NAME = C.COLUMN_NAME
     where c.table_catalog =@DBName AND c.TABLE_NAME = @TableName";
        #endregion

        private static PGSqlProvider _Instance = new PGSqlProvider();
        public static PGSqlProvider Instance
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
            string connectionString = CreatePGSqlConnectionString(server, port, uid, pwd, database, charset);
            return connectionString;
        }

        public static string CreatePGSqlConnectionString(string server, int port, string uid, string pwd, string database,
            string charset = "utf8", string sslMode = "none")
        {
            return $"Server={server};Port={port};Database={database};Uid={uid};Pwd={pwd};CharSet={charset};SslMode={sslMode};";
        }

        protected override DbProviderFactory GetFactory()
        {
            return Npgsql.NpgsqlFactory.Instance;
        }

        public override string GetLastIdentity(string tableName = "", string colName = "")
        {
            return $"RETURNING {colName}";
        }

        public override string GetName(string columnName)
        {
            return $"{columnName}";
        }

        public override string GetParameter(string columnName)
        {
            return $"@{columnName}";
        }

        public override TableSchema GetTableSchema(DbConnection db, string tableName)
        {
            var sql = PGSql_GetTableSchema;
            var items = db.ExecuteQuery<DynamicEntity>(sql, "DBName", db.Database, "TableName", tableName)
                .Select(q =>
                {
                    var col = new Column
                    {
                        Name = q.GetString("column_name"),
                        ColumnOrdinal = q.GetInt32("ORDINAL_POSITION".ToLower(), 0),
                        ColumnType = q.GetString("COLUMN_TYPE".ToLower()),
                        DataType = q.GetString("DATA_TYPE".ToLower(), ""),
                        Length = q.GetInt64("DATA_LENGTH".ToLower(), 0),
                        IsAutoIncrement = q.GetString("IS_IDENTITY".ToLower(), "").Equals("YES"),
                        DefaultValue = q.GetString("COLUMN_DEFAULT".ToLower(), ""),
                        NullAble = q.GetString("IS_NULLABLE".ToLower(), "").Equals("Y"),
                        IsKey = q.GetString("IS_PRIMARY_KEY".ToLower(), "").Equals("Y"),
                        IsReadOnly = q.GetString("IS_IDENTITY".ToLower(), "").Equals("Y")
                    };
                    col.PropertyName = col.Name.ToFormatedName();
                    col.ResetComment(q.GetString("COLUMN_COMMENT".ToLower()));
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
            var sql = PGSql_GetTables;
            var items = db.ExecuteQuery<DynamicEntity>(sql, "DBName", db.Database);
            return items.Select(s => s.GetString("table_name"));
        }

        public override string ExistsTable(string dbName, string tableName)
        {
            return string.Format(PGSql_CheckTable, dbName, tableName);
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

        public override string CreateInsert(TableSchema table, bool reload = false, bool reloadAutoColumnOnly = false)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("INSERT INTO {0}\r\n(", GetName(table.Name));
            IEnumerable<string> names = GetInsertFieldNames(table);
            AppendNames(stringBuilder, names);
            stringBuilder.Append(")");
            stringBuilder.Append("\r\nVALUES(");
            AppendParameters(stringBuilder, names);
            stringBuilder.Append(")");
            if (reload)
            {
                stringBuilder.Append("\r\n");
                Column autoIncrementColumn = table.GetAutoIncrementColumn();
                if (autoIncrementColumn != null)
                {
                    if (reloadAutoColumnOnly)
                    {
                        string value = $"{GetLastIdentity(table.Name, autoIncrementColumn.Name)} AS {GetName(autoIncrementColumn.Name)};";
                        stringBuilder.Append(value);
                    }
                    else
                    {
                        string where = $"{GetName(autoIncrementColumn.Name)}={GetLastIdentity(table.Name, autoIncrementColumn.Name)}";
                        stringBuilder.Append(CreateSelect(table, where));
                    }
                }
                else
                {
                    stringBuilder.Append(CreateSelectByKeys(table));
                }
            }

            return stringBuilder.ToString();
        }

        private static IEnumerable<string> GetInsertFieldNames(TableSchema table)
        {
            return table.Columns.Where(c => !c.IsAutoIncrement).Select(c => c.Name);
        }
        private static IEnumerable<string> GetAllFieldNames(TableSchema table)
        {
            return table.Columns.Select(c => c.Name);
        }

        public void CreateMapperItems(SqlTableGroup tableGroup, TableSchema table)
        {
            var hasAutokey = table.HasAutoIncrementColumn();
            var keys = table.Key.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (keys.Length == 0)
            {
                return;
            }

            CheckMapperItem(tableGroup, "InsertFields", (s) => s.AddText(GetInsertFieldNames(table).Select(f => GetName(f)).AggregateStrings()));
            CheckMapperItem(tableGroup, "InsertParameters", (s) => s.AddText(GetInsertFieldNames(table).Select(f => GetParameter(f)).AggregateStrings()));
            CheckMapperItem(tableGroup, "AllFields", (s) => s.AddText(GetAllFieldNames(table).Select(f => GetName(f)).AggregateStrings()));
            CheckMapperItem(tableGroup, "SelectAllFields", (s) => s.AddText("SELECT ").AddRef("AllFields").AddText($"FROM {GetName(table.Name)}"));
            CheckMapperItem(tableGroup, "FilterPrimaryKey", (s) => s.AddText("\r\nWHERE "+keys.Select(f => $"{GetName(f)}={GetParameter(f)}").AggregateStrings(" AND ")));

            CheckMapperSqlItem(tableGroup, "Insert",SqlType.Scalar, (s) =>
            {
                s.Mapper.AddText($"INSERT INTO {GetName(table.Name)} (")
                    .AddRef("InsertFields")
                    .AddText(")\r\nVALUES(")
                    .AddRef("InsertParameters")
                    .AddText(");");
                if (hasAutokey)
                {
                    var autoColumn = table.GetAutoIncrementColumn();
                    if (autoColumn != null)
                    {
                        var lastID = GetLastIdentity(table.Name, autoColumn.Name);
                        s.Mapper.AddText($"\r\n{lastID}");
                    }
                }
            });

            CheckMapperSqlItem(tableGroup, "Delete",SqlType.Execute, (s) =>
            {
                s.Mapper.AddText($"DELETE FROM {GetName(table.Name)} ")
                    .AddRef("FilterPrimaryKey");
            });

            CheckMapperSqlItem(tableGroup, "Update",SqlType.Execute, (s) =>
            {
                s.Mapper.AddText($"UPDATE {GetName(table.Name)} SET ")
                    .AddText(GetInsertFieldNames(table).Select(c => $"{GetName(c)}={GetParameter(c)}").AggregateStrings(",\r\n"))
                    .AddRef("FilterPrimaryKey");
            });

            CheckMapperSqlItem(tableGroup, "SelectAll", SqlType.Query, (s) =>
            {
                s.Mapper.AddRef("SelectAllFields"); 
            });

            CheckMapperSqlItem(tableGroup, "SelectByKeys", SqlType.Query, (s) =>
            {
                s.Mapper.AddRef("SelectAllFields")
                    .AddRef("FilterPrimaryKey");
            });

        }

        private static void CheckMapperSqlItem(SqlTableGroup table, string sqlname,SqlType Type, Action<SqlItem> reset)
        {
            var sql = table.Items.FirstOrDefault(f => f.Equals(sqlname));
            if (sql == null)
            {
                sql = new SqlItem
                {
                    Name = sqlname,
                    SqlType = Type,
                    Mapper = new SqlMapper()
                };
                reset?.Invoke(sql);
                table.Items.Add(sql);
            }
        }

        private static void CheckMapperItem(SqlTableGroup table, string sqlname, Action<SqlMapper> reset)
        {
            var sql = table.Mappers.FirstOrDefault(f => f.Equals(sqlname));
            if (sql == null)
            {
                sql = new SqlMapper
                {
                    Name = sqlname
                };
                reset?.Invoke(sql);
                table.Mappers.Add(sql);
            }
        }
    }
    #endregion
}
