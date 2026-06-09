namespace SQLdata_Generator.Models
{
    public class TableInfo
    {
        public string TableName { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;

        public override string ToString() => TableName;
    }
}
