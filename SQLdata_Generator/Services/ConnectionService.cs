using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Prism.Mvvm;

namespace SQLdata_Generator.Services
{
    public class ConnectionService : BindableBase, IConnectionService
    {
        private string _server = "localhost";
        public string Server
        {
            get => _server;
            set => SetProperty(ref _server, value);
        }

        private string _authType = "SQL Server认证";
        public string AuthType
        {
            get => _authType;
            set
            {
                SetProperty(ref _authType, value);
                RaisePropertyChanged(nameof(IsSqlAuth));
            }
        }

        private string _userId = "sa";
        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _isServerConnected;
        public bool IsServerConnected
        {
            get => _isServerConnected;
            set
            {
                SetProperty(ref _isServerConnected, value);
                RaisePropertyChanged(nameof(IsConnected));
            }
        }

        public bool IsConnected => _isServerConnected;

        private List<string> _databaseList = new();
        public List<string> DatabaseList
        {
            get => _databaseList;
            set => SetProperty(ref _databaseList, value);
        }

        private string _connectionStatus = string.Empty;
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        private int _tableCount;
        public int TableCount
        {
            get => _tableCount;
            set => SetProperty(ref _tableCount, value);
        }

        public bool IsSqlAuth => AuthType == "SQL Server认证";

        public string ConnectionString => BuildConnectionString();

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                InitialCatalog = "master",
                TrustServerCertificate = true
            };

            if (IsSqlAuth)
            {
                builder.UserID = UserId;
                builder.Password = Password;
            }
            else
            {
                builder.IntegratedSecurity = true;
            }

            return builder.ConnectionString;
        }
    }
}
