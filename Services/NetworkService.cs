using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Threading;
using DownloadTimeCalculator.Models;
using DownloadTimeCalculator.Services.Interfaces;

namespace DownloadTimeCalculator.Services
{
    public class NetworkService : INetworkService
    {
        private DispatcherTimer? _networkMonitorTimer;
        private NetworkInterface[]? _networkInterfaces;
        private long _lastBytesReceived = 0;
        private long _lastBytesSent = 0;
        private DateTime _lastCheckTime = DateTime.Now;
        private string? _targetAdapterName = null;

        public event EventHandler<NetworkStats>? NetworkStatsUpdated;
        public bool IsMonitoring { get; private set; }

        public NetworkService()
        {
            _networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        }

        public void StartMonitoring()
        {
            if (IsMonitoring) return;

            _networkMonitorTimer = new DispatcherTimer();
            _networkMonitorTimer.Interval = TimeSpan.FromSeconds(1);
            _networkMonitorTimer.Tick += NetworkMonitorTimer_Tick;
            _networkMonitorTimer.Start();

            _lastCheckTime = DateTime.Now;
            UpdateNetworkStats();
            IsMonitoring = true;
        }

        public void StopMonitoring()
        {
            if (!IsMonitoring) return;

            _networkMonitorTimer?.Stop();
            _networkMonitorTimer = null;
            IsMonitoring = false;
        }

        private void NetworkMonitorTimer_Tick(object? sender, EventArgs e)
        {
            UpdateNetworkStats();
        }

        private void UpdateNetworkStats()
        {
            if (_networkInterfaces == null) return;

            long totalBytesReceived = 0;
            long totalBytesSent = 0;

            foreach (var ni in _networkInterfaces)
            {
                if (!string.IsNullOrEmpty(_targetAdapterName) && ni.Description != _targetAdapterName)
                    continue;

                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var stats = ni.GetIPStatistics();
                    totalBytesReceived += stats.BytesReceived;
                    totalBytesSent += stats.BytesSent;
                }
            }

            DateTime currentTime = DateTime.Now;
            double elapsedSeconds = (currentTime - _lastCheckTime).TotalSeconds;

            if (elapsedSeconds > 0)
            {
                double downloadSpeed = (totalBytesReceived - _lastBytesReceived) / elapsedSeconds;
                double uploadSpeed = (totalBytesSent - _lastBytesSent) / elapsedSeconds;

                var stats = new NetworkStats
                {
                    DownloadSpeedBytesPerSecond = downloadSpeed,
                    UploadSpeedBytesPerSecond = uploadSpeed,
                    FormattedDownloadSpeed = FormatSpeed(downloadSpeed),
                    FormattedUploadSpeed = FormatSpeed(uploadSpeed)
                };

                NetworkStatsUpdated?.Invoke(this, stats);
            }

            _lastBytesReceived = totalBytesReceived;
            _lastBytesSent = totalBytesSent;
            _lastCheckTime = currentTime;
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024)
                return $"{bytesPerSecond:F0} B/s";
            else if (bytesPerSecond < 1024 * 1024)
                return $"{bytesPerSecond / 1024:F2} KB/s";
            else
                return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
        }

        public NetworkStats GetCurrentStats()
        {
            if (_networkInterfaces == null)
                return new NetworkStats();

            long totalBytesReceived = 0;
            long totalBytesSent = 0;

            foreach (var ni in _networkInterfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var stats = ni.GetIPStatistics();
                    totalBytesReceived += stats.BytesReceived;
                    totalBytesSent += stats.BytesSent;
                }
            }

            DateTime currentTime = DateTime.Now;
            double elapsedSeconds = (currentTime - _lastCheckTime).TotalSeconds;

            if (elapsedSeconds > 0)
            {
                double downloadSpeed = (totalBytesReceived - _lastBytesReceived) / elapsedSeconds;
                double uploadSpeed = (totalBytesSent - _lastBytesSent) / elapsedSeconds;
                
                 // Avoid negative spikes (if stats reset)
                if (downloadSpeed < 0) downloadSpeed = 0;
                if (uploadSpeed < 0) uploadSpeed = 0;

                return new NetworkStats
                {
                    DownloadSpeedBytesPerSecond = downloadSpeed,
                    UploadSpeedBytesPerSecond = uploadSpeed,
                    FormattedDownloadSpeed = FormatSpeed(downloadSpeed),
                    FormattedUploadSpeed = FormatSpeed(uploadSpeed)
                };
            }

            return new NetworkStats();
        }

        public IEnumerable<string> GetAvailableAdapters()
        {
             if (_networkInterfaces == null) return Enumerable.Empty<string>();
             
             var adapters = _networkInterfaces
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(ni => ni.Description)
                .ToList();
                
             adapters.Insert(0, "All Adapters");
             return adapters;
        }

        public void SetTargetAdapter(string? adapterName)
        {
            if (adapterName == "All Adapters") _targetAdapterName = null;
            else _targetAdapterName = adapterName;
            
            // Recalculate baselines immediately to avoid huge spikes
            long totalRx = 0;
            long totalTx = 0;
            
            if (_networkInterfaces != null)
            {
               foreach (var ni in _networkInterfaces)
               {
                   if (!string.IsNullOrEmpty(_targetAdapterName) && ni.Description != _targetAdapterName) continue;
                   // Note: OperationalStatus check might be skipped if we want to track it even if down temporarily, 
                   // but usually we want to match UpdateNetworkStats logic.
                   if (ni.OperationalStatus == OperationalStatus.Up && 
                       ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                   {
                        var stats = ni.GetIPStatistics();
                        totalRx += stats.BytesReceived;
                        totalTx += stats.BytesSent;
                   }
               }
            }
            
            _lastBytesReceived = totalRx;
            _lastBytesSent = totalTx;
            _lastCheckTime = DateTime.Now;
        }
    }
}

