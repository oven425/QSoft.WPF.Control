using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace QSoft.WPF.Control
{
    public class RadioButtonList1 : Selector
    {
        static RadioButtonList1()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButtonList1), new FrameworkPropertyMetadata(typeof(RadioButtonList1)));
        }
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RadioButton;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RadioButton();
        }


    }
}
