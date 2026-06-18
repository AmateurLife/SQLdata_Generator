#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Prism.Commands;
using Prism.Mvvm;
using SQLdata_Generator.Models;
using SQLdata_Generator.Services;

namespace SQLdata_Generator.ViewModels
{
    public class DeveloperViewModel : BindableBase
    {
        private readonly IDatabaseService _dbService;
        private readonly IConnectionService _connService;

        public IConnectionService ConnectionService => _connService;

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
                if (SetProperty(ref _selectedDatabase, value))
                {
                    SelectedTable = null;
                    ColumnInfos = new ObservableCollection<ColumnInfo>();
                    TableData = null;
                    ClearResults();
                    if (!string.IsNullOrEmpty(value))
                        _ = LoadTablesAsync(value);
                }
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
                if (SetProperty(ref _selectedTable, value))
                {
                    if (value != null && !string.IsNullOrEmpty(SelectedDatabase))
                        _ = LoadTableInfoAsync(SelectedDatabase, value.TableName);
                    else
                    {
                        ColumnInfos = new ObservableCollection<ColumnInfo>();
                        TableData = null;
                    }
                }
            }
        }

        private ObservableCollection<ColumnInfo> _columnInfos = new();
        public ObservableCollection<ColumnInfo> ColumnInfos
        {
            get => _columnInfos;
            set => SetProperty(ref _columnInfos, value);
        }

        private DataView? _tableData;
        public DataView? TableData
        {
            get => _tableData;
            set => SetProperty(ref _tableData, value);
        }

        private bool _isShowingStructure = true;
        public bool IsShowingStructure
        {
            get => _isShowingStructure;
            set
            {
                if (SetProperty(ref _isShowingStructure, value))
                    RaisePropertyChanged(nameof(IsShowingData));
            }
        }

        public bool IsShowingData => !_isShowingStructure;

        private string _sqlText = string.Empty;
        public string SqlText
        {
            get => _sqlText;
            set
            {
                SetProperty(ref _sqlText, value);
                ExecuteSqlCommand.RaiseCanExecuteChanged();
            }
        }

        private DataView? _queryResults;
        public DataView? QueryResults
        {
            get => _queryResults;
            set
            {
                SetProperty(ref _queryResults, value);
                RaisePropertyChanged(nameof(HasQueryResults));
            }
        }

        public bool HasQueryResults => _queryResults != null && _queryResults.Count > 0;

        private string? _queryMessage;
        public string? QueryMessage
        {
            get => _queryMessage;
            set
            {
                SetProperty(ref _queryMessage, value);
                RaisePropertyChanged(nameof(HasQueryMessage));
            }
        }

        public bool HasQueryMessage => !string.IsNullOrEmpty(_queryMessage);

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                RaisePropertyChanged(nameof(IsNotBusy));
                RefreshDatabasesCommand.RaiseCanExecuteChanged();
                ExecuteSqlCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsNotBusy => !_isBusy;

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public DelegateCommand RefreshDatabasesCommand { get; }
        public DelegateCommand ShowStructureCommand { get; }
        public DelegateCommand ShowDataCommand { get; }
        public DelegateCommand ExecuteSqlCommand { get; }

        public DeveloperViewModel(IDatabaseService dbService, IConnectionService connService)
        {
            _dbService = dbService;
            _connService = connService;

            RefreshDatabasesCommand = new DelegateCommand(
                async () => await RefreshDatabasesAsync(), () => IsNotBusy);

            ShowStructureCommand = new DelegateCommand(
                () => IsShowingStructure = true);

            ShowDataCommand = new DelegateCommand(
                () => IsShowingStructure = false);

            ExecuteSqlCommand = new DelegateCommand(
                async () => await ExecuteSqlAsync(),
                () => IsNotBusy && !string.IsNullOrWhiteSpace(SqlText) && !string.IsNullOrEmpty(SelectedDatabase));

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
            TableData = null;
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

        private async Task LoadTableInfoAsync(string databaseName, string tableName)
        {
            IsBusy = true;
            StatusText = $"正在加载 [{tableName}] 的表信息...";
            try
            {
                var connStr = GetConnectionString(databaseName);

                var columns = await _dbService.GetTableSchemaAsync(connStr, tableName);
                ColumnInfos = new ObservableCollection<ColumnInfo>(columns);

                var dataResult = await _dbService.ExecuteSqlAsync(connStr,
                    $"SELECT TOP 5 * FROM [{tableName}]");
                TableData = dataResult.IsQuery ? dataResult.ResultTable?.DefaultView : null;

                StatusText = $"[{tableName}] 共 {columns.Count} 个字段";
            }
            catch (Exception ex)
            {
                StatusText = $"✗ 加载失败: {ex.Message}";
                ColumnInfos = new ObservableCollection<ColumnInfo>();
                TableData = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteSqlAsync()
        {
            if (string.IsNullOrWhiteSpace(SqlText) || string.IsNullOrEmpty(SelectedDatabase)) return;

            IsBusy = true;
            StatusText = "正在执行SQL...";
            QueryResults = null;
            QueryMessage = null;

            try
            {
                var connStr = GetConnectionString(SelectedDatabase);
                var result = await _dbService.ExecuteSqlAsync(connStr, SqlText);

                if (result.IsQuery)
                {
                    QueryResults = result.ResultTable?.DefaultView;
                    var rowCount = result.ResultTable?.Rows.Count ?? 0;
                    StatusText = $"✓ 查询完成，返回 {rowCount} 行";
                }
                else
                {
                    QueryMessage = $"执行成功，{result.RowsAffected} 行受影响";
                    StatusText = $"✓ 执行完成，{result.RowsAffected} 行受影响";
                }
            }
            catch (Exception ex)
            {
                QueryMessage = $"✗ 执行失败: {ex.Message}";
                StatusText = $"✗ 执行失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearResults()
        {
            QueryResults = null;
            QueryMessage = null;
        }

        private string GetConnectionString(string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(_connService.ConnectionString)
            {
                InitialCatalog = databaseName
            };
            return builder.ConnectionString;
        }
    }
}
