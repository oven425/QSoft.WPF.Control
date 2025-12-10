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
using System.Windows.Media;

namespace QSoft.WPF.Control.Behaviors
{
    public class SliderThumbAdornerBehavior : Behavior<Slider>
    {
        SliderValueAdorner? m_Adorner;
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(UIElement), typeof(SliderValueAdorner));
        public FrameworkElement Content
        {
            get { return (FrameworkElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public double Offset { set; get; } = 0;
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
            this.AssociatedObject.ValueChanged += AssociatedObject_ValueChanged;
        }

        private void AssociatedObject_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            m_Adorner?.InvalidateArrange();

        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if(m_Adorner is null)
            {

                var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
                if (adornerLayer != null)
                {
                    m_Adorner = new SliderValueAdorner(this.AssociatedObject, Content, Offset)
                        .Init();
                    adornerLayer.Add(m_Adorner);
                }
            }
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
            base.OnDetaching();
        }
    }
    public class SliderValueAdorner(Slider slider, FrameworkElement content, double offset) : Adorner(slider)
    {
        //private readonly Slider _slider;
        //readonly FrameworkElement m_Content;
        double m_PreValue = slider.Value;
        //double m_Offset = 0;
        //public SliderValueAdorner(Slider slider, FrameworkElement content, double offset)
        //    : base(slider)
        //{
        //    _slider = slider;
        //    this.m_Content = content;
        //    AddVisualChild(m_Content);
        //    m_Offset = offset;
        //    m_PreValue = _slider.Value;

        //}
        public SliderValueAdorner Init()
        {
            AddVisualChild(content);
            return this;
        }


        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => content;

        protected override Size ArrangeOverride(Size finalSize)
        {
            if(content == null) return finalSize;
            var track = slider.Template.FindName("PART_Track", slider) as Track;
            if (track?.Thumb != null)
            {
                var thumbPos = track.Thumb.TranslatePoint(new Point(track.Thumb.ActualWidth / 2, 0), track);
                
                if (slider.Value < this.m_PreValue)
                {
                    thumbPos = track.Thumb.TranslatePoint(new Point(-track.Thumb.ActualWidth, 0), track);
                }
                double x = thumbPos.X;
                if (slider.Value < this.m_PreValue)
                {
                    x = x + track.Thumb.ActualWidth/2;
                }

                content.Measure(finalSize);
                double textWidth = content.DesiredSize.Width;

                double y = thumbPos.Y - content.DesiredSize.Height - offset;

                //if (track.Thumb.ActualWidth> textWidth)
                //{
                //    var horoffset = (track.Thumb.ActualWidth - textWidth) / 2;
                //    x = x + horoffset;
                //}
                //else
                //{
                //    var horoffset = (track.Thumb.ActualWidth - textWidth) / 2;
                //    x = x + horoffset;
                //}

                var horoffset = (track.Thumb.ActualWidth - textWidth) / 2;
                x = x + horoffset;

                this.m_PreValue = slider.Value;
                if (x<0)
                {
                    x = 0;
                }
                else if (x+textWidth > slider.ActualWidth)
                {
                    x = slider.ActualWidth - textWidth;
                }
                System.Diagnostics.Trace.WriteLine($"orgx:{thumbPos.X} x:{x} tw:{track.Thumb.ActualWidth} w:{textWidth}");
                content.Arrange(new Rect(x, y, textWidth, content.DesiredSize.Height));
            }

            return finalSize;
        }
    }
}
