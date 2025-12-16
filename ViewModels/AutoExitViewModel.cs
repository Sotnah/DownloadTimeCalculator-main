using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.Windows;
using DownloadTimeCalculator.Models;
using DownloadTimeCalculator.Services.Interfaces;
using DownloadTimeCalculator.ViewModels.Base;

namespace DownloadTimeCalculator.ViewModels
{
    public class AutoExitViewModel : ViewModelBase
    {
        private readonly INetworkService _networkService;
        private readonly ISystemPowerService _powerService;
        private DispatcherTimer? _clockTimer;
        private bool _isAutoExitEnabled = false;

        private bool _isShutdownPending = false;
        private bool _isThresholdError = false;
        private bool _isDurationError = false;
        private DateTime? _lowSpeedStartTime = null;
        private const double MIN_DOWNLOAD_SPEED_THRESHOLD = 12500; // 100 kbps = 100000 bits/s = 12500 bytes/s
        private const int LOW_SPEED_DURATION_SECONDS = 60; // 60 seconds (1 minute)

        private PowerActionType _selectedPowerAction = PowerActionType.Shutdown;
        private double? _thresholdValue = null;
        private int _thresholdUnitIndex = 0; // 0: KB/s, 1: MB/s
        private int? _durationSeconds = null;
        private string? _selectedNetworkAdapter;

        private string _currentTime = string.Empty;
        private string _downloadSpeed = "0 B/s";
        private string _uploadSpeed = "0 B/s";
        private string _countdownText = string.Empty;

        public AutoExitViewModel(INetworkService networkService, ISystemPowerService powerService)
        {
            _networkService = networkService;
            _powerService = powerService;
            _networkService.NetworkStatsUpdated += NetworkService_NetworkStatsUpdated;

            // Default selection
            _selectedNetworkAdapter = "All Adapters";

            InitializeClock();
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string DownloadSpeed
        {
            get => _downloadSpeed;
            set => SetProperty(ref _downloadSpeed, value);
        }

        public string UploadSpeed
        {
            get => _uploadSpeed;
            set => SetProperty(ref _uploadSpeed, value);
        }

        public bool IsAutoExitEnabled
        {
            get => _isAutoExitEnabled;
            set
            {
                if (value)
                {
                    // Validation Check before enabling
                    bool hasError = false;
                    if (!_thresholdValue.HasValue)
                    {
                        IsThresholdError = true;
                        hasError = true;
                    }
                    if (!_durationSeconds.HasValue)
                    {
                        IsDurationError = true;
                        hasError = true;
                    }

                    if (hasError)
                    {
                        // Do not enable, notify change to keep UI in sync (off)
                        // Use Dispatcher to ensure UI updates after current binding operation
                        Application.Current.Dispatcher.InvokeAsync(() => OnPropertyChanged(nameof(IsAutoExitEnabled)));
                        return;
                    }
                }

                if (SetProperty(ref _isAutoExitEnabled, value))
                {
                    _lowSpeedStartTime = null;
                    _isShutdownPending = false;
                    CountdownText = string.Empty;
                }
            }
        }

        public bool IsThresholdError
        {
            get => _isThresholdError;
            set => SetProperty(ref _isThresholdError, value);
        }

        public bool IsDurationError
        {
            get => _isDurationError;
            set => SetProperty(ref _isDurationError, value);
        }

        public IEnumerable<PowerActionType> PowerActions => Enum.GetValues(typeof(PowerActionType)).Cast<PowerActionType>();
        public IEnumerable<string> NetworkAdapters => _networkService.GetAvailableAdapters();

        public string? SelectedNetworkAdapter
        {
            get => _selectedNetworkAdapter;
            set
            {
                if (SetProperty(ref _selectedNetworkAdapter, value))
                {
                    _networkService.SetTargetAdapter(value);
                }
            }
        }

        public PowerActionType SelectedPowerAction
        {
            get => _selectedPowerAction;
            set => SetProperty(ref _selectedPowerAction, value);
        }

        public double? ThresholdValue
        {
            get => _thresholdValue;
            set
            {
                if (SetProperty(ref _thresholdValue, value))
                {
                    if (value.HasValue)
                    {
                        IsThresholdError = false;
                    }
                }
            }
        }

        public int ThresholdUnitIndex
        {
            get => _thresholdUnitIndex;
            set => SetProperty(ref _thresholdUnitIndex, value);
        }

        public int? DurationSeconds
        {
            get => _durationSeconds;
            set
            {
                if (SetProperty(ref _durationSeconds, value))
                {
                    if (value.HasValue)
                    {
                        IsDurationError = false;
                    }
                }
            }
        }

        public string CountdownText
        {
            get => _countdownText;
            set => SetProperty(ref _countdownText, value);
        }

        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
            ClockTimer_Tick(null, EventArgs.Empty);
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void NetworkService_NetworkStatsUpdated(object? sender, NetworkStats stats)
        {
            DownloadSpeed = stats.FormattedDownloadSpeed;
            UploadSpeed = stats.FormattedUploadSpeed;

            if (_isAutoExitEnabled && !_isShutdownPending)
            {
                CheckAutoExit(stats.DownloadSpeedBytesPerSecond);
            }
        }

        private void CheckAutoExit(double downloadSpeedBytesPerSecond)
        {
            DateTime currentTime = DateTime.Now;

            // Ensure configuration is valid
            if (!_thresholdValue.HasValue || !_durationSeconds.HasValue)
            {
                return;
            }

            // Calculate threshold in bytes/sec
            double thresholdBytes = _thresholdValue.Value * 1024; // Base KB
            if (_thresholdUnitIndex == 1) thresholdBytes *= 1024; // MB

            // If download speed is below threshold
            if (downloadSpeedBytesPerSecond < thresholdBytes)
            {
                // Start tracking low speed period
                if (!_lowSpeedStartTime.HasValue)
                {
                    _lowSpeedStartTime = currentTime;
                }
                else
                {
                    // Check if low speed has persisted for the required duration
                    double lowSpeedDuration = (currentTime - _lowSpeedStartTime.Value).TotalSeconds;
                    int remainingSeconds = _durationSeconds.Value - (int)lowSpeedDuration;

                    if (remainingSeconds > 0)
                    {
                        // Update countdown display
                        CountdownText = $"Shutdown in {remainingSeconds}s";
                    }
                    else
                    {
                        _isShutdownPending = true;
                        CountdownText = "Performing Action...";
                        _powerService.PerformAction(_selectedPowerAction);
                    }
                }
            }
            else
            {
                // Download speed is above threshold, reset tracking
                _lowSpeedStartTime = null;
                CountdownText = string.Empty;
            }
        }

        public void Cleanup()
        {
            _clockTimer?.Stop();
            _networkService.NetworkStatsUpdated -= NetworkService_NetworkStatsUpdated;
        }
    }
}

