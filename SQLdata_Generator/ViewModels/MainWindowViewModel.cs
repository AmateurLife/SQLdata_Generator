using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using SQLdata_Generator.Views;

namespace SQLdata_Generator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        private string _title = "SQL数据生成器";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _currentView = "DatabaseManageView";
        public string CurrentView
        {
            get => _currentView;
            set
            {
                SetProperty(ref _currentView, value);
                RaisePropertyChanged(nameof(IsNotOnGeneratorView));
                RaisePropertyChanged(nameof(IsNotOnImporterView));
                RaisePropertyChanged(nameof(IsNotOnManageView));
                RaisePropertyChanged(nameof(IsNotOnDeveloperView));
            }
        }

        public bool IsNotOnGeneratorView => CurrentView != "DataGeneratorView";
        public bool IsNotOnImporterView => CurrentView != "DataImporterView";
        public bool IsNotOnManageView => CurrentView != "DatabaseManageView";
        public bool IsNotOnDeveloperView => CurrentView != "DeveloperView";

        public DelegateCommand NavigateGeneratorCommand { get; }
        public DelegateCommand NavigateImporterCommand { get; }
        public DelegateCommand NavigateManageCommand { get; }
        public DelegateCommand NavigateDeveloperCommand { get; }

        public MainWindowViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;

            NavigateGeneratorCommand = new DelegateCommand(() =>
            {
                _regionManager.RequestNavigate("ContentRegion", "DataGeneratorView");
                CurrentView = "DataGeneratorView";
            });

            NavigateImporterCommand = new DelegateCommand(() =>
            {
                _regionManager.RequestNavigate("ContentRegion", "DataImporterView");
                CurrentView = "DataImporterView";
            });

            NavigateManageCommand = new DelegateCommand(() =>
            {
                _regionManager.RequestNavigate("ContentRegion", "DatabaseManageView");
                CurrentView = "DatabaseManageView";
            });

            NavigateDeveloperCommand = new DelegateCommand(() =>
            {
                _regionManager.RequestNavigate("ContentRegion", "DeveloperView");
                CurrentView = "DeveloperView";
            });

            regionManager.RegisterViewWithRegion("LoginRegion", typeof(DatabaseLoginView));
            regionManager.RegisterViewWithRegion("ContentRegion", typeof(DatabaseManageView));
        }
    }
}
