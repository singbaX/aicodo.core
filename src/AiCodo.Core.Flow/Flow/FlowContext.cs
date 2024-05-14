// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Flow
{
    using AiCodo.Flow.Configs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    public class FlowContext
    {
        public const string RootContextArgName = "_RootContext";
        public const string RootArgsName = "_RootArgs";
        public const string ParentArgsName = "_ParentArgs";

        private Dictionary<string, object> _Args { get; set; } = new Dictionary<string, object>();

        private Dictionary<string, object> _Results { get; set; } = new Dictionary<string, object>();

        public string ID { get; set; }

        public string TrackID { get; set; }

        public string FlowID { get; set; }

        public string FlowName { get; set; }

        public string LastErrorCode { get; private set; } = "";

        public FlowContext(Dictionary<string, object> args = null)
        {
            if (args != null)
            {
                foreach (var item in args)
                {
                    _Args[item.Key] = item.Value;
                }
            }
        }

        public FlowContext SetArgs(string name, object value)
        {
            _Args[name] = value;
            return this;
        }

        public bool TryGetArg(string name, out object value)
        {
            return _Args.TryGetValue(name, out value);
        }

        public IEnumerable<KeyValuePair<string, object>> GetArgs()
        {
            foreach (var item in _Args)
            {
                yield return item;
            }
        }

        public IEnumerable<KeyValuePair<string, object>> GetResults()
        {
            foreach (var item in _Results)
            {
                yield return item;
            }
        }

        public virtual Task<DynamicEntity> ExecuteFlow(FunctionFlowConfig flow)
        {
            FlowID = flow.ID;
            FlowName = flow.Name;

            return Task.Run(() =>
            {
                if (flow.LockMode == LockMode.Current)
                {
                    lock (flow)
                    {
                        return RunActions(flow);
                    }
                }

                if (flow.LockMode == LockMode.Global && flow.LockID.IsNotEmpty())
                {
                    var lockID = flow.LockID;
                    if (lockID.StartsWith("="))
                    {
                        lockID = lockID.Substring(1).Eval(_Args).ToString();
                    }
                    lock (Locks.GetLock(lockID))
                    {
                        return RunActions(flow);
                    }
                }

                return RunActions(flow);
            });
        }

        protected virtual DynamicEntity RunActions(FunctionFlowConfig flow)
        {
            var actions = flow.GetActions().ToList();
            var data = ExecuteFlowActions(flow, actions, out var flowArgs);
            if (flow.Results.Count > 0)
            {
                var exp = ExpressionHelper.GetInterpreter(flowArgs);
                foreach (var r in flow.Results)
                {
                    object value = null;
                    if (r.Expression.IsNullOrEmpty())
                    {
                        if (flowArgs.TryGetValue(r.Name, out value))
                        {
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        value = exp.Eval(r.Expression);
                    }
                    data.SetValue(r.Name, value);
                }
            }
            return data;
        }

        public DynamicEntity ExecuteFlowActions(FunctionFlowConfig flow, IEnumerable<FlowActionBase> actions, out Dictionary<string, object> flowArgs)
        {
            var result = new DynamicEntity();
            var errorCode = "";
            var errorMessage = "";
            flowArgs = CreateFlowArgs(flow);

            flowArgs[RootContextArgName] = this;
            flowArgs[RootArgsName] = flowArgs;
            flowArgs[ParentArgsName] = null;
            flowArgs["HasError"] = false;
            try
            {
                var actionIndex = 0;
                IFunctionResult lastActionResult = null;
                foreach (var action in actions)
                {
                    var actionStartTime = DateTime.Now;
                    flowArgs["LastErrorCode"] = LastErrorCode;
                    var exp = ExpressionHelper.GetInterpreter(flowArgs);
                    errorCode = "";
                    errorMessage = "";
                    actionIndex++;

                    #region execute action
                    try
                    {
                        var tryCount = 0;
                        var goNext = false;

                        while (!goNext)
                        {
                            tryCount++;
                            if (!action.TryRun(flow, flowArgs, out lastActionResult))
                            {
                                goNext = true;
                                break;
                            }

                            #region 处理返回值
                            LastErrorCode = lastActionResult.ErrorCode;

                            if (lastActionResult.IsOk)
                            {
                                ResetResult(result, flowArgs, action, lastActionResult);
                                goNext = true;
                                break;
                            }
                            else
                            {
                                AddLog($"执行[{action.Name}]出错：{errorCode}-{lastActionResult.ErrorMessage}");
                                if (flow.ErrorMode == FlowErrorMode.Break)
                                {
                                    errorCode = OnSetError(result, action, lastActionResult);
                                    goNext = false;
                                    break;
                                }

                                if (flow.ErrorMode == FlowErrorMode.Continue)
                                {
                                    flowArgs["HasError"] = true;
                                    goNext = true;
                                    break;
                                }

                                if (flow.ErrorMode == FlowErrorMode.Retry)
                                {
                                    if (flow.RetryCount > 0 && flow.RetryCount > tryCount)
                                    {
                                        goNext = true;
                                        break;
                                    }
                                    else
                                    {
                                        errorCode = OnSetError(result, action, lastActionResult);
                                        goNext = false;
                                    }
                                }
                            }
                            #endregion
                        }

                        if (!goNext)
                        {
                            break;
                        }
                    }
                    catch (CodeException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        CheckFileNotFoundException(ex);
                        if (errorCode.IsNullOrEmpty())
                        {
                            errorCode = ErrorCodes.Unknow;
                        }
                        AddLog(ex.ToString(), Category.Exception);
                        throw new CodeException(errorCode, ex.Message);
                    }
                    #endregion
                }
                if (lastActionResult != null)
                {
                    errorCode = lastActionResult.ErrorCode;
                    errorMessage = lastActionResult.ErrorMessage;
                }
                if ((errorCode.IsNotEmpty() && errorCode != ErrorCodes.Ok) || errorMessage.IsNotEmpty())
                {
                    throw new CodeException(errorCode, errorMessage);
                }
            }
            catch (CodeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errorCode = "";
                errorMessage = ex.Message;
            }

            return result;
        }

        private Dictionary<string, object> CreateFlowArgs(FunctionFlowConfig flow)
        {
            var flowArgs = new Dictionary<string, object>();
            _Args.ForEach(a => flowArgs[a.Key] = a.Value);

            var flowParameters = flow.GetParameters().ToList();
            if (flowParameters.Count > 0)
            {
                flowParameters.ForEach(p =>
                {
                    object value = "";
                    if (p.DefaultValue.StartsWith("="))
                    {
                        value = p.DefaultValue.Substring(1).Eval(flowArgs);
                    }
                    else
                    {
                        if (flowArgs.TryGetValue(p.Name, out value))
                        {
                        }
                        else
                        {
                            value = p.DefaultValue;
                        }
                    }
                    flowArgs[p.Name] = p.GetValue(value);
                });
            }

            return flowArgs;
        }

        static void ResetResult(DynamicEntity result, Dictionary<string, object> flowArgs, FlowActionBase action, IFunctionResult functionResult)
        {
            foreach (var r in action.ResultParameters)
            {
                object rvalue = null;
                if (r.Name.IsNullOrEmpty() && r.DefaultValue.IsNotEmpty())
                {
                    if (r.DefaultValue.StartsWith("="))
                    {
                        rvalue = r.DefaultValue.Eval(flowArgs);
                    }
                    else
                    {
                        rvalue = r.DefaultValue;
                    }
                }
                else if (!functionResult.TryGetValue(r.Name, out rvalue))
                {
                    throw FlowErrors.CreateError_FlowConfigError(action.Name, $"没有返回值[{r.Name}");
                }

                rvalue = r.GetValue(rvalue);
                if (r.ResultName.IsNotEmpty())
                {
                    result.SetValue(r.ResultName, rvalue);
                }

                if (r.ResetInputName.IsNotEmpty())
                {
                    flowArgs[r.ResetInputName] = rvalue;
                }

                if (rvalue != null && rvalue is IList list)
                {
                }
            }
        }

        private string OnSetError(DynamicEntity result, FlowActionBase action, IFunctionResult actionResult)
        {
            string errorCode = actionResult.ErrorCode;
            result.SetValue("ErrorCode", errorCode);
            result.SetValue("ErrorMessage", actionResult.ErrorMessage);
            return errorCode;
        }

        private void CheckFileNotFoundException(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            if (ex is FileNotFoundException fex)
            {
                if (fex.FileName.IsNotEmpty())
                {
                    var names = fex.FileName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (names.Length == 0)
                    {
                        return;
                    }

                    var fileName = $"{names[0]}.dll".FixedAppBasePath();
                    if (fileName.IsFileExists())
                    {
                        try
                        {
                            var asm = Assembly.LoadFrom(fileName);
                        }
                        catch (Exception loadEx)
                        {
                            AddLog(loadEx.ToString());
                        }
                    }
                }
            }
        }

        internal static object GetDefaultValue(ParameterItem p)
        {
            return p.GetValue(p.DefaultValue);
        }

        private void AddLog(string line, Category category = Category.Info)
        {
            this.Log($"[{ID}] {line}", category);
        }
    }
}
