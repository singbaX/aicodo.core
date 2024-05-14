using System;
using System.Collections.Generic;
using System.Text;

namespace AiCodo
{
    public static partial class ExpressionHelper
    {
        static Dictionary<string, Delegate> _DefaultFunctions = new Dictionary<string, Delegate>
        {
            {"IF",new IF(IF) },
            {"Format",new FormatDelegate(Format) },
            {"FileExists",new BoolDelegate(file=>file.IsFileExists()) },
            {"DirExists",new BoolDelegate(dir=>dir.IsDirectoryExists()) },
        };
    }
}