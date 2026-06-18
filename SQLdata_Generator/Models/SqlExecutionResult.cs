#nullable enable

using System.Data;

namespace SQLdata_Generator.Models
{
    public class SqlExecutionResult
    {
        public bool IsQuery { get; set; }
        public DataTable? ResultTable { get; set; }
        public int RowsAffected { get; set; }
    }
}
