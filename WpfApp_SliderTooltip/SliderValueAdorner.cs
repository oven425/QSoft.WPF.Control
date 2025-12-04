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
        //SliderThumbReferenceBehavior 
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
            //m_Adorner?.InvalidateArrange();

        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if(m_Adorner is null)
            {
                if (Content is ContentPresenter presenter)
                {
                    if (presenter.Content is TextBlock tb)
                    {
                        // tb 就是你在 XAML 裡定義的 textt
                        Debug.WriteLine($"綁定到 TextBlock: {tb.Name}, Text={tb.Text}");
                        tb.Arrange(new Rect(50,0, tb.DesiredSize.Width, tb.DesiredSize.Height));
                    }
                }
                //var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
                //if (adornerLayer != null)
                //{
                //    m_Adorner = new SliderValueAdorner(this.AssociatedObject, Content, Offset);
                //    adornerLayer.Add(m_Adorner);
                //}
            }
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
            base.OnDetaching();
        }
    }
    public class SliderValueAdorner : Adorner
    {
        private readonly Slider _slider;
        readonly FrameworkElement m_Content;
        double m_PreValue = double.NaN;
        double m_Offset = 0;
        public SliderValueAdorner(Slider slider, FrameworkElement content, double offset)
            : base(slider)
        {
            _slider = slider;
            this.m_Content = content;
            AddVisualChild(m_Content);
            m_Offset = offset;
            m_PreValue = _slider.Value;

        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => m_Content;

        protected override Size ArrangeOverride(Size finalSize)
        {
            if(m_Content == null) return finalSize;
            var track = _slider.Template.FindName("PART_Track", _slider) as Track;
            if (track?.Thumb != null)
            {
                var thumbPos = track.Thumb.TranslatePoint(new Point(track.Thumb.ActualWidth / 2, 0), track);
                
                if (_slider.Value < this.m_PreValue)
                {
                    thumbPos = track.Thumb.TranslatePoint(new Point(-track.Thumb.ActualWidth, 0), track);
                }
                double x = thumbPos.X;
                this.m_PreValue = _slider.Value;


                m_Content.Measure(finalSize);
                double textWidth = m_Content.DesiredSize.Width;

                double y = thumbPos.Y - m_Content.DesiredSize.Height - m_Offset;

                if (track.Thumb.ActualWidth> textWidth)
                {
                    var horoffset = (track.Thumb.ActualWidth - textWidth) / 2;
                    //x = x + horoffset;
                }
                else
                {
                    var horoffset = (track.Thumb.ActualWidth - textWidth) / 2;
                    x = x + horoffset;
                }
                    
                if (x<0)
                {
                    x = 0;
                }
                else if (x+textWidth > _slider.ActualWidth)
                {
                    x = _slider.ActualWidth - textWidth;
                }
                System.Diagnostics.Trace.WriteLine($"orgx:{thumbPos.X} x:{x} w:{textWidth}");
                m_Content.Arrange(new Rect(x, y, textWidth, m_Content.DesiredSize.Height));
            }

            return finalSize;
        }
    }
}
