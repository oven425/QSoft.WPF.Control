using System;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QSoft.WPF.Control
{
    /// <summary>
    /// 依照步驟 1a 或 1b 執行，然後執行步驟 2，以便在 XAML 檔中使用此自訂控制項。
    ///
    /// 步驟 1a) 於存在目前專案的 XAML 檔中使用此自訂控制項。
    /// 加入此 XmlNamespace 屬性至標記檔案的根項目為 
    ///要使用的: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:QSoft.WPF.Control"
    ///
    ///
    /// 步驟 1b) 於存在其他專案的 XAML 檔中使用此自訂控制項。
    /// 加入此 XmlNamespace 屬性至標記檔案的根項目為 
    ///要使用的: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:QSoft.WPF.Control;assembly=QSoft.WPF.Control"
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
    ///     <MyNamespace:RadioButtonList1/>
    ///
    /// </summary>
    public class RadioButtonList1 : Selector
    {
        static RadioButtonList1()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButtonList1), new FrameworkPropertyMetadata(typeof(RadioButtonList1)));
        }

        // 1. 屬性定義保持不變，但建議設為 public 以便在 XAML 中使用
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(RadioButtonList1), new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }


        // 3. 覆寫 GetContainerForItemOverride 來指定項目容器就是 RadioButton
        // 這樣 ItemsSource 中的每個資料項目都會被一個 RadioButton 包裹
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RadioButton();
        }

        // 4. IsItemItsOwnContainerOverride 仍然需要，以處理直接在 XAML 中放入 RadioButton 的情況
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RadioButton;
        }

        // 5. 【重要】PrepareContainerForItemOverride 大幅簡化
        // 我們不再需要手動附加事件，而是依賴樣式中的繫結
        // 但我們可以在這裡設定從 List 傳遞到每個 RadioButton 的屬性，例如 GroupName
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is RadioButton radioButton)
            {
                // 設定 GroupName 可以確保在同一個視覺樹下的多個 RadioButtonList 不會互相干擾
                // 使用 UniqueId 確保每個 List 實例有唯一的群組
                radioButton.GroupName = "123";
            }
        }
    }
}
