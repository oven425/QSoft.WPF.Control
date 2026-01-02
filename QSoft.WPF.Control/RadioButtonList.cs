using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace QSoft.WPF.Control
{
    public class RadioButtonList : Selector
    {
        readonly static DependencyProperty GroupNameProperty = DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(RadioButtonList), new PropertyMetadata(Guid.NewGuid().ToString()));
        [Category("RadioButtonList")]
        public string GroupName
        {
            get => (string)GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }
        readonly static DependencyProperty ICommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(RadioButtonList));
        [Category("RadioButtonList")]
        public ICommand Command
        {
            get => (ICommand)GetValue(ICommandProperty);
            set => SetValue(ICommandProperty, value);
        }

        static RadioButtonList()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButtonList), new FrameworkPropertyMetadata(typeof(RadioButtonList)));
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RadioButton;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            //return base.GetContainerForItemOverride();
            return new RadioButton();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is RadioButton radioButton)
            {
                if (item == this.SelectedItem)
                {
                    radioButton.IsChecked = true;
                }
                radioButton.Checked += OnRadioButtonChecked;
                radioButton.SetBinding(RadioButton.CommandProperty, new Binding()
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Command), []),
                    Mode = BindingMode.OneWay
                });
                radioButton.SetBinding(RadioButton.GroupNameProperty, new Binding()
                {
                    Source = this,
                    Path = new PropertyPath(nameof(GroupName), null),
                    Mode = BindingMode.OneWay
                });
            }
            else if (element is FrameworkElement container)
            {
                container.Loaded += OnContainerLoaded;
            }
            else
            {

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
                var control = VisualTreeHelper.GetChild(container, 0);
                var radioButton= control as RadioButton;

                if (radioButton != null)
                {
                    if(string.IsNullOrEmpty(radioButton.GroupName))
                    {
                        radioButton.GroupName = this.GroupName;
                    }
                }
            }
        }

        private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                var obj = this.ItemsSource == null? radioButton:radioButton.DataContext;
                this.SetCurrentValue(Selector.SelectedItemProperty, obj);

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
