using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace WpfApp_DllHijacking
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetDefaultDllDirectories(uint DirectoryFlags);
        private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
        public const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
        
        public App()
        {
            SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_SYSTEM32);
        }
    }

}
