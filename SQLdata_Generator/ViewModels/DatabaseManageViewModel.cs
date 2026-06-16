#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using SQLdata_Generator.Models;
using SQLdata_Generator.Services;

namespace SQLdata_Generator.ViewModels
{
    public class DatabaseManageViewModel : BindableBase
    {
        private readonly IDatabaseService _dbService;
        private readonly IConnectionService _connService;
        private readonly IExcelService _excelService;

        public IConnectionService ConnectionService => _connService;

        public static string[] CommonColumnTypes { get; } =
        [
            "int", "bigint", "smallint", "tinyint",
            "decimal", "numeric", "float", "real", "money", "smallmoney",
            "varchar", "nvarchar", "char", "nchar", "text", "ntext",
            "datetime", "datetime2", "date", "smalldatetime", "datetimeoffset", "time",
            "bit", "uniqueidentifier", "xml", "varbinary"
        ];

        private static readonly HashSet<string> StringTypes = new(StringComparer.OrdinalIgnoreCase)
            { "varchar", "nvarchar", "char", "nchar", "varbinary", "binary" };

        private static readonly HashSet<string> DecimalTypes = new(StringComparer.OrdinalIgnoreCase)
            { "decimal", "numeric", "money", "smallmoney" };

        private static readonly HashSet<string> PrecisionTypes = new(StringComparer.OrdinalIgnoreCase)
            { "decimal", "numeric", "money", "smallmoney", "datetime2", "datetimeoffset", "time", "float", "real" };

        private static readonly HashSet<string> IdentityTypes = new(StringComparer.OrdinalIgnoreCase)
            { "tinyint", "smallint", "int", "bigint", "decimal", "numeric" };

        private ObservableCollection<string> _allDatabases = new();
        public ObservableCollection<string> AllDatabases
        {
            get => _allDatabases;
            set => SetProperty(ref _allDatabases, value);
        }

        private string? _selectedDatabase;
        public string? SelectedDatabase
        {
            get => _selectedDatabase;
            set
            {
                SetProperty(ref _selectedDatabase, value);
                CreateTableCommand.RaiseCanExecuteChanged();
                DropDatabaseCommand.RaiseCanExecuteChanged();
                if (!string.IsNullOrEmpty(value))
                    _ = LoadTablesAsync(value);
            }
        }

        private ObservableCollection<TableInfo> _allTables = new();
        public ObservableCollection<TableInfo> AllTables
        {
            get => _allTables;
            set => SetProperty(ref _allTables, value);
        }

        private TableInfo? _selectedTable;
        public TableInfo? SelectedTable
        {
            get => _selectedTable;
            set
            {
                SetProperty(ref _selectedTable, value);
                DropTableCommand.RaiseCanExecuteChanged();
                AddColumnCommand.RaiseCanExecuteChanged();
                DropColumnCommand.RaiseCanExecuteChanged();
                if (value != null && !string.IsNullOrEmpty(SelectedDatabase))
                    _ = LoadColumnsAsync(SelectedDatabase, value.TableName);
                else
                    ColumnInfos = new ObservableCollection<ColumnInfo>();
            }
        }

        private ObservableCollection<ColumnInfo> _columnInfos = new();
        public ObservableCollection<ColumnInfo> ColumnInfos
        {
            get => _columnInfos;
            set => SetProperty(ref _columnInfos, value);
        }

        private string _newDatabaseName = string.Empty;
        public string NewDatabaseName
        {
            get => _newDatabaseName;
            set
            {
                SetProperty(ref _newDatabaseName, value);
                CreateDatabaseCommand.RaiseCanExecuteChanged();
            }
        }

