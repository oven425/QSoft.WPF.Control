using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp_Tooltip
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainUI mainui = new MainUI();
            mainui.Items.Add("a");
            mainui.Items.Add("b");
            this.DataContext = mainui;
        }
    }

    public class MainUI
    {
        public ObservableCollection<string> Items { set; get; } = [];
     }
}