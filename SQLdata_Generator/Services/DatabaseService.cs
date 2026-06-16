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
                SELECT c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE, c.CHARACTER_MAXIMUM_LENGTH,
                       c.NUMERIC_PRECISION, c.NUMERIC_SCALE,
                       CAST(ISNULL(sc.is_identity, 0) AS bit) AS IS_IDENTITY
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN sys.objects o ON o.name = c.TABLE_NAME AND o.type = 'U'
                    AND o.schema_id = SCHEMA_ID(ISNULL(c.TABLE_SCHEMA, 'dbo'))
                LEFT JOIN sys.columns sc ON sc.object_id = o.object_id AND sc.name = c.COLUMN_NAME
                WHERE c.TABLE_NAME = @tableName
                ORDER BY c.ORDINAL_POSITION";

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
                    NumericPrecision = reader["NUMERIC_PRECISION"] is DBNull ? null : Convert.ToInt32(reader["NUMERIC_PRECISION"]),
                    NumericScale = reader["NUMERIC_SCALE"] as int?,
                    IsIdentity = reader["IS_IDENTITY"] is not DBNull && (bool)reader["IS_IDENTITY"]
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

        public async Task<List<string>> GetAllDatabasesAsync(string connectionString)
        {
            var databases = new List<string>();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT name FROM sys.databases
                WHERE database_id > 4
                ORDER BY name";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                databases.Add(reader["name"]?.ToString() ?? string.Empty);
            }

            return databases;
        }

        public async Task CreateDatabaseAsync(string connectionString, string databaseName)
        {
            var sql = $"CREATE DATABASE [{databaseName}]";
            await ExecuteNonQueryAsync(connectionString, sql);
        }

        public async Task DropDatabaseAsync(string connectionString, string databaseName)
        {
            var sql = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]";
            await ExecuteNonQueryAsync(connectionString, sql);
        }

        public async Task CreateTableAsync(string connectionString, string tableName, string columnsDefinition)
        {
            var sql = $"CREATE TABLE [{tableName}] ({columnsDefinition})";
            await ExecuteNonQueryAsync(connectionString, sql);
        }

        public async Task DropTableAsync(string connectionString, string tableName)
        {
            var sql = $"DROP TABLE [{tableName}]";
            await ExecuteNonQueryAsync(connectionString, sql);
        }

        public async Task ExecuteNonQueryAsync(string connectionString, string sql)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
