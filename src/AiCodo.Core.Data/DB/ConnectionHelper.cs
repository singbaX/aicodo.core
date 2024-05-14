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
    static class ConnectionHelper
    {
        public static DbDataAdapter CreateAdapter(this SqlConnection sqlConn)
        {
            var factory = GetProviderFactory(sqlConn.ProviderName);
            var adp = factory.CreateDataAdapter();
            return adp;
        }

        public static DbConnection Open(this SqlConnection sqlConn)
        {
            return Open(sqlConn.ProviderName, sqlConn.GetConnectionString());
        }

        public static DbConnection Open(string providerName, string connectionString)
        {
            DbConnection conn = null;
            DbProviderFactory p = GetProviderFactory(providerName);

            conn = p.CreateConnection();
            conn.ConnectionString = connectionString;
            conn.Open();
            return conn;
        }

        public static DbProviderFactory GetProviderFactory(string providerName)
        {
            var p = DbProviderFactories.GetFactory(providerName);
            if (p == null)
            {
                throw new Exception($"Provider not found ({providerName})");
            }

            return p;
        }

        public static IEnumerable<TableSchema> LoadTables(this SqlConnection sqlConn)
        {
            var provider = DbProviderFactories.GetProvider(sqlConn.ProviderName);
            var conn = sqlConn.Open();
            foreach (var table in provider.LoadTables(conn).ToList())
            {
                yield return table;
            }
        }
    }
}
