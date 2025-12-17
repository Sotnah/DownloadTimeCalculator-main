using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DownloadTimeCalculator.Services;
using DownloadTimeCalculator.Services.Interfaces;
using DownloadTimeCalculator.ViewModels;

namespace DownloadTimeCalculator
{
    public partial class MainWindow : Window
    {
        // P/Invoke declarations for DwmSetWindowAttribute
        // LibraryImport for DwmSetWindowAttribute
        [LibraryImport("dwmapi.dll")]
        private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_BORDER_COLOR = 34;

        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void InitializeServices()
        {
            INetworkService networkService = new NetworkService();
            ISystemPowerService powerService = new PowerService();

            _viewModel = new MainViewModel(networkService, powerService);
            this.DataContext = _viewModel;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get window handle
            IntPtr hwnd = new WindowInteropHelper(this).EnsureHandle();

            // Set border color to black (0xFF000000 in ARGB format)
            // Set border color to black (0xFF000000 in ARGB format)
            int borderColor = unchecked((int)0xFF000000); // Black color
            int result = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            // Just ignoring result for now as it is cosmetic, but assigning satisfies the IDE warning.
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Cleanup();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Eğer tıklama bir Button üzerindeyse veya Button'un içindeyse, pencereyi sürükleme
                DependencyObject? source = e.Source as DependencyObject;
                while (source != null)
                {
                    if (source is System.Windows.Controls.Button)
                    {
                        return;
                    }
                    source = System.Windows.Media.VisualTreeHelper.GetParent(source);
                }
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
