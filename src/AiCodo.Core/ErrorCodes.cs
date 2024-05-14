using System.Collections.Generic;

namespace AiCodo
{
    public class ErrorCodes : IErrorCodes
    {
        #region 基本错误类
        /// <summary>
        /// 正常，非错误
        /// </summary>
        public const string Ok = "";

        /// <summary>
        /// 未处理的错误
        /// </summary>
        public const string Unknow = "1";

        /// <summary>
        /// 请求的操作不存在
        /// </summary>
        public const string BadRequest = "2";

        //超时
        public const string TimeoutError = "3";
        #endregion

        #region 属性 Current 
        private static IErrorCodes _Current = null;
        private static object _LoadLock = new object();
        public static IErrorCodes Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_LoadLock)
                    {
                        if (_Current == null)
                        {
                            if (ServiceLocator.Current != null && ServiceLocator.Current.TryGet<IErrorCodes>(out var errors))
                            {
                                _Current = errors;
                            }
                            else
                            {
                                _Current = new ErrorCodes();
                            }
                        }
                    }
                }
                return _Current;
            }
        }
        #endregion

        public ErrorCodes()
        {
            Set(Ok, "");
            Set(Unknow, "未处理的错误：{0}");
            Set(BadRequest, "未定义的请求：{0}");
            Set(TimeoutError, "执行超时：{0}");
        }

        #region 内置实现
        Dictionary<string, string> _ErrorMessages = new Dictionary<string, string>();

        public IErrorCodes Set(string code, string message)
        {
            lock (_ErrorMessages)
            {
                _ErrorMessages[code] = message;
            }
            return this;
        }

        public string GetErrorMessage(string code, params object[] args)
        {
            if (_ErrorMessages.TryGetValue(code, out var message))
            {
                return string.Format(message, args);
            }
            return "";
        }
        #endregion
    }
}
