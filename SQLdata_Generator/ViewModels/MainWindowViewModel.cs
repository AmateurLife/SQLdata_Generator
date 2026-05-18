#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using SQLdata_Generator.Models;
using SQLdata_Generator.Services;

namespace SQLdata_Generator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IDatabaseService _databaseService;

        private string _connectionString = string.Empty;
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                SetProperty(ref _connectionString, value);
                TestConnectionCommand.RaiseCanExecuteChanged();
            }
        }

        private string _connectionStatus = string.Empty;
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref _isConnected, value);
                LoadSchemaCommand.RaiseCanExecuteChanged();
            }
        }

        private string _tableName = string.Empty;
        public string TableName
        {
            get => _tableName;
            set
            {
                SetProperty(ref _tableName, value);
                LoadSchemaCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isSchemaLoaded;
        public bool IsSchemaLoaded
        {
            get => _isSchemaLoaded;
            set
            {
                SetProperty(ref _isSchemaLoaded, value);
                GeneratePreviewCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<ColumnConfigViewModel> _columnConfigs = new();
        public ObservableCollection<ColumnConfigViewModel> ColumnConfigs
        {
            get => _columnConfigs;
            set => SetProperty(ref _columnConfigs, value);
        }

        private int _recordCount = 100;
        public int RecordCount
        {
            get => _recordCount;
            set
            {
                SetProperty(ref _recordCount, value);
                GeneratePreviewCommand.RaiseCanExecuteChanged();
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

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                RaisePropertyChanged(nameof(IsNotBusy));
                TestConnectionCommand.RaiseCanExecuteChanged();
                LoadSchemaCommand.RaiseCanExecuteChanged();
                GeneratePreviewCommand.RaiseCanExecuteChanged();
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

        public DelegateCommand TestConnectionCommand { get; }
        public DelegateCommand LoadSchemaCommand { get; }
        public DelegateCommand GeneratePreviewCommand { get; }
        public DelegateCommand SaveToDatabaseCommand { get; }

        public MainWindowViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;

            TestConnectionCommand = new DelegateCommand(
                async () => await TestConnectionAsync(),
                () => !IsBusy && !string.IsNullOrWhiteSpace(ConnectionString));

            LoadSchemaCommand = new DelegateCommand(
                async () => await LoadSchemaAsync(),
                () => !IsBusy && IsConnected && !string.IsNullOrWhiteSpace(TableName));

            GeneratePreviewCommand = new DelegateCommand(
                GeneratePreview,
                () => !IsBusy && IsSchemaLoaded && RecordCount > 0);

            SaveToDatabaseCommand = new DelegateCommand(
                async () => await SaveToDatabaseAsync(),
                () => !IsBusy && PreviewData != null && PreviewData.Count > 0);

            Title = "SQL数据生成器";
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private async Task TestConnectionAsync()
        {
            IsBusy = true;
            ConnectionStatus = "正在连接...";
            try
            {
                var success = await _databaseService.TestConnectionAsync(ConnectionString);
                IsConnected = success;
                ConnectionStatus = success ? "✓ 连接成功" : "✗ 连接失败";
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"✗ 连接失败: {ex.Message}";
                IsConnected = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSchemaAsync()
        {
            IsBusy = true;
            try
            {
                var columns = await _databaseService.GetTableSchemaAsync(ConnectionString, TableName);
                if (columns.Count == 0)
                {
                    ConnectionStatus = "✗ 未找到该表，请检查表名";
                    IsSchemaLoaded = false;
                    return;
                }

                var configs = columns.Select(c => new ColumnConfigViewModel(c));
                ColumnConfigs = new ObservableCollection<ColumnConfigViewModel>(configs);
                IsSchemaLoaded = true;
                ConnectionStatus = $"✓ 已加载表 [{TableName}]，共 {columns.Count} 个字段";
                PreviewData = null;
                ProgressValue = 0;
                ProgressText = string.Empty;
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"✗ 加载失败: {ex.Message}";
                IsSchemaLoaded = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void GeneratePreview()
        {
            if (ColumnConfigs.Count == 0) return;

            var dt = new DataTable();

            foreach (var config in ColumnConfigs)
            {
                dt.Columns.Add(config.ColumnName, GetNetType(config));
            }

            var random = new Random();
            for (int i = 0; i < RecordCount; i++)
            {
                var row = dt.NewRow();
                foreach (var config in ColumnConfigs)
                {
                    row[config.ColumnName] = GenerateValue(config, random, i) ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }

            PreviewData = dt.DefaultView;
            ProgressText = $"预览生成完成，共 {RecordCount} 条记录";
        }

        private object? GenerateValue(ColumnConfigViewModel config, Random random, int rowIndex)
        {
            switch (config.SelectedMode)
            {
                case "范围随机":
                    return GenerateNumericValue(config, random);
                case "固定值":
                    if (string.IsNullOrEmpty(config.FixedValue))
                        return DBNull.Value;
                    if (IsBitType(config))
                        return config.FixedValue.Trim().ToLowerInvariant() is "true" or "1";
                    return config.FixedValue;
                case "数据集随机":
                    return PickFromDataset(config, random);
                case "时间倒推":
                    return GenerateDateTimeValue(random);
                default:
                    return DBNull.Value;
            }
        }

        private object GenerateNumericValue(ColumnConfigViewModel config, Random random)
        {
            if (!double.TryParse(config.MinValue, out double min) ||
                !double.TryParse(config.MaxValue, out double max))
                return 0;

            if (min > max)
                (min, max) = (max, min);

            double value = min + random.NextDouble() * (max - min);
            int? scale = config.ColumnInfo.NumericScale;

            return config.ColumnInfo.DataType.ToLowerInvariant() switch
            {
                "int" or "smallint" or "tinyint" => (int)Math.Round(value),
                "bigint" => (long)Math.Round(value),
                "decimal" or "numeric" or "money" or "smallmoney" => Math.Round(value, scale ?? 2),
                "float" or "real" => value,
                _ => value
            };
        }

        private object PickFromDataset(ColumnConfigViewModel config, Random random)
        {
            if (string.IsNullOrWhiteSpace(config.DatasetValues))
                return DBNull.Value;

            var values = config.DatasetValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => v.Length > 0)
                .ToArray();

            if (values.Length == 0) return DBNull.Value;

            if (IsBitType(config))
            {
                var picked = values[random.Next(values.Length)];
                return picked.Trim().ToLowerInvariant() is "true" or "1";
            }

            return values[random.Next(values.Length)];
        }

        private static bool IsBitType(ColumnConfigViewModel config)
        {
            return config.ColumnInfo.DataType.Equals("bit", StringComparison.OrdinalIgnoreCase);
        }

        private DateTime GenerateDateTimeValue(Random random)
        {
            var now = DateTime.Now;
            var start = now.AddMinutes(-RecordCount);
            var range = (now - start).TotalMinutes;
            return start.AddMinutes(random.NextDouble() * range);
        }

        private static Type GetNetType(ColumnConfigViewModel config)
        {
            return config.ColumnInfo.DataType.ToLowerInvariant() switch
            {
                "int" or "smallint" or "tinyint" or "bigint" => typeof(long),
                "decimal" or "numeric" or "float" or "real" or "money" or "smallmoney" => typeof(double),
                "bit" => typeof(bool),
                "datetime" or "datetime2" or "date" or "smalldatetime" or "datetimeoffset" => typeof(DateTime),
                _ => typeof(string)
            };
        }

        private async Task SaveToDatabaseAsync()
        {
            if (PreviewData == null) return;

            IsBusy = true;
            ProgressValue = 0;
            ProgressMax = 100;
            ProgressText = "正在保存...";

            try
            {
                var dt = PreviewData.Table!;
                var progress = new Progress<int>(p =>
                {
                    ProgressValue = p;
                    ProgressText = $"已保存 {p}%...";
                });

                await _databaseService.InsertDataAsync(ConnectionString, TableName, dt, progress);
                ProgressText = $"✓ 保存完成，共插入 {dt.Rows.Count} 条记录到 [{TableName}]";
            }
            catch (Exception ex)
            {
                ProgressText = $"✗ 保存失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
