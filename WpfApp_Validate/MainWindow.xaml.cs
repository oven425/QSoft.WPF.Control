using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

namespace WpfApp_Validate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainUI m_MainUI;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = m_MainUI = new MainUI();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.m_MainUI.Name = "";
        }
    }

    public partial class MainUI : ObservableValidator
    {
        private string name;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value, true);
        }
    }


}