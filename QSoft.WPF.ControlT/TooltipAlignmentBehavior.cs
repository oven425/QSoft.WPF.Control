using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
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
        public double Gap { set; get; } = 10;
        public HorizontalAlignment HorizontalAlignment
        {
            get;set;
        }
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += AssociatedObject_Loaded;    
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
                var w = fw - tooltip_w;
                tooltip.HorizontalOffset = w;
                tooltip.VerticalOffset = -this.Gap;

            }
                
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }
    }

    public enum HorizontalPlacement
    {
        Left = 0, Center = 1, Right = 2
    }

    public enum VerticalPlacement
    {
        Top = 0, Center = 1, Bottom = 2
    }

    public class CustomToolTipPlacementBehavior : Behavior<FrameworkElement>
    {
        public HorizontalPlacement HorizontalPlacement { get; set; }
        public VerticalPlacement VerticalPlacement { get; set; }
    }

    public class CustomToolTipPrioritizedPlacementBehavior : CustomToolTipPlacementBehavior
    {
        protected virtual CustomPopupPlacement[] CalculatePopupPlacement(Size popupSize, Size targetSize, Point offset)
        {
            var verticalOffsets = GetVerticalOffsets(VerticalPlacement, popupSize.Height, targetSize.Height, offset.Y);
            var horizontalOffsets = GetHorizontalOffsets(HorizontalPlacement, popupSize.Width, targetSize.Width, offset.X);

            var placement1 = new CustomPopupPlacement(new Point(horizontalOffsets[0], verticalOffsets[0]), PopupPrimaryAxis.Vertical);
            var placement2 = new CustomPopupPlacement(new Point(horizontalOffsets[1], verticalOffsets[1]), PopupPrimaryAxis.Horizontal);

            return new[] { placement1, placement2 };
        }

        private static double[] GetHorizontalOffsets(HorizontalPlacement horizontalPlacement, double popupWidth, double targetWidth, double offsetWidth)
        {
            var horizontalOffsets = Enumerable.Repeat<double>(offsetWidth, 2).ToArray();
            switch (horizontalPlacement)
            {
                case HorizontalPlacement.Left:
                    horizontalOffsets[0] += -popupWidth;
                    horizontalOffsets[1] += targetWidth;
                    break;

                case HorizontalPlacement.Center:
                    horizontalOffsets[0] += targetWidth / 2 - popupWidth / 2;
                    horizontalOffsets[1] = horizontalOffsets[0];
                    break;

                case HorizontalPlacement.Right:
                    horizontalOffsets[0] += targetWidth;
                    horizontalOffsets[1] += -popupWidth;
                    break;

                default:
                    throw new Exception("Invalid Vertical Placement");
            }

            return horizontalOffsets;
        }

        private static double[] GetVerticalOffsets(VerticalPlacement verticalPlacement, double popupHeight, double targetHeight, double offsetHeight)
        {
            var verticalOffsets = Enumerable.Repeat<double>(offsetHeight, 2).ToArray();

            switch (verticalPlacement)
            {
                case VerticalPlacement.Top:
                    verticalOffsets[0] += -popupHeight;
                    verticalOffsets[1] += targetHeight;
                    break;

                case VerticalPlacement.Center:
                    verticalOffsets[0] += targetHeight / 2 - popupHeight / 2;
                    verticalOffsets[1] += verticalOffsets[0];
                    break;

                case VerticalPlacement.Bottom:
                    verticalOffsets[0] += targetHeight;
                    verticalOffsets[1] += -popupHeight;
                    break;

                default:
                    throw new Exception("Invalid Vertical Placement");
            }

            return verticalOffsets;
        }

    }
}
