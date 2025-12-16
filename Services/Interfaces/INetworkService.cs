using DownloadTimeCalculator.Models;
using System;
using System.Collections.Generic;

namespace DownloadTimeCalculator.Services.Interfaces
{
    public interface INetworkService
    {
        event EventHandler<NetworkStats>? NetworkStatsUpdated;
        NetworkStats GetCurrentStats();
        void StartMonitoring();
        void StopMonitoring();
        bool IsMonitoring { get; }
        
        IEnumerable<string> GetAvailableAdapters();
        void SetTargetAdapter(string? adapterName);
    }
}

