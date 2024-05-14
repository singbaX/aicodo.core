// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace AiCodo.Data
{
    public interface IAlterTable
    {
        string CreateChangeColumn(string tableName, Column c, string afterName = "");
        string CreateAddColumn(string tableName, Column c, string afterName = "");
    }

    public interface IColumnDefaultValue
    {
        void SetDefaultValue(string dataType, string defaultValue);
        string GetDefaultValue(string dataType);
    }

    public interface ICreateMapper
    {
        void CreateMapperItems(SqlTableGroup tableGroup,TableSchema table);
    }

    public interface IDbProvider
    {
        DbProviderFactory Factory { get; }

        string ResetQueryLimit(string sql, int from, int count);
        string ResetQueryTotal(string sql);
        string GetLastIdentity(string tableName = "", string fieldName = "");
        
        string GetName(string columnName);
        
        string GetParameter(string columnName);

        IEnumerable<string> GetParameters(string commandText);

        TableSchema GetTableSchema(DbConnection db, string tableName);

        IEnumerable<string> GetTables(DbConnection db);

        IEnumerable<TableSchema> LoadTables(DbConnection db);

        #region DbCommand
        DbCommand CreateCommand(DbConnection db, string sql, params object[] nameValues);
        IEnumerable<T> ToItems<T>(IDataReader reader) where T : IEntity, new();

        string CreateFilter(ISqlFilter filter, IEntity parameters, string prefix, ref int pIndex);
        string CreateOrderBy(params Sort[] sort);

        #endregion

        string CreateConnectionString(DynamicEntity args);
        bool CheckTable(DbConnection conn, TableSchema table, out string error);
        string ExistsTable(string dbName, string tableName);
        string CreateTable(TableSchema table);

        string CreateAlterTable(TableSchema table, TableSchema oldTable);

        string CreateInsert(TableSchema table,
            bool reload = false, bool reloadAutoColumnOnly = false);

        string CreateUpdate(TableSchema table);

        string CreateDelete(TableSchema table);

        string CreateCount(TableSchema table);

        string CreateSelect(TableSchema table);

        string CreateSelect(TableSchema table, string where);

        string CreateSelectByKeys(TableSchema table);

        string CreateView(string name, string select, bool reset = true);

        string CreateInsert(TableSchema table, string[] columns);

        string CreateUpdate(TableSchema table, string[] columns);

        string CreateSelect(TableSchema table, string[] columns, string where = "");
    }
}
