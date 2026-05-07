using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    /// Listview_Average.xaml 的互動邏輯
    /// </summary>
    public partial class Listview_Average : UserControl
    {
        MainUI m_MainUI;
        public Listview_Average()
        {
            InitializeComponent();
            this.DataContext = this.m_MainUI = new MainUI();
        }

        private void listview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listview = (ListView)sender;
            var gridview = listview.View as GridView;
            
            var w = listview.ActualWidth / gridview.Columns.Count;
            foreach(GridViewColumn oo in gridview.Columns)
            {
                
                oo.Width = w;
            }
        }


    }

    public partial class MainUI:ObservableObject
    {
        public ObservableCollection<Data> Datas { get; set; } = new ObservableCollection<Data>()
        {
            new Data(){ Name="A", Age=10, Time=DateTime.Now.AddDays(1) },
            new Data(){ Name="B", Age=20, Time=DateTime.Now.AddDays(2) },
            new Data(){ Name="C", Age=30, Time=DateTime.Now.AddDays(3) },
        };

        [RelayCommand]
        void Delete(Data data)
        {
            Datas.Remove(data);
        }
        [ObservableProperty]
        SortColumn? _SortColumn = null;

        [RelayCommand]
        void Sort(string dp)
        {
            if(SortColumn is null || SortColumn.Name != dp)
            {
                SortColumn = new SortColumn()
                {
                    Name = dp,
                    Direction = SortDirection.Ascending
                };
            }
            else
            {
                if (SortColumn.Direction == SortDirection.Ascending)
                {
                    SortColumn.Direction = SortDirection.Descending;
                }
                else if (SortColumn.Direction == SortDirection.Descending)
                {
                    SortColumn.Direction = SortDirection.Ascending;
                }
            }
            var dataView = CollectionViewSource.GetDefaultView(this.Datas);
            dataView.SortDescriptions.Clear();
            var sd = new SortDescription(dp, ListSortDirection.Descending);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
    }

    public class SortColumn
    {
        public string Name { get; set; }
        public SortDirection Direction { get; set; }
    }

    public enum SortDirection
    {   
        None,
        Ascending,
        Descending
    }

    public class Data
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime Time { set; get; }
    }


    public class Aquarium : UIElement
    {
        // Register an attached dependency property with the specified
        // property name, property type, owner type, and property metadata.
        public static readonly DependencyProperty HasFishProperty =
            DependencyProperty.RegisterAttached(
          "HasFish",
          typeof(bool),
          typeof(Aquarium),
          new FrameworkPropertyMetadata(defaultValue: false,
              flags: FrameworkPropertyMetadataOptions.AffectsRender)
        );

        // Declare a get accessor method.
        public static bool GetHasFish(UIElement target) =>
            (bool)target.GetValue(HasFishProperty);

        // Declare a set accessor method.
        public static void SetHasFish(UIElement target, bool value) =>
            target.SetValue(HasFishProperty, value);
    }
}
