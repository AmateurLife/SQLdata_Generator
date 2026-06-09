using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SQLdata_Generator.Models;

namespace SQLdata_Generator.Services
{
    public class DatabaseService : IDatabaseService
    {
        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ColumnInfo>> GetTableSchemaAsync(string connectionString, string tableName)
        {
            var columns = new List<ColumnInfo>();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH,
                       NUMERIC_PRECISION, NUMERIC_SCALE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName
                ORDER BY ORDINAL_POSITION";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tableName", tableName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnInfo
                {
                    ColumnName = reader["COLUMN_NAME"]?.ToString() ?? string.Empty,
                    DataType = reader["DATA_TYPE"]?.ToString() ?? string.Empty,
                    IsNullable = reader["IS_NULLABLE"]?.ToString() == "YES",
                    MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] as int?,
                    NumericPrecision = reader["NUMERIC_PRECISION"] as int?,
                    NumericScale = reader["NUMERIC_SCALE"] as int?
                });
            }

            return columns;
        }

        public async Task<List<TableInfo>> GetAllTablesAsync(string connectionString)
        {
            var tables = new List<TableInfo>();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT TABLE_NAME, TABLE_TYPE
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(new TableInfo
                {
                    TableName = reader["TABLE_NAME"]?.ToString() ?? string.Empty,
                    TableType = reader["TABLE_TYPE"]?.ToString() ?? string.Empty
                });
            }

            return tables;
        }

        public async Task InsertDataAsync(string connectionString, string tableName, DataTable data, IProgress<int> progress)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = tableName,
                BatchSize = 1000,
                NotifyAfter = data.Rows.Count > 100 ? data.Rows.Count / 100 : 1
            };

            foreach (DataColumn column in data.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            int totalRows = data.Rows.Count;
            bulkCopy.SqlRowsCopied += (_, e) =>
            {
                progress?.Report((int)(e.RowsCopied * 100 / totalRows));
            };

            await bulkCopy.WriteToServerAsync(data);
            progress?.Report(100);
        }
    }
}
