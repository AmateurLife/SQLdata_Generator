#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using SQLdata_Generator.Services;

namespace SQLdata_Generator.ViewModels
{
    public class DatabaseLoginViewModel : BindableBase
    {
        private readonly IConnectionService _connService;
        private readonly IDatabaseService _dbService;

        public IConnectionService ConnectionService => _connService;

        public List<string> AuthTypes { get; } = ["SQL Server认证", "Windows集成认证"];

        public string Server
        {
            get => _connService.Server;
            set
            {
                if (_connService.Server != value)
                {
                    _connService.Server = value;
                    RaisePropertyChanged();
                    ServerConnectCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Database
        {
            get => _connService.Database;
            set
            {
                if (_connService.Database != value)
                {
                    _connService.Database = value;
                    RaisePropertyChanged();
                    if (!string.IsNullOrEmpty(value) && _connService.IsServerConnected)
                        _ = LoadTablesAsync();
                }
            }
        }

        public string AuthType
        {
            get => _connService.AuthType;
            set
            {
                if (_connService.AuthType != value)
                {
                    _connService.AuthType = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsSqlAuth));
                }
            }
        }

        public string UserId
        {
            get => _connService.UserId;
            set
            {
                if (_connService.UserId != value)
                {
                    _connService.UserId = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Password
        {
            get => _connService.Password;
            set
            {
                if (_connService.Password != value)
                {
                    _connService.Password = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsSqlAuth => _connService.IsSqlAuth;

        public string ConnectionStatus
        {
            get => _connService.ConnectionStatus;
            set
            {
                _connService.ConnectionStatus = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand ServerConnectCommand { get; }

        public DatabaseLoginViewModel(IConnectionService connService, IDatabaseService dbService)
        {
            _connService = connService;
            _dbService = dbService;

            _connService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IConnectionService.AuthType))
                {
                    RaisePropertyChanged(nameof(IsSqlAuth));
                }
            };

            ServerConnectCommand = new DelegateCommand(
                async () => await ConnectToServerAsync(),
                () => !string.IsNullOrWhiteSpace(Server));
        }

        private async Task ConnectToServerAsync()
        {
            ConnectionStatus = "正在连接服务器...";
            _connService.Database = string.Empty;
            _connService.DatabaseList = new List<string>();
            _connService.IsServerConnected = false;
            _connService.TableCount = 0;

            try
            {
                var success = await _dbService.TestConnectionAsync(_connService.ConnectionString);
                _connService.IsServerConnected = success;

                if (success)
                {
                    var databases = await _dbService.GetAllDatabasesAsync(_connService.ConnectionString);
                    _connService.DatabaseList = databases;
                    ConnectionStatus = $"✓ 已连接服务器，共 {databases.Count} 个库，请选择数据库";
                }
                else
                {
                    ConnectionStatus = "✗ 服务器连接失败";
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"✗ 服务器连接失败: {ex.Message}";
                _connService.IsServerConnected = false;
            }
        }

        private async Task LoadTablesAsync()
        {
            ConnectionStatus = "正在加载表信息...";
            try
            {
                var tables = await _dbService.GetAllTablesAsync(_connService.ConnectionString);
                _connService.TableCount = tables.Count;
                ConnectionStatus = $"✓ 已连接，共 {tables.Count} 个表";
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"✗ 加载表信息失败: {ex.Message}";
                _connService.Database = string.Empty;
            }
        }
    }
}
