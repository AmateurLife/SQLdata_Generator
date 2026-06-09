using System.ComponentModel;

namespace SQLdata_Generator.Services
{
    public interface IConnectionService : INotifyPropertyChanged
    {
        string Server { get; set; }
        string Database { get; set; }
        string AuthType { get; set; }
        string UserId { get; set; }
        string Password { get; set; }
        bool IsConnected { get; set; }
        string ConnectionStatus { get; set; }
        int TableCount { get; set; }
        string ConnectionString { get; }
        bool IsSqlAuth { get; }
    }
}
