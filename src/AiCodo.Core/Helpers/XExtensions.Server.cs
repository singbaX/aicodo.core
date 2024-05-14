// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System.Xml.Xsl;
    using System.Xml;
    using System.IO;
    using System.Xml.Serialization;
    using System.Text;

    public static partial class BaseHelper
    {
        public static void SaveXDoc(this object xobj, string filename)
        {
            int index = filename.LastIndexOf('\\');
            if (index > 0)
            {
                string path = filename.Substring(0, index);
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
            }

            XmlSerializer xs = new XmlSerializer(xobj.GetType());
            Stream stream = System.IO.File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None); 
            var streamWriter = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                
            });
            xs.Serialize(streamWriter, xobj);
            stream.Close();
        }

        public static T LoadXDoc<T>(this string filename)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            object obj = xs.Deserialize(stream);
            stream.Close();
            return (T)obj;
        }

        public static string XslTrans(string sourcefile, string transfile)
        {
            XslCompiledTransform xtrans = new XslCompiledTransform();
            xtrans.Load(transfile);

            XmlDocument newdoc = new XmlDocument();
            newdoc.Load(sourcefile);
            return XslTrans(xtrans, newdoc);
        }

        public static string XslTrans(XslCompiledTransform xtrans, XmlDocument xdoc)
        {
            System.IO.StringWriter result = new System.IO.StringWriter();
            xtrans.Transform(xdoc, null, result);
            return result.ToString();
        }

        public static string GetStringUsingXslStream(this string source, string transtring)
        {
            XslCompiledTransform xtrans = new XslCompiledTransform();
            MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(transtring));
            xtrans.Load(XmlReader.Create(ms));

            XmlDocument newdoc = new XmlDocument();
            newdoc.LoadXml(source);
            return XslTrans(xtrans, newdoc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="transfile"></param>
        /// <returns></returns>
        public static string GetStringUsingXslFile(this string source, string transfile)
        {
            XslCompiledTransform xtrans = new XslCompiledTransform();
            xtrans.Load(transfile);

            XmlDocument newdoc = new XmlDocument();
            newdoc.LoadXml(source);
            return XslTrans(xtrans, newdoc);
        }

        public static StreamReader ReadStream(this string filename)
        {
            StreamReader sr = new StreamReader(filename);
            return sr;
        }

    }
}
