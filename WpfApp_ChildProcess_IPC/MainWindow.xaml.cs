using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
                //CreateNoWindow = true,
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
                    if (line is null) break;
                    var message = JsonSerializer.Deserialize<ClockMessage>(line);
                    System.Diagnostics.Trace.WriteLine(message);
                    this.textblock.Text = $"{message.Culture}-{message.Timestamp:yyyy/MM/dd HH:mm:ss}";
                }
                this.textblock.Text = $"{this.textblock.Text}-End";

            }
            catch (Exception ex)
            {
                
            }
            
        }
    }

    public sealed record ClockMessage
    {
        [JsonPropertyName("culture")]
        public string Culture { get; init; } = "";

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; init; }
    }

    public class MainUI : INotifyPropertyChanged
    {
        string m_Text = "";
        public string Text
        {
            set { m_Text = value; this.Update(); }
            get => this.m_Text;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void Update([CallerMemberName]string name="")
            =>this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}