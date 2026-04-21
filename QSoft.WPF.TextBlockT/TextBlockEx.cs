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
        private const double IndentUnit = 0.0;
        private const double RowSpacing = 0.0;

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items),
                typeof(ObservableCollection<TextBlockExElement>), typeof(TextBlockEx),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsChanged));

        public ObservableCollection<TextBlockExElement> Items
        {
            get => (ObservableCollection<TextBlockExElement>)GetValue(ItemsProperty);
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
                new FrameworkPropertyMetadata(new FontFamily("Arial"),
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public TextBlockEx()
        {
            Items = new ObservableCollection<TextBlockExElement>();
        }

        protected override IEnumerator LogicalChildren
            => (Items ?? new ObservableCollection<TextBlockExElement>()).GetEnumerator();

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (TextBlockEx)d;
            if (e.OldValue is ObservableCollection<TextBlockExElement> old)
            {
                old.CollectionChanged -= ctrl.OnCollectionChanged;
                foreach (var item in old) ctrl.RemoveLogicalChild(item);
            }
            if (e.NewValue is ObservableCollection<TextBlockExElement> nw)
            {
                foreach (var item in nw) ctrl.AddLogicalChild(item);
                nw.CollectionChanged += ctrl.OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (TextBlockExElement item in e.OldItems) RemoveLogicalChild(item);
            if (e.NewItems != null)
                foreach (TextBlockExElement item in e.NewItems) AddLogicalChild(item);

            InvalidateMeasure();
            InvalidateVisual();
        }

        private double PixelsPerDip()
        {
            try   { return VisualTreeHelper.GetDpi(this).PixelsPerDip; }
            catch { return 1.0; }
        }

        private FormattedText MakeText(TextBlockExElement item, string text, double maxWidth, double? fontSize = null)
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

        private FormattedText MakeSymbolText(Symbol sym, double maxWidth)
        {
            var ft = new FormattedText(
                string.IsNullOrEmpty(sym.Text) ? " " : sym.Text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(sym.FontFamily, sym.FontStyle, sym.FontWeight, sym.FontStretch),
                sym.FontSize,
                sym.Foreground,
                PixelsPerDip());

            ft.MaxTextWidth = Math.Max(1.0, maxWidth);
            return ft;
        }

        private double GetSymbolTotalWidth(TextBlockExElement item, double availableWidth)
        {
            var sym = item.Symbol;
            if (sym == null || string.IsNullOrEmpty(sym.Text)) return 0;

            double padH    = sym.Padding.Left + sym.Padding.Right;
            double avail   = Math.Max(1.0, availableWidth - padH);
            var    symFt   = MakeSymbolText(sym, avail);
            return sym.Padding.Left + symFt.WidthIncludingTrailingWhitespace + sym.Padding.Right;
        }

        private static double GetSymbolOffsetY(Symbol sym, double symTextH, double rowContentH)
        {
            double extra = rowContentH - symTextH - sym.Padding.Top - sym.Padding.Bottom;
            return sym.VerticalAlignment switch
            {
                VerticalAlignment.Center  => sym.Padding.Top + Math.Max(0, extra / 2.0),
                VerticalAlignment.Bottom  => sym.Padding.Top + Math.Max(0, extra),
                VerticalAlignment.Stretch => sym.Padding.Top + Math.Max(0, extra / 2.0),
                _                         => sym.Padding.Top,   // Top（預設）
            };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;
            if (items is null || items.Count == 0) return Size.Empty;

            double avW  = availableSize.Width;
            double y    = 0;
            double maxW = 0;

            foreach (var item in items)
            {
                double indent   = item.IndentLevel * IndentUnit;
                double itemPadH = item.Padding.Left + item.Padding.Right;
                double itemPadV = item.Padding.Top  + item.Padding.Bottom;
                double symTotalW = GetSymbolTotalWidth(item, avW - indent - itemPadH);

                double txtX    = indent + item.Padding.Left + symTotalW;
                double txtAvail = Math.Max(1.0, avW - txtX - item.Padding.Right);
                var    txtFt   = MakeText(item, item.Text, txtAvail);

                y    += txtFt.Height + itemPadV + RowSpacing;
                maxW  = Math.Max(maxW, txtX + txtFt.Width + item.Padding.Right);
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

            double avW       = ActualWidth;
            double avH       = ActualHeight;
            int    lastIdx   = -1;
            bool   isPartial = false;
            double partialH  = 0;
            double testY     = 0;

            for (int i = 0; i < items.Count; i++)
            {
                var    item      = items[i];
                double indent    = item.IndentLevel * IndentUnit;
                double itemPadH  = item.Padding.Left + item.Padding.Right;
                double itemPadV  = item.Padding.Top  + item.Padding.Bottom;

                double symTotalW = GetSymbolTotalWidth(item, avW - indent - itemPadH);
                double txtX      = indent + item.Padding.Left + symTotalW;
                double txtAvail  = Math.Max(1.0, avW - txtX - item.Padding.Right);

                double txtH      = MakeText(item, item.Text, txtAvail).Height;
                double rowH      = txtH + itemPadV;
                double remaining = avH - testY;

                if (rowH <= remaining + 0.5)
                {
                    lastIdx   = i;
                    isPartial = false;
                    testY    += rowH + RowSpacing;
                }
                else
                {
                    double contentH = remaining - itemPadV;

                    if (contentH < item.FontSize * 0.8)
                        break;

                    lastIdx   = i;
                    isPartial = true;
                    partialH  = contentH;
                    break;
                }
            }

            if (lastIdx < 0) return;

            bool   hasMore = isPartial || lastIdx < items.Count - 1;
            double y       = 0;

            for (int i = 0; i <= lastIdx; i++)
            {
                var    item     = items[i];
                double indent   = item.IndentLevel * IndentUnit;
                double itemPadL = item.Padding.Left;
                double itemPadT = item.Padding.Top;
                double itemPadV = itemPadT + item.Padding.Bottom;
                double itemPadH = itemPadL + item.Padding.Right;

                double symTotalW = 0;
                var    sym       = item.Symbol;
                FormattedText? symFt = null;

                if (sym != null && !string.IsNullOrEmpty(sym.Text))
                {
                    double symAvail = Math.Max(1.0, avW - indent - itemPadH
                                                       - sym.Padding.Left - sym.Padding.Right);
                    symFt     = MakeSymbolText(sym, symAvail);
                    symTotalW = sym.Padding.Left + symFt.WidthIncludingTrailingWhitespace + sym.Padding.Right;
                }

                double txtX     = indent + itemPadL + symTotalW;
                double txtAvail = Math.Max(1.0, avW - txtX - item.Padding.Right);
                var    txtFt    = MakeText(item, item.Text, txtAvail);
                double drawY    = y + itemPadT;

                if (i == lastIdx && hasMore)
                {
                    if (isPartial)
                    {
                        txtFt.MaxTextHeight = Math.Max(item.FontSize, partialH);
                        txtFt.Trimming      = TextTrimming.CharacterEllipsis;
                    }
                    else
                    {
                        var withEllipsis = MakeText(item, item.Text.TrimEnd() + " ...", txtAvail);
                        if (Math.Abs(withEllipsis.Height - txtFt.Height) >= 1.0)
                        {
                            withEllipsis.MaxTextHeight = txtFt.Height;
                            withEllipsis.Trimming      = TextTrimming.CharacterEllipsis;
                        }
                        txtFt = withEllipsis;
                    }
                }
                dc.DrawText(txtFt, new Point(txtX, drawY));

                if (symFt != null && sym != null)
                {
                    double symOffsetY = GetSymbolOffsetY(sym, symFt.Height, txtFt.Height);
                    dc.DrawText(symFt, new Point(
                        indent + itemPadL + sym.Padding.Left,
                        y + itemPadT + symOffsetY));
                }

                y += txtFt.Height + itemPadV + RowSpacing;
            }
        }
    }
}
