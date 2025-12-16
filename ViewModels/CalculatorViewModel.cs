using System;
using DownloadTimeCalculator.Models;
using DownloadTimeCalculator.ViewModels.Base;

namespace DownloadTimeCalculator.ViewModels
{
    public class CalculatorViewModel : ViewModelBase
    {
        private double _fileSize;
        private int _sizeUnitIndex = 3; // GB default
        private double _speed;
        private int _speedUnitIndex = 2; // Mbps default
        private string _result = "...";
        private string _eta = "Finish Time: --:--";

        public string FileSizeText
        {
            get => _fileSize > 0 ? _fileSize.ToString() : string.Empty;
            set
            {
                if (double.TryParse(value, out double size))
                {
                    if (SetProperty(ref _fileSize, size))
                    {
                        OnPropertyChanged(nameof(FileSizeText));
                        Calculate();
                    }
                }
                else if (string.IsNullOrEmpty(value))
                {
                    if (SetProperty(ref _fileSize, 0))
                    {
                        OnPropertyChanged(nameof(FileSizeText));
                        Calculate();
                    }
                }
            }
        }

        public double FileSize
        {
            get => _fileSize;
            set
            {
                if (SetProperty(ref _fileSize, value))
                {
                    OnPropertyChanged(nameof(FileSizeText));
                    Calculate();
                }
            }
        }

        public int SizeUnitIndex
        {
            get => _sizeUnitIndex;
            set
            {
                if (SetProperty(ref _sizeUnitIndex, value))
                {
                    Calculate();
                }
            }
        }

        public string SpeedText
        {
            get => _speed > 0 ? _speed.ToString() : string.Empty;
            set
            {
                if (double.TryParse(value, out double speed))
                {
                    if (SetProperty(ref _speed, speed))
                    {
                        OnPropertyChanged(nameof(SpeedText));
                        Calculate();
                    }
                }
                else if (string.IsNullOrEmpty(value))
                {
                    if (SetProperty(ref _speed, 0))
                    {
                        OnPropertyChanged(nameof(SpeedText));
                        Calculate();
                    }
                }
            }
        }

        public double Speed
        {
            get => _speed;
            set
            {
                if (SetProperty(ref _speed, value))
                {
                    OnPropertyChanged(nameof(SpeedText));
                    Calculate();
                }
            }
        }

        public int SpeedUnitIndex
        {
            get => _speedUnitIndex;
            set
            {
                if (SetProperty(ref _speedUnitIndex, value))
                {
                    Calculate();
                }
            }
        }

        public string Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        public string ETA
        {
            get => _eta;
            set => SetProperty(ref _eta, value);
        }

        private void Calculate()
        {
            if (_fileSize <= 0 || _speed <= 0)
            {
                Result = "...";
                ETA = "Finish Time: --:--";
                return;
            }

            // 1. Dosya Boyutunu BİT'e çevir
            // Indexler: 0=Bytes, 1=KB, 2=MB, 3=GB, 4=TB
            // 1 Byte = 8 Bit
            double sizeBits = _fileSize * 8 * Math.Pow(1024, _sizeUnitIndex);

            // 2. Hızı BİT/SANİYE'ye çevir
            double speedBits = 0;
            if (_speedUnitIndex == 4)
            {
                // "MB/s (Real)" seçeneği. Bu Megabyte/sn demektir.
                // Önce Byte -> Bit (x8), sonra Mega -> Birim (x1.000.000)
                speedBits = _speed * 8 * 1000 * 1000;
            }
            else
            {
                // Standart internet birimleri (bps, Kbps, Mbps, Gbps) - 1000 tabanlı
                speedBits = _speed * Math.Pow(1000, _speedUnitIndex);
            }

            if (speedBits > 0)
            {
                double totalSeconds = sizeBits / speedBits;

                // TimeSpan overflow kontrolü
                double maxTimeSpanSeconds = TimeSpan.MaxValue.TotalSeconds;
                if (totalSeconds > maxTimeSpanSeconds || double.IsInfinity(totalSeconds) || double.IsNaN(totalSeconds))
                {
                    Result = "∞";
                    ETA = "Finish Time: --:--";
                    return;
                }

                try
                {
                    TimeSpan t = TimeSpan.FromSeconds(totalSeconds);

                    // A) Süreyi Formatla (Duration)
                    if (t.TotalDays >= 1)
                        Result = $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m";
                    else if (t.TotalHours >= 1)
                        Result = $"{(int)t.TotalHours}h {t.Minutes}m {t.Seconds}s";
                    else
                        Result = $"{t.Minutes}m {t.Seconds}s";

                    // B) Bitiş Saatini Hesapla (ETA)
                    DateTime finishTime = DateTime.Now.AddSeconds(totalSeconds);
                    ETA = $"Finish Time: {finishTime.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture)}";
                }
                catch (Exception)
                {
                    Result = "∞";
                    ETA = "Finish Time: --:--";
                }
            }
            else
            {
                Result = "...";
                ETA = "Finish Time: --:--";
            }
        }
    }
}

