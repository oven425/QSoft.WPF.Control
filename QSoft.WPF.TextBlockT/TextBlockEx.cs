using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace QSoft.WPF.TextBlockT
{
    public class TextBlockEx : FrameworkElement
    {
        private const double IndentUnit  = 16.0;
        private const double SymbolWidth = 20.0;
        private const double RowSpacing  = 3.0;

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items),
                typeof(ObservableCollection<BulletItem>), typeof(TextBlockEx),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsChanged));

        public ObservableCollection<BulletItem> Items
        {
            get => (ObservableCollection<BulletItem>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(TextBlockEx),
                new FrameworkPropertyMetadata(12.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(TextBlockEx),
                new FrameworkPropertyMetadata(Brushes.Black,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(TextBlockEx),
                new FrameworkPropertyMetadata(new FontFamily("Segoe UI"),
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public TextBlockEx()
        {
            Items = new ObservableCollection<BulletItem>();
        }

        protected override IEnumerator LogicalChildren
            => (Items ?? new ObservableCollection<BulletItem>()).GetEnumerator();

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (TextBlockEx)d;
            if (e.OldValue is ObservableCollection<BulletItem> old)
            {
                old.CollectionChanged -= ctrl.OnCollectionChanged;
                foreach (var item in old) ctrl.RemoveLogicalChild(item);
            }
            if (e.NewValue is ObservableCollection<BulletItem> nw)
            {
                foreach (var item in nw) ctrl.AddLogicalChild(item);
                nw.CollectionChanged += ctrl.OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (BulletItem item in e.OldItems) RemoveLogicalChild(item);
            if (e.NewItems != null)
                foreach (BulletItem item in e.NewItems) AddLogicalChild(item);

            InvalidateMeasure();
            InvalidateVisual();
        }

        private double PixelsPerDip()
        {
            try   { return VisualTreeHelper.GetDpi(this).PixelsPerDip; }
            catch { return 1.0; }
        }

        // fontSize 為 null 時使用 item.FontSize
        private FormattedText MakeText(BulletItem item, string text, double maxWidth,
                                       double? fontSize = null)
        {
            var ft = new FormattedText(
                string.IsNullOrEmpty(text) ? " " : text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(item.FontFamily, item.FontStyle, item.FontWeight, item.FontStretch),
                fontSize ?? item.FontSize,
                item.Foreground,
                PixelsPerDip());

            ft.MaxTextWidth = Math.Max(1.0, maxWidth);
            return ft;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;
            if (items is null || items.Count == 0) return Size.Empty;

            double avW = double.IsInfinity(availableSize.Width) ? 600 : availableSize.Width;
            double y = 0, maxW = 0;

            foreach (var item in items)
            {
                double txtX = item.IndentLevel * IndentUnit + SymbolWidth;
                var ft = MakeText(item, item.Text, avW - txtX);
                y    += ft.Height + RowSpacing;
                maxW  = Math.Max(maxW, txtX + ft.Width);
            }
            if (y > 0) y -= RowSpacing;

            return new Size(
                double.IsInfinity(availableSize.Width)  ? maxW : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? y    : Math.Min(y, availableSize.Height));
        }

        protected override void OnRender(DrawingContext dc)
        {
            var items = Items;
            if (items is null || items.Count == 0) return;

            double avW     = ActualWidth;
            double avH     = ActualHeight;
            int    lastIdx   = -1;
            bool   isPartial = false;
            double partialH  = 0;
            double testY     = 0;

            for (int i = 0; i < items.Count; i++)
            {
                var    item      = items[i];
                double txtX      = item.IndentLevel * IndentUnit + SymbolWidth;
                double naturalH  = MakeText(item, item.Text, avW - txtX).Height;
                double remaining = avH - testY;

                if (naturalH <= remaining + 0.5)
                {
                    lastIdx   = i;
                    isPartial = false;
                    testY    += naturalH + RowSpacing;
                }
                else if (remaining >= item.FontSize * 0.8)
                {
                    lastIdx   = i;
                    isPartial = true;
                    partialH  = remaining;
                    break;
                }
                else break;
            }

            if (lastIdx < 0) return;

            bool   hasMore = isPartial || lastIdx < items.Count - 1;
            double y       = 0;

            for (int i = 0; i <= lastIdx; i++)
            {
                var    item   = items[i];
                double indent = item.IndentLevel * IndentUnit;
                double txtX   = indent + SymbolWidth;
                var    symFt  = MakeText(item, item.Symbol, SymbolWidth,
                                         item.SymbolFontSize ?? item.FontSize);
                var    txtFt  = MakeText(item, item.Text,   avW - txtX);

                dc.DrawText(symFt, new Point(indent, y));

                if (i == lastIdx && hasMore)
                {
                    if (isPartial)
                    {
                        txtFt.MaxTextHeight = Math.Max(item.FontSize, partialH);
                        txtFt.Trimming      = TextTrimming.CharacterEllipsis;
                        dc.DrawText(txtFt, new Point(txtX, y));
                    }
                    else
                    {
                        var withEllipsis = MakeText(item, item.Text.TrimEnd() + " ...", avW - txtX);
                        if (Math.Abs(withEllipsis.Height - txtFt.Height) < 1.0)
                        {
                            dc.DrawText(withEllipsis, new Point(txtX, y));
                        }
                        else
                        {
                            withEllipsis.MaxTextHeight = txtFt.Height;
                            withEllipsis.Trimming      = TextTrimming.CharacterEllipsis;
                            dc.DrawText(withEllipsis, new Point(txtX, y));
                        }
                    }
                }
                else
                {
                    dc.DrawText(txtFt, new Point(txtX, y));
                }

                y += txtFt.Height + RowSpacing;
            }
        }
    }
}
