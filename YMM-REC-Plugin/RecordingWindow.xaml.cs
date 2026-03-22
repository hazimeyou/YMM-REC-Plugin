using System.Windows;
using YMM_REC_Plugin.Services;

namespace YMM_REC_Plugin
{
    public partial class RecordingWindow : Window
    {
        public RecordingWindow(string? initialText = null)
        {
            InitializeComponent();
            DataContext = new RecordingWindowViewModel(initialText);
            LogService.Write("RecordingWindow: opened");
            Closed += (_, _) => LogService.Write("RecordingWindow: closed");
        }
    }
}
