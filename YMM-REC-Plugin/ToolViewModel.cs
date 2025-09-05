using NAudio.Wave;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace YMM_REC_Plugin
{
    public class ToolViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> AvailableDevices { get; } = new ObservableCollection<string>();
        public ObservableCollection<int> SampleRates { get; } = new ObservableCollection<int> { 8000, 16000, 22050, 44100, 48000, 96000 };
        public ObservableCollection<int> BitDepths { get; } = new ObservableCollection<int> { 8, 16, 24, 32 };
        public ObservableCollection<int> Channels { get; } = new ObservableCollection<int> { 1, 2 };

        private string selectedDevice;
        public string SelectedDevice { get => selectedDevice; set { selectedDevice = value; OnPropertyChanged(nameof(SelectedDevice)); } }

        private int selectedSampleRate = 44100;
        public int SelectedSampleRate { get => selectedSampleRate; set { selectedSampleRate = value; OnPropertyChanged(nameof(SelectedSampleRate)); } }

        private int selectedBitDepth = 16;
        public int SelectedBitDepth { get => selectedBitDepth; set { selectedBitDepth = value; OnPropertyChanged(nameof(SelectedBitDepth)); } }

        private int selectedChannel = 1;
        public int SelectedChannel { get => selectedChannel; set { selectedChannel = value; OnPropertyChanged(nameof(SelectedChannel)); } }

        private string recordingStatus = "停止中";
        public string RecordingStatus { get => recordingStatus; set { recordingStatus = value; OnPropertyChanged(nameof(RecordingStatus)); } }

        private double currentVolume;
        public double CurrentVolume { get => currentVolume; set { currentVolume = value; OnPropertyChanged(nameof(CurrentVolume)); } }

        private double inputVolume = 1.0;
        public double InputVolume { get => inputVolume; set { inputVolume = value; OnPropertyChanged(nameof(InputVolume)); } }

        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand ChooseSavePathCommand { get; }

        private WaveInEvent waveIn;
        private WaveFileWriter writer;
        private string currentFilePath;

        public ToolViewModel()
        {
            StartRecordingCommand = new RelayCommand(StartRecording);
            StopRecordingCommand = new RelayCommand(StopRecording);
            ChooseSavePathCommand = new RelayCommand(ChooseSavePath);

            RefreshMicrophones();
        }

        public void RefreshMicrophones()
        {
            AvailableDevices.Clear();
            for (int n = 0; n < WaveInEvent.DeviceCount; n++)
            {
                var deviceInfo = WaveInEvent.GetCapabilities(n);
                AvailableDevices.Add(deviceInfo.ProductName);
            }
            if (AvailableDevices.Count > 0) SelectedDevice = AvailableDevices[0];
        }

        private void ChooseSavePath()
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "WAV ファイル (*.wav)|*.wav",
                FileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav"
            };
            if (dlg.ShowDialog() == true)
            {
                currentFilePath = dlg.FileName;
                RecordingStatus = $"保存先: {currentFilePath}";
            }
        }

        private void StartRecording()
        {
            if (SelectedDevice == null) return;

            int deviceIndex = AvailableDevices.IndexOf(SelectedDevice);
            if (deviceIndex < 0) return;

            try
            {
                waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceIndex,
                    WaveFormat = new WaveFormat(SelectedSampleRate, SelectedBitDepth, SelectedChannel)
                };

                if (string.IsNullOrEmpty(currentFilePath))
                    currentFilePath = Path.Combine(Path.GetTempPath(), $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

                writer = new WaveFileWriter(currentFilePath, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, a) =>
                {
                    if (writer != null)
                    {
                        // 音量調整
                        byte[] buffer = new byte[a.BytesRecorded];
                        Array.Copy(a.Buffer, buffer, a.BytesRecorded);

                        for (int i = 0; i < buffer.Length; i += 2)
                        {
                            short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                            double amplified = sample * InputVolume;
                            if (amplified > short.MaxValue) amplified = short.MaxValue;
                            if (amplified < short.MinValue) amplified = short.MinValue;
                            short outputSample = (short)amplified;
                            buffer[i] = (byte)(outputSample & 0xFF);
                            buffer[i + 1] = (byte)((outputSample >> 8) & 0xFF);
                        }

                        writer.Write(buffer, 0, buffer.Length);

                        // 音量メーター
                        double sum = 0;
                        for (int i = 0; i < buffer.Length; i += 2)
                        {
                            short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                            double sample32 = sample / 32768.0;
                            sum += sample32 * sample32;
                        }
                        CurrentVolume = Math.Sqrt(sum / (buffer.Length / 2));
                    }
                };

                waveIn.RecordingStopped += (s, a) => StopRecordingInternal();

                waveIn.StartRecording();
                RecordingStatus = $"録音中… ({currentFilePath})";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"録音開始エラー: {ex.Message}";
            }
        }

        private void StopRecording() => waveIn?.StopRecording();

        private void StopRecordingInternal()
        {
            try
            {
                writer?.Flush();
                writer?.Dispose();
                writer = null;

                waveIn?.Dispose();
                waveIn = null;

                RecordingStatus = $"停止中. 録音ファイル: {currentFilePath}";
                CurrentVolume = 0;
            }
            catch (Exception ex)
            {
                RecordingStatus = $"停止時エラー: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
