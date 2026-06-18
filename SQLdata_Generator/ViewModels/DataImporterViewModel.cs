#nullable enable

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using SQLdata_Generator.Models;
using SQLdata_Generator.Services;

namespace SQLdata_Generator.ViewModels
{
    public class DataImporterViewModel : BindableBase, IDisposable
    {
        private readonly IDatabaseService _dbService;
        private readonly IExcelService _excelService;
        private readonly IConnectionService _connService;

        public IConnectionService ConnectionService => _connService;

        private string _selectedDatabase = string.Empty;
        public string SelectedDatabase
        {
            get => _selectedDatabase;
            set
            {
                if (SetProperty(ref _selectedDatabase, value))
                {
                    SelectedTable = null;
                    IsSchemaLoaded = false;
                    RaisePropertyChanged(nameof(IsDatabaseSelected));
                    LoadSchemaCommand.RaiseCanExecuteChanged();
                    if (!string.IsNullOrEmpty(value))
                        _ = LoadTablesAsync();
                }
            }
        }

        public bool IsDatabaseSelected => !string.IsNullOrEmpty(_selectedDatabase);

        private ObservableCollection<TableInfo> _tables = new();
        public ObservableCollection<TableInfo> Tables
        {
            get => _tables;
            set => SetProperty(ref _tables, value);
        }

        private TableInfo? _selectedTable;
        public TableInfo? SelectedTable
        {
            get => _selectedTable;
            set
            {
                SetProperty(ref _selectedTable, value);
                LoadSchemaCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isSchemaLoaded;
        public bool IsSchemaLoaded
        {
            get => _isSchemaLoaded;
            set => SetProperty(ref _isSchemaLoaded, value);
        }

        private ObservableCollection<ColumnMappingViewModel> _columnMappings = new();
        public ObservableCollection<ColumnMappingViewModel> ColumnMappings
        {
            get => _columnMappings;
            set => SetProperty(ref _columnMappings, value);
        }

        private string _excelFilePath = string.Empty;
        public string ExcelFilePath
        {
            get => _excelFilePath;
            set
            {
                SetProperty(ref _excelFilePath, value);
                ReadExcelCommand.RaiseCanExecuteChanged();
            }
        }

        private DataView? _previewData;
        public DataView? PreviewData
        {
            get => _previewData;
            set
            {
                SetProperty(ref _previewData, value);
                SaveToDatabaseCommand.RaiseCanExecuteChanged();
            }
        }

        private DataTable? _insertData;

        private string _previewInfo = string.Empty;
        public string PreviewInfo
        {
            get => _previewInfo;
            set => SetProperty(ref _previewInfo, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                RaisePropertyChanged(nameof(IsNotBusy));
                LoadSchemaCommand.RaiseCanExecuteChanged();
                ReadExcelCommand.RaiseCanExecuteChanged();
                SaveToDatabaseCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsNotBusy => !_isBusy;

        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private int _progressMax = 100;
        public int ProgressMax
        {
            get => _progressMax;
            set => SetProperty(ref _progressMax, value);
        }

        private string _progressText = string.Empty;
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public DelegateCommand LoadSchemaCommand { get; }
        public DelegateCommand BrowseExcelCommand { get; }
        public DelegateCommand ReadExcelCommand { get; }
        public DelegateCommand SaveToDatabaseCommand { get; }

        public DataImporterViewModel(IDatabaseService dbService, IExcelService excelService, IConnectionService connService)
        {
            _dbService = dbService;
            _excelService = excelService;
            _connService = connService;

            LoadSchemaCommand = new DelegateCommand(
                async () => await LoadSchemaAsync(),
                () => !IsBusy && IsDatabaseSelected && SelectedTable != null);

            BrowseExcelCommand = new DelegateCommand(
                BrowseExcel, () => !IsBusy);

            ReadExcelCommand = new DelegateCommand(
                ReadExcel, () => !IsBusy && IsSchemaLoaded && !string.IsNullOrWhiteSpace(ExcelFilePath));

            SaveToDatabaseCommand = new DelegateCommand(
                async () => await SaveToDatabaseAsync(),
                () => !IsBusy && _insertData != null && _insertData.Rows.Count > 0);

            _connService.PropertyChanged += OnConnServicePropertyChanged;
        }

        private async Task LoadTablesAsync()
        {
            try
            {
                var tables = await _dbService.GetAllTablesAsync(GetConnectionString(SelectedDatabase));
                Tables = new ObservableCollection<TableInfo>(tables);
            }
            catch
            {
                Tables = new ObservableCollection<TableInfo>();
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

        private async Task LoadSchemaAsync()
        {
            if (SelectedTable == null) return;

            IsBusy = true;
            try
            {
                var columns = await _dbService.GetTableSchemaAsync(GetConnectionString(SelectedDatabase), SelectedTable.TableName);
                if (columns.Count == 0)
                {
                    IsSchemaLoaded = false;
                    return;
                }

                var mappings = columns.Select(c => new ColumnMappingViewModel(c));
                ColumnMappings = new ObservableCollection<ColumnMappingViewModel>(mappings);
                IsSchemaLoaded = true;
                _insertData?.Dispose();
                _insertData = null;
                PreviewData = null;
                PreviewInfo = string.Empty;
                ProgressValue = 0;
                ProgressText = string.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void BrowseExcel()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                Title = "选择Excel文件"
            };
            if (dialog.ShowDialog() == true)
                ExcelFilePath = dialog.FileName;
        }

        private void ReadExcel()
        {
            if (!IsSchemaLoaded || string.IsNullOrWhiteSpace(ExcelFilePath)) return;

            IsBusy = true;
            try
            {
                var excelDt = _excelService.ReadExcel(ExcelFilePath);
                if (excelDt.Rows.Count == 0)
                {
                    PreviewInfo = "Excel文件中没有数据";
                    _insertData?.Dispose();
                    _insertData = null;
                    PreviewData = null;
                    return;
                }

                var excelHeaders = excelDt.Columns.Cast<DataColumn>()
                    .Select(c => c.ColumnName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var mapping in ColumnMappings)
                {
                    if (excelHeaders.Contains(mapping.ColumnName))
                    {
                        mapping.IsMatched = true;
                        mapping.ExcelColumnName = mapping.ColumnName;
                        mapping.MatchStatus = $"✓ {mapping.ColumnName}";
                    }
                    else
                    {
                        mapping.IsMatched = false;
                        mapping.ExcelColumnName = string.Empty;
                        mapping.MatchStatus = "⚠ 未匹配";
                    }
                }

                _insertData?.Dispose();
                _insertData = BuildInsertDataTable(excelDt);
                PreviewData = _insertData.DefaultView;
                PreviewInfo = $"共读取 {_insertData.Rows.Count} 条记录，" +
                              $"{ColumnMappings.Count(m => m.IsMatched)}/{ColumnMappings.Count} 个字段已匹配";
                SaveToDatabaseCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                PreviewInfo = $"✗ 读取Excel失败: {ex.Message}";
                PreviewData = null;
                _insertData = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private DataTable BuildInsertDataTable(DataTable excelDt)
        {
            var insertDt = new DataTable();
            var matchedMappings = ColumnMappings.Where(m => m.IsMatched).ToList();

            foreach (var mapping in matchedMappings)
                insertDt.Columns.Add(mapping.ColumnName);

            foreach (DataRow excelRow in excelDt.Rows)
            {
                var newRow = insertDt.NewRow();
                foreach (var mapping in matchedMappings)
                    newRow[mapping.ColumnName] = excelRow[mapping.ExcelColumnName] ?? DBNull.Value;
                insertDt.Rows.Add(newRow);
            }
            return insertDt;
        }

        private async Task SaveToDatabaseAsync()
        {
            if (_insertData == null || SelectedTable == null) return;

            IsBusy = true;
            ProgressValue = 0;
            ProgressMax = 100;
            ProgressText = "正在插入...";

            try
            {
                var progress = new Progress<int>(p =>
                {
                    ProgressValue = p;
                    ProgressText = $"已插入 {p}%...";
                });

                await _dbService.InsertDataAsync(GetConnectionString(SelectedDatabase), SelectedTable.TableName, _insertData, progress);
                ProgressText = $"✓ 插入完成，共同步 {_insertData.Rows.Count} 条记录到 [{SelectedTable.TableName}]";
            }
            catch (Exception ex)
            {
                ProgressText = $"✗ 插入失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnConnServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IConnectionService.IsServerConnected))
                LoadSchemaCommand.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            _connService.PropertyChanged -= OnConnServicePropertyChanged;
            _insertData?.Dispose();
        }
    }
}
