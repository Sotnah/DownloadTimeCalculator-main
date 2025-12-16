using DownloadTimeCalculator.Models;

namespace DownloadTimeCalculator.Services.Interfaces
{
    public interface ISystemPowerService
    {
        void PerformAction(PowerActionType actionType);
    }
}

