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
                    UpdateStatusState();
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

        public static IEnumerable<PowerActionType> PowerActions => Enum.GetValues(typeof(PowerActionType)).Cast<PowerActionType>();
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
                        if (value.Value > 10000)
                        {
                            _thresholdValue = 10000;
                            OnPropertyChanged(nameof(ThresholdValue));
                        }
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
                        if (value.Value > 86400)
                        {
                            _durationSeconds = 86400;
                            OnPropertyChanged(nameof(DurationSeconds));
                        }
                        IsDurationError = false;
                    }
                }
            }
        }


        private void InitializeClock()
        {
            if (_clockTimer == null)
            {
                _clockTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _clockTimer.Tick += ClockTimer_Tick;
            }
            if (!_clockTimer.IsEnabled) _clockTimer.Start();
            ClockTimer_Tick(null, EventArgs.Empty);
        }

        private string _statusBase = "Waiting for input";
        private string _statusSuffix = "";
        private bool _isActionActive = false;
        private bool _isWaitingForInput = true;
        private int _dotCounter = 0;

        public string StatusBase
        {
            get => _statusBase;
            set => SetProperty(ref _statusBase, value);
        }

        public string StatusSuffix
        {
            get => _statusSuffix;
            set => SetProperty(ref _statusSuffix, value);
        }

        public bool IsActionActive
        {
            get => _isActionActive;
            set => SetProperty(ref _isActionActive, value);
        }

        public bool IsWaitingForInput
        {
            get => _isWaitingForInput;
            set
            {
                if (SetProperty(ref _isWaitingForInput, value))
                {
                    // Reset suffix when leaving waiting state
                    if (!value) StatusSuffix = "";
                }
            }
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
            UpdateStatusState();
        }

        private void UpdateStatusState()
        {
            // 1. Check for Validation Errors
            bool hasErrors = !_thresholdValue.HasValue || !_durationSeconds.HasValue;

            if (hasErrors)
            {
                IsWaitingForInput = true;
                IsActionActive = false;
                // StatusBase = "Waiting for input"; // REMOVED: Keep old text for Fade Out transition

                // Animation Logic: "" -> "." -> ".." -> "..." -> ""
                _dotCounter = (_dotCounter + 1) % 4;
                StatusSuffix = new string('.', _dotCounter);

                // Reset other states
                _lowSpeedStartTime = null;
                return;
            }

            // 2. Valid Inputs - Calculate Action Text
            IsWaitingForInput = false; // Transitioning out of Waiting state
            string actionName = _selectedPowerAction.ToString();
            int duration = _durationSeconds ?? 0;

            // 3. Check if Active (Auto Exit Enabled AND Low Speed Triggered)
            if (_isAutoExitEnabled && !_isShutdownPending && _lowSpeedStartTime.HasValue)
            {
                // We are in countdown mode
                IsActionActive = true;

                // Recalculate remaining
                double lowSpeedDuration = (DateTime.Now - _lowSpeedStartTime.Value).TotalSeconds;
                int remainingSeconds = duration - (int)lowSpeedDuration;

                if (remainingSeconds > 0)
                {
                    StatusBase = $"{actionName} in {remainingSeconds}s";
                }
                else
                {
                    StatusBase = "Performing Action...";
                }
                StatusSuffix = "";
                return;
            }

            // 4. Default / Preview State (Valid but idle or high speed)
            IsActionActive = false;
            StatusBase = $"{actionName} in {duration}s";
            StatusSuffix = "";
        }

        private void NetworkService_NetworkStatsUpdated(object? sender, NetworkStats stats)
        {
            DownloadSpeed = stats.FormattedDownloadSpeed;
            UploadSpeed = stats.FormattedUploadSpeed;

            // Logic logic for tracking Low Speed
            if (_isAutoExitEnabled && !_isShutdownPending)
            {
                CheckSpeedThreshold(stats.DownloadSpeedBytesPerSecond);
            }
            else
            {
                // Reset if disabled
                _lowSpeedStartTime = null;
            }

            // Force status update on network tick too for responsiveness? 
            // Actually ClockTimer handles Animation (1s tick).
            // Network tick is faster. The Countdown update should be smooth.
            // Let's let ClockTimer handle visual updates to avoid spamming PropertyChanged events.
            // But we need to update _lowSpeedStartTime here.
        }

        private void CheckSpeedThreshold(double downloadSpeedBytesPerSecond)
        {
            if (!_thresholdValue.HasValue) return;

            // Calculate threshold in bytes/sec
            double thresholdBytes = _thresholdValue.Value * 1024; // Base KB
            if (_thresholdUnitIndex == 1) thresholdBytes *= 1024; // MB

            if (downloadSpeedBytesPerSecond < thresholdBytes)
            {
                if (!_lowSpeedStartTime.HasValue)
                {
                    _lowSpeedStartTime = DateTime.Now;
                    // Trigger state update immediately to switch to Green instantly
                    UpdateStatusState();
                }

                // Check for completion
                if (_durationSeconds.HasValue)
                {
                    double lowSpeedDuration = (DateTime.Now - _lowSpeedStartTime.Value).TotalSeconds;
                    if (lowSpeedDuration >= _durationSeconds.Value)
                    {
                        _isShutdownPending = true;
                        _powerService.PerformAction(_selectedPowerAction);
                    }
                }
            }
            else
            {
                if (_lowSpeedStartTime.HasValue)
                {
                    _lowSpeedStartTime = null;
                    // Trigger update immediately to switch back linearly
                    UpdateStatusState();
                }
            }
        }

        public void Cleanup()
        {
            _clockTimer?.Stop();
            _networkService.NetworkStatsUpdated -= NetworkService_NetworkStatsUpdated;
        }
    }
}

