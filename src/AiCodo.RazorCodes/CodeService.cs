// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Codes
{
    using AiCodo.Data;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    public class CodeContext<T>
    {
        public SqlCode Sql { get; set; }

        public T Context { get; set; }

        public CodeSetting Setting { get { return CodeSetting.Current; } }
    }

    public static class CodeService
    {
        static Dictionary<string, Action<DynamicEntity>> _Commands = new Dictionary<string, Action<DynamicEntity>>
        {
            {"createmysql",CreateMySqlConfig },
            {"addconn",CreateConnection },
            {"load",LoadSql },
            {"reload",Reload },
            {"merge",Merge },
            {"reloadtables",ReloadTables },
            {"checkchange",CheckChange },
            {"resetsql",ReloadTables },
            {"codetable",CreateTableFile },
            {"codesql",CreateSqlFile },
            {"codesqlall",CreateSqlAllFile },
            {"codeconn",CreateConnFile },
            {"xslt",XsltService.RunCmd },
        };

        static CodeService()
        {
            TemplateRoot = "templates".FixedAppConfigPath();
            CodeRoot = "codes".FixedAppBasePath();
        }

        public static string TemplateRoot { get; set; }

        public static string CodeRoot { get; set; }

        static SqlCode SqlCode { get; set; } = new SqlCode();

        static Action _WatchAction = null;

        public static void RunCodeCommand(CodeCommandItem item, params string[] args)
        {
            var templateFile = Path.Combine(TemplateRoot, item.FileName);
            if (!templateFile.IsFileExists())
            {
                throw new Exception($"模板文件不存在[{item.FileName}]");
            }
            var model = new DynamicEntity();
            var argNames = item.ArgNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < argNames.Length; i++)
            {
                var value = args.Length > i ? args[i] : "";
                model.SetValue(argNames[i], value);
            }

            var code = RunTemplateFile<DynamicEntity>(templateFile, model);
            "Code".Log($"命令[{item.Name}]执行完成：{code}");
        }

        #region 执行命令
        public static bool TryExecuteCommand(string commandName, DynamicEntity args)
        {
            if (_Commands.TryGetValue(commandName.ToLower(), out Action<DynamicEntity> cmd))
            {
                try
                {
                    "Code".Log($"执行命令：{commandName}");
                    cmd(args);
                    if (args.ContainsKey("watch"))
                    {
                        _WatchAction = () => cmd(args);
                        if (SqlCode.Watch())
                        {
                            SqlCode.ConfigFileChanged += SqlCode_ConfigFileChanged;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    "Code".Log($"执行命令[{commandName}]出错：{ex.ToString()}", Category.Exception);
                }
            }
            else
            {
                "Code".Log($"命令[{commandName}]不存在");
            }
            return false;
        }

        private static void SqlCode_ConfigFileChanged(object sender, EventArgs e)
        {
            _WatchAction?.Invoke();
        }
        #endregion

        static void CreateConnection(DynamicEntity args)
        {
            var type = args.GetString("type");
            if (type.IsNullOrEmpty())
            {
                throw new Exception($"参数[type](连接类型)没有指定");
            }
            var p = DbProviderFactories.GetProvider(type);
            if (p == null)
            {
                throw new Exception($"连接类型[{type}]没有注册");
            }
            var connectionString = p.CreateConnectionString(args);
            
        }

        #region 创建配置
        static void CreateMySqlConfig(DynamicEntity args)
        {
            var name = args.GetString("name");
            var server = args.GetString("server", "localhost");
            var port = args.GetInt32("port", 3306);
            var uid = args.GetString("uid", "root");
            var pwd = args.GetString("pwd", "sa123456");
            var database = args.GetString("database", "");
            var charset = args.GetString("charset", "utf8");
            var fileName = args.GetString("filename", "");

            if (database.IsNullOrEmpty())
            {
                throw new Exception($"参数[database](数据库名)没有指定");
            }
            if (name.IsNullOrEmpty())
            {
                name = database;
            }
            string connectionString = CreateMySqlConnectionString(server, port, uid, pwd, database, charset);
            var sql = new SqlData();
            sql.Connections.Add(new SqlConnection
            {
                Name = name,
                ProviderName = "mysql",
                ConnectionString = connectionString
            });
            sql.ReloadTables();
            if (fileName.IsNullOrEmpty())
            {
                fileName = "sql.xml";
            }
            fileName = fileName.FixedAppDataPath();
            sql.SaveXDoc(fileName);
            Console.WriteLine($"配置文件已生成：{fileName}");
        }

        public static string CreateMySqlConnectionString(string server, int port, string uid, string pwd, string database, string charset = "utf8")
        {
            return $"Server={server};Port={port};Database={database};Uid={uid};Pwd={pwd};CharSet={charset};";
        }
        #endregion

        #region 加载配置
        static void Reload(DynamicEntity args)
        {
            if (SqlCode != null)
            {
                SqlCode.Reload();
            }
        }

        static void Merge(DynamicEntity args)
        {
            var fileName = args.GetString("filename");
            fileName = fileName.FixedAppDataPath();
            if (fileName.IsFileNotExists())
            {
                throw new Exception($"文件不存在[{fileName}]");
            }
            SqlCode.Merge(fileName);
        }

        static void LoadSql(DynamicEntity args)
        {
            _LoadSqlCode(args);
        }

        private static void _LoadSqlCode(DynamicEntity args)
        {
            var fileName = args.GetString("filename");
            fileName = fileName.FixedAppDataPath();
            if (fileName.IsFileNotExists())
            {
                throw new Exception($"文件不存在[{fileName}]");
            }
            var sqlcode = new SqlCode(fileName);
            SqlCode = sqlcode;
            "Code".Log($"配置文件已加载[{fileName}]");

            var templateRoot = args.GetString("templateroot");
            if (templateRoot.IsNotEmpty())
            {
                TemplateRoot = templateRoot.FixedAppDataPath();
            }
            else
            {
                TemplateRoot = System.IO.Path.Combine(fileName.GetParentPath(), "templates");
            }
            "Code".Log($"模板路径[{TemplateRoot}]");

            var codeRoot = args.GetString("coderoot");
            if (codeRoot.IsNotEmpty())
            {
                CodeRoot = codeRoot.FixedAppDataPath();
            }
            else
            {
                CodeRoot = System.IO.Path.Combine(fileName.GetParentPath(), "codes");
            }
            "Code".Log($"代码路径[{CodeRoot}]");
        }
        #endregion

        #region 刷新表结构
        static void ReloadTables(DynamicEntity args)
        {
            SqlCode.Config.ReloadTables();
            SqlCode.Config.Save();
        }
        #endregion

        static void CheckChange(DynamicEntity args)
        {
            var fileName = args.GetString("filename", "");
            if (fileName.IsNullOrEmpty())
            {
                throw new Exception($"必须参数[fileName]");
            }
            fileName = fileName.FixedAppBasePath();
            var sql = SqlCode.Config.CheckUpdate();
            sql.WriteTo(fileName);
        }

        //static void ExportXlsx(DynamicEntity args)
        //{
        //    CheckSqlCode();
        //    var fileName = args.GetString("filename", "");
        //    if (fileName.IsNullOrEmpty())
        //    {
        //        throw new Exception($"必须参数[fileName]");
        //    }

        //    fileName = fileName.FixedAppBasePath();
        //    using (var workbook = new XLWorkbook())
        //    {
        //        var worksheet = workbook.Worksheets.Add("Schema");
        //        var row = 0;
        //        var tableRow = 0;
        //        var fieldIndex = 0;

        //        foreach (var table in SqlCode.GetTables())
        //        {
        //            row++;
        //            tableRow = row;
        //            fieldIndex = 0;

        //            worksheet.Range(worksheet.Cell($"A{tableRow}").Address, worksheet.Cell($"E{tableRow}").Address)
        //                .Merge();
        //            worksheet.Cell($"A{tableRow}").Value = $"{table.Name}";

        //            row++;
        //            worksheet.Cell($"A{row}").Value = $"序号";
        //            worksheet.Cell($"B{row}").Value = $"名称";
        //            worksheet.Cell($"C{row}").Value = $"类型";
        //            worksheet.Cell($"D{row}").Value = $"主键";
        //            worksheet.Cell($"E{row}").Value = $"说明";

        //            var rangeHeader = worksheet.Range(worksheet.Cell($"A{row}").Address, worksheet.Cell($"E{row}").Address);
        //            rangeHeader.Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        //            rangeHeader.Style.Font.FontColor = XLColor.White;
        //            rangeHeader.Style.Font.Bold = true;

        //            foreach (var col in table.Columns.OrderBy(c => c.ColumnOrdinal))
        //            {
        //                row++;
        //                worksheet.Cell($"A{row}").Value = $"{col.ColumnOrdinal}";
        //                worksheet.Cell($"B{row}").Value = $"{col.Name}";
        //                worksheet.Cell($"C{row}").Value = $"{col.ColumnType}";
        //                worksheet.Cell($"D{row}").Value = $"{(col.IsKey ? "Y" : "")}";
        //                worksheet.Cell($"E{row}").Value = $"{col.Comment}";
        //            }

        //            var rangeTable = worksheet.Range(worksheet.Cell($"A{tableRow}").Address, worksheet.Cell($"E{row}").Address);
        //            rangeTable.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        //            rangeTable.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        //            worksheet.NamedRanges.Add(table.CodeName, rangeTable);

        //            row++;
        //        }
        //        //worksheet.Cell("A1").Value = "Hello World!";
        //        //worksheet.Cell("A2").FormulaA1 = "=MID(A1, 7, 5)";
        //        worksheet.Columns("A", "E").AdjustToContents();
        //        workbook.SaveAs(fileName);
        //    }
        //    "Codo".Log($"文件已导出[{fileName}]");
        //}


        static void CheckSqlCode()
        {
            if (SqlCode == null)
            {
                throw new Exception($"配置文件未加载");
            }
        }

        static void ResetSql(DynamicEntity args)
        {
            var tableName = args.GetString("table");
            if (tableName.IsNullOrEmpty())
            {
                throw new Exception($"缺少表名参数[table]");
            }

            var table = SqlCode.GetTable(tableName);
            if (table == null)
            {
                throw new Exception($"表配置不存在[{tableName}]");
            }
            var provider = table.GetProvider();

        }
        static void CreateConnFile(DynamicEntity args)
        {
            var name = args.GetString("name");
            if (name.IsNullOrEmpty())
            {
                throw new Exception("[name]名称不能为空");
            }
            else
            {
                var model = SqlCode.GetConn(name);
                if (model == null)
                {
                    throw new Exception($"数据库连接不存在[{name}]");
                }
                CreateFileWithTemplate(model, args);
            }
        }

        /// <summary>
        /// 生成代码类固定参数
        /// template
        /// </summary>
        /// <param name="args"></param>
        static void CreateTableFile(DynamicEntity args)
        {
            var tableName = args.GetString("name");
            if (tableName.IsNullOrEmpty())
            {
                foreach (var table in SqlCode.GetTables())
                {
                    CreateFileWithTemplate(table, args);
                }
            }
            else
            {
                var table = SqlCode.GetTable(tableName);
                if (table == null)
                {
                    throw new Exception($"表配置不存在[{tableName}]");
                }
                CreateFileWithTemplate(table, args);
            }
        }

        static void CreateSqlAllFile(DynamicEntity args)
        {
            var tables = SqlCode.GetSqlTables();
            CreateFileWithTemplate(tables, args);
        }

        static void CreateSqlFile(DynamicEntity args)
        {
            var tableName = args.GetString("name");
            if (tableName.IsNullOrEmpty())
            {
                throw new Exception($"缺少表名参数[table]");
            }

            var table = SqlCode.GetSqlTable(tableName);
            if (table == null)
            {
                throw new Exception($"表配置不存在[{tableName}]");
            }
            CreateFileWithTemplate(table, args);
        }

        public static void CreateFileWithTemplate<T>(T model, string templateName, string codeName)
        {
            var templateFile = GetTemplateFileName(templateName);
            if (templateFile.IsFileNotExists())
            {
                throw new Exception($"模板文件不存在[{templateFile}]");
            }
            var codeFile = GetCodeFileName(codeName);
            var code = RunTemplateFile<T>(templateFile, model);
            code.WriteTo(codeFile);
            "Code".Log($"生成文件[{codeFile}]");
        }

        public static string CreateCodeWithTemplate<T>(T model, string templateName)
        {
            var templateFile = GetTemplateFileName(templateName);
            if (templateFile.IsFileNotExists())
            {
                throw new Exception($"模板文件不存在[{templateFile}]");
            }
            var code = RunTemplateFile<T>(templateFile, model);
            return code;
        }

        static void CreateFileWithTemplate<T>(T model, DynamicEntity args)
        {
            var templateName = args.GetString("template");
            if (templateName.IsNullOrEmpty())
            {
                throw new Exception($"缺少模板参数[template]");
            }

            var codeName = args.GetString("code");
            if (templateName.IsNullOrEmpty())
            {
                throw new Exception($"缺少代码文件名参数[code]");
            }

            var templateFile = GetTemplateFileName(templateName);
            if (templateFile.IsFileNotExists())
            {
                throw new Exception($"模板文件不存在[{templateFile}]");
            }
            var codeFile = GetCodeFileName(codeName);
            var code = RunTemplateFile<T>(templateFile, model);
            code.WriteTo(codeFile);
            "Code".Log($"生成文件[{codeFile}]");
        }

        static string RunTemplateFile<T>(string fileName, T context)
        {
            return RazorHelper.RunTemplateFile<CodeContext<T>>(fileName,
                new CodeContext<T>
                {
                    Context = context,
                    Sql = SqlCode
                });
        }

        static string GetTemplateFileName(string fileName)
        {
            if (fileName.IsFileExists())
            {
                return fileName;
            }
            var item = CodeSetting.Current.Templates.FirstOrDefault(t => t.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                if (item.FileName.IsFileExists())
                {
                    return item.FileName;
                }
                var templateFile = Path.Combine(TemplateRoot, item.FileName);
                if (templateFile.IsFileExists())
                {
                    return templateFile;
                }
            }
            return Path.Combine(TemplateRoot, fileName);
        }

        public static string GetCodeFileName(string fileName)
        {
            if (fileName.IndexOf(':') > 0 || fileName.StartsWith("/"))
            {
                return fileName;
            }
            return Path.Combine(CodeRoot, fileName);
        }

        //public static void BeginCreateCodeFiles(SqlData sqlData, CodeSetting setting)
        //{
        //    //var setting = new CodeSetting
        //    //{
        //    //    Namespace = "Web",
        //    //    Output = "..\\..\\g"
        //    //};

        //    //var sqlData = SqlData.Load("..\\..\\Configs\\sqldata.xml");

        //    CreateDbContextFiles(sqlData, setting);
        //    CreateSqlCommands(sqlData, setting);
        //    CreateEntityFiles(sqlData, setting);
        //    CreateDataSetFiles(sqlData, setting);

        //    CreateTableDocFiles(sqlData, setting);
        //}

        //internal static SqlData Load(string file)
        //{
        //    var sqlData = SqlData.Load(file);
        //    return sqlData;
        //}

        //internal static SqlData Reload(string file)
        //{
        //    var sqlData = SqlData.Load(file);
        //    sqlData.ReloadTables();
        //    return sqlData;
        //}

        //#region 代码文件
        //private static void CreateDbContextFiles(SqlData sqlData, CodeSetting setting)
        //{
        //    var page = new SqlConnectionsFile
        //    {
        //        Config = sqlData,
        //        Setting = setting
        //    };
        //    var content = page.TransformText();
        //    var fileName = "Connections.cs";
        //    content.WriteTo(System.IO.Path.Combine(setting.Output, fileName));
        //    Console.WriteLine($"Connection文件:{fileName}");
        //}

        //private static void CreateDataSetFiles(SqlData sqlData, CodeSetting setting)
        //{
        //    Func<string, string> getFile = (name) =>
        //    {
        //        if (string.IsNullOrEmpty(setting.DataSetClassNameFormat))
        //        {
        //            return $"{name}.cs";
        //        }
        //        else
        //        {
        //            return $"{string.Format(setting.DataSetClassNameFormat, name)}.cs";
        //        }
        //    };


        //    sqlData.Groups.ForEach(c =>
        //    {
        //        c.Items.ForEach(t =>
        //        {
        //            var page = new DataSet
        //            {
        //                Config = sqlData,
        //                Table =t,
        //                Setting =setting,
        //                Types = Types
        //            };
        //            if (!string.IsNullOrEmpty(setting.SystemParameterTypes))
        //            {
        //                var systemParameterTypes = setting.SystemParameterTypes.Split(new char[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
        //                    .Select(p =>
        //                    {
        //                        var kv = p.Split(new char[] {':', '='}, StringSplitOptions.RemoveEmptyEntries);
        //                        return new KeyValuePair<string, string>(kv[0], kv[1]);
        //                    }).ToDictionary(d => d.Key, d => d.Value);
        //                page.SystemParameterTypes = systemParameterTypes;
        //            }

        //            var table = sqlData.GetTable(t.TableName, t.ConnectionName); 
        //            var content = page.TransformText();
        //            var fileName = getFile(table.DataSetName);
        //            content.WriteTo(System.IO.Path.Combine(setting.Output, fileName));
        //            Console.WriteLine($"实体类文件:{fileName}");
        //        });
        //    });
        //}

        //private static void CreateSqlCommands(SqlData sqlData, CodeSetting setting)
        //{
        //    var page = new SqlCommands(sqlData, setting);
        //    var content = page.TransformText();
        //    var fileName = "SqlCommands.cs";
        //    content.WriteTo(System.IO.Path.Combine(setting.Output, fileName));
        //    Console.WriteLine($"SQL文件:{fileName}");
        //}

        //private static void CreateEntityFiles(SqlData sqlData, CodeSetting setting)
        //{
        //    Func<string, string> getFile = (name) =>
        //     {
        //         if (string.IsNullOrEmpty(setting.EntityClassNameFormat))
        //         {
        //             return $"{name}.cs";
        //         }
        //         else
        //         {
        //             return $"{string.Format(setting.EntityClassNameFormat, name)}.cs";
        //         }
        //     };
        //    sqlData.Connections.ForEach(c =>
        //    {
        //        c.Tables.ForEach(t =>
        //        {
        //            var page = new SqlEntity(setting);
        //            page.Table = t;
        //            var content = page.TransformText();
        //            var fileName = getFile(t.CodeName);
        //            content.WriteTo(System.IO.Path.Combine(setting.Output, fileName));
        //            Console.WriteLine($"实体类文件:{fileName}");
        //        });
        //    });
        //}
        //#endregion


        //private static void CreateTableDocFiles(SqlData sqlData, CodeSetting setting)
        //{
        //    Func<string, string> getFile = (name) =>
        //     {
        //         if (string.IsNullOrEmpty(setting.EntityClassNameFormat))
        //         {
        //             return $"{name}.md";
        //         }
        //         else
        //         {
        //             return $"{string.Format(setting.EntityClassNameFormat, name)}.md";
        //         }
        //     };
        //    sqlData.Connections.ForEach(c =>
        //    {
        //        c.Tables.ForEach(t =>
        //        {
        //            var page = new TableSchemaDoc { Table = t, Setting = setting}; 
        //            var content = page.TransformText();
        //            var fileName = getFile(t.CodeName);
        //            content.WriteTo(System.IO.Path.Combine(setting.Output, fileName));
        //            Console.WriteLine($"表说明文件:{fileName}");
        //        });
        //    });
        //}

    }
}
