using System;
using System.Collections.Generic;
using System.Text;

namespace AiCodo
{
    public interface IErrorCodes
    {
        IErrorCodes Set(string code, string message);

        string GetErrorMessage(string code, params object[] args);
    }
}
