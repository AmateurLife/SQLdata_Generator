using System;
using System.Collections.Generic;

namespace SQLdata_Generator.Models
{
    public class ColumnInfo
    {
        internal static readonly HashSet<string> PrecisionDisplayTypes = new(StringComparer.OrdinalIgnoreCase)
            { "decimal", "numeric", "money", "smallmoney", "datetime2", "datetimeoffset", "time", "float", "real" };

        internal static readonly HashSet<string> ScaleDisplayTypes = new(StringComparer.OrdinalIgnoreCase)
            { "decimal", "numeric", "money", "smallmoney" };

        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public int? MaxLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }

        public string TypeDisplay
        {
            get
            {
                if (NumericPrecision.HasValue && NumericScale.HasValue && NumericScale.Value > 0
                    && ScaleDisplayTypes.Contains(DataType))
                    return $"{DataType}({NumericPrecision}, {NumericScale})";
                if (MaxLength.HasValue && MaxLength.Value > 0)
                    return $"{DataType}({MaxLength})";
                if (NumericPrecision.HasValue && NumericPrecision.Value > 0
                    && PrecisionDisplayTypes.Contains(DataType))
                    return $"{DataType}({NumericPrecision})";
                return DataType;
            }
        }
    }
}
