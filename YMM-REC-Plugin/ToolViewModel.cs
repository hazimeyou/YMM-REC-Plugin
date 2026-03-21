using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using YMM_REC_Plugin.Models;
using YMM_REC_Plugin.Services;

namespace YMM_REC_Plugin
{
    public class ToolViewModel : INotifyPropertyChanged
    {
        private readonly RecordingService recordingService;
        private readonly TimelineInsertService timelineInsertService;

        public ObservableCollection<string> AvailableDevices { get; } = new();

        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand RefreshDevicesCommand { get; }

        private string? selectedDevice;
        public string? SelectedDevice
        {
            get => selectedDevice;
            set
            {
                selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));
                RaiseCommandStates();
            }
        }

        private string recordingStatus = "停止中";
        public string RecordingStatus
        {
            get => recordingStatus;
            set
            {
                recordingStatus = value;
                OnPropertyChanged(nameof(RecordingStatus));
            }
        }

        private double currentVolume;
        public double CurrentVolume
        {
            get => currentVolume;
            set
            {
                currentVolume = value;
                OnPropertyChanged(nameof(CurrentVolume));
            }
        }

        private string recordsDirectory = string.Empty;
        public string RecordsDirectory
        {
            get => recordsDirectory;
            set
            {
                recordsDirectory = value;
                OnPropertyChanged(nameof(RecordsDirectory));
            }
        }

        public bool IsRecording => recordingService.IsRecording;

        public ToolViewModel()
        {
            var recordPathService = new RecordPathService();
            recordingService = new RecordingService(recordPathService);
            timelineInsertService = new TimelineInsertService();

            recordingService.DataAvailable += OnRecordingDataAvailable;
            recordingService.RecordingStateChanged += OnRecordingStateChanged;

            StartRecordingCommand = new RelayCommand(StartRecording, CanStartRecording);
            StopRecordingCommand = new RelayCommand(StopRecording, CanStopRecording);
            RefreshDevicesCommand = new RelayCommand(RefreshMicrophones);

            RecordsDirectory = recordPathService.GetRecordsDirectory();
            RefreshMicrophones();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void RefreshMicrophones()
        {
            AvailableDevices.Clear();

            foreach (var deviceName in recordingService.GetAvailableDeviceNames())
            {
                AvailableDevices.Add(deviceName);
            }

            if (AvailableDevices.Count > 0)
            {
                if (SelectedDevice is null || !AvailableDevices.Contains(SelectedDevice))
                    SelectedDevice = AvailableDevices[0];
            }
            else
            {
                SelectedDevice = null;
                RecordingStatus = "録音デバイスが見つかりません。";
            }

            RaiseCommandStates();
        }

        private bool CanStartRecording() => !IsRecording && !string.IsNullOrWhiteSpace(SelectedDevice);

        private bool CanStopRecording() => IsRecording;

        private void StartRecording()
        {
            if (SelectedDevice is null)
            {
                RecordingStatus = "録音デバイスを選択してください。";
                return;
            }

            try
            {
                recordingService.StartRecording(SelectedDevice);
                RecordingStatus = $"録音中... 保存先: {RecordsDirectory}";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"録音開始に失敗しました: {ex.Message}";
                RaiseCommandStates();
            }
        }

        private async void StopRecording()
        {
            await StopRecordingAsync();
        }

        private async Task StopRecordingAsync()
        {
            RecordedFileInfo? recordedFile;

            try
            {
                recordedFile = recordingService.StopRecording();
            }
            catch (Exception ex)
            {
                RecordingStatus = $"録音停止に失敗しました: {ex.Message}";
                RaiseCommandStates();
                return;
            }

            CurrentVolume = 0;
            RaiseCommandStates();

            if (recordedFile is null)
            {
                RecordingStatus = "録音データがないため保存・追加を行いませんでした。";
                return;
            }

            if (recordedFile.DataLength <= 0)
            {
                RecordingStatus = $"録音データ長が 0 のため追加しませんでした: {recordedFile.FilePath}";
                return;
            }

            RecordingStatus = $"録音停止。タイムラインへ追加中... {recordedFile.FilePath}";

            try
            {
                await timelineInsertService.InsertAsync(recordedFile);
                RecordingStatus = $"録音停止。タイムラインへ追加しました: {recordedFile.FilePath}";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"タイムライン追加に失敗しました: {ex.Message}";
            }
        }

        private void OnRecordingDataAvailable(object? sender, RecordingDataEventArgs e)
        {
            CurrentVolume = e.Volume;
        }

        private void OnRecordingStateChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsRecording));
            RaiseCommandStates();
        }

        private void RaiseCommandStates()
        {
            if (StartRecordingCommand is RelayCommand start)
                start.RaiseCanExecuteChanged();

            if (StopRecordingCommand is RelayCommand stop)
                stop.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
