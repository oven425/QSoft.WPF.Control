using System.Windows;
using System.Windows.Media;

namespace QSoft.WPF.TextBlockT
{
    public class BulletItem : FrameworkContentElement
    {
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register(nameof(Symbol), typeof(string),
                typeof(BulletItem), new PropertyMetadata("•"));

        public string Symbol
        {
            get => (string)GetValue(SymbolProperty);
            set => SetValue(SymbolProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string),
                typeof(BulletItem), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty IndentLevelProperty =
            DependencyProperty.Register(nameof(IndentLevel), typeof(int),
                typeof(BulletItem), new PropertyMetadata(0));

        public int IndentLevel
        {
            get => (int)GetValue(IndentLevelProperty);
            set => SetValue(IndentLevelProperty, value);
        }

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight),
                typeof(BulletItem),
                new FrameworkPropertyMetadata(FontWeights.Normal,
                    FrameworkPropertyMetadataOptions.Inherits));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(nameof(FontSize), typeof(double),
                typeof(BulletItem),
                new FrameworkPropertyMetadata(12.0,
                    FrameworkPropertyMetadataOptions.Inherits));

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        // Symbol 專屬字號，未設定時跟隨 FontSize
        public static readonly DependencyProperty SymbolFontSizeProperty =
            DependencyProperty.Register(nameof(SymbolFontSize), typeof(double?),
                typeof(BulletItem),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.Inherits));

        public double? SymbolFontSize
        {
            get => (double?)GetValue(SymbolFontSizeProperty);
            set => SetValue(SymbolFontSizeProperty, value);
        }

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily),
                typeof(BulletItem),
                new FrameworkPropertyMetadata(new FontFamily("Segoe UI"),
                    FrameworkPropertyMetadataOptions.Inherits));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle),
                typeof(BulletItem),
                new FrameworkPropertyMetadata(FontStyles.Normal,
                    FrameworkPropertyMetadataOptions.Inherits));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public static readonly DependencyProperty FontStretchProperty =
            DependencyProperty.Register(nameof(FontStretch), typeof(FontStretch),
                typeof(BulletItem),
                new FrameworkPropertyMetadata(FontStretches.Normal,
                    FrameworkPropertyMetadataOptions.Inherits));

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush),
                typeof(BulletItem),
                new FrameworkPropertyMetadata(Brushes.Black,
                    FrameworkPropertyMetadataOptions.Inherits));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }
    }
}