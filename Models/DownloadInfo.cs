using System;

namespace DownloadTimeCalculator.Models
{
    public class DownloadInfo
    {
        public double FileSize { get; set; }
        public int SizeUnitIndex { get; set; } // 0=Bytes, 1=KB, 2=MB, 3=GB, 4=TB
        public double Speed { get; set; }
        public int SpeedUnitIndex { get; set; } // 0=bps, 1=Kbps, 2=Mbps, 3=Gbps, 4=MB/s
        public TimeSpan EstimatedTime { get; set; }
        public DateTime EstimatedFinishTime { get; set; }
    }
}

