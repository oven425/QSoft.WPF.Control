using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QSoft.WPF.ShowMoreT
{
    public class ShowMore : ContentControl
    {
        public static readonly DependencyProperty ExpanderHeightProperty = DependencyProperty.Register("ExpanderHeight", typeof(double), typeof(ShowMore), new PropertyMetadata(100.0));
        
        public static readonly DependencyProperty IsExpandProperty = DependencyProperty.Register("IsExpand", typeof(bool), typeof(ShowMore), new PropertyMetadata(false, IsExpandPropertyChanged));
        static void IsExpandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ShowMore showMore)
            {
                if (showMore.IsExpand)
                {
                    showMore.Height = double.NaN;
                }
                else
                {
                    showMore.Height = showMore.ExpanderHeight;
                }
            }
        }

        static ShowMore()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ShowMore), new FrameworkPropertyMetadata(typeof(ShowMore)));
        }

        public double ExpanderHeight
        {
            set { SetValue(ExpanderHeightProperty, value); }
            get {  return (double)GetValue(ExpanderHeightProperty);}
        }
        public bool IsExpand
        {
            set { SetValue(IsExpandProperty, value);}
            get {  return (bool)GetValue(IsExpandProperty);}
        }

        FrameworkElement m_ShowMore;
        FrameworkElement m_Content;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if(this.GetTemplateChild("PART_Header") is  FrameworkElement showmore)
            {
                m_ShowMore = showmore;
            }

            if(this.GetTemplateChild("PART_Content") is FrameworkElement content)
            {
                this.m_Content = content;
            }
            this.Loaded += ShowMore_Loaded;
        }

        private void ShowMore_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.IsExpand)
            {
                this.Height = double.NaN;
            }
            else
            {
                this.Height = this.ExpanderHeight;
            }
        }


        protected override Size MeasureOverride(Size constraint)
        {
            if (m_Content is null) return constraint;
            this.m_Content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (this.m_Content.DesiredSize.Height > this.ExpanderHeight)
            {
                m_ShowMore.Visibility = Visibility.Visible;
            }
            else
            {
                m_ShowMore.Visibility = Visibility.Hidden;
            }

            return base.MeasureOverride(constraint);
        }
    }
}
