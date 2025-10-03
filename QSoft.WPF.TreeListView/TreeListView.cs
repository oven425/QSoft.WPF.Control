using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QSoft.WPF.TreeListView
{
    /// <summary>
    /// 依照步驟 1a 或 1b 執行，然後執行步驟 2，以便在 XAML 檔中使用此自訂控制項。
    ///
    /// 步驟 1a) 於存在目前專案的 XAML 檔中使用此自訂控制項。
    /// 加入此 XmlNamespace 屬性至標記檔案的根項目為 
    ///要使用的: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:QSoft.WPF.TreeListView"
    ///
    ///
    /// 步驟 1b) 於存在其他專案的 XAML 檔中使用此自訂控制項。
    /// 加入此 XmlNamespace 屬性至標記檔案的根項目為 
    ///要使用的: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:QSoft.WPF.TreeListView;assembly=QSoft.WPF.TreeListView"
    ///
    /// 您還必須將 XAML 檔所在專案的專案參考加入
    /// 此專案並重建，以免發生編譯錯誤: 
    ///
    ///     在 [方案總管] 中以滑鼠右鍵按一下目標專案，並按一下
    ///     [加入參考]->[專案]->[瀏覽並選取此專案]
    ///
    ///
    /// 步驟 2)
    /// 開始使用 XAML 檔案中的控制項。
    ///
    ///     <MyNamespace:TreeListView/>
    ///
    /// </summary>
    public class TreeListView : TreeView
    {
        public static readonly DependencyProperty ExpenderStyleProperty;
        public static readonly DependencyProperty ViewProperty;
        static TreeListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListView), new FrameworkPropertyMetadata(typeof(TreeListView)));
            ViewProperty = DependencyProperty.Register("View", typeof(ViewBase), typeof(TreeListView));
            ExpenderStyleProperty = DependencyProperty.Register("ExpenderStyle", typeof(Style), typeof(TreeListView));
            
        }

        public ViewBase View
        {
            set {  SetValue(ViewProperty, value); }
            get {  return (ViewBase)GetValue(ViewProperty);}
        }

        public Style ExpenderStyle
        {
            set { SetValue(ExpenderStyleProperty, value); }
            get { return (Style)GetValue(ExpenderStyleProperty); }
        }

        public override void OnApplyTemplate()
        {
            if(this.View is GridView gridview && 
                this.GetTemplateChild("header") is GridViewHeaderRowPresenter hp)
            {
                
                DataTemplate template = new DataTemplate();
                FrameworkElementFactory panel = new FrameworkElementFactory(typeof(DockPanel));
                template.VisualTree = panel;
                FrameworkElementFactory togglebuttonFactory = new FrameworkElementFactory(typeof(ToggleButton));
                togglebuttonFactory.SetValue(ToggleButton.StyleProperty, this.ExpenderStyle);
                togglebuttonFactory.SetValue(ToggleButton.VerticalAlignmentProperty, VerticalAlignment.Center);
                panel.AppendChild(togglebuttonFactory);

                togglebuttonFactory.Name = "Expander";
                template.RegisterName(togglebuttonFactory.Name, togglebuttonFactory);

                togglebuttonFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding()
                {
                    Path = new PropertyPath("IsExpanded",null),
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TreeListViewItem), 1)
                });
                togglebuttonFactory.SetBinding(ToggleButton.MarginProperty, new Binding()
                {
                    Path = new PropertyPath("Level",null),
                    Converter = new LevelToIndentConverter(),
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TreeListViewItem), 1)
                });


                var contentcontrolFactory = new FrameworkElementFactory(typeof(ContentControl));
                if(this.ItemTemplate is HierarchicalDataTemplate hd && hd.ItemTemplateSelector is not null)
                {
                    contentcontrolFactory.SetBinding(ContentControl.ContentProperty, new Binding());
                    contentcontrolFactory.SetValue(ContentControl.ContentTemplateSelectorProperty, hd.ItemTemplateSelector);

                }
                else if(this.ItemTemplateSelector is not null)
                {

                }
                else if (gridview.Columns[0].DisplayMemberBinding is not null)
                {
                    contentcontrolFactory.SetBinding(ContentControl.ContentProperty, gridview.Columns[0].DisplayMemberBinding);
                }
                else if (gridview.Columns[0].CellTemplate is not null)
                {
                    contentcontrolFactory.SetBinding(ContentControl.ContentProperty, new Binding());
                    contentcontrolFactory.SetValue(ContentControl.ContentTemplateProperty, gridview.Columns[0].CellTemplate);
                }
                else if (gridview.Columns[0].CellTemplateSelector is not null)
                {
                    contentcontrolFactory.SetBinding(ContentControl.ContentProperty, new Binding());
                    contentcontrolFactory.SetValue(ContentControl.ContentTemplateSelectorProperty, gridview.Columns[0].CellTemplateSelector);

                }
                panel.AppendChild(contentcontrolFactory);


                var datatrigger = new DataTrigger()
                {
                    Binding = new Binding()
                    {
                        Path = new PropertyPath("HasItems", null),
                        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TreeListViewItem), 1)
                    },
                    Value = false,
                    Setters =
                    {
                        new Setter()
                        {
                            TargetName = "Expander",
                            Property = ToggleButton.VisibilityProperty,
                            Value = Visibility.Hidden
                        }
                    }
                };

                template.Triggers.Add(datatrigger);
                hp.Columns = gridview.Columns;
                hp.Columns[0].DisplayMemberBinding = null;
                hp.Columns[0].CellTemplate = template;
            }
            base.OnApplyTemplate();
        }

        protected override void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
        {
            base.OnItemTemplateChanged(oldItemTemplate, newItemTemplate);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var item = new TreeListViewItem();
            
            if (this.View is GridView gridview)
            {
                item.ColumnCollection = gridview.Columns;
            }
            return item;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            var aa = base.IsItemItsOwnContainerOverride(item);
            bool _isTreeLVI = item is TreeListViewItem;
            return _isTreeLVI;
        }
    }

}
