using System.Windows.Controls;

namespace DownloadTimeCalculator.Views
{
    public partial class AutoExitView : UserControl
    {
        public AutoExitView()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}

