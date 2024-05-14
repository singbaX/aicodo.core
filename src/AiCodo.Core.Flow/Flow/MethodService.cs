// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiCodo.Flow
{
    public interface IMethodService
    {
        IFunctionResult Run(string name, Dictionary<string, object> args);

        IEnumerable<NameItem> GetItems();

        IFunctionItem GetItem(string name);
    }

    public static class MethodServiceFactory
    {
        static Dictionary<string, IMethodService> _ServiceItems = new Dictionary<string, IMethodService>
        {
            {"func",FuncService.Current }
        };

        public static void RegisterService(string name, IMethodService serivce)
        {
            _ServiceItems[name.ToLower()] = serivce;
        }

        public static IMethodService GetSerivce(string name)
        {
            if (_ServiceItems.TryGetValue(name, out var serivce))
            {
                return serivce;
            }
            return null;
        }

        public static IFunctionItem GetItem(string name)
        {
            if (TryGetServiceOfFunctionName(name, out var funcName, out var serivce))
            {
                return serivce.GetItem(funcName);
            }
            return null;
        }

        public static IFunctionResult Run(string name, Dictionary<string, object> args)
        {
            string funcName;
            IMethodService serivce;
            if (TryGetServiceOfFunctionName(name, out funcName, out serivce))
            {
                return serivce.Run(funcName, args);
            }
            throw new Exception($"服务不存在");
        }

        private static bool TryGetServiceOfFunctionName(string name, out string funcName, out IMethodService serivce)
        {
            var serviceIndex = name.IndexOf('.');
            funcName = "";
            var serivceName = "";
            if (serviceIndex <= 0)
            {
                serivceName = "func";
                funcName = name;
            }
            else
            {
                serivceName = name.Substring(0, serviceIndex);
                funcName = name.Substring(serviceIndex + 1);
            }

            serivce = GetSerivce(serivceName);
            if (serivce == null)
            {
                return false;
            }
            return true;
        }
    }
}
