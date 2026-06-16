namespace SQLdata_Generator.Models
{
    public class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public int? MaxLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }

        public string TypeDisplay
        {
            get
            {
                if (NumericPrecision.HasValue && NumericScale.HasValue && NumericScale.Value > 0)
                    return $"{DataType}({NumericPrecision}, {NumericScale})";
                if (MaxLength.HasValue && MaxLength.Value > 0)
                    return $"{DataType}({MaxLength})";
                if (NumericPrecision.HasValue && NumericPrecision.Value > 0)
                    return $"{DataType}({NumericPrecision})";
                return DataType;
            }
        }
    }
}
