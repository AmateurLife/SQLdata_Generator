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

        private bool _isNullable = true;
        public bool IsNullable
        {
            get => _isNullable;
            set => SetProperty(ref _isNullable, value);
        }

        public string ToDefinition()
        {
            var typeDef = ColumnType;
            if (int.TryParse(Length, out int len) && len > 0)
                typeDef = $"{ColumnType}({len})";
            return $"[{ColumnName}] {typeDef} {(IsNullable ? "NULL" : "NOT NULL")}";
        }
    }
}
