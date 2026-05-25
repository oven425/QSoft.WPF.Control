using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace QSoft.WPF.TextBlockT
{
    
    public class TextBlockExElementBase: FrameworkContentElement
    {
        public virtual TextBlockExElement[] Elements => [];
    }

    [System.Windows.Markup.ContentProperty("Children")]
    public class TextBlockExElementGroup : TextBlockExElementBase
    {
        public FreezableCollection<TextBlockExElementBase> Children { get; set; } = [];

        public override TextBlockExElement[] Elements =>
            [.. Children.SelectMany(c => c.Elements)];

        public TextBlockExElementGroup()
        {
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var child in Children)
                child.DataContext = e.NewValue;
        }

        private void OnChildrenChanged(object? sender, EventArgs e)
        {
            foreach (var child in Children)
            {
                if (child.DataContext != DataContext)
                    child.DataContext = DataContext;
            }
        }
    }

    [System.Windows.Markup.ContentProperty("List")]
    public class TextBlockExElementArray : TextBlockExElementBase
    {
        public FreezableCollection<TextBlockExElement> List { set; get; } = [];

        public override TextBlockExElement[] Elements => [.. List];

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(TextBlockExElementArray),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is TextBlockExElementArray self)
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
                typeof(TextBlockExElementArray),
                new PropertyMetadata(null, OnItemTemplateChanged));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((TextBlockExElementArray)d).RefreshItems();

        private void RefreshItems()
        {
            List.Clear();

            if (ItemsSource == null) return;

            foreach (var item in ItemsSource)
            {
                TextBlockExElement? element = null;

                if (ItemTemplate != null)
                {
                    // 從 DataTemplate 實例化，並設定 DataContext 讓 {Binding} 生效
                    element = ItemTemplate.LoadContent() as TextBlockExElement;
                    if (element != null)
                        element.DataContext = item;
                }

                // 沒有 ItemTemplate 時退而求其次：直接把值塞入 Text
                element ??= new TextBlockExElement { Text = item?.ToString() ?? string.Empty };

                List.Add(element);
            }
        }
    }
    public class Symbol:DependencyObject
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(Symbol), new PropertyMetadata(""));
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(Symbol), new FrameworkPropertyMetadata(12.0));
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty VerticalAlignmentProperty = DependencyProperty.Register(nameof(VerticalAlignment), typeof(VerticalAlignment), typeof(Symbol), new FrameworkPropertyMetadata(VerticalAlignment.Top));
        public VerticalAlignment VerticalAlignment
        {
            get => (VerticalAlignment)GetValue(VerticalAlignmentProperty);
            set => SetValue(VerticalAlignmentProperty, value);
        }

        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(Symbol), new FrameworkPropertyMetadata(new Thickness()));
        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(Symbol), new FrameworkPropertyMetadata(new FontFamily("Arial"), FrameworkPropertyMetadataOptions.Inherits));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(Symbol),  new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register(nameof(FontStretch), typeof(FontStretch),  typeof(Symbol), new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(Symbol), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.Inherits));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(Symbol), new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }
    }
    public class TextBlockExElement : TextBlockExElementBase
    {
        public override TextBlockExElement[] Elements => [this];
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(TextBlockExElement), new FrameworkPropertyMetadata(new Thickness()));
        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }
        public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(nameof(Symbol), typeof(Symbol), typeof(TextBlockExElement), new PropertyMetadata());

        public Symbol Symbol
        {
            get => (Symbol)GetValue(SymbolProperty);
            set => SetValue(SymbolProperty, value);
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBlockExElement), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(nameof(IndentLevel), typeof(int), typeof(TextBlockExElement), new PropertyMetadata(0));

        public int IndentLevel
        {
            get => (int)GetValue(IndentLevelProperty);
            set => SetValue(IndentLevelProperty, value);
        }

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(TextBlockExElement), new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(nameof(FontSize), typeof(double),
                typeof(TextBlockExElement),
                new FrameworkPropertyMetadata(12.0,
                    FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(TextBlockExElement), new FrameworkPropertyMetadata(new FontFamily("Arial"), FrameworkPropertyMetadataOptions.Inherits));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(TextBlockExElement), new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register(nameof(FontStretch), typeof(FontStretch), typeof(TextBlockExElement), new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.Inherits));

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        //public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(nameof(Foreground), typeof(Brush),  typeof(TextBlockExElement), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.Inherits|FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ForegroundProperty =
            TextBlockEx.ForegroundProperty.AddOwner(
                typeof(TextBlockExElement),
                new FrameworkPropertyMetadata(
                    Brushes.Black,
                    FrameworkPropertyMetadataOptions.Inherits |
                    FrameworkPropertyMetadataOptions.AffectsRender, ForegroundPropertyChanged));
        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private static void ForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe && fe.Parent is TextBlockEx parent)
            {
                parent.InvalidateVisual();
            }
        }
    }

}
