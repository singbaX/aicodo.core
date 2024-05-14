// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo.Data;
using RazorEngineCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AiCodo.Codes
{
    class CodeCompliledItem
    {
        public string Key { get; set; }

        public string FileName { get; set; }

        public DateTime ComplileTime { get; set; }

        //ITemplateRunner<T>
        public object Runner { get; set; }
    }

    public static class RazorHelper
    {
        static Dictionary<string, CodeCompliledItem> _Templates
            = new Dictionary<string, CodeCompliledItem>();

        static List<Assembly> _TemplateAssemblies = new List<Assembly>
        {
            typeof(System.Linq.Enumerable).Assembly,
            typeof(System.IO.File).Assembly,
            typeof(ObjectConverters).Assembly,
            typeof(SqlData).Assembly,
        };

        public static void AddTemplateAssembly(Assembly asm)
        {
            if (_TemplateAssemblies.Contains(asm)) return;
            _TemplateAssemblies.Add(asm);
        }

        public static string RunTemplateFile<T>(this string fileName, T model, params object[] viewBagParameters)
        {
            if (!fileName.IsFileExists())
            {
                throw new Exception($"文件不存在[{fileName}]");
            }

            if (model == null)
            {
                throw new Exception($"文件模型不能为空[{fileName}]");
            }

            var info = new FileInfo(fileName);
            var key = fileName.ToLower();
            if (_Templates.TryGetValue(key, out CodeCompliledItem item))
            {
                if (item.ComplileTime > info.LastWriteTime)
                {
                    var runner = item.Runner as IRazorEngineCompiledTemplate<RazorEngineTemplateBase<T>>;
                    return runner.Run(c => c.Model = model);
                }
            }

            var fileContent = fileName.ReadFileText();
            var template = CreateTemplate<T>(fileContent);
            item = new CodeCompliledItem
            {
                Key = key,
                FileName = fileName,
                ComplileTime = DateTime.Now,
                Runner = template
            };
            _Templates[key] = item;
            return template.Run(c => c.Model = model);
        }

        public static IRazorEngineCompiledTemplate<RazorEngineTemplateBase<T>> CreateTemplate<T>(string fileContent)
        {
            IRazorEngine razorEngine = new RazorEngine();
            IRazorEngineCompiledTemplate<RazorEngineTemplateBase<T>> compiledTemplate = razorEngine.Compile<RazorEngineTemplateBase<T>>(fileContent, builder =>
            {
                _TemplateAssemblies.ForEach(a =>
                {
                    builder.AddAssemblyReference(a);
                });
                //builder.AddAssemblyReferenceByName("System.Security"); // by name
                //builder.AddAssemblyReference(typeof(System.IO.File)); // by type
                //builder.AddAssemblyReference(Assembly.Load("source")); // by reference
            });
            return compiledTemplate;
        }
    }

    public class HtmlSafeTemplate : RazorEngineTemplateBase
    {
        class RawContent
        {
            public object Value { get; set; }

            public RawContent(object value)
            {
                Value = value;
            }
        }


        public object Raw(object value)
        {
            return new RawContent(value);
        }

        public override void Write(object obj = null)
        {
            object value = obj is RawContent rawContent
                ? rawContent.Value
                : System.Web.HttpUtility.HtmlEncode(obj);

            base.Write(value);
        }

        public override void WriteAttributeValue(string prefix, int prefixOffset, object value, int valueOffset, int valueLength, bool isLiteral)
        {
            value = value is RawContent rawContent
                ? rawContent.Value
                : System.Web.HttpUtility.HtmlAttributeEncode(value?.ToString());

            base.WriteAttributeValue(prefix, prefixOffset, value, valueOffset, valueLength, isLiteral);
        }
    }
}
