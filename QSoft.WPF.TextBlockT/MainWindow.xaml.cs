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

namespace QSoft.WPF.TextBlockT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainUI
            {
                TotalSums =
                [
                    new() {
                        Sum = "100",
                        Items = ["Item1", "Item2", "Item3"]
                    },
                    new() {
                        Sum = "200",
                        Items = ["Item4", "Item5"]
                    }
                ]
            };
         }
    }

    public class MainUI
    {
        public ObservableCollection<TotalSum> TotalSums { set; get; } = [];
    }

    public class TotalSum
    {
        public string Sum { set; get; }
        public List<string> Items { set; get; }
    }
}