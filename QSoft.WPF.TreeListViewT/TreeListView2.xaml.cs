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
    /// TreeListView2.xaml 的互動邏輯
    /// </summary>
    public partial class TreeListView2 : UserControl
    {
        public TreeListView2()
        {
            InitializeComponent();
            this.DataContext = new TreeListView2VM();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(this.listview.View is GridView gridview)
            {
                var t1 = gridview.Columns[0] as GridViewColumn;
            }
        }
    }

    public class TreeListView2VM
    {
        public ObservableCollection<Group2> TreeDs { set; get; } = [];
        public TreeListView2VM()
        {
            this.TreeDs.Add(new Group2()
            {
                GroupName = "Group1",
                Items = 
                [
                    new(){ItemName = "Item1"},
                    new(){ItemName = "Item2"}
                ]
            });
        }
    }

    public class Group2
    {
        public string GroupName { set; get; }
        public ObservableCollection<Item2> Items { set; get; } = [];
    }

    public class Item2
    {
        public string ItemName { set; get; }
    }
}
