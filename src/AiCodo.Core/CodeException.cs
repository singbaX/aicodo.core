using System;

namespace AiCodo
{
    public class CodeException : Exception
    {
        public string ErrorCode { get; private set; }

        public CodeException()
        {

        }

        public CodeException(string code, string message) : base(message)
        {
            ErrorCode = code;
        }
    }
}
