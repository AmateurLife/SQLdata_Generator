using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using SQLdata_Generator.Models;

namespace SQLdata_Generator.Services
{
    public interface IDatabaseService
    {
        Task<bool> TestConnectionAsync(string connectionString);
        Task<List<ColumnInfo>> GetTableSchemaAsync(string connectionString, string tableName);
        Task<List<TableInfo>> GetAllTablesAsync(string connectionString);
        Task InsertDataAsync(string connectionString, string tableName, DataTable data, IProgress<int> progress);

        Task<List<string>> GetAllDatabasesAsync(string connectionString);
        Task CreateDatabaseAsync(string connectionString, string databaseName);
        Task DropDatabaseAsync(string connectionString, string databaseName);
        Task CreateTableAsync(string connectionString, string tableName, string columnsDefinition);
        Task DropTableAsync(string connectionString, string tableName);
        Task ExecuteNonQueryAsync(string connectionString, string sql);
        Task<Models.SqlExecutionResult> ExecuteSqlAsync(string connectionString, string sql);
    }
}
