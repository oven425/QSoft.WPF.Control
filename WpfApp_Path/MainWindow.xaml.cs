using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp_Path
{
    public partial class MainWindow : Window
    {
        private const int IconSize = 16;

        public MainWindow()
        {
            InitializeComponent();

            var sourceCircle = new EllipseGeometry(new Rect(0.6, 0.6, 14.8, 14.8));
            var sourceQuestion = (Geometry)FindResource("QuestionGeometry");
            var combinedGeometry = (Geometry)FindResource("HelpCircleGeometry");

            var onePixel = CompareRaster(sourceCircle, sourceQuestion, combinedGeometry, 1);
            var tenPixels = CompareRaster(sourceCircle, sourceQuestion, combinedGeometry, 10);

            ComparisonResult.Text =
                $"WPF 光柵逐像素結果：16 × 16 px 有 {onePixel.DifferentPixels} 個不同像素，" +
                $"160 × 160 px 有 {tenPixels.DifferentPixels} 個不同像素 " +
                $"({tenPixels.DifferencePercent:F4}%，最大單色通道差 {tenPixels.MaximumChannelDifference}/255，" +
                $"平均通道差 {tenPixels.MeanAbsoluteChannelDifference:F4}/255)。";
        }

        private static RasterDifference CompareRaster(
            Geometry sourceCircle,
            Geometry sourceQuestion,
            Geometry combinedGeometry,
            int scale)
        {
            var sourceVisual = new DrawingVisual();
            using (var drawingContext = sourceVisual.RenderOpen())
            {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.PushOpacity(0.5);
                drawingContext.DrawGeometry(null, new Pen(Brushes.White, 1.2), sourceCircle);
                drawingContext.DrawGeometry(Brushes.White, null, sourceQuestion);
                drawingContext.Pop();
                drawingContext.Pop();
            }

            var combinedVisual = new DrawingVisual();
            using (var drawingContext = combinedVisual.RenderOpen())
            {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.PushOpacity(0.5);
                drawingContext.DrawGeometry(Brushes.White, null, combinedGeometry);
                drawingContext.Pop();
                drawingContext.Pop();
            }

            var pixelWidth = IconSize * scale;
            var pixelHeight = IconSize * scale;
            var sourcePixels = RenderPixels(sourceVisual, pixelWidth, pixelHeight);
            var combinedPixels = RenderPixels(combinedVisual, pixelWidth, pixelHeight);

            var differentPixels = 0;
            var maximumChannelDifference = 0;
            long totalChannelDifference = 0;

            for (var pixel = 0; pixel < pixelWidth * pixelHeight; pixel++)
            {
                var pixelIsDifferent = false;
                for (var channel = 0; channel < 4; channel++)
                {
                    var index = pixel * 4 + channel;
                    var difference = Math.Abs(sourcePixels[index] - combinedPixels[index]);
                    totalChannelDifference += difference;
                    maximumChannelDifference = Math.Max(maximumChannelDifference, difference);
                    pixelIsDifferent |= difference != 0;
                }

                if (pixelIsDifferent)
                {
                    differentPixels++;
                }
            }

            var totalPixels = pixelWidth * pixelHeight;
            return new RasterDifference(
                differentPixels,
                differentPixels * 100d / totalPixels,
                maximumChannelDifference,
                totalChannelDifference / (double)(sourcePixels.Length));
        }

        private static byte[] RenderPixels(Visual visual, int width, int height)
        {
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);
            return pixels;
        }

        private readonly record struct RasterDifference(
            int DifferentPixels,
            double DifferencePercent,
            int MaximumChannelDifference,
            double MeanAbsoluteChannelDifference);
    }
}
