using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp_ChildProcess_IPC
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
        Process? m_ChildProcess;
        async private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.m_ChildProcess is not null) return;
            var pi = new ProcessStartInfo()
            {
                FileName = "powershell",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "-ExecutionPolicy Bypass -File Clock.ps1"
            };
            try
            {
                this.m_ChildProcess = new Process()
                {
                    StartInfo = pi,
                };
                m_ChildProcess.Start();
                while (true)
                {
                    var line = await this.m_ChildProcess.StandardOutput.ReadLineAsync();
                    if (line is null) continue;
                    
                    var message = JsonSerializer.Deserialize<ClockMessage>(line);
                    System.Diagnostics.Trace.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                
            }
            
        }
    }

    public sealed record ClockMessage
    {
        [JsonPropertyName("culture")]
        public string Culture { get; init; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; init; }
    }
}