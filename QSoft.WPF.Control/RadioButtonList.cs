using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QSoft.WPF.Control
{
    public class RadioButtonList : Selector
    {
        static RadioButtonList()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButtonList), new FrameworkPropertyMetadata(typeof(RadioButtonList)));
        }

        // 這個方法不需要改變，它能正確區分兩種模式：
        // 1. item是RadioButton -> true
        // 2. item是數據物件 -> false
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RadioButton;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            // 模式一：容器本身就是 RadioButton (直接在 XAML 中定義 RadioButton)
            if (element is RadioButton radioButton)
            {
                radioButton.Checked += OnRadioButtonChecked;
                // 同步初始狀態
                if (item == this.SelectedItem)
                {
                    radioButton.IsChecked = true;
                }
            }
            // 模式二：容器是為數據生成的 (使用 ItemsSource)
            else if (element is FrameworkElement container)
            {
                // 因為 DataTemplate 可能尚未套用，我們需要等到容器載入後再去尋找 RadioButton
                container.Loaded += OnContainerLoaded;
            }
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            if (element is RadioButton radioButton)
            {
                radioButton.Checked -= OnRadioButtonChecked;
            }
            else if (element is FrameworkElement container)
            {
                container.Loaded -= OnContainerLoaded;
                // 同時也嘗試移除內部 RadioButton 的事件，以防萬一
                var childRadioButton = FindVisualChild<RadioButton>(container);
                if (childRadioButton != null)
                {
                    childRadioButton.Checked -= OnRadioButtonChecked;
                }
            }
        }

        // 新增的事件處理器，用於處理 ItemsSource 模式下的容器載入
        private void OnContainerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement container)
            {
                // 容器載入後，就可以安全地在它的可視化樹中尋找 RadioButton
                var radioButton = FindVisualChild<RadioButton>(container);
                if (radioButton != null)
                {
                    radioButton.Checked -= OnRadioButtonChecked; // 先移除，避免重複掛接
                    radioButton.Checked += OnRadioButtonChecked;

                    // 同步初始狀態
                    // 在 ItemsSource 模式下，我們比較 SelectedValue 和 RadioButton 的 Tag
                    if (this.SelectedValue != null && this.SelectedValue.Equals(radioButton.Tag))
                    {
                        radioButton.IsChecked = true;
                    }
                }
            }
        }

        // OnRadioButtonChecked 現在更加通用
        private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                // 關鍵改動：直接設定 SelectedValue
                // 無論是哪種模式，我們都從被點擊的 RadioButton 的 Tag 屬性取值。
                // 這樣做可以完美對應 SelectedValuePath="Tag" 的意圖。
                // SetCurrentValue 用於在不破壞綁定的情況下更新值。
                this.SetCurrentValue(Selector.SelectedValueProperty, radioButton.Tag);
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // 處理被取消選擇的項目
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] != null)
            {
                UpdateRadioButtonState(e.RemovedItems[0], false);
            }

            // 處理新被選擇的項目
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                UpdateRadioButtonState(e.AddedItems[0], true);
            }
        }

        // 輔助方法：根據數據項目來更新對應的 RadioButton 狀態
        private void UpdateRadioButtonState(object item, bool isChecked)
        {
            // ItemContainerGenerator.ContainerFromItem 可以找到數據項目對應的容器
            var container = this.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;

            // 如果 container 是 null，有可能是因為虛擬化或者尚未生成
            // 這種情況下我們暫不處理，等待容器生成時在 OnContainerLoaded 中同步狀態
            if (container == null) return;

            // 模式一：容器就是RadioButton
            if (container is RadioButton directButton)
            {
                directButton.IsChecked = isChecked;
                return;
            }

            // 模式二：在容器中尋找RadioButton
            var radioButton = FindVisualChild<RadioButton>(container);
            if (radioButton != null)
            {
                radioButton.IsChecked = isChecked;
            }
        }

        // 標準的輔助方法：在可視化樹中尋找指定類型的子元素
        private static T FindVisualChild<T>(DependencyObject parent) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }
    }

}
