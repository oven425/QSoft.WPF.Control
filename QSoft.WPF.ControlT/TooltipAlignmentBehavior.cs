using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace QSoft.WPF.Behavior
{
    public class TooltipAlignmentBehavior:Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty GapProperty = DependencyProperty.Register(nameof(Gap), typeof(double), typeof(TooltipAlignmentBehavior));
        [Category("TooltipAlignmentBehavior")]
        public double Gap
        {             
            get => (double)GetValue(GapProperty); 
            set => SetValue(GapProperty, value); 
        }

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(nameof(Offset), typeof(double), typeof(TooltipAlignmentBehavior));
        [Category("TooltipAlignmentBehavior")]
        public double Offset
        {
            get => (double)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        public static readonly DependencyProperty HorizontalAlignmentProperty = DependencyProperty.Register(nameof(HorizontalAlignment), typeof(HorizontalAlignment), typeof(TooltipAlignmentBehavior), new PropertyMetadata(HorizontalAlignment.Right));
        [Category("TooltipAlignmentBehavior")]
        public HorizontalAlignment HorizontalAlignment
        {             
            get => (HorizontalAlignment)GetValue(HorizontalAlignmentProperty);
            set => SetValue(HorizontalAlignmentProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.AssociatedObject.ToolTip is ToolTip tooltip)
            {
                tooltip.Opened -= Tooltip_Opened;
            }
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.AssociatedObject.ToolTip is ToolTip tooltip)
            {
                tooltip.Opened += Tooltip_Opened;
            }
        }

        private void Tooltip_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ToolTip tooltip)
            {
                var tooltip_w = tooltip.ActualWidth;
                var fw = this.AssociatedObject.ActualWidth;

                var ww = this.HorizontalAlignment switch
                {
                    HorizontalAlignment.Center => (fw - tooltip_w) / 2,
                    HorizontalAlignment.Right => fw - tooltip_w,
                    _=>0
                };
                tooltip.HorizontalOffset = ww+this.Offset;
                tooltip.VerticalOffset = -this.Gap;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        }
    }

}
