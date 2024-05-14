// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;

namespace AiCodo.Data
{
    public static class ConfigService
    {
        #region connection
        public static void CreateConnection(DynamicEntity data)
        {
            var name = data.GetString("name");
            if (name.IsNullOrEmpty())
            {
                throw new Exception("缺少必须参数[name]");
            }
            if (SqlData.Current.Connections[name] != null)
            {
                throw new Exception($"连接[{name}]已存在");
            }
            var providerName = data.GetString("provider");
            if (providerName.IsNullOrEmpty())
            {
                throw new Exception("缺少必须参数[provider]");
            }
            var provider = DbProviderFactories.GetProvider(providerName);
            if (provider == null)
            {
                throw new Exception($"没有对应的数据库处理[{providerName}]");
            }
            var connectionString = provider.CreateConnectionString(data);
            var sqlConn = new SqlConnection { Name = name, ProviderName = providerName, ConnectionString = connectionString };
            SqlData.Current.Connections.Add(sqlConn);
            sqlConn.ReloadTables();
            SqlData.Current.GenerateItems();
            SqlData.Current.Save();
        }
        #endregion
    }
}
