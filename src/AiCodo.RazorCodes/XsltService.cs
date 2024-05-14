using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Xsl;
using System.Xml;

namespace AiCodo.Codes
{
    public class XsltService
    {
        public static void RunCmd(DynamicEntity args)
        {
            var fileName = args.GetString("filename", "");
            if (fileName.IsNullOrEmpty())
            {
                throw new Exception($"必须参数[fileName]");
            }
            var xmlFile = args.GetString("xmlfile", "");
            if (xmlFile.IsNullOrEmpty())
            {
                throw new Exception($"必须参数[xmlFile]");
            }
            var xsltFile = args.GetString("xsltfile", "");
            if (xsltFile.IsNullOrEmpty())
            {
                throw new Exception($"必须参数[xsltFile]");
            }
            XsltConvert(xmlFile, xsltFile, fileName);
        }

        public static void XsltConvert(string xmlFile, string xsltFile,string fileName)
        {
            var xmldoc=new XmlDocument();
            xmldoc.Load(xmlFile);
            XsltConvert(xmldoc, xsltFile).WriteTo(fileName);
        }

        public static string XsltConvert(string xmlFile, string xsltFile)
        {
            var xmldoc=new XmlDocument();
            xmldoc.Load(xmlFile);
            return XsltConvert(xmldoc, xsltFile);
        }

        public static string XsltConvert(XmlDocument doc, string xsltFile)
        {
            //XslTransform xslt = new XslTransform();
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(xsltFile);
            var sw = new StringWriter();
            xslt.Transform(doc.CreateNavigator(), null, sw);
            return sw.ToString();
        }
    }
}
