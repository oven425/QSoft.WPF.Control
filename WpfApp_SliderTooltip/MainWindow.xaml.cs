using QSoft.WPF.Control.Behaviors;
using System.ComponentModel;
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

namespace WpfApp_SliderTooltip
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var adornerLayer = AdornerLayer.GetAdornerLayer(MySlider);
            //if (adornerLayer != null)
            //{
            //    adornerLayer.Add(new SliderValueAdorner(MySlider));
            //}
        }
    }

    public class MainUI:INotifyPropertyChanged
            {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        int m_S1;
        public int S1
        {
            get => m_S1;
            set
            {
                if (m_S1 != value)
                {
                    m_S1 = value;
                    OnPropertyChanged(nameof(S1));
                }
            }
        }

        private int _sliderValue;
        public int SliderValue
        {
            get { return _sliderValue; }
            set
            {
                if (_sliderValue != value)
                {
                    _sliderValue = value;
                    OnPropertyChanged(nameof(SliderValue));
                }
            }
        }
    }
}