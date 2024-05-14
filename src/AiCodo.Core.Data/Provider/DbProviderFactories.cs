// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace AiCodo.Data
{ 
    public static class DbProviderFactories
    {
        static Dictionary<string, IDbProvider> _Items
            = new Dictionary<string, IDbProvider>();

        static DbProviderFactories()
        {
            //SetFactory("mysql", MySqlProvider.Instance);
            //SetFactory("MySql.Data.MySqlClient", MySqlProvider.Instance);
            //SetFactory("sqlite", SqliteProvider.Instance);
            //SetFactory("sql", MSSqlProvider.Instance);
        }

        public static IEnumerable<string> GetProviderNames()
        {
            return _Items.Keys.ToList();
        }

        public static IDbProvider GetProvider(string name)
        {
            if (_Items.TryGetValue(name, out IDbProvider provider))
            {
                return provider;
            }
            return null;
        }

        public static DbProviderFactory GetFactory(string name)
        {
            if (_Items.TryGetValue(name, out IDbProvider provider))
            {
                return provider.Factory;
            }
            return null;
        }

        public static void SetFactory(string name, IDbProvider provider)
        {
            lock (_Items)
            {
                _Items[name] = provider;
                "DbProviderFactories".Log($"Add [{name}]={provider.GetType()}");
            }
        }
    }
}
