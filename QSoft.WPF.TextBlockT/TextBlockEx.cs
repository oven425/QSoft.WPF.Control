using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;

namespace QSoft.WPF.TextBlockT
{

    
    public class TextBlockEx : FrameworkElement
    {
        public static readonly DependencyProperty LeadingElementProperty =  DependencyProperty.Register(nameof(LeadingElement), typeof(UIElement), typeof(TextBlockEx),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnLeadingElementChanged));

        public UIElement? LeadingElement
        {
            get => (UIElement?)GetValue(LeadingElementProperty);
            set => SetValue(LeadingElementProperty, value);
        }

        protected override int VisualChildrenCount
            => LeadingElement is null ? 0 : 1;

        protected override Visual GetVisualChild(int index)
        {
            if (LeadingElement is null || index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return LeadingElement;
        }

        private static void OnLeadingElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (TextBlockEx)d;
            if (e.OldValue is UIElement oldEl)
                ctrl.RemoveVisualChild(oldEl);
            if (e.NewValue is UIElement newEl)
                ctrl.AddVisualChild(newEl);
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items),
                typeof(ObservableCollection<TextBlockExElementBase>), typeof(TextBlockEx),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsChanged));

        public ObservableCollection<TextBlockExElementBase> Items
        {
            get => (ObservableCollection<TextBlockExElementBase>)GetValue(ItemsProperty);
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

        public static readonly DependencyProperty BackgroundProperty =
           DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(TextBlockEx),
               new FrameworkPropertyMetadata(Brushes.Yellow,
                   FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
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
            Items = new ObservableCollection<TextBlockExElementBase>();
        }

        protected override IEnumerator LogicalChildren
            => (Items ?? new ObservableCollection<TextBlockExElementBase>()).GetEnumerator();

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


        protected override Size MeasureOverride(Size availableSize)
        {
            LeadingElement?.Measure(availableSize);

            var items = Items;
            if (items is null || items.Count == 0) return LeadingElement?.DesiredSize ?? Size.Empty;

            double avW = availableSize.Width;

            double w = 0;
            double h = 0;
            var ll = items.SelectMany(x => x.Elements, (x,y)=>new { x, y });
            for(int i=0; i<items.Count; i++)
            {
                for(int j=0; j<items[i].Elements.Length; j++)
                {
                    var oo = items[i].Elements[j];
                    var symbolft = MakeSymbolText(oo.Symbol, avW);
                    var txtft = MakeText(oo, oo.Text, avW);
                    w = w + oo.Padding.Left + oo.Padding.Right + oo.Symbol.Padding.Left + oo.Symbol.Padding.Right + symbolft.Width + txtft.Width;
                    h = h + oo.Padding.Top + oo.Padding.Bottom + oo.Symbol.Padding.Top + oo.Symbol.Padding.Bottom + symbolft.Height + txtft.Height;
                }
                if (items[i] is TextBlockExElementArray tr)
                {
                    
                    var tp = tr.ItemTemplate.LoadContent();
                }
            }
            
            if(availableSize.Width < w)
            {
                w = availableSize.Width;
            }
            if(availableSize.Height < h)
            {
                h = availableSize.Height;
            }
            return new Size(w, h);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // 排版到左上角 (0, 0)
            if (LeadingElement is not null)
            {
                var desired = LeadingElement.DesiredSize;
                LeadingElement.Arrange(new Rect(0, 0, desired.Width, desired.Height));
            }
            return finalSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));
            var items = Items;
            if (items is null || items.Count == 0) return;

            double avW = ActualWidth;
            double avH = ActualHeight;

            List<(Point symbolpt, Point textpt, FormattedText symbolft, FormattedText txtft, bool addtrim)> renderlist = [];

            var symbol_y = 0.0;
            var text_y = 0.0;
            FormattedText? m_trimmingft = null;
            for(int j=0;j<items.Count; j++)
            {
                for (int i = 0; i < items[j].Elements.Length; i++)
                {
                    var item = items[j].Elements[i];
                    var w = avW - item.Symbol.Padding.Left - item.Symbol.Padding.Right;
                    var symbolft = MakeSymbolText(item.Symbol, avW);
                    w = w - symbolft.WidthIncludingTrailingWhitespace - item.Symbol.Padding.Left - item.Symbol.Padding.Right;
                    var txtft = MakeText(item, item.Text, w);

                    txtft.MaxTextHeight = avH - text_y;
                    symbol_y = symbol_y + item.Symbol.Padding.Top;
                    text_y = text_y + item.Padding.Top;
                    var symbolpt = new Point(item.Symbol.Padding.Left, symbol_y);
                    //if (txtft.Height > 0)
                    //{
                    //    dc.DrawText(symbolft, symbolpt);
                    //}


                    var text_x = item.Symbol.Padding.Left + symbolft.WidthIncludingTrailingWhitespace + item.Symbol.Padding.Right + item.Padding.Left;
                    var textpt = new Point(text_x, text_y);

                    //dc.DrawText(txtft, textpt);




                    text_y = symbol_y = text_y + txtft.Height + item.Padding.Bottom;

                    if (txtft.Height == 0)
                    {
                        if (renderlist.Any())
                        {
                            var aa = renderlist.Last();
                            var aa1 = (aa.symbolpt, aa.textpt, aa.symbolft, m_trimmingft, true);
                            renderlist.Remove(aa);
                            renderlist.Add(aa1);
                        }

                        break;
                    }

                    if (text_y > avH)
                    {

                        break;
                    }
                    m_trimmingft = MakeText(item, item.Text.TrimEnd() + " ...", w);
                    m_trimmingft.MaxTextHeight = txtft.MaxTextHeight;
                    renderlist.Add((symbolpt, textpt, symbolft, txtft, false));
                }

                foreach (var oo in renderlist)
                {
                    if (oo.txtft.Height > 0)
                    {
                        dc.DrawText(oo.symbolft, oo.symbolpt);

                    }
                    dc.DrawText(oo.txtft, oo.textpt);
                }

            }
        }
    }
}
