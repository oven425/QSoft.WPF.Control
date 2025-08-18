using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace QSoft.WPF.Control
{
    public class RadioButtonList : Selector
    {
        readonly static DependencyProperty ICommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(RadioButtonList));
        public ICommand Command
        {
            get { return (ICommand)GetValue(ICommandProperty); }
            set { SetValue(ICommandProperty, value); }
        }
        static RadioButtonList()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButtonList), new FrameworkPropertyMetadata(typeof(RadioButtonList)));
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RadioButton;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is RadioButton radioButton)
            {
                
                radioButton.Loaded += RadioButton_Loaded;
                radioButton.Checked += OnRadioButtonChecked;
                if (item == this.SelectedItem)
                {
                    //radioButton.IsChecked = true;
                }
            }
            else if (element is FrameworkElement container)
            {
                container.Loaded += OnContainerLoaded;
            }
        }

        private void RadioButton_Loaded(object sender, RoutedEventArgs e)
        {
            if(sender is RadioButton radioButton)
            {
                if (radioButton.Command == null && this.Command != null)
                {
                    radioButton.Command = this.Command;
                }
            }
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            if (element is RadioButton radioButton)
            {
                radioButton.Loaded -= RadioButton_Loaded;
                radioButton.Checked -= OnRadioButtonChecked;
            }
            else if (element is FrameworkElement container)
            {
                container.Loaded -= OnContainerLoaded;
                var childRadioButton = FindVisualChild<RadioButton>(container);
                if (childRadioButton != null)
                {
                    childRadioButton.Checked -= OnRadioButtonChecked;
                }
            }
        }

        private void OnContainerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement container)
            {
                var radioButton = FindVisualChild<RadioButton>(container);

                if (radioButton != null)
                {
                    radioButton.Checked -= OnRadioButtonChecked;
                    radioButton.Checked += OnRadioButtonChecked;

                    if (this.SelectedValue != null && this.SelectedValue.Equals(GetChildProperty(radioButton)))
                    {
                        radioButton.IsChecked = true;
                    }
                }
            }
        }

        object? GetChildProperty(RadioButton rbtn)
        {
            if (string.IsNullOrEmpty(this.SelectedValuePath))
            {
                return rbtn.DataContext;
            }
            else
            {
                var pp = typeof(RadioButton).GetProperty(this.SelectedValuePath);
                var vv = pp?.GetValue(rbtn, null);
                if(vv == null)
                {
                    pp = rbtn.DataContext.GetType().GetProperty(this.SelectedValuePath);
                    vv = pp?.GetValue(rbtn.DataContext, null);
                }
                return vv;
            }
        }

        private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                if (this.SelectedValuePath != string.Empty)
                {
                    var vv = this.GetChildProperty(radioButton);
                    this.SetCurrentValue(Selector.SelectedValueProperty, vv);
                }
                else
                {
                    this.SetCurrentValue(Selector.SelectedValueProperty, radioButton.DataContext);
                }
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (e.RemovedItems.Count > 0)
            {
                UpdateRadioButtonState(e.RemovedItems[0], false);
            }

            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                UpdateRadioButtonState(e.AddedItems[0], true);
            }
        }

        private void UpdateRadioButtonState(object? item, bool isChecked)
        {
            if (item == null) return;
            var container = this.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;

            if (container is null) return;

            if (container is RadioButton directButton)
            {
                directButton.IsChecked = isChecked;
                return;
            }

            var radioButton = FindVisualChild<RadioButton>(container);
            if (radioButton != null)
            {
                radioButton.IsChecked = isChecked;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T cc)
                {
                    return cc;
                }
                else
                {
                    T? childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return default;
        }
    }

}
