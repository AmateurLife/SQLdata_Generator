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
                    TestConnectionCommand.RaiseCanExecuteChanged();
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
                    TestConnectionCommand.RaiseCanExecuteChanged();
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

        public DelegateCommand TestConnectionCommand { get; }

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

            TestConnectionCommand = new DelegateCommand(
                async () => await TestConnectionAsync(),
                () => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(Database));
        }

        private async Task TestConnectionAsync()
        {
            ConnectionStatus = "正在连接...";
            try
            {
                var success = await _dbService.TestConnectionAsync(_connService.ConnectionString);
                _connService.IsConnected = success;

                if (success)
                {
                    var tables = await _dbService.GetAllTablesAsync(_connService.ConnectionString);
                    _connService.TableCount = tables.Count;
                    ConnectionStatus = $"✓ 已连接，共发现 {tables.Count} 个表";
                }
                else
                {
                    ConnectionStatus = "✗ 连接失败";
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"✗ 连接失败: {ex.Message}";
                _connService.IsConnected = false;
            }
        }
    }
}
