using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace QSoft.WPF.Control.Behaviors
{
    public class SliderThumbReferenceBehavior : Behavior<Slider>
    {

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(FrameworkElement), typeof(SliderThumbReferenceBehavior));
        public FrameworkElement Content
        {
            get { return (FrameworkElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
        FrameworkElement? m_Content;

        public double Offset { set; get; } = 0;
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
            this.AssociatedObject.ValueChanged += AssociatedObject_ValueChanged;
        }
        double m_PreValue = 0;
        private void AssociatedObject_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.m_Content == null) return;
            var track = AssociatedObject.Template.FindName("PART_Track", AssociatedObject) as Track;
            if (track?.Thumb != null)
            {
                var thumbPos = track.Thumb.TranslatePoint(new Point(track.Thumb.ActualWidth / 2, 0), track);

                if (AssociatedObject.Value < this.m_PreValue)
                {
                    thumbPos = track.Thumb.TranslatePoint(new Point(-track.Thumb.ActualWidth, 0), track);
                }
                double x = thumbPos.X;
                this.m_PreValue = AssociatedObject.Value;


                //m_Content.Measure(finalSize);
                double textWidth = m_Content.DesiredSize.Width;

                double y = thumbPos.Y - m_Content.DesiredSize.Height;

                if (track.Thumb.ActualWidth > textWidth)
                {
                    var horoffset = (track.Thumb.ActualWidth - textWidth) / 2;
                    //x = x + horoffset;
                }
                else
                {
                    var horoffset = (track.Thumb.ActualWidth - textWidth) / 2;
                    x = x + horoffset;
                }

                if (x < 0)
                {
                    x = 0;
                }
                else if (x + textWidth > AssociatedObject.ActualWidth)
                {
                    x = AssociatedObject.ActualWidth - textWidth;
                }
                System.Diagnostics.Trace.WriteLine($"orgx:{thumbPos.X} x:{x} w:{textWidth}");
                //m_Content.Arrange(new Rect(x, y, textWidth, m_Content.DesiredSize.Height));
            }
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (Content is ContentPresenter presenter)
            {
                if (presenter.Content is FrameworkElement fe)
                {
                    this.m_Content = fe;
                }
            }
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
            this.AssociatedObject.ValueChanged -= AssociatedObject_ValueChanged;
            base.OnDetaching();
        }

    }
}
