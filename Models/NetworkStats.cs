namespace DownloadTimeCalculator.Models
{
    public class NetworkStats
    {
        public double DownloadSpeedBytesPerSecond { get; set; }
        public double UploadSpeedBytesPerSecond { get; set; }
        public string FormattedDownloadSpeed { get; set; } = string.Empty;
        public string FormattedUploadSpeed { get; set; } = string.Empty;
    }
}

