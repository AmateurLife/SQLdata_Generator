using System.Data;

namespace SQLdata_Generator.Services
{
    public interface IExcelService
    {
        DataTable ReadExcel(string filePath);
    }
}
