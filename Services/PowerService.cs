using System;
using System.Diagnostics;
using System.Windows;
using DownloadTimeCalculator.Models;
using DownloadTimeCalculator.Services.Interfaces;

namespace DownloadTimeCalculator.Services
{
    public class PowerService : ISystemPowerService
    {
        public void PerformAction(PowerActionType actionType)
        {
            try
            {
                switch (actionType)
                {
                    case PowerActionType.Shutdown:
                        Process.Start("shutdown", "/s /t 0");
                        break;
                    case PowerActionType.Restart:
                        Process.Start("shutdown", "/r /t 0");
                        break;
                    case PowerActionType.Hibernate:
                        Process.Start("shutdown", "/h");
                        break;
                    case PowerActionType.Sleep:
                        // rundll32 powrprof.dll,SetSuspendState 0,1,0 -> Sleep (Hibernate=False)
                        Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güç işlemi gerçekleştirilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

