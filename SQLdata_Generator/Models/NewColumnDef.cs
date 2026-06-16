using Prism.Mvvm;

namespace SQLdata_Generator.Models
{
    public class NewColumnDef : BindableBase
    {
        private string _columnName = string.Empty;
        public string ColumnName
        {
            get => _columnName;
            set => SetProperty(ref _columnName, value);
        }

        private string _columnType = "int";
        public string ColumnType
        {
            get => _columnType;
            set => SetProperty(ref _columnType, value);
        }

        private string _length = string.Empty;
        public string Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        private string _precision = string.Empty;
        public string Precision
        {
            get => _precision;
            set => SetProperty(ref _precision, value);
        }

        private string _scale = string.Empty;
        public string Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        private bool _isNullable = true;
        public bool IsNullable
        {
            get => _isNullable;
            set => SetProperty(ref _isNullable, value);
        }

        public string ToDefinition()
        {
            var typeDef = BuildTypeDef(ColumnType, Length, Precision, Scale);
            return $"[{ColumnName}] {typeDef} {(IsNullable ? "NULL" : "NOT NULL")}";
        }

        public static string BuildTypeDef(string columnType, string length, string precision, string scale)
        {
            if (int.TryParse(precision, out int p) && p > 0 && int.TryParse(scale, out int s) && s > 0)
                return $"{columnType}({p}, {s})";
            if (int.TryParse(precision, out int p2) && p2 > 0)
                return $"{columnType}({p2})";
            if (int.TryParse(length, out int len) && len > 0)
                return $"{columnType}({len})";
            return columnType;
        }
    }
}
