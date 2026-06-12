using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace QSoft.WPF.TextBlockT
{

    
    public class TextBlockEx : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(TextBlockEx),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlockEx self)
            {
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                    oldCollection.CollectionChanged -= self.OnSourceCollectionChanged;

                if (e.NewValue is INotifyCollectionChanged newCollection)
                    newCollection.CollectionChanged += self.OnSourceCollectionChanged;
                self.RefreshItems();
            }

        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => RefreshItems();

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(
                nameof(ItemTemplate),
                typeof(DataTemplate),
                typeof(TextBlockEx),
                new PropertyMetadata(null, OnItemTemplateChanged));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((TextBlockEx)d).RefreshItems();

        private void RefreshItems()
        {
            this.Items.Clear();

            if (ItemsSource == null) return;

            foreach (var item in ItemsSource)
            {
                TextBlockExElementBase? element = null;

                if (ItemTemplate != null)
                {
                    element = ItemTemplate.LoadContent() as TextBlockExElementBase;
                    if (element != null)
                        element.DataContext = item;
                }
                element ??= new TextBlockExElement { Text = item?.ToString() ?? string.Empty };
                this.Items.Add(element);
            }
        }

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
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.Inherits|FrameworkPropertyMetadataOptions.AffectsRender));

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

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(TextBlockEx), new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(TextBlockEx), new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register(nameof(FontStretch), typeof(FontStretch), typeof(TextBlockEx), new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
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
            if (e.OldValue is ObservableCollection<TextBlockExElementBase> old)
            {
                old.CollectionChanged -= ctrl.OnCollectionChanged;
                foreach (var item in old) ctrl.RemoveLogicalChild(item);
            }
            if (e.NewValue is ObservableCollection<TextBlockExElementBase> nw)
            {
                foreach (var item in nw) ctrl.AddLogicalChild(item);
                nw.CollectionChanged += ctrl.OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var ff = sender as FrameworkElement;
            if (e.OldItems != null)
                foreach (TextBlockExElementBase item in e.OldItems) RemoveLogicalChild(item);
            if (e.NewItems != null)
                foreach (TextBlockExElementBase item in e.NewItems)
                {
                    AddLogicalChild(item);
                }

            InvalidateMeasure();
            InvalidateVisual();
        }

        private double PixelsPerDip()
        {
            try   { return VisualTreeHelper.GetDpi(this).PixelsPerDip; }
            catch { return 1.0; }
        }

        private FormattedText MakeText(TextBlockExElement item, string text, double maxWidth)
        {
            var ft = new FormattedText(
                string.IsNullOrEmpty(text) ? " " : text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(item.FontFamily, item.FontStyle, item.FontWeight, item.FontStretch),
                item.FontSize,
                item.Foreground,
                PixelsPerDip());
            //ft.Trimming = TextTrimming.None;
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
            for(int i=0; i<items.Count; i++)
            {
                for(int j=0; j<items[i].Elements.Length; j++)
                {
                    var oo = items[i].Elements[j];
                    //var symbolft = MakeSymbolText(oo.Symbol, avW);
                    //var txtft = MakeText(oo, oo.Text, avW);
                    //w = w + oo.Padding.Left + oo.Padding.Right + oo.Symbol.Padding.Left + oo.Symbol.Padding.Right + symbolft.Width + txtft.Width;
                    //h = h + oo.Padding.Top + oo.Padding.Bottom + oo.Symbol.Padding.Top + oo.Symbol.Padding.Bottom + symbolft.Height + txtft.Height;


                    var txtft = MakeText(oo, oo.Text, avW);
                    w = w + oo.Padding.Left + oo.Padding.Right +  txtft.Width;
                    h = h + oo.Padding.Top + oo.Padding.Bottom +  txtft.Height;
                    if(oo.Symbol!=null)
                    {
                        var symbolft = MakeSymbolText(oo.Symbol, avW);
                        w = w + oo.Symbol.Padding.Left + oo.Symbol.Padding.Right + symbolft.Width;
                        h = h + oo.Symbol.Padding.Top + oo.Symbol.Padding.Bottom + symbolft.Height;
                    }

                }

            }
            if(LeadingElement != null)
            {
                w = w + LeadingElement.DesiredSize.Width;
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
            double left = 0;
            if(this.LeadingElement != null)
            {
                left = LeadingElement.DesiredSize.Width;
            }
            double avW = ActualWidth - left;
            double avH = ActualHeight;

            List<(Point symbolpt, Point textpt, FormattedText? symbolft, FormattedText txtft)> renderlist = [];

            var symbol_y = 0.0;
            var text_y = 0.0;
            FormattedText? m_trimmingft = null;
            bool isend = false;
            for(int j=0;j<items.Count; j++)
            {
                for (int i = 0; i < items[j].Elements.Length; i++)
                {
                    var item = items[j].Elements[i];
                    var w = avW;
                    FormattedText? symbolft = null;
                    if (item.Symbol != null)
                    {
                        w = w - item.Symbol.Padding.Left - item.Symbol.Padding.Right;
                        symbolft = MakeSymbolText(item.Symbol, avW);
                        w = w - symbolft.WidthIncludingTrailingWhitespace - item.Symbol.Padding.Left - item.Symbol.Padding.Right;
                    }
                    
                    var txtft = MakeText(item, item.Text, w);
                    var maxh = avH - text_y;
                    isend = txtft.Height > maxh;
                    txtft.MaxTextHeight = maxh;


                    symbol_y = symbol_y + (item.Symbol == null ? 0.0 : item.Symbol.Padding.Top);
                    text_y = text_y + item.Padding.Top;
                    var symbolpt = new Point((item.Symbol == null ? 0.0 :item.Symbol.Padding.Left+left), symbol_y);
                    var text_x = item.Padding.Left;
                    if(item.Symbol != null)
                    {
                        text_x = text_x + item.Symbol.Padding.Left + item.Symbol.Padding.Right;
                        if(symbolft != null)
                        {
                            text_x = text_x+ symbolpt.X + symbolft.WidthIncludingTrailingWhitespace;
                        }
                    }
                    else
                    {
                        text_x = text_x + left;
                    }
                    var textpt = new Point(text_x, text_y);

                    text_y = symbol_y = text_y + txtft.Height + item.Padding.Bottom;

                    if (txtft.Height == 0)
                    {
                        if (renderlist.Count > 0)
                        {
                            var aa = renderlist[^1];
                            var aa1 = (aa.symbolpt, aa.textpt, aa.symbolft, m_trimmingft);
                            renderlist.Remove(aa);
                            renderlist.Add(aa1);
                        }
                        isend = true;
                        break;
                    }

                    if (text_y > avH)
                    {
                        break;
                    }
                    m_trimmingft = MakeText(item, item.Text.TrimEnd() + "...", w);
                    m_trimmingft.MaxTextHeight = txtft.MaxTextHeight;
                    renderlist.Add((symbolpt, textpt, symbolft, txtft));
                }
                if (isend)
                {
                    break;
                }


            }

            foreach (var oo in renderlist)
            {
                if (oo.txtft.Height > 0 && oo.symbolft != null)
                {
                    dc.DrawText(oo.symbolft, oo.symbolpt);
                }
                dc.DrawText(oo.txtft, oo.textpt);
            }
        }
    }
}
