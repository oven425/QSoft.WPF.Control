using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QSoft.WPF.Control;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
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

        private void RadioButtonList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var radioButtonList = sender as RadioButtonList;
            System.Diagnostics.Trace.WriteLine($"SelectedValue:{radioButtonList.SelectedValue} SelectedValuePath:{radioButtonList.SelectedValuePath}");

        }
    }

    public partial class MainUI:ObservableObject
    {
        [ObservableProperty]
        int _Fix = 5;

        [RelayCommand]
        async Task SetFix(int data)
        {
            await Task.Delay(1000);
        }

        [RelayCommand]
        async Task SetFix1()
        {
            await Task.Delay(1000);
        }

        public ObservableCollection<int> Items { set; get; } = [1,2,3,4,5];
        [ObservableProperty]
        int _Item = 3;

        [RelayCommand(AllowConcurrentExecutions = false)]
        async Task SetItem()
        {
            await Task.Delay(1000);
        }

        [ObservableProperty]
        int _ID;

        [ObservableProperty]
        DataItem _DataItem = new(2, "Item 2");
        [ObservableProperty]
        int _DataItemId = 3;
        public ObservableCollection<DataItem> DataItems { set; get; } =
        [
            new DataItem(1, "Item 1"),
            new DataItem(2, "Item 2"),
            new DataItem(3, "Item 3"),
            new DataItem(4, "Item 4"),
            new DataItem(5, "Item 5")
        ];

        [RelayCommand(AllowConcurrentExecutions = false)]
        async Task SetDataItem(DataItem data)
        {
            await Task.Delay(100);
        }

        [RelayCommand]
        async Task  Delay()
        {
            await Task.Delay(1000);
        }
    }

    public record DataItem(int Id, string Name);

}