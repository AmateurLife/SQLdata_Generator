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
        Task InsertDataAsync(string connectionString, string tableName, DataTable data, IProgress<int> progress);
    }
}