        private string _newTableName = string.Empty;
        public string NewTableName
        {
            get => _newTableName;
            set
            {
                SetProperty(ref _newTableName, value);
                CreateTableCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<NewColumnDef> _newColumns = new();
        public ObservableCollection<NewColumnDef> NewColumns
        {
            get => _newColumns;
            set => SetProperty(ref _newColumns, value);
        }

        private string _newColumnName = string.Empty;
        public string NewColumnName
        {
            get => _newColumnName;
            set
            {
                SetProperty(ref _newColumnName, value);
                AddColumnCommand.RaiseCanExecuteChanged();
            }
        }

        private string _newColumnType = "int";
        public string NewColumnType
        {
            get => _newColumnType;
            set
            {
                SetProperty(ref _newColumnType, value);
                RaisePropertyChanged(nameof(IsLengthEnabled));
                RaisePropertyChanged(nameof(IsPrecisionEnabled));
                RaisePropertyChanged(nameof(IsScaleEnabled));
                RaisePropertyChanged(nameof(IsIdentityEnabled));
                if (!IsLengthEnabled) NewColumnLength = string.Empty;
                if (!IsPrecisionEnabled) NewColumnPrecision = string.Empty;
                if (!IsScaleEnabled) NewColumnScale = string.Empty;
                if (!IsIdentityEnabled) NewColumnIsIdentity = false;
            }
        }

        private string _newColumnLength = string.Empty;
        public string NewColumnLength
        {
            get => _newColumnLength;
            set => SetProperty(ref _newColumnLength, value);
        }

        private string _newColumnPrecision = string.Empty;
        public string NewColumnPrecision
        {
            get => _newColumnPrecision;
            set => SetProperty(ref _newColumnPrecision, value);
        }

        private string _newColumnScale = string.Empty;
        public string NewColumnScale
        {
            get => _newColumnScale;
            set => SetProperty(ref _newColumnScale, value);
        }

        public bool IsLengthEnabled => StringTypes.Contains(NewColumnType);
        public bool IsPrecisionEnabled => PrecisionTypes.Contains(NewColumnType);
        public bool IsScaleEnabled => DecimalTypes.Contains(NewColumnType);

        public bool IsIdentityEnabled => IdentityTypes.Contains(NewColumnType);

        private bool _newColumnIsIdentity;
        public bool NewColumnIsIdentity
        {
            get => _newColumnIsIdentity;
            set => SetProperty(ref _newColumnIsIdentity, value);
        }

        private bool _newColumnNullable = true;
        public bool NewColumnNullable
        {
            get => _newColumnNullable;
            set => SetProperty(ref _newColumnNullable, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                RaisePropertyChanged(nameof(IsNotBusy));
                RefreshDatabasesCommand.RaiseCanExecuteChanged();
                CreateDatabaseCommand.RaiseCanExecuteChanged();
                DropDatabaseCommand.RaiseCanExecuteChanged();
                CreateTableCommand.RaiseCanExecuteChanged();
                DropTableCommand.RaiseCanExecuteChanged();
                AddColumnCommand.RaiseCanExecuteChanged();
                DropColumnCommand.RaiseCanExecuteChanged();
                DownloadTemplateCommand.RaiseCanExecuteChanged();
                BulkImportFromExcelCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsNotBusy => !_isBusy;

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _bulkImportResult = string.Empty;
        public string BulkImportResult
        {
            get => _bulkImportResult;
            set
            {
                SetProperty(ref _bulkImportResult, value);
                RaisePropertyChanged(nameof(HasBulkImportResult));
            }
        }

        public bool HasBulkImportResult => !string.IsNullOrEmpty(_bulkImportResult);

        public DelegateCommand RefreshDatabasesCommand { get; }
        public DelegateCommand CreateDatabaseCommand { get; }
        public DelegateCommand DropDatabaseCommand { get; }
        public DelegateCommand CreateTableCommand { get; }
        public DelegateCommand DropTableCommand { get; }
        public DelegateCommand AddColumnCommand { get; }
        public DelegateCommand<string> DropColumnCommand { get; }
        public DelegateCommand AddNewColumnDefCommand { get; }
        public DelegateCommand DownloadTemplateCommand { get; }
        public DelegateCommand BulkImportFromExcelCommand { get; }

        public DatabaseManageViewModel(IDatabaseService dbService, IConnectionService connService, IExcelService excelService)
        {
            _dbService = dbService;
            _connService = connService;
            _excelService = excelService;

            RefreshDatabasesCommand = new DelegateCommand(
                async () => await RefreshDatabasesAsync(), () => IsNotBusy);

            CreateDatabaseCommand = new DelegateCommand(
                async () => await CreateDatabaseAsync(),
                () => IsNotBusy && !string.IsNullOrWhiteSpace(NewDatabaseName));

            DropDatabaseCommand = new DelegateCommand(
                async () => await DropDatabaseAsync(),
                () => IsNotBusy && !string.IsNullOrEmpty(SelectedDatabase));

            CreateTableCommand = new DelegateCommand(
                async () => await CreateTableAsync(),
                () => IsNotBusy && !string.IsNullOrEmpty(SelectedDatabase) &&
                      !string.IsNullOrWhiteSpace(NewTableName) && NewColumns.Count > 0);

            DropTableCommand = new DelegateCommand(
                async () => await DropTableAsync(),
                () => IsNotBusy && SelectedTable != null && !string.IsNullOrEmpty(SelectedDatabase));

            AddColumnCommand = new DelegateCommand(
                async () => await AddColumnAsync(),
                () => IsNotBusy && SelectedTable != null && !string.IsNullOrEmpty(SelectedDatabase) &&
                      !string.IsNullOrWhiteSpace(NewColumnName));

            DropColumnCommand = new DelegateCommand<string>(
                async (colName) => await DropColumnAsync(colName),
                (_) => IsNotBusy && SelectedTable != null);

            AddNewColumnDefCommand = new DelegateCommand(
                () => NewColumns.Add(new NewColumnDef()));

            DownloadTemplateCommand = new DelegateCommand(
                async () => await DownloadTemplateAsync(), () => IsNotBusy);

            BulkImportFromExcelCommand = new DelegateCommand(
                async () => await BulkImportFromExcelAsync(), () => IsNotBusy);

            _newColumns.CollectionChanged += (_, _) => CreateTableCommand.RaiseCanExecuteChanged();

            _connService.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(IConnectionService.IsServerConnected) && _connService.IsServerConnected)
                    await RefreshDatabasesAsync();
            };

            if (_connService.IsServerConnected)
                _ = RefreshDatabasesAsync();
        }

        private async Task RefreshDatabasesAsync()
        {
            IsBusy = true;
            StatusText = "正在加载数据库列表...";
            try
            {
                var dbs = await _dbService.GetAllDatabasesAsync(_connService.ConnectionString);
                AllDatabases = new ObservableCollection<string>(dbs);
                StatusText = $"已加载 {dbs.Count} 个数据库";
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 加载失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadTablesAsync(string databaseName)
        {
            IsBusy = true;
            StatusText = $"正在加载 [{databaseName}] 的表...";
            SelectedTable = null;
            ColumnInfos = new ObservableCollection<ColumnInfo>();
            try
            {
                var tables = await _dbService.GetAllTablesAsync(GetConnectionString(databaseName));
                AllTables = new ObservableCollection<TableInfo>(tables);
                StatusText = $"[{databaseName}] 共 {tables.Count} 个表";
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 加载失败: {ex.Message}";
                AllTables = new ObservableCollection<TableInfo>();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadColumnsAsync(string databaseName, string tableName)
        {
            IsBusy = true;
            StatusText = $"正在加载 [{tableName}] 的表结构...";
            try
            {
                var columns = await _dbService.GetTableSchemaAsync(GetConnectionString(databaseName), tableName);
                ColumnInfos = new ObservableCollection<ColumnInfo>(columns);
                StatusText = $"[{tableName}] 共 {columns.Count} 个字段";
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 加载失败: {ex.Message}";
                ColumnInfos = new ObservableCollection<ColumnInfo>();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateDatabaseAsync()
        {
            IsBusy = true;
            StatusText = $"正在创建数据库 [{NewDatabaseName}]...";
            try
            {
                await _dbService.CreateDatabaseAsync(_connService.ConnectionString, NewDatabaseName);
                StatusText = $"✓ 数据库 [{NewDatabaseName}] 创建成功";
                NewDatabaseName = string.Empty;
                await RefreshDatabasesAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 创建失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DropDatabaseAsync()
        {
            if (string.IsNullOrEmpty(SelectedDatabase)) return;
            IsBusy = true;
            StatusText = $"正在删除数据库 [{SelectedDatabase}]...";
            try
            {
                await _dbService.DropDatabaseAsync(_connService.ConnectionString, SelectedDatabase);
                StatusText = $"✓ 数据库 [{SelectedDatabase}] 已删除";
                SelectedDatabase = null;
                AllTables = new ObservableCollection<TableInfo>();
                ColumnInfos = new ObservableCollection<ColumnInfo>();
                await RefreshDatabasesAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 删除失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateTableAsync()
        {
            if (string.IsNullOrEmpty(SelectedDatabase)) return;
            IsBusy = true;
            StatusText = $"正在创建表 [{NewTableName}]...";
            try
            {
                var colsDef = string.Join(", ", NewColumns.Select(c => c.ToDefinition()));
                await _dbService.CreateTableAsync(GetConnectionString(SelectedDatabase), NewTableName, colsDef);
                StatusText = $"✓ 表 [{NewTableName}] 创建成功";
                NewTableName = string.Empty;
                NewColumns = new ObservableCollection<NewColumnDef>();
                await LoadTablesAsync(SelectedDatabase);
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 创建失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DropTableAsync()
        {
            if (SelectedTable == null || string.IsNullOrEmpty(SelectedDatabase)) return;
            IsBusy = true;
            StatusText = $"正在删除表 [{SelectedTable.TableName}]...";
            try
            {
                await _dbService.DropTableAsync(GetConnectionString(SelectedDatabase), SelectedTable.TableName);
                StatusText = $"✓ 表 [{SelectedTable.TableName}] 已删除";
                SelectedTable = null;
                ColumnInfos = new ObservableCollection<ColumnInfo>();
                await LoadTablesAsync(SelectedDatabase);
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 删除失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddColumnAsync()
        {
            if (SelectedTable == null || string.IsNullOrEmpty(SelectedDatabase)) return;
            IsBusy = true;
            StatusText = $"正在添加列 [{NewColumnName}]...";
            try
            {
                var typeDef = NewColumnDef.BuildTypeDef(NewColumnType, NewColumnLength, NewColumnPrecision, NewColumnScale);
                var nullDef = NewColumnNullable ? "NULL" : "NOT NULL";
                var sql = NewColumnIsIdentity
                    ? $"ALTER TABLE [{SelectedTable.TableName}] ADD [{NewColumnName}] {typeDef} {nullDef} IDENTITY(1,1)"
                    : $"ALTER TABLE [{SelectedTable.TableName}] ADD [{NewColumnName}] {typeDef} {nullDef}";
                await _dbService.ExecuteNonQueryAsync(GetConnectionString(SelectedDatabase), sql);
                StatusText = $"✓ 列 [{NewColumnName}] 添加成功";
                NewColumnName = string.Empty;
                NewColumnLength = string.Empty;
                NewColumnPrecision = string.Empty;
                NewColumnScale = string.Empty;
                NewColumnIsIdentity = false;
                await LoadColumnsAsync(SelectedDatabase, SelectedTable.TableName);
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 添加失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DropColumnAsync(string? columnName)
        {
            if (SelectedTable == null || string.IsNullOrEmpty(SelectedDatabase) || string.IsNullOrEmpty(columnName)) return;
            IsBusy = true;
            StatusText = $"正在删除列 [{columnName}]...";
            try
            {
                var sql = $"ALTER TABLE [{SelectedTable.TableName}] DROP COLUMN [{columnName}]";
                await _dbService.ExecuteNonQueryAsync(GetConnectionString(SelectedDatabase), sql);
                StatusText = $"✓ 列 [{columnName}] 已删除";
                await LoadColumnsAsync(SelectedDatabase, SelectedTable.TableName);
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 删除失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string GetConnectionString(string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(_connService.ConnectionString)
            {
                InitialCatalog = databaseName
            };
            return builder.ConnectionString;
        }

        private async Task DownloadTemplateAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = $"{(!string.IsNullOrWhiteSpace(NewTableName) ? NewTableName : "表")}_字段模板.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            IsBusy = true;
            StatusText = "正在生成模板...";
            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("字段定义");

                string[] headers = ["字段名", "类型", "长度", "精度", "小数位", "可空", "自增"];
                for (int i = 0; i < headers.Length; i++)
                    sheet.Cell(1, i + 1).Value = headers[i];

                int row = 2;
                bool hasExisting = false;
                if (!string.IsNullOrEmpty(SelectedDatabase) && !string.IsNullOrWhiteSpace(NewTableName))
                {
                    var columns = await _dbService.GetTableSchemaAsync(GetConnectionString(SelectedDatabase), NewTableName);
                    if (columns.Count > 0)
                    {
                        foreach (var col in columns)
                        {
                            sheet.Cell(row, 1).Value = col.ColumnName;
                            sheet.Cell(row, 2).Value = col.DataType;
                            sheet.Cell(row, 3).Value = col.MaxLength?.ToString() ?? "";
                            sheet.Cell(row, 4).Value = ColumnInfo.PrecisionDisplayTypes.Contains(col.DataType) ? col.NumericPrecision?.ToString() ?? "" : "";
                            sheet.Cell(row, 5).Value = ColumnInfo.ScaleDisplayTypes.Contains(col.DataType) ? col.NumericScale?.ToString() ?? "" : "";
                            sheet.Cell(row, 6).Value = col.IsNullable ? "是" : "否";
                            sheet.Cell(row, 7).Value = col.IsIdentity ? "是" : "否";
                            row++;
                        }
                        hasExisting = true;
                    }
                }
                if (!hasExisting)
                {
                    sheet.Cell(row, 1).Value = "id";
                    sheet.Cell(row, 2).Value = "int";
                    sheet.Cell(row, 6).Value = "否";
                    sheet.Cell(row, 7).Value = "是";
                    row++;
                    sheet.Cell(row, 1).Value = "name";
                    sheet.Cell(row, 2).Value = "varchar";
                    sheet.Cell(row, 3).Value = "50";
                    sheet.Cell(row, 6).Value = "是";
                    sheet.Cell(row, 7).Value = "否";
                }

                workbook.SaveAs(dialog.FileName);
                StatusText = $"✓ 模板已保存到 {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 生成模板失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task BulkImportFromExcelAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                Title = "选择字段定义Excel文件"
            };
            if (dialog.ShowDialog() != true) return;

            IsBusy = true;
            StatusText = "正在导入...";
            try
            {
                var dt = await Task.Run(() => _excelService.ReadExcel(dialog.FileName));
                if (dt.Rows.Count == 0)
                {
                    BulkImportResult = "✗ Excel 文件中没有数据行";
                    return;
                }

                int success = 0;
                var errors = new List<string>();

                for (int r = 0; r < dt.Rows.Count; r++)
                {
                    var row = dt.Rows[r];
                    var colName = row[0]?.ToString()?.Trim() ?? "";
                    var colType = row[1]?.ToString()?.Trim() ?? "";
                    var length   = row[2]?.ToString()?.Trim() ?? "";
                    var precision = row[3]?.ToString()?.Trim() ?? "";
                    var scale    = row[4]?.ToString()?.Trim() ?? "";
                    var nullable = row[5]?.ToString()?.Trim() ?? "";
                    var identity = row[6]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrEmpty(colName))
                    {
                        errors.Add($"第{r + 1}行: 字段名不能为空");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(colType) && !CommonColumnTypes.Contains(colType, StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"第{r + 1}行 '{colName}': 类型 '{colType}' 不在有效类型列表中");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(length) && (!int.TryParse(length, out int vl) || vl <= 0))
                    {
                        errors.Add($"第{r + 1}行 '{colName}': 长度 '{length}' 应为正整数");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(precision) && (!int.TryParse(precision, out int vp) || vp <= 0))
                    {
                        errors.Add($"第{r + 1}行 '{colName}': 精度 '{precision}' 应为正整数");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(scale) && (!int.TryParse(scale, out int vs) || vs <= 0))
                    {
                        errors.Add($"第{r + 1}行 '{colName}': 小数位 '{scale}' 应为正整数");
                        continue;
                    }

                    bool isNull = !nullable.Equals("否", StringComparison.OrdinalIgnoreCase)
                               && !nullable.Equals("NO", StringComparison.OrdinalIgnoreCase)
                               && !nullable.Equals("false", StringComparison.OrdinalIgnoreCase)
                               && !nullable.Equals("0");
                    bool isIdent = identity.Equals("是", StringComparison.OrdinalIgnoreCase)
                                || identity.Equals("YES", StringComparison.OrdinalIgnoreCase)
                                || identity.Equals("true", StringComparison.OrdinalIgnoreCase)
                                || identity.Equals("1");

                    NewColumns.Add(new NewColumnDef
                    {
                        ColumnName = colName,
                        ColumnType = colType,
                        Length = length,
                        Precision = precision,
                        Scale = scale,
                        IsNullable = isNull,
                        IsIdentity = isIdent
                    });
                    success++;
                }

                var result = $"✓ 成功导入 {success} 个字段";
                if (errors.Count > 0)
                    result += "\n  " + string.Join("\n  ", errors);
                BulkImportResult = result;
                StatusText = $"✓ 成功导入 {success} 个字段";
            }
            catch (Exception ex)
            {
                BulkImportResult = $"✗ 导入失败: {ex.Message}";
                StatusText = $"✗ 导入失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
