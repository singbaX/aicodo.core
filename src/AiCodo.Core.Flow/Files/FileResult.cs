using System;
using System.Collections.Generic;
using System.Text;

namespace AiCodo.Flow
{
    public class FileResult : IFileResult
    {
        public FileResult(string fileName)
        {
            FileName = fileName;
        }
        public string FileName { get; set; }
    }
}
