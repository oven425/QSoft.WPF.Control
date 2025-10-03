using QSoft.WPF.TreeListViewT;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace QSoft.WPF.TreeListViewT
{
    /// <summary>
    /// TreeListView1.xaml 的互動邏輯
    /// </summary>
    public partial class TreeListView1 : UserControl
    {
        public TreeListView1()
        {
            InitializeComponent();
        }
    }

    public class Item
    {
        public string Name { set; get; }

    }

    public class Group
    {
        public string Name { set; get; }
        public bool IsGroup { set; get; }
        public ObservableCollection<Item> Items { set; get; } = [];
    }

    public class TestItemSelector : DataTemplateSelector
    {
        public DataTemplate? Group1 { set; get; }
        public DataTemplate? Item1 { set; get; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var dt = item switch
            {
                Group => Group1,
                Item => Item1,
                _ => null
            };
            return dt ?? base.SelectTemplate(item, container);
        }
    }
}
