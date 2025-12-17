using System.Windows.Controls;

namespace DownloadTimeCalculator.Views
{
    public partial class CalculatorView : UserControl
    {
        public CalculatorView()
        {
            InitializeComponent();
        }

        [System.Text.RegularExpressions.GeneratedRegex("[^0-9]+")]
        private static partial System.Text.RegularExpressions.Regex NumbersOnlyRegex();

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = NumbersOnlyRegex().IsMatch(e.Text);
        }
    }
}

