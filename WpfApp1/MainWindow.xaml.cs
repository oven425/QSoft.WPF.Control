using Microsoft.Xaml.Behaviors;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void slider_t_Loaded(object sender, RoutedEventArgs e)
        {
            var slider = sender as Slider;
            var track = slider.Template.FindName("PART_Track", slider) as FrameworkElement;
            this.popup.PlacementTarget = track;
        }
    }

    public class SliderThumbToolTipBehavior : Behavior<Slider>
    {
        ToolTip? tooltip;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AddHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)Thumb_DragStarted);
            AssociatedObject.AddHandler(Thumb.DragDeltaEvent, (DragDeltaEventHandler)Thumb_DragDelta);
            AssociatedObject.AddHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)Thumb_DragCompleted);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)Thumb_DragStarted);
            AssociatedObject.RemoveHandler(Thumb.DragDeltaEvent, (DragDeltaEventHandler)Thumb_DragDelta);
            AssociatedObject.RemoveHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)Thumb_DragCompleted);
        }

        void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (Placement != AutoToolTipPlacement.None && e.OriginalSource is Thumb thumb)
            {
                if (tooltip == null)
                {
                    tooltip = new ToolTip
                    {
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(),
                        Placement = PlacementMode.Custom,
                        PlacementTarget = thumb,
                        CustomPopupPlacementCallback = ToolTip_CustomPopupPlacementCallback
                    };
                }

                thumb.ToolTip = tooltip;
                tooltip.Content = ToolTipTemplate.LoadContent();
                tooltip.IsOpen = true;
                TooltipReposition();
            }
        }

        void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            TooltipReposition();
        }

        void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (Placement != AutoToolTipPlacement.None && e.OriginalSource is Thumb thumb && tooltip != null)
            {
                //tooltip.IsOpen = false;
            }
        }

        void TooltipReposition()
        {
            if (tooltip != null)
            {
                double temp;
                if (AssociatedObject.Orientation == Orientation.Horizontal)
                {
                    temp = tooltip.HorizontalOffset;
                    tooltip.HorizontalOffset += 0.125;
                    tooltip.HorizontalOffset = temp;
                }
                else
                {
                    temp = tooltip.VerticalOffset;
                    tooltip.VerticalOffset += 0.125;
                    tooltip.VerticalOffset = temp;
                }
            }
        }

        CustomPopupPlacement[] ToolTip_CustomPopupPlacementCallback(Size popupSize, Size targetSize, Point offset)
        {
            CustomPopupPlacement? ret = null;

            switch (Placement)
            {
                case AutoToolTipPlacement.TopLeft:
                    if (AssociatedObject.Orientation == Orientation.Horizontal)
                        ret = new CustomPopupPlacement(new Point((targetSize.Width - popupSize.Width) * 0.5, -popupSize.Height), PopupPrimaryAxis.Horizontal);
                    else
                        ret = new CustomPopupPlacement(new Point(-popupSize.Width, (targetSize.Height - popupSize.Height) * 0.5), PopupPrimaryAxis.Vertical);
                    break;
                case AutoToolTipPlacement.BottomRight:
                    if (AssociatedObject.Orientation == Orientation.Horizontal)
                        ret = new CustomPopupPlacement(new Point((targetSize.Width - popupSize.Width) * 0.5, targetSize.Height), PopupPrimaryAxis.Horizontal);
                    else
                        ret = new CustomPopupPlacement(new Point(targetSize.Width, (targetSize.Height - popupSize.Height) * 0.5), PopupPrimaryAxis.Vertical);
                    break;
            }

            if (ret != null)
                return new CustomPopupPlacement[] { ret.Value };
            else
                return Array.Empty<CustomPopupPlacement>();
        }



        public AutoToolTipPlacement Placement
        {
            get { return (AutoToolTipPlacement)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(nameof(Placement), typeof(AutoToolTipPlacement), typeof(SliderThumbToolTipBehavior), new PropertyMetadata(default(AutoToolTipPlacement)));


        public DataTemplate ToolTipTemplate
        {
            get { return (DataTemplate)GetValue(ToolTipTemplateProperty); }
            set { SetValue(ToolTipTemplateProperty, value); }
        }
        public static readonly DependencyProperty ToolTipTemplateProperty =
            DependencyProperty.Register(nameof(ToolTipTemplate), typeof(DataTemplate), typeof(SliderThumbToolTipBehavior), new PropertyMetadata(default(DataTemplate)));
    }
}