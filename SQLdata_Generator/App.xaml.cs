using System.Windows;
using Prism.Ioc;
using SQLdata_Generator.Services;
using SQLdata_Generator.Views;

namespace SQLdata_Generator
{
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IConnectionService, ConnectionService>();
            containerRegistry.RegisterSingleton<IDatabaseService, DatabaseService>();
            containerRegistry.RegisterSingleton<IExcelService, ExcelService>();

            containerRegistry.RegisterForNavigation<DataGeneratorView>();
            containerRegistry.RegisterForNavigation<DataImporterView>();
            containerRegistry.RegisterForNavigation<DatabaseLoginView>();
        }
    }
}
