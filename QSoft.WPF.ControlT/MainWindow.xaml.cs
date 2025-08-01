using CommunityToolkit.Mvvm.ComponentModel;
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

namespace QSoft.WPF.ControlT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainUI();
        }
    }

    public partial class MainUI:ObservableObject
    {
        [ObservableProperty]
        int _Fix = 5;

        public ObservableCollection<int> Items { set; get; } = [1,2,3,4,5];
        [ObservableProperty]
        int _Item = 3;
    }

}