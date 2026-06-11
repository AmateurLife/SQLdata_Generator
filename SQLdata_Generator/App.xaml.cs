using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Prism.Ioc;
using SQLdata_Generator.Services;
using SQLdata_Generator.Views;

namespace SQLdata_Generator
{
    public partial class App
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

            var fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "iconfont.ttf");
            Resources["iconfont"] = new FontFamily(new Uri($"file:///{fontPath.Replace('\\', '/')}"), "iconfont");
        }

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
