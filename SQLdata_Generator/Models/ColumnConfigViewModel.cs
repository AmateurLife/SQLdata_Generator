using System.Collections.Generic;
using Prism.Mvvm;

namespace SQLdata_Generator.Models
{
    public class ColumnConfigViewModel : BindableBase
    {
        public ColumnInfo ColumnInfo { get; }

        public string ColumnName => ColumnInfo.ColumnName;
        public string DataType => ColumnInfo.DataType;

        public List<string> AvailableModes { get; }

        private string _selectedMode = string.Empty;
        public string SelectedMode
        {
            get => _selectedMode;
            set
            {
                SetProperty(ref _selectedMode, value);
                RaisePropertyChanged(nameof(IsNumericMode));
                RaisePropertyChanged(nameof(IsFixedMode));
                RaisePropertyChanged(nameof(IsDatasetMode));
                RaisePropertyChanged(nameof(IsAutoBackwardMode));
            }
        }

        private string _minValue = string.Empty;
        public string MinValue
        {
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }

        private string _maxValue = string.Empty;
        public string MaxValue
        {
            get => _maxValue;
            set => SetProperty(ref _maxValue, value);
        }

        private string _fixedValue = string.Empty;
        public string FixedValue
        {
            get => _fixedValue;
            set => SetProperty(ref _fixedValue, value);
        }

        private string _datasetValues = string.Empty;
        public string DatasetValues
        {
            get => _datasetValues;
            set => SetProperty(ref _datasetValues, value);
        }

        public bool IsNumericMode => SelectedMode == "范围随机";
        public bool IsFixedMode => SelectedMode == "固定值";
        public bool IsDatasetMode => SelectedMode == "数据集随机";
        public bool IsAutoBackwardMode => SelectedMode == "时间倒推";

        public ColumnConfigViewModel(ColumnInfo columnInfo)
        {
            ColumnInfo = columnInfo;
            var type = columnInfo.DataType.ToLowerInvariant();

            if (IsNumericType(type))
            {
                AvailableModes = ["范围随机"];
                SelectedMode = "范围随机";
            }
            else if (IsDateTimeType(type))
            {
                AvailableModes = ["时间倒推"];
                SelectedMode = "时间倒推";
            }
            else if (IsStringType(type) || type == "bit")
            {
                AvailableModes = ["固定值", "数据集随机"];
                SelectedMode = "固定值";
            }
            else
            {
                AvailableModes = ["固定值"];
                SelectedMode = "固定值";
            }
        }

        private static bool IsNumericType(string type)
        {
            return type is "int" or "bigint" or "smallint" or "tinyint"
                or "decimal" or "numeric" or "float" or "real"
                or "money" or "smallmoney";
        }

        private static bool IsStringType(string type)
        {
            return type is "varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext";
        }

        private static bool IsDateTimeType(string type)
        {
            return type is "datetime" or "datetime2" or "date" or "smalldatetime" or "datetimeoffset" or "time";
        }
    }
}
