using DownloadTimeCalculator.Services.Interfaces;
using DownloadTimeCalculator.ViewModels.Base;

namespace DownloadTimeCalculator.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly INetworkService _networkService;
        private CalculatorViewModel _calculatorViewModel;
        private AutoExitViewModel _autoExitViewModel;

        public MainViewModel(INetworkService networkService, ISystemPowerService powerService)
        {
            _networkService = networkService;
            _calculatorViewModel = new CalculatorViewModel();
            _autoExitViewModel = new AutoExitViewModel(networkService, powerService);
            
            _networkService.StartMonitoring();
        }

        public CalculatorViewModel CalculatorViewModel
        {
            get => _calculatorViewModel;
            set => SetProperty(ref _calculatorViewModel, value);
        }

        public AutoExitViewModel AutoExitViewModel
        {
            get => _autoExitViewModel;
            set => SetProperty(ref _autoExitViewModel, value);
        }

        public void Cleanup()
        {
            _networkService.StopMonitoring();
            _autoExitViewModel.Cleanup();
        }
    }
}

