using AiCodo.Data;
using AiCodo.Flow.Configs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiCodo.Flow
{
    public static class ServiceFactory
    {
        static Dictionary<string, Func<string, Dictionary<string, object>, Task<ServiceResult>>>
            _Services = new Dictionary<string, Func<string, Dictionary<string, object>, Task<ServiceResult>>>()
            {
                {"sql",RunSql },
                {"func",RunFunc },
                {"flow", RunFlow}
            };

        static ServiceFactory()
        {
            MethodServiceFactory.RegisterService("sql", SqlMethodService.Current);
        }

        public static Task<ServiceResult> Run(string serviceName, Dictionary<string, object> args)
        {
            if (ServiceIndex.Current.TryGetItem(serviceName, out var item))
            {
                return RunItem(item, args);
            }
            throw new Exception($"服务配置不存在[{serviceName}]");
        }

        public static Task<ServiceResult> RunItem(ServiceItemBase item, Dictionary<string, object> args)
        {
            if (_Services.TryGetValue(item.Type.ToLower(), out var func))
            {
                return func(item.ServiceName, args);
            }
            return Task.FromResult(new ServiceResult { Error = $"流程类型错误[{item.ID}-{item.Type}]" });
        }

        private static Task<ServiceResult> Run(FunctionFlowConfig flow, Dictionary<string, object> args)
        {
            var context = new FlowContext(args);
            return context.ExecuteFlow(flow)
                .ContinueWith<ServiceResult>(t =>
                {
                    SqlConnectionContext connContext = null;
                    if (context.TryGetArg(SqlMethodService.ConnectionArgName, out var connItem))
                    {
                        connContext = connItem as SqlConnectionContext;
                    }

                    if (t.Exception != null)
                    {
                        if (connContext != null)
                        {
                            connContext.HasError = true;
                            connContext.End();
                        }

                        if (t.Exception.InnerException is CodeException cex)
                        {
                            return new ServiceResult { Error = cex.Message, ErrorCode = cex.ErrorCode };
                        }

                        if (t.Exception.InnerException is CodeException fex)
                        {
                            return new ServiceResult { Error = fex.Message, ErrorCode = fex.ErrorCode };
                        }
                        var error = t.Exception.InnerException != null ?
                            t.Exception.InnerException.Message :
                            t.Exception.Message;

                        var result = new ServiceResult { Error = error };
                        return result;
                    }
                    else
                    {
                        var data = t.Result;
                        var errorCode = data.GetString("ErrorCode");
                        var errorMessage = data.GetString("ErrorMessage");
                        var hasError = errorCode.IsNotNullOrEmpty() || errorMessage.IsNotNullOrEmpty();

                        if (connContext != null)
                        {
                            connContext.HasError = hasError;
                            connContext.End();
                        }

                        data.Remove("ErrorCode");
                        data.Remove("ErrorMessage");
                        var result = new ServiceResult { Data = t.Result, Error = errorMessage, ErrorCode = errorCode };

                        return result;
                    }
                });
        }

        private static Task<ServiceResult> RunFlow(string serviceName, Dictionary<string, object> args)
        {
            var config = FunctionFlowConfig.Load(serviceName);
            if (config == null)
            {
                return Task.FromResult(new ServiceResult { Error = $"流程配置错误[{serviceName}-加载配置失败]" });
            }
            return Run(config, args);
        }

        private static Task<ServiceResult> RunFunc(string serviceName, Dictionary<string, object> args)
        {
            var result = MethodServiceFactory.Run(serviceName, args);
            if (result.IsOk)
            {
                if (result.TryGetValue("Result", out var data))
                {
                    return Task.FromResult(new ServiceResult { Data = data });
                }
            }
            return Task.FromResult(new ServiceResult { Error = result.ErrorMessage, ErrorCode = result.ErrorCode });
        }

        private static Task<ServiceResult> RunSql(string serviceName, Dictionary<string, object> args)
        {
            return Task.Run<ServiceResult>(() =>
            {
                ServiceResult result = null;
                try
                {
                    var sqlContext = new SqlRequest
                    {
                        SqlName = serviceName,
                        Parameters = args
                    };
                    var data = sqlContext.Execute();

                    result = new ServiceResult
                    {
                        Data = data
                    };
                }
                catch (Exception ex)
                {
                    result = new ServiceResult
                    {
                        Error = ex.Message
                    };
                }
                return result;
            });
        }
    }
}
