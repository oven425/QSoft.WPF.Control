using Microsoft.Win32;
using QRCoder;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.PointOfService;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;


namespace WpfApp_PoS
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

        MainUI m_MainUI;
        private BitmapSource ConvertBitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // 讓它可以跨執行緒使用

                return bitmapImage;
            }
        }

        async private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.m_MainUI != null)return;
            this.DataContext = this.m_MainUI = new MainUI();
            RequestLicense sj = new RequestLicense();
            sj.Version = 1;
            //HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS
            string keyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    object productName = key.GetValue("ProductName");
                    sj.Version1.OS = productName.ToString();
                }
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS"))
            {
                if (key != null)
                {
                    object productName = key.GetValue("SystemManufacturer");
                    sj.Version1.Manufacturer = productName.ToString();
                }
            }

            this.m_MainUI.SendJson = JsonSerializer.Serialize(sj, new JsonSerializerOptions() { WriteIndented = true });

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode($"mailto:someone@example.com?subject=Hello&body={this.m_MainUI.SendJson}", QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            this.image.Source = ConvertBitmapToImageSource(qrCodeImage);
            

        }

        
    }

    public class MainUI : INotifyPropertyChanged
    {
        WriteableBitmap m_Preview;
        public WriteableBitmap Preview
        {
            set { m_Preview = value; this.Update(); }
            get => m_Preview;
        }
        string m_SendJson;
        public string SendJson
        {
            set { m_SendJson = value;this.Update(); }
            get => m_SendJson;
        }

        string m_License;
        public string License
        {
            set { m_License = value; this.Update(); }
            get => m_License;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void Update([CallerMemberName] string name="")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RequestLicense1
    {
        public string Manufacturer { set; get; }
        public string OS { set; get; }
    }

    public class RequestLicense
    {
        public int Version { set; get; }
        public RequestLicense1 Version1 { set; get; } = new RequestLicense1();
    }
}