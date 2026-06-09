using Prism.Mvvm;

namespace SQLdata_Generator.Models
{
    public class ColumnMappingViewModel : BindableBase
    {
        public string ColumnName { get; }
        public string DataType { get; }
        public bool IsNullable { get; }

        private bool _isMatched;
        public bool IsMatched
        {
            get => _isMatched;
            set => SetProperty(ref _isMatched, value);
        }

        private string _excelColumnName = string.Empty;
        public string ExcelColumnName
        {
            get => _excelColumnName;
            set => SetProperty(ref _excelColumnName, value);
        }

        private string _matchStatus = string.Empty;
        public string MatchStatus
        {
            get => _matchStatus;
            set => SetProperty(ref _matchStatus, value);
        }

        public ColumnMappingViewModel(ColumnInfo columnInfo)
        {
            ColumnName = columnInfo.ColumnName;
            DataType = columnInfo.DataType;
            IsNullable = columnInfo.IsNullable;
            _isMatched = false;
            _matchStatus = "未匹配";
        }
    }
}
