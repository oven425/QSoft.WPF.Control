using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
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
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(FrameworkElement), typeof(SliderValueAdorner));
        public FrameworkElement Content
        {
            get { return (FrameworkElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register("Offset", typeof(double), typeof(SliderValueAdorner));
        public double Offset
        {
            get { return (double)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
            
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if(m_Adorner is null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
                if (adornerLayer != null)
                {
                    m_Adorner = new SliderValueAdorner(this.AssociatedObject, Content);
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
    public class SliderValueAdorner : Adorner
    {
        private readonly Slider _slider;
        readonly FrameworkElement m_Content;
        public SliderValueAdorner(Slider slider, FrameworkElement content)
            : base(slider)
        {

            _slider = slider;
            this.m_Content = content;
            AddVisualChild(m_Content);
            //    _textBlock = new TextBlock
            //    {
            //        Background = Brushes.LightYellow,
            //        Padding = new Thickness(4),
            //        FontSize = 12,
            //        Text = $"{_slider.Value:F0} cm"
            //    };

            //    AddVisualChild(_textBlock);

            _slider.ValueChanged += (s, e) =>
            {
                //_textBlock.Text = $"{_slider.Value:F0} cm";
                InvalidateArrange();
            };
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => m_Content;

        protected override Size ArrangeOverride(Size finalSize)
        {
            if(m_Content == null) return finalSize;
            var track = _slider.Template.FindName("PART_Track", _slider) as Track;
            if (track?.Thumb != null)
            {
                var thumbPos = track.Thumb.TranslatePoint(new Point(0, 0), this);

                double thumbWidth = track.Thumb.ActualWidth;

                m_Content.Measure(finalSize);
                double textWidth = m_Content.DesiredSize.Width;

                double x = thumbPos.X + (thumbWidth / 2) - (textWidth / 2);
                double y = thumbPos.Y - m_Content.DesiredSize.Height-3;

                double sliderLeft = 0;
                double sliderRight = _slider.ActualWidth;

                if (x < sliderLeft) x = sliderLeft;
                if (x + textWidth > sliderRight) x = sliderRight - textWidth;

                m_Content.Arrange(new Rect(x, y, textWidth, m_Content.DesiredSize.Height));
            }

            return finalSize;
        }
    }
}
