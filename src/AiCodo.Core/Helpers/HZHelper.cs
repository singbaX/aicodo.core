using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AiCodo
{
    public static class HZHelper
    {
        public static bool IsHZ(this char c)
        {
            return (c >= 0x4e00 && c <= 0x9fff) || (c >= 0x3400 && c <= 0x4dbf);
        }

        public static string ConvertTo(this string sourceText, string langType)
        {

            return sourceText;
        }
    }
}
