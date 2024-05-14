// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AiCodo.Codes
{
    public static class CodeCommands
    {
        static Dictionary<string, Action<string[]>> _Methods = new Dictionary<string, Action<string[]>>
        {
            {"reloadtables",(args)=>ReloadTables() },
            {"listtables",(args)=>ListTables() },
            {"code",(args)=>CreateCodeOfConfig(args[0],args.Length>1?args[1]:"") },
            {"codetable",(args)=>CreateCodeOfTable(args[0],args[1],args[2]) },
            {"codesql",(args)=>CreateCodeOfSqlTable(args[0],args[1],args[2]) },
            {"codecmd",RunCmd},
            {"set" ,(args)=>SetValue(args[0],args[1]) },
            {"runsql" ,(args)=>RunSql(args[0],args.Length>1? args[1]:"") },
        };

        public static bool IsRunning { get; set; } = false;

        static Dictionary<string, Func<string, string>> _SetMethods = new Dictionary<string, Func<string, string>>
        {
            {"configroot",v=> { ApplicationConfig.LocalConfigFolder=v.FixedAppBasePath();return ApplicationConfig.LocalConfigFolder; } },
            {"templateroot",v=> { CodeService.TemplateRoot=v.FixedAppBasePath();return CodeService.TemplateRoot; } },
            {"coderoot",v=> { CodeService.CodeRoot=v.FixedAppBasePath();return CodeService.CodeRoot; } }
        };

        static CodeCommands()
        {

        }

        public static IEnumerable<KeyValuePair<string, Action<string[]>>> GetMethods()
        {
            foreach (var item in _Methods)
            {
                yield return item;
            }
            foreach(var cmd in CodeSetting.Current.Commands)
            {
                yield return new KeyValuePair<string, Action<string[]>>(cmd.Name, (args) =>
                {
                    CodeService.RunCodeCommand(cmd, args);
                });
            }
        }

        static void ReloadTables()
        {
            "CodeCommands".Log($"重新加载表结构");
            SqlData.Current.ReloadTables();
            SqlData.Current.Save();
            "CodeCommands".Log($"表结构重新加载完成");
        }

        static void ListTables()
        {
            SqlData.Current.Connections.ForEach(c =>
            {
                Console.WriteLine($"Connection [{c.Name}]");
                c.Tables.ForEach(t =>
                {
                    Console.WriteLine($"\t[{t.Name}]-[{t.DisplayName}]");
                });
            });
        }

        public static void RunCmd(params string[] args)
        {
            if (args.Length == 0) return;
            var cmdName = args[0];
            var cmdArgs = new DynamicEntity();
            for (int i = 1; i < args.Length - 1; i += 2)
            {
                cmdArgs.Add(args[i], args[i + 1]);
            }
            Codes.CodeService.TryExecuteCommand(cmdName, cmdArgs);
        }

        private static void CreateCodeOfConfig(string templateName, string fileName)
        {
            var model = SqlData.Current;
            if (model == null)
            {
                nameof(CodeCommands).Log($"配置文件不存在", Category.Exception);
                return;
            }

            var codeText = Codes.CodeService.CreateCodeWithTemplate(model, templateName);
            nameof(CodeCommands).Log($"执行模板[{templateName}]\r\n{codeText}");
            if (fileName.IsNotNullOrEmpty())
            {
                var codeFile = CodeService.GetCodeFileName(fileName);
                codeText.WriteTo(codeFile);
                nameof(CodeCommands).Log($"文件已生成[{codeFile}]");
            }
        }

        private static void CreateCodeOfTable(string tableName, string templateName, string fileName)
        {
            var table = SqlData.Current.GetTable(tableName);
            if (table == null)
            {
                nameof(CodeCommands).Log($"Table 不存在", Category.Exception);
                return;
            }
            var codeFile = CodeService.GetCodeFileName(fileName);
            Codes.CodeService.CreateCodeWithTemplate(table, templateName)
                .WriteTo(codeFile);
            nameof(CodeCommands).Log($"文件已生成[{codeFile}]");
        }

        private static void CreateCodeOfSqlTable(string tableName, string templateName, string fileName)
        {
            var table = SqlData.Current.GetSqlTable(tableName);
            if (table == null)
            {
                nameof(CodeCommands).Log($"SqlTable 不存在", Category.Exception);
                return;
            }
            var codeFile = CodeService.GetCodeFileName(fileName);
            Codes.CodeService.CreateCodeWithTemplate(table, templateName)
                .WriteTo(codeFile);
            nameof(CodeCommands).Log($"文件已生成[{codeFile}]");
        }

        private static string SetValue(string key, string value)
        {
            if (_SetMethods.TryGetValue(key.ToLower(), out var method))
            {
                try
                {
                    return method(value);
                }
                catch (Exception ex)
                {
                    return $"设置值出错：[{ex}]";
                }
            }
            return $"无效的设置名称[{key}]";
        }

        public static string ReadCommandName(string line, out string[] args)
        {
            var name = "";
            var listArgs = new List<string>();
            line = line.Trim();
            var index = line.IndexOf(' ');
            if (index > 0)
            {
                name = line.Substring(0, index);
                line = line.Substring(index + 1).Trim();

                while (line.Length > 0)
                {
                    if (line[0] == '"')
                    {
                        index = line.IndexOf('"', 1);
                        if (index > 0)
                        {
                            listArgs.Add(line.Substring(1, index - 1));
                            line = line.Substring(index + 1).Trim();
                            continue;
                        }
                        else
                        {
                            listArgs.Add(line);
                            break;
                        }
                    }
                    else
                    {
                        index = line.IndexOf(' ');
                        if (index > 0)
                        {
                            listArgs.Add(line.Substring(0, index));
                            line = line.Substring(index + 1).Trim();
                            continue;
                        }
                        else
                        {
                            listArgs.Add(line);
                            break;
                        }
                    }
                }

            }
            else
            {
                name = line;
            }

            args = listArgs.ToArray();
            return name;
        }
        #region 运行SQL命令
        private static void RunSql(string sqlName, string values)
        {
            DynamicEntity args = null;
            if (values.EndsWith(".json"))
            {
                var fileName = values.FixedAppBasePath();
                if (!fileName.IsFileExists())
                {
                    nameof(CodeCommands).Log($"参数文件不存在[{fileName}]");
                    return;
                }
                args = fileName.ReadFileText();
            }
            else
            {
                args = values;
            }
            var sqlItem = SqlData.Current.GetSqlItem(sqlName);
            if (sqlItem == null)
            {
                nameof(CodeCommands).Log($"命令配置不存在[{sqlName}]");
                return;
            }
            switch (sqlItem.SqlType)
            {
                case SqlType.Query:
                    RunQuery(sqlItem, args);
                    break;
                case SqlType.QueryOne:
                    RunQueryOne(sqlItem, args);
                    break;
                case SqlType.Execute:
                    RunExecute(sqlItem, args);
                    break;
                case SqlType.Scalar:
                    RunScalar(sqlItem, args);
                    break;
                default:
                    break;
            }
        }

        private static void RunQuery(SqlItem sqlItem, DynamicEntity args)
        {
            var items = sqlItem.ExecuteQuery<DynamicEntity>(args).ToList();
            if (items.Count == 0)
            {
                nameof(CodeCommands).Log($"没有记录");
                return;
            }
            var sb = new StringBuilder();
            var names = items[0].Keys.ToList();
            sb.AppendLine($"{names.AggregateStrings()}");
            foreach (var item in items)
            {
                sb.AppendLine($"{names.Select(name => $"{item[name]}").AggregateStrings()}");
            }
            nameof(CodeCommands).Log($"记录条数[{items.Count}]\r\n{sb.ToString()}");
        }

        private static void RunQueryOne(SqlItem sqlItem, DynamicEntity args)
        {
            var items = sqlItem.ExecuteQuery<DynamicEntity>(args).ToList();
            if (items.Count == 0)
            {
                nameof(CodeCommands).Log($"没有记录");
                return;
            }
            var sb = new StringBuilder();
            var names = items[0].Keys.ToList();
            sb.Append($"{names.AggregateSplitStrings("\t")}\t\n");
            foreach (var item in items)
            {
                foreach (var name in names)
                {
                    sb.Append(item[name]);
                }
                sb.Append("\r\n");
            }
            nameof(CodeCommands).Log($"没有记录\r\n{sb.ToString()}");
        }

        private static void RunExecute(SqlItem sqlItem, DynamicEntity args)
        {
            var count = sqlItem.ExecuteNoneQuery(args);
            nameof(CodeCommands).Log($"影响记录[{count}]");
        }

        private static void RunScalar(SqlItem sqlItem, DynamicEntity args)
        {
            var value = sqlItem.ExecuteScalar(args);
            nameof(CodeCommands).Log($"返回值[{value}]");
        }
        #endregion
    }
}
