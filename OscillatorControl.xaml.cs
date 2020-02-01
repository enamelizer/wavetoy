using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace wavetoy
{
    /// <summary>
    /// Interaction logic for OscillatorControl.xaml
    /// </summary>
    public partial class OscillatorControl : UserControl
    {
        public OscillatorControl()
        {
            InitializeComponent();
        }
        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }
    }
}
