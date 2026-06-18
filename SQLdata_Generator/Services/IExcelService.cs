using System.Data;

namespace SQLdata_Generator.Services
{
    public interface IExcelService
    {
        DataTable ReadExcel(string filePath);
        void WriteExcel(string filePath, DataTable data);
    }
}
