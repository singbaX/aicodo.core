// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class SqlService
    {
        static object _LoadLock = new object();

        static SqlService()
        {
        }

        #region 属性 Config
        public static SqlData Config
        {
            get
            {
                return SqlData.Current;
            }
        }
        #endregion 

        public static object ExecuteSql(string sqlName, params object[] nameValues)
        {
            SqlItem sql = GetSqlItem(sqlName);
            switch (sql.SqlType)
            {
                case SqlType.QueryOne:
                    return sql.ExecuteQuery<DynamicEntity>(nameValues).FirstOrDefault();
                case SqlType.Execute:
                    return sql.ExecuteNoneQuery(nameValues);
                case SqlType.Scalar:
                    return sql.ExecuteScalar(nameValues);
                case SqlType.Query:
                default:
                    return sql.ExecuteQuery<DynamicEntity>(nameValues);
            }
        }

        public static IEnumerable<T> ExecuteSqlQuery<T>(string sqlName,
            ISqlFilter filter, Sort sort,
            int pageIndex, int pageSize)
            where T : IEntity, new()
        {
            var sqlContext = new SqlContext(sqlName);
            if (filter != null)
            {
                sqlContext.SetFilter(filter);
            }
            if (sort != null)
            {
                sqlContext.SetSorts(sort);
            }
            if (pageSize > 0)
            {
                sqlContext.SetPage(pageIndex, pageSize);
            }
            return sqlContext.ExecuteQuery<T>();
        }

        public static int ExecuteSqlCount(string sqlName, ISqlFilter filter)
        {
            var sqlContext = new SqlContext(sqlName);
            if (filter != null)
            {
                sqlContext.SetFilter(filter);
            }
            return sqlContext.GetTotalCount();
        }

        private static SqlItem GetSqlItem(string sqlName)
        {
            var config = Config;
            if (config == null)
            {
                throw new Exception("配置文件没有加载");
            }

            var sql = Config.GetSqlItem(sqlName);
            return sql;
        }

        public static IEnumerable<T> ExecuteQuery<T>(string sqlName, params object[] nameValues) where T : IEntity, new()
        {
            SqlItem sql = GetSqlItem(sqlName);
            if(sql == null)
            {
                throw new Exception($"命令[{sqlName}]不存在");
            }
            return sql.ExecuteQuery<T>(nameValues);
        }

        public static int ExecuteNoneQuery(string sqlName, params object[] nameValues)
        {
            SqlItem sql = GetSqlItem(sqlName);
            if(sql == null)
            {
                throw new Exception($"命令[{sqlName}]不存在");
            }
            return sql.ExecuteNoneQuery(nameValues);
        }

        public static object ExecuteScalar(string sqlName, params object[] nameValues)
        {
            SqlItem sql = GetSqlItem(sqlName);
            if(sql == null)
            {
                throw new Exception($"命令[{sqlName}]不存在");
            }
            return sql.ExecuteScalar(nameValues);
        }
    }
}
