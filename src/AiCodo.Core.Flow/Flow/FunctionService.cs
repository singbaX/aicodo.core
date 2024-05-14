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
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ServiceMethodAttribute : Attribute
    {
        public ServiceMethodAttribute()
        {
        }

        public ServiceMethodAttribute(string serviceName)
        {
            ServiceName = serviceName;
        }

        public string ServiceName { get; private set; }
    }

    public class MethodStartEventArgs : EventArgs
    {
        public int ThreadID { get; } = Thread.CurrentThread.ManagedThreadId;
        public DateTime CreateTime { get; } = DateTime.Now;

        public string Name { get; }

        public Dictionary<string, object> Parameters { get; }

        public MethodStartEventArgs(string name, Dictionary<string, object> parameters)
        {
            Name = name;
            Parameters = parameters;
        }
    }

    public class MethodEndEventArgs : EventArgs
    {
        public int ThreadID { get; } = Thread.CurrentThread.ManagedThreadId;
        public DateTime CreateTime { get; } = DateTime.Now;
        public string ErrorCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public Dictionary<string, object> Parameters { get; private set; }

        public MethodEndEventArgs()
        {
            ErrorCode = ErrorCodes.Ok;
            ErrorMessage = "";
        }

        public MethodEndEventArgs(Dictionary<string, object> parameters) : this()
        {
            Parameters = parameters;
        }

        public MethodEndEventArgs(string errorCode, string errorMessage, Dictionary<string, object> parameters)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Parameters = parameters;
        }

        public MethodEndEventArgs SetParameter(string name, object value)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, object>();
            }
            Parameters[name] = value;
            return this;
        }
    }


    public partial class FuncService : IMethodService
    {
        private Dictionary<string, IFunctionItem> _Items = new Dictionary<string, IFunctionItem>();

        #region 属性 Current
        private static FuncService _Current = new FuncService();
        public static FuncService Current
        {
            get
            {
                return _Current;
            }
        }
        #endregion

        public void RegisterItem(string name, IFunctionItem item)
        {
            _Items[name] = item;
        }

        public void RegisterType<T>()
        {
            var type = typeof(T);
            RegisterType(type);
        }

        public void RegisterType(Type type)
        {
            foreach (var m in type.GetMethods())
            {
                if (!m.IsStatic)
                {
                    continue;
                }
                var attr = (ServiceMethodAttribute)m.GetCustomAttribute(typeof(ServiceMethodAttribute));
                if (attr != null)
                {
                    var typeName = type.Name;
                    if (typeName.EndsWith("Service"))
                    {
                        typeName = typeName.Substring(0, typeName.Length - 7);
                    }
                    var serviceName = attr.ServiceName.IsNullOrEmpty() ? $"{type.Name}.{m.Name}" : attr.ServiceName;
                    RegisterMethod(serviceName, m);
                }
            }
        }

        public void RegisterMethod(string name, MethodInfo method)
        {
            FunctionItemConfig item = CreateFunctionItem(name, method);
            _Items.Add(name, item);
            FuncService.SetMethod(name, method);
            this.Log($"注册服务方法：{name}");
        }

        private static FunctionItemConfig CreateFunctionItem(string methodName, MethodInfo method)
        {
            var item = new FunctionItemConfig
            {
                Name = methodName,
                DisplayName = methodName,
                //Location = new FunctionItemLocation
                //{
                //    AsmName = type.Assembly.GetName().Name,
                //    FileName = "",
                //    ClassName = type.FullName,
                //    MethodName = methodName
                //}
            };
            ResetItemParameters(item, method);
            return item;
        }

        static void ResetItemParameters(FunctionItemConfig item, MethodInfo method)
        {
            var returnType = method.ReturnType;
            var parameters = item.Parameters.ToList();
            item.Parameters.Clear();
            method.GetParameters()
                .ForEach(mp =>
                {
                    var p = parameters.FirstOrDefault(f => f.Name.Equals(mp.Name, StringComparison.OrdinalIgnoreCase));
                    if (p == null)
                    {
                        p = new ParameterItem
                        {
                            Name = mp.Name,
                            TypeName = GetParameterTypeName(mp.ParameterType)
                        };
                    }
                    else if (p.TypeName == "String")
                    {
                        p.TypeName = GetParameterTypeName(mp.ParameterType);
                    }
                    item.Parameters.Add(p);
                });

            var resultParameters = item.ResultParameters.ToList();
            item.ResultParameters.Clear();
        }

        private static string GetParameterTypeName(Type type)
        {
            if (type.IsByRef)
            {
                var atype = type.Assembly.GetType(type.FullName.TrimEnd('&'));
                if (atype != null)
                {
                    type = atype;
                }
                else
                {
                    throw new Exception($"类型错误[{type.FullName}]");
                }
            }

            if (type == typeof(bool))
            {
                return "Bool";
            }

            if (type == typeof(int))
            {
                return "Int";
            }

            if (type == typeof(float) || type == typeof(Single))
            {
                return "Single";
            }
            if (type == typeof(double))
            {
                return "Double";
            }

            return "String";
        }


        public IFunctionItem GetItem(string name)
        {
            return GetFunctionItem(name);
        }

        private IFunctionItem GetFunctionItem(string name)
        {
            if (_Items.TryGetValue(name, out var item))
            {
                return item;
            }
            return FunctionConfig.Current.GetItem(name);
        }

        public IEnumerable<NameItem> GetItems()
        {
            if (_Items.Count > 0)
            {
                foreach (var item in _Items)
                {
                    yield return new NameItem(item.Key, item.Value.DisplayName);
                }
            }
            foreach (var item in FunctionConfig.Current.Items
                .Select(f => new NameItem(f.Name, f.DisplayName)))
            {
                yield return item;
            }
        }

        public IFunctionResult Run(string name, Dictionary<string, object> args)
        {
            var item = GetFunctionItem(name);
            if (item == null)
            {
                throw new MissingMethodException(name);
            }
            if (item is FunctionItemConfig funcItem)
            {
                return Run(funcItem, args);
            }
            else
            {
                throw new MissingMethodException(name);
            }
        }
    }

    public partial class FuncService
    {
        static CollectibleAssemblyLoadContext _LoadContext = null;

        static FuncService()
        {
            _LoadContext = new CollectibleAssemblyLoadContext();
        }

        #region 方法的查找及调用
        static readonly Dictionary<string, MethodInfo> _Methods = new Dictionary<string, MethodInfo>();

        public static object RunMethod(string name, params object[] args)
        {
            #region 动态获取方法并缓存
            if (!_Methods.TryGetValue(name, out MethodInfo method))
            {
                lock (_Methods)
                {
                    if (!_Methods.TryGetValue(name, out method))
                    {
                        var methodItem = FunctionConfig.Current.CommonMethods.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (methodItem == null)
                        {
                            throw new Exception($"方法[{name}]不存在或没有配置");
                        }
                        method = GetMethod(methodItem);
                        _Methods[name] = method;
                    }
                }
            }
            #endregion

            //执行方法
            object value = null;
            try
            {
                value = method.Invoke(null, args);
                return value;
            }
            catch (Exception ex)
            {
                nameof(FuncService).Log(ex.ToString(), Category.Exception);
                throw new Exception($"执行算法方法[{name}]出错：{ex.Message}", ex);
            }
        }

        //public static IFunctionResult Run(string itemName, Dictionary<string, object> args)
        //{
        //    var item = FunctionConfig.Current.Items.FirstOrDefault(f => f.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        //    if (item != null && item is FunctionItemConfig itemconfig)
        //    {
        //        return Run(itemconfig, args);
        //    }
        //    return null;
        //}

        static IFunctionResult Run(FunctionItemConfig item, Dictionary<string, object> args)
        {
            if (args == null)
            {
                throw new ArgumentException($"执行方法缺少必须参数", nameof(args));
            }

            MethodInfo method = GetMethod(item);

            //参数名称大小写忽略
            var lowerArgs = new Dictionary<string, object>();
            foreach(var a in args)
            {
                lowerArgs[a.Key.ToLower()] = a.Value;
            }

            #region 设置方法参数
            var pinfos = method.GetParameters();
            var parameters = pinfos.Select(p =>
            {
                if (lowerArgs.TryGetValue(p.Name.ToLower(), out object v))
                {
                    return v;
                }

                if (p.HasDefaultValue)
                {
                    return p.DefaultValue;
                }
                throw new ArgumentException($"执行方法缺少必须参数[{method.Name}-{p.Name}]", p.Name);
            }).ToArray();
            #endregion

            OnMethodStart(item.Name, pinfos, parameters);
            var value = RunItemMethod(item, method, parameters);
            OnMethodEnd(item.Name, value);
            return value;
        }

        static IFunctionResult RunItemMethod(FunctionItemConfig item, MethodInfo method, object[] parameters)
        {
            object value = null;
            try
            {
                value = method.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                ex.WriteErrorLog();
                return new FunctionResult
                {
                    ErrorCode = FlowErrors.MethodInnerError,
                    ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.Message
                };
            }

            if (value == null)
            {
                nameof(FuncService).Log($"执行方法无返回值：[{item.Name}]-[{item.DisplayName}]");
                return null;
            }

            if (value is IFunctionResult result)
            {
                return result;
            }
            return ToFunctionResult(value);
        }

        private static void OnMethodStart(string name, ParameterInfo[] pinfos, object[] parameters)
        {

        }

        private static void OnMethodEnd(string name, IFunctionResult value)
        {

        }
        static IFunctionResult ToFunctionResult(object value)
        {
            if (value is IFunctionResult r)
            {
                return r;
            }

            var result = new FunctionResult();
            if (value is string strValue)
            {
                result.Data.Add("Result", strValue);
            }
            else
            {
                var valueType = value.GetType();
                if (valueType == typeof(void))
                {
                    result.Data.Add("Result", true);
                }
                else if (valueType.IsClass)
                {
                    if (value is IDictionary dic)
                    {
                        foreach (var key in dic)
                        {
                            result.Data.Add(key.ToString(), dic[key]);
                        }
                    }
                    else
                    {
                        result.Data.Add("Result", value);
                    }
                }
                else
                {
                    result.Data.Add("Result", value);
                }
            }
            return result;
        }

        public static MethodInfo GetMethod(FunctionItemConfig item)
        {
            var name = item.Name;
            #region 动态获取方法并缓存
            if (!_Methods.TryGetValue(name, out MethodInfo method))
            {
                lock (_Methods)
                {
                    if (!_Methods.TryGetValue(name, out method))
                    {
                        method = GetMethod(item.Location);
                        _Methods[name] = method;
                    }
                }
            }
            #endregion
            return method;
        }

        private static object RunMethod(MethodInfo method, Dictionary<string, object> args)
        {
            object value = null;
            #region 设置方法参数
            var pinfos = method.GetParameters();
            var parameters = pinfos.Select(p =>
            {
                if (args.TryGetValue(p.Name, out object v))
                {
                    return v;
                }
                if (p.HasDefaultValue)
                {
                    return p.DefaultValue;
                }
                throw new ArgumentException($"执行方法缺少必须参数[{method.Name}-{p.Name}]", p.Name);
            }).ToArray();

            #region 添加日志
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"开始执行：[{method.Name}]");
            for (int i = 0; i < pinfos.Length; i++)
            {
                var p = pinfos[i];
                if (parameters[i] != null && parameters[i] is IList list)
                {
                    sb.AppendLine($"{p.Name}= List,Count:{list.Count}");
                }
                else
                {
                    sb.AppendLine($"{p.Name}={parameters[i]}");
                }
            }
            nameof(FuncService).Log(sb.ToString());
            #endregion

            #endregion

            try
            {
                nameof(FuncService).Log($"Start [{method.Name}]");
                value = method.Invoke(null, parameters);
                nameof(FuncService).Log($"End [{method.Name}]");
                return value;
            }
            catch (Exception ex)
            {
                ex.WriteErrorLog();
                var sbError = new System.Text.StringBuilder();
                sbError.AppendLine($"执行方法出错【{method.Name}】:{ex.Message}");
                sbError.AppendLine(ex.ToString());
                //method.GetParameters().ForEach(p => sbError.AppendLine($"[{p.Name}]-{p.ParameterType}"));
                nameof(FuncService).Log(sbError.ToString(), Category.Exception);
                throw new Exception($"执行方法出错【{method.Name}】：{ex.Message}", ex);
            }
        }

        public static Type GetType(string asmName, string asmFileName, string className)
        {
            Assembly asm = GetAssembly(asmName, asmFileName);
            var cls = asm.GetType(className);
            if (cls == null)
            {
                throw new Exception($"算法配置错误，程序集[{asmName}]中未找到[{className}]");
            }
            return cls;
        }

        private static Assembly GetAssembly(string asmName, string asmFileName)
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(w => w.GetName() != null && w.GetName().Name.Equals(asmName, StringComparison.OrdinalIgnoreCase));

            if (asm == null)
            {
                asm = _LoadContext.GetAssemblies()
                    .FirstOrDefault(w => w.GetName() != null && w.GetName().Name.Equals(asmName, StringComparison.OrdinalIgnoreCase));
            }

            #region 如果程序集未加载，则通过配置加载程序集
            if (asm == null)
            {
                if (asmFileName.IsNullOrEmpty())
                {
                    throw new Exception($"算法配置错误，没有指定程序集路径[FileName]");
                }

                var fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, asmFileName);
                if (!System.IO.File.Exists(fileName))
                {
                    throw new Exception($"算法配置错误，文件不存在[{fileName}]");
                }

                try
                {
                    asm = _LoadContext.LoadFromAssemblyPath(fileName);
                    //asm = Assembly.LoadFrom(fileName);
                    if (asm != null)
                    {
                        LoadAllReferenced(asm);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"算法配置错误，程序集[{asmName}]加载失败：{ex.Message}", ex);
                }
            }
            #endregion
            return asm;
        }

        public static MethodInfo GetMethod(FunctionItemLocation item)
        {
            MethodInfo method;
            if (item.IsEmptyConfig())
            {
                throw new Exception($"算法配置错误，没有指定程序集路径");
            }
            var cls = GetType(item.AsmName, item.FileName, item.ClassName);
            if (cls == null)
            {
                throw new Exception($"算法配置错误，程序集[{item.AsmName}]中未找到[{item.ClassName}]");
            }

            method = cls.GetMethod(item.MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MethodNotFoundException { MethodName = item.MethodName };
            }
            return method;
        }

        private static void LoadAllReferenced(Assembly asm)
        {
            asm.GetReferencedAssemblies()
                .ForEach(a =>
                {
                    if (a != null && a.Name.IsNotEmpty())
                    {
                        if (_LoadContext.GetAssemblies()
                             .FirstOrDefault(w => w.GetName() != null && w.GetName().Name.Equals(a.Name, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var fileName = $"{a.Name}.dll".FixedAppBasePath();
                            if (fileName.IsFileExists())
                            {
                                try
                                {
                                    var subAsm = _LoadContext.LoadFromAssemblyPath(fileName);
                                    // var subAsm = Assembly.LoadFrom(fileName);
                                    typeof(FuncService).Log($"加载文件{fileName}");
                                    LoadAllReferenced(subAsm);
                                }
                                catch (Exception loadEx)
                                {
                                    typeof(FuncService).Log($"Load{fileName}\r\n {loadEx.ToString()}");
                                }
                            }
                        }
                    }
                });
        }
        #endregion

        public static void SetMethod(string name, MethodInfo method)
        {
            _Methods[name] = method;
        }

        //public static void ResetItemParameters(FunctionItemConfig item, MethodInfo method)
        //{
        //    var returnType = method.ReturnType;
        //    var parameters = item.Parameters.ToList();
        //    item.Parameters.Clear();
        //    method.GetParameters()
        //        .ForEach(mp =>
        //        {
        //            var p = parameters.FirstOrDefault(f => f.Name.Equals(mp.Name, StringComparison.OrdinalIgnoreCase));
        //            if (p == null)
        //            {
        //                p = new ParameterItem
        //                {
        //                    Name = mp.Name,
        //                    TypeName = GetParameterTypeName(mp.ParameterType)
        //                };
        //            }
        //            else if (p.TypeName == "String")
        //            {
        //                p.TypeName = GetParameterTypeName(mp.ParameterType);
        //            }
        //            item.Parameters.Add(p);
        //        });

        //    var resultParameters = item.ResultParameters.ToList();
        //    item.ResultParameters.Clear();
        //    var dataProperty = returnType.GetProperty("Data");
        //    if (dataProperty != null)
        //    {
        //        if (dataProperty.PropertyType.IsClass)
        //        {
        //            if (dataProperty.PropertyType == typeof(Dictionary<string, object>))
        //            {

        //            }
        //            else
        //            {
        //                foreach (var pinfo in dataProperty.PropertyType.GetProperties())
        //                {
        //                    var p = resultParameters.FirstOrDefault(f => f.Name.Equals(pinfo.Name, StringComparison.OrdinalIgnoreCase));
        //                    if (p == null)
        //                    {
        //                        p = new ResultParameter
        //                        {
        //                            Name = pinfo.Name,
        //                            TypeName = GetParameterTypeName(pinfo.PropertyType)
        //                        };
        //                    }
        //                    item.ResultParameters.Add(p);
        //                }
        //            }
        //        }

        //        if (dataProperty.PropertyType == typeof(bool))
        //        {
        //            var p = resultParameters.FirstOrDefault(f => f.Name.Equals("Result", StringComparison.OrdinalIgnoreCase));
        //            if (p == null)
        //            {
        //                p = new ResultParameter
        //                {
        //                    Name = "Result",
        //                    TypeName = GetParameterTypeName(typeof(bool))
        //                };
        //            }
        //            item.ResultParameters.Add(p);
        //        }
        //    }
        //}

        //private static string GetParameterTypeName(Type type)
        //{
        //    if (type.IsByRef)
        //    {
        //        var atype = type.Assembly.GetType(type.FullName.TrimEnd('&'));
        //        if (atype != null)
        //        {
        //            type = atype;
        //        }
        //        else
        //        {
        //            throw new Exception($"类型错误[{type.FullName}]");
        //        }
        //    }

        //    if (type == typeof(bool))
        //    {
        //        return "Bool";
        //    }

        //    if (type == typeof(int))
        //    {
        //        return "Int";
        //    }

        //    if (type == typeof(float) || type == typeof(Single))
        //    {
        //        return "Single";
        //    }
        //    if (type == typeof(double))
        //    {
        //        return "Double";
        //    }

        //    return "String";
        //}
    }

    public class CollectibleAssemblyLoadContext
    {
        private Dictionary<string, Assembly> _Assemblies = new Dictionary<string, Assembly>();

        public IEnumerable<Assembly> GetAssemblies()
        {
            foreach (var a in _Assemblies)
            {
                yield return a.Value;
            }
            //foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    yield return a;
            //}
        }

        public Assembly LoadFromAssemblyPath(string fileName)
        {
            var asm = Assembly.LoadFrom(fileName);
            _Assemblies[asm.GetName().Name] = asm;
            return asm;
        }
    }
}
