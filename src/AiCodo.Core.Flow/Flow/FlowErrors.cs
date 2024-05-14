namespace AiCodo.Flow
{
    //流程类错误 100-199
    public static class FlowErrors
    {
        static FlowErrors()
        {
            ErrorCodes.Current
                .Set(ServiceDenied, "服务拒绝访问:{0}")
                .Set(ServiceNotFound, "服务没有定义:{0}")
                .Set(FlowNotFound, "流程没有定义:{0}")
                .Set(FuncNotFound, "函数没有定义:{0}")
                .Set(SqlNotFound, "SQL命令没有定义:{0}")
                .Set(FlowConfigError, "流程配置错误:{0}-{1}")
                .Set(AssertError, "执行流程断言异常：{0}")
                .Set(MethodInnerError, "执行函数异常：函数名{0},错误{1}")
                .Set(MethodParameterError, "函数参数错误：函数名{0},参数{1}");
        }


        //用户无权限访问服务
        public const string ServiceDenied = "100";

        //服务不存在
        public const string ServiceNotFound = "101";

        //流程不存在
        public const string FlowNotFound = "102";

        //方法不存在
        public const string FuncNotFound = "103";

        //SQL不存在
        public const string SqlNotFound = "104";

        //流程配置错误
        public const string FlowConfigError = "110";

        //断言错误
        public const string AssertError = "111";

        //在执行函数内部发送的错误
        public const string MethodInnerError = "112";

        //执行函数参数错误
        public const string MethodParameterError = "113";

        public static CodeException CreateError_ServiceNotFound(string serviceName)
        {
            return new CodeException(ServiceNotFound, ErrorCodes.Current.GetErrorMessage(ServiceNotFound, serviceName));
        }

        public static CodeException CreateError_FlowNotFound(string flowName)
        {
            return new CodeException(FlowNotFound, ErrorCodes.Current.GetErrorMessage(FlowNotFound, flowName));
        }

        public static CodeException CreateError_FuncNotFound(string funcName)
        {
            return new CodeException(FuncNotFound, ErrorCodes.Current.GetErrorMessage(FuncNotFound, funcName));
        }

        public static CodeException CreateError_SqlNotFound(string sqlName)
        {
            return new CodeException(SqlNotFound, ErrorCodes.Current.GetErrorMessage(SqlNotFound, sqlName));
        }

        public static CodeException CreateError_FlowConfigError(string flowName,string error)
        {
            return new CodeException(FlowConfigError, ErrorCodes.Current.GetErrorMessage(FlowConfigError, flowName,error));
        }

        public static CodeException CreateError_AssertError(string error)
        {
            return new CodeException(AssertError, ErrorCodes.Current.GetErrorMessage(AssertError, error));
        }

        public static CodeException CreateError_MethodInnerError(string methodName,string error)
        {
            return new CodeException(MethodInnerError, ErrorCodes.Current.GetErrorMessage(MethodInnerError, methodName, error));
        }

        public static CodeException CreateError_MethodParameterError(string methodName,string error)
        {
            return new CodeException(MethodParameterError, ErrorCodes.Current.GetErrorMessage(MethodParameterError, methodName, error));
        }
    }
}
