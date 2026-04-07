using System;
using System.Collections.Generic;
using System.Linq;
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

namespace WPF_ComboboxT
{
    /// <summary>
    /// ComboBox_1.xaml 的互動邏輯
    /// </summary>
    public partial class ComboBox_1 : UserControl
    {
        public ComboBox_1()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (FrameworkElement oo in this.combobox.Items)
            {


            }
        }
    }
}
