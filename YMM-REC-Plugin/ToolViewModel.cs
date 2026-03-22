using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using YMM_REC_Plugin.Services;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;

namespace YMM_REC_Plugin
{
    public class ToolViewModel : INotifyPropertyChanged, ITimelineToolViewModel
    {
        private readonly RecordingService recordingService;
        private readonly TimelineInsertService timelineInsertService;

        public ObservableCollection<string> AvailableDevices { get; } = new();

        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public ICommand OpenRecordingWindowCommand { get; }

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

        private string recordingStatus = "待機中";
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

        public static Timeline? TimelineInstance { get; private set; }
        public Timeline Timeline { get; set; } = null!;

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
            OpenRecordingWindowCommand = new RelayCommand(OpenRecordingWindow, CanOpenRecordingWindow);

            RecordsDirectory = recordPathService.GetRecordsDirectory();
            RefreshMicrophones();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void RefreshMicrophones()
        {
            LogService.Write("ToolView: RefreshMicrophones");
            AvailableDevices.Clear();

            foreach (var deviceName in recordingService.GetAvailableDeviceNames())
            {
                AvailableDevices.Add(deviceName);
            }

            if (AvailableDevices.Count > 0)
            {
                if (SelectedDevice is null || !AvailableDevices.Contains(SelectedDevice))
                    SelectedDevice = AvailableDevices[0];
                LogService.Write($"ToolView: Devices found. count={AvailableDevices.Count}, selected={SelectedDevice}");
            }
            else
            {
                SelectedDevice = null;
                RecordingStatus = "録音デバイスが見つかりません。";
                LogService.Write("ToolView: No devices found");
            }

            RaiseCommandStates();
        }

        private bool CanStartRecording() => !IsRecording && !string.IsNullOrWhiteSpace(SelectedDevice);

        private bool CanStopRecording() => IsRecording;

        private bool CanOpenRecordingWindow() => !IsRecording;

        private void StartRecording()
        {
            if (SelectedDevice is null)
            {
                RecordingStatus = "録音デバイスを選択してください。";
                LogService.Write("ToolView: StartRecording blocked. device not selected");
                return;
            }

            try
            {
                LogService.Write($"ToolView: StartRecording requested. device={SelectedDevice}");
                recordingService.StartRecording(SelectedDevice);
                RecordingStatus = $"録音中... 保存先: {RecordsDirectory}";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"録音開始に失敗しました: {ex.Message}";
                LogService.Write("ToolView: StartRecording failed", ex);
                RaiseCommandStates();
            }
        }

        private async void StopRecording()
        {
            try
            {
                LogService.Write("ToolView: StopRecording requested");
                var recordedFile = recordingService.StopRecording();

                CurrentVolume = 0;
                RaiseCommandStates();

                if (recordedFile is null)
                {
                    RecordingStatus = "録音データがありません。保存をスキップしました。";
                    LogService.Write("ToolView: StopRecording returned null");
                    return;
                }

                if (recordedFile.DataLength <= 0)
                {
                    RecordingStatus = $"録音データ長が 0 のため追加しませんでした: {recordedFile.FilePath}";
                    LogService.Write($"ToolView: StopRecording dataLength=0. file={recordedFile.FilePath}");
                    return;
                }

                RecordingStatus = $"録音停止。タイムラインへ追加中... {recordedFile.FilePath}";

                await timelineInsertService.InsertAsync(recordedFile);
                RecordingStatus = $"録音停止。タイムラインへ追加しました: {recordedFile.FilePath}";
                LogService.Write($"ToolView: Audio timeline insert completed. file={recordedFile.FilePath}");
            }
            catch (Exception ex)
            {
                RecordingStatus = $"録音停止に失敗しました: {ex.Message}";
                LogService.Write("ToolView: StopRecording failed", ex);
            }
        }

        private void OpenRecordingWindow()
        {
            var selectionService = new TimelineSelectionService();
            var serif = selectionService.TryGetSelectedSerif();
            LogService.Write($"ToolView: OpenRecordingWindow. selectedSerifLength={serif?.Length ?? 0}");
            var window = new RecordingWindow(null)
            {
                Owner = Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.ShowDialog();
            LogService.Write("ToolView: RecordingWindow closed");
        }

        private void OnRecordingDataAvailable(object? sender, Models.RecordingDataEventArgs e)
        {
            CurrentVolume = e.Volume;
        }

        private void OnRecordingStateChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsRecording));
            RaiseCommandStates();
        }

        public void SetTimelineToolInfo(TimelineToolInfo info)
        {
            TimelineInstance = info.Timeline;
            LogService.Write("ToolView: TimelineInstance set");
        }

        private void RaiseCommandStates()
        {
            if (StartRecordingCommand is RelayCommand start)
                start.RaiseCanExecuteChanged();

            if (StopRecordingCommand is RelayCommand stop)
                stop.RaiseCanExecuteChanged();

            if (OpenRecordingWindowCommand is RelayCommand open)
                open.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
