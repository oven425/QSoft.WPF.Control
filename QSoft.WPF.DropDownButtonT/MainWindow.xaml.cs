using System.Windows;
using System.Windows.Controls;

namespace QSoft.WPF.DropDownButtonT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void Log(string msg) => text_log.AppendText($"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}");



    }
}
