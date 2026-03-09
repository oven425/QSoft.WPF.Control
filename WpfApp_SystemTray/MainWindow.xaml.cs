using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace WpfApp_SystemTray
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

        // 基礎常數定義
        private const int NIM_ADD = 0x00;
        private const int NIM_MODIFY = 0x01;
        private const int NIM_DELETE = 0x02;
        private const int NIF_MESSAGE = 0x01;
        private const int NIF_ICON = 0x02;
        private const int NIF_TIP = 0x04;
        private const int NIF_INFO = 0x10;
        private const int NIIF_NONE = 0x00;        // 無圖示
        private const int NIIF_INFO = 0x01;        // 資訊圖示 (藍色 i)
        private const int NIIF_WARNING = 0x02;     // 警告圖示 (黃色三角形)
        private const int NIIF_ERROR = 0x03;       // 錯誤圖示 (紅色圓圈 X)
        private const int NIIF_USER = 0x04;        // 使用自定義圖示 (需搭配 hBalloonIcon)
        private const int NIIF_NOSOUND = 0x10;     // 不播放提示音
        private const int WM_TRAYMOUSE = 0x8000; // 自定義訊息 ID

        // 滑鼠事件常數
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 514;
        private const int WM_RBUTTONDOWN = 0x0204;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        NOTIFYICONDATA nid = new();

        
        private void button_create_Click(object sender, RoutedEventArgs e)
        {
            nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
            nid.hIcon = System.Drawing.Icon.ExtractAssociatedIcon("red.ico").Handle;
            //nid.uCallbackMessage = WMAPP_NOTIFYCALLBACK;
            nid.szTip = this.Title;
            Shell_NotifyIcon(NIM_ADD, ref nid);
        }

        private void button_remove_Click(object sender, RoutedEventArgs e)
        {
            nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
            nid.hIcon = System.Drawing.Icon.ExtractAssociatedIcon("red.ico").Handle;
            nid.szTip = this.Title;
            Shell_NotifyIcon(NIM_DELETE, ref nid);
        }

        private void combobox_ico_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var icocolor = this.combobox_ico.SelectedIndex switch
            {
                0 => "red.ico",
                1 => "green.ico",
                2 => "blue.ico",
                _ => null
            };

            if (icocolor != null)
            {
                nid.uFlags = NIF_ICON;
                nid.hIcon = System.Drawing.Icon.ExtractAssociatedIcon(icocolor).Handle;
                Shell_NotifyIcon(NIM_MODIFY, ref nid);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wh = new(System.Windows.Application.Current.MainWindow);
            nid.cbSize = Marshal.SizeOf(nid);
            nid.hWnd = wh.Handle;
            nid.uTimeoutOrVersion = 4;
            nid.uCallbackMessage = WM_TRAYMOUSE;
            HwndSource source = HwndSource.FromHwnd(wh.Handle);
            source.AddHook(MyWindowProc);
        }

        private IntPtr MyWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TRAYMOUSE)
            {
                int mouseEvent = lParam.ToInt32();
                if (mouseEvent == WM_LBUTTONDOWN)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
                else if (mouseEvent == WM_RBUTTONDOWN)
                {
                    // 在這裡可以手動彈出 WPF 的 ContextMenu
                    MessageBox.Show("你點了右鍵！");
                }
                else if(mouseEvent == WM_LBUTTONUP)
                {
                    // 在這裡可以手動彈出 WPF 的 ContextMenu
                    MessageBox.Show("WM_LBUTTONUP");
                }
            }
            return IntPtr.Zero;
        }

        private void button_showballonmsg_Click(object sender, RoutedEventArgs e)
        {
            // 準備要傳遞的結構
            nid.uFlags = NIF_INFO; // 告訴系統我們要更新氣泡訊息
            nid.szInfo = $"{DateTime.Now.ToString()}";     // 氣泡內容
            nid.szInfoTitle = this.Title; // 氣泡標題
            nid.hIcon = System.Drawing.Icon.ExtractAssociatedIcon("red.ico").Handle;

            nid.dwInfoFlags = NIIF_INFO; // 顯示資訊圖示 (藍色小 i)
            nid.uTimeoutOrVersion = (int)TimeSpan.FromSeconds(5).TotalMilliseconds; // 顯示 5 秒 (單位是毫秒)

            // 使用 NIM_MODIFY 來更新現有的圖示狀態，送出通知
            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }
    }
}