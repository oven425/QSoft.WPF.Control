using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using WinRT;

namespace WpfApp_PoS
{
    /// <summary>
    /// Step2.xaml 的互動邏輯
    /// </summary>
    public partial class Step2 : UserControl
    {
        public Step2()
        {
            InitializeComponent();
            var ll = new License();
            ll.Version1.Begin = DateTime.Now;
            ll.Version1.End = DateTime.MinValue;
            var jsonstr = JsonSerializer.Serialize(ll, new JsonSerializerOptions { WriteIndented = true } );
        }
        //qxbn yfub kdct vyrb
        private BarcodeScanner _scanner;
        private ClaimedBarcodeScanner _claimedScanner;
        MediaCapture _mediaCapture;
        MediaFrameReader _mediaFrameReader;
        MainUI m_MainUI;
        async private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_MainUI = this.DataContext as MainUI;
            if (DesignerProperties.GetIsInDesignMode(this)) return;
            if (_claimedScanner != null)
            {
                await _claimedScanner.StartSoftwareTriggerAsync();
                await _mediaFrameReader.StartAsync();
                return;
            }
            _scanner = await BarcodeScanner.GetDefaultAsync();

            if (_scanner != null)
            {
                // 2. 佔用掃描器
                _claimedScanner = await _scanner.ClaimScannerAsync();

                if (_claimedScanner != null)
                {
                    var symbologies = new List<uint> { BarcodeSymbologies.Qr};
                    await _claimedScanner.SetActiveSymbologiesAsync([BarcodeSymbologies.Qr]);
                    _claimedScanner.DataReceived += ClaimedScanner_DataReceived;
                    await _claimedScanner.EnableAsync();
                }

                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
                {
                    VideoDeviceId = _scanner.VideoDeviceId,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                });
                //var b1 = _mediaCapture.VideoDeviceController.CameraOcclusionInfo.IsOcclusionKindSupported(Windows.Media.Devices.CameraOcclusionKind.Lid);
                //var b2 = _mediaCapture.VideoDeviceController.CameraOcclusionInfo.IsOcclusionKindSupported(Windows.Media.Devices.CameraOcclusionKind.CameraHardware);
                //_mediaCapture.VideoDeviceController.CameraOcclusionInfo.StateChanged += CameraOcclusionInfo_StateChanged;

                var fs = _mediaCapture.FrameSources.FirstOrDefault();
                _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(fs.Value, MediaEncodingSubtypes.Bgra8);
                _mediaFrameReader.FrameArrived += Mr_FrameArrived;
            }
        }

        async private void Mr_FrameArrived(Windows.Media.Capture.Frames.MediaFrameReader sender, Windows.Media.Capture.Frames.MediaFrameArrivedEventArgs args)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;
            if (softwareBitmap is not null)
            {
                try
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        using var m = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read);
                        using var reference = m.CreateReference();
                        if (this.m_MainUI.Preview == null)
                        {
                            this.m_MainUI.Preview = new WriteableBitmap((int)softwareBitmap.PixelWidth, (int)softwareBitmap.PixelHeight, 96, 96, PixelFormats.Bgr32, null);

                        }
                        this.m_MainUI.Preview.Lock();

                        unsafe
                        {
                            (reference.As<IMemoryBufferByteAccess>()).GetBuffer(out var ptr, out var capacity);

                            NativeMemory.Copy(ptr, (void*)this.m_MainUI.Preview.BackBuffer, capacity);
                            this.m_MainUI.Preview.AddDirtyRect(new Int32Rect(0, 0, this.m_MainUI.Preview.PixelWidth, this.m_MainUI.Preview.PixelHeight));
                        }
                        this.m_MainUI.Preview.Unlock();
                    });
                }
                catch(Exception ee)
                {

                }
                
            }
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        //private void CameraOcclusionInfo_StateChanged(Windows.Media.Devices.CameraOcclusionInfo sender, Windows.Media.Devices.CameraOcclusionStateChangedEventArgs args)
        //{
        //    //throw new NotImplementedException();
        //}

        private void ClaimedScanner_DataReceived(ClaimedBarcodeScanner sender, BarcodeScannerDataReceivedEventArgs args)
        {
            var scanData = args.Report.ScanDataLabel;
            using var reader = DataReader.FromBuffer(scanData);
            string result = reader.ReadString(scanData.Length);
            this.m_MainUI.License = result;
            this.m_MainUI.IsLicensePass = 1;
            try
            {
                var license = JsonSerializer.Deserialize<License>(result);
                if(license != null)
                {
                    if (license.Version1.Begin < DateTime.Now && license.Version1.End > DateTime.Now)
                    {
                        this.m_MainUI.IsLicensePass = 2;
                    }
                }
            }
            catch(Exception ee)
            {

            }
        }

        async private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            await _mediaFrameReader.StopAsync();
            await _claimedScanner.StopSoftwareTriggerAsync();
            
        }
    }
}
