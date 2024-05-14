// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo;
using AiCodo.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AiCodo.Codes
{
    public static class CmdHelper
    {
        static string _LastError = "";
        static Dictionary<string, Action<string[]>> _Methods = new Dictionary<string, Action<string[]>>();
        public static void Init(string configRoot, string codeRoot)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


            Console.WriteLine(@"欢迎使用[AiCodo]命令行执行模式，如果您有任何疑问或建议，请联系作者(SingbaX)：singba@163.com");
            Console.WriteLine(@"AiCodo命令行执行模式，支持以下命令：");
            Console.WriteLine(@"刷新表：reloadtables");
            Console.WriteLine(@"列出所有表：listtables");
            Console.WriteLine(@"基于表结果生成代码：codetable {表名} {模板名} {文件名}");
            Console.WriteLine(@"基于表命令生成代码：codesql {表名} {模板名} {文件名}");
            Console.WriteLine(@"基于配置文件代码：code {模板名称} {文件名}");
            Console.WriteLine(@"开始直接支持CodeService的命令，命令codecmd
导出excel表结构：codecmd export filename schema.xlsx 
用xslt样式转换xml：codecmd xslt xmlfile a.xml xsltfile x.xslt filename newfile.xx
让写代码的速度︿(￣︶￣)︿起来
");

            RazorHelper.AddTemplateAssembly(typeof(INotifyCollectionChanged).Assembly);
            RazorHelper.AddTemplateAssembly(typeof(List<>).Assembly);
            RazorHelper.AddTemplateAssembly(typeof(System.Collections.Generic.Queue<>).Assembly);
            RazorHelper.AddTemplateAssembly(typeof(System.Xml.XmlDocument).Assembly);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName()?.Name;
                if (name.IsNotNullOrEmpty() && name.StartsWith("AiCodo"))
                {
                    RazorHelper.AddTemplateAssembly(asm);
                }
            }


            if (System.IO.Directory.Exists(configRoot))
            {
                ApplicationConfig.LocalConfigFolder = configRoot;
            }

            codeRoot = codeRoot.FixedAppBasePath();
            CodeService.CodeRoot = codeRoot;
            if (!System.IO.Directory.Exists(codeRoot))
            {
                System.IO.Directory.CreateDirectory(codeRoot);
            }

            Console.WriteLine($"配置路径：{ApplicationConfig.LocalConfigFolder}");
            Console.WriteLine($"代码路径：{CodeService.CodeRoot}");

            //AiCodo:Set db provider
            DbProviderFactories.SetFactory("mysql", MySqlProvider.Instance);
            Console.WriteLine("添加mysql数据库依赖");

            CodeCommands.GetMethods().ForEach(item => _Methods[item.Key] = item.Value);

            Console.WriteLine("代码模板");
            var codeSettingFile = "CodeSetting.xml".FixedAppConfigPath();
            if (codeSettingFile.IsFileNotExists())
            {
                new CodeSetting().SaveXDoc(codeSettingFile);
                "APP".Log($"代码设置文件不存在，创建默认文件：{codeSettingFile}");
            }
        }

        public static void StartCommandLine(string configRoot, string codeRoot)
        {
            CodeSetting.Current.Templates.ForEach(t =>
            {
                Console.WriteLine($"[{t.Name}] - [{t.FileName}]");
            });

            _Methods.Add("showerror", (arr) => Console.WriteLine(_LastError));

            var configWatcher = new ConfigFileWatchCommands();
            configWatcher.StartWatch()
                .AddAction("sql.xml", f => SqlData.ReloadCurrent())
                .AddAction("CodeSetting.xml", f => CodeSetting.ReloadCurrent());

            _Methods.Add("watch", wargs =>
            {
                if (wargs.Length > 1)
                {
                    var fileName = wargs[0];
                    configWatcher.AddAction(fileName, file =>
                    {
                        var line = wargs.Skip(1).AggregateStrings(" ");
                        if (line.IsNotEmpty())
                        {
                            Console.WriteLine($"文件[{fileName}]修改，执行命令[{line}]");
                            _LastError = RunCommandLine(_Methods, line);
                        }
                    });
                }
            });

            ReadInput();
        }

        static void ReadInput()
        {
            var line = "";
            while (true)
            {
                Console.Write("请输入命令：");
                line = Console.ReadLine();
                if (line.Length == 0)
                {
                    Console.WriteLine("请问是要关闭程序吗？（y/n）");
                    line = Console.ReadLine();
                    if (line.IsNullOrEmpty() || line.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    continue;
                }

                _LastError = RunCommandLine(_Methods, line);
            }
        }

        static string RunCommandLine(Dictionary<string, Action<string[]>> methods, string line)
        {
            var error = "";
            var name = CodeCommands.ReadCommandName(line, out string[] cmdArgs);
            if (methods.TryGetValue(name.ToLower(), out var action))
            {
                try
                {
                    CodeCommands.IsRunning = true;
                    action(cmdArgs);
                }
                catch (Exception ex)
                {
                    "Program".Log($"执行命令错误：{ex.Message}");
                    error = ex.ToString();
                    var errIndex = error.IndexOf("error: (");
                    if (errIndex > 0)
                    {
                        var endIndex = error.IndexOfAny(new char[] { '\r', '\n' }, errIndex + 1);
                        if (endIndex > 0)
                        {
                            "Program".Log($"执行命令错误：{error.Substring(errIndex, endIndex - errIndex)}");
                        }
                    }
                }
                finally
                {
                    CodeCommands.IsRunning = false;
                }
            }
            else
            {
                //Console.WriteLine($"无效的命令：{name}");
                try
                {
                    RunCommand(line, out string result);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine($"无效的命令：{name}");
                }
            }

            return error;
        }

        static void RunCommand(string commandLine, out string result)
        {
            result = "";
            Process cmd = new Process();
            cmd.StartInfo.WorkingDirectory = ApplicationConfig.BaseDirectory;
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.Arguments = $"/c {commandLine}";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.WaitForExit();
            cmd.StandardInput.Close();

            result = cmd.StandardOutput.ReadToEnd();
            var error = cmd.StandardError.ReadToEnd();
            if (result.IsNotEmpty())
            {
                Console.WriteLine(result);
            }
            if (error.IsNotEmpty())
            {
                Console.WriteLine(error);
            }
            result = $"退出[{cmd.ExitCode}]";
            cmd.Dispose();
        }
    }
}
