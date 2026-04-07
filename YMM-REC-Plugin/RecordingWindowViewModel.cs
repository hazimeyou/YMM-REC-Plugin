using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;
using YMM_REC_Plugin.Models;
using YMM_REC_Plugin.Services;

namespace YMM_REC_Plugin
{
    public enum RecordingDialogState
    {
        Idle,
        Recording,
        Recorded
    }

    public class RecordingWindowViewModel : INotifyPropertyChanged
    {
        private readonly RecordPathService recordPathService;
        private readonly RecordingService recordingService;
        private readonly VoiceTimelineInsertService voiceTimelineInsertService;
        private readonly TimelineSelectionService timelineSelectionService;
        private readonly RecordingScriptItem scriptItem;

        private string scriptText = string.Empty;
        private string status = "待機中";
        private double currentVolume;
        private RecordingDialogState state = RecordingDialogState.Idle;
        private bool commandsReady;
        private WaveOutEvent? playbackOutput;
        private AudioFileReader? playbackReader;
        private bool isPlaying;

        public RecordingWindowViewModel(string? initialText)
        {
            recordPathService = new RecordPathService();
            recordingService = new RecordingService(recordPathService);
            voiceTimelineInsertService = new VoiceTimelineInsertService();
            timelineSelectionService = new TimelineSelectionService();
            scriptItem = new RecordingScriptItem();

            StartRecordingCommand = new RelayCommand(StartRecording, CanStartRecording);
            StopRecordingCommand = new RelayCommand(StopRecording, CanStopRecording);
            AddToTimelineCommand = new RelayCommand(AddToTimeline, CanAddToTimeline);
            RegenerateCommand = new RelayCommand(Regenerate, CanRegenerate);
            PlayCommand = new RelayCommand(Play, CanPlay);

            if (!string.IsNullOrWhiteSpace(initialText))
            {
                scriptText = initialText;
            }
            else
            {
                scriptText = timelineSelectionService.TryGetSelectedSerif() ?? string.Empty;
            }
            OnPropertyChanged(nameof(ScriptText));
            OnPropertyChanged(nameof(DisplayText));
            LogService.Write($"RecordingWindowVM: initialized. initialTextLength={initialText?.Length ?? 0}, selectedLength={ScriptText.Length}");

            recordingService.DataAvailable += OnRecordingDataAvailable;
            recordingService.RecordingStateChanged += OnRecordingStateChanged;

            var silentPath = recordPathService.GetOrCreateSilentWavPath(TimeSpan.FromSeconds(5));
            if (!string.IsNullOrWhiteSpace(silentPath))
            {
                scriptItem.AudioFilePath = silentPath;
                scriptItem.Duration = TimeSpan.FromSeconds(5);
                scriptItem.CreatedAt = DateTime.Now;
                scriptItem.IsRecorded = false;
            }

            commandsReady = true;
            RaiseCommandStates();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ScriptText
        {
            get => scriptText;
            set
            {
                scriptText = value;
                OnPropertyChanged(nameof(ScriptText));
                OnPropertyChanged(nameof(DisplayText));
                if (commandsReady)
                    RaiseCommandStates();
            }
        }

        public string DisplayText => string.IsNullOrWhiteSpace(ScriptText) ? "セリフが選択されていません。" : ScriptText;

        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public double CurrentVolume
        {
            get => currentVolume;
            set
            {
                currentVolume = value;
                OnPropertyChanged(nameof(CurrentVolume));
            }
        }

        public RecordingDialogState State
        {
            get => state;
            private set
            {
                state = value;
                OnPropertyChanged(nameof(State));
                RaiseCommandStates();
            }
        }

        public RelayCommand StartRecordingCommand { get; }
        public RelayCommand StopRecordingCommand { get; }
        public RelayCommand AddToTimelineCommand { get; }
        public RelayCommand RegenerateCommand { get; }
        public RelayCommand PlayCommand { get; }

        private bool CanStartRecording() => State != RecordingDialogState.Recording && !string.IsNullOrWhiteSpace(ScriptText);

        private bool CanStopRecording() => State == RecordingDialogState.Recording;

        private bool CanAddToTimeline() => State == RecordingDialogState.Recorded;

        private bool CanRegenerate() => State != RecordingDialogState.Recording && !string.IsNullOrWhiteSpace(ScriptText);

        private bool CanPlay()
        {
            if (State == RecordingDialogState.Recording || isPlaying)
                return false;

            return !string.IsNullOrWhiteSpace(scriptItem.AudioFilePath) && File.Exists(scriptItem.AudioFilePath);
        }

        private void StartRecording()
        {
            StopPlayback();

            if (string.IsNullOrWhiteSpace(ScriptText))
            {
                ScriptText = timelineSelectionService.TryGetSelectedSerif() ?? string.Empty;
            }

            var deviceName = recordingService.GetAvailableDeviceNames().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                Status = "録音デバイスが見つかりません。";
                LogService.Write("RecordingWindow: StartRecording blocked. device not found");
                return;
            }

            if (string.IsNullOrWhiteSpace(ScriptText))
            {
                Status = "セリフが選択されていません。タイムラインのセリフを選択してください。";
                LogService.Write("RecordingWindow: StartRecording blocked. no serif selected");
                return;
            }

            try
            {
                LogService.Write($"RecordingWindow: StartRecording requested. device={deviceName}, textLength={ScriptText.Length}");
                recordingService.StartRecording(deviceName);
                State = RecordingDialogState.Recording;
                Status = "録音中...";
            }
            catch (Exception ex)
            {
                Status = $"録音開始に失敗しました: {ex.Message}";
                LogService.Write("RecordingWindow: StartRecording failed", ex);
                State = RecordingDialogState.Idle;
            }
        }

        private async void StopRecording()
        {
            try
            {
                LogService.Write("RecordingWindow: StopRecording requested");
                var recordedFile = recordingService.StopRecording();
                CurrentVolume = 0;

                if (recordedFile is null || recordedFile.DataLength <= 0)
                {
                    Status = "録音データがありません。再度録音してください。";
                    LogService.Write("RecordingWindow: StopRecording returned null or empty");
                    State = RecordingDialogState.Idle;
                    return;
                }

                scriptItem.AudioFilePath = recordedFile.FilePath;
                scriptItem.Text = ScriptText;
                scriptItem.Duration = GetAudioDuration(recordedFile.FilePath);
                scriptItem.CreatedAt = DateTime.Now;
                scriptItem.IsRecorded = true;

                State = RecordingDialogState.Recorded;
                Status = $"録音完了: {recordedFile.FilePath}";
                LogService.Write($"RecordingWindow: StopRecording completed. file={recordedFile.FilePath}");

                LogService.Write($"RecordingWindow: AutoAddToTimeline start. textLength={scriptItem.Text.Length}, audio={scriptItem.AudioFilePath}");
                await voiceTimelineInsertService.InsertAsync(scriptItem);
                State = RecordingDialogState.Idle;
                if (timelineSelectionService.TryMoveToNextSerif(scriptItem.Text, out var nextSerif) && !string.IsNullOrWhiteSpace(nextSerif))
                {
                    ScriptText = nextSerif;
                    Status = "次のセリフを準備しました。録音開始できます。";
                    LogService.Write($"RecordingWindow: Next serif prepared. length={nextSerif.Length}");
                }
                else
                {
                    Status = "タイムラインへ追加しました。続けて録音できます。";
                }
                LogService.Write("RecordingWindow: AutoAddToTimeline completed");
            }
            catch (Exception ex)
            {
                Status = $"録音停止に失敗しました: {ex.Message}";
                LogService.Write("RecordingWindow: StopRecording failed", ex);
                State = RecordingDialogState.Idle;
            }
        }

        private void Regenerate()
        {
            StopPlayback();
            var silentPath = recordPathService.GetOrCreateSilentWavPath(TimeSpan.FromSeconds(5));
            if (!string.IsNullOrWhiteSpace(silentPath))
            {
                scriptItem.AudioFilePath = silentPath;
                scriptItem.Duration = TimeSpan.FromSeconds(5);
                scriptItem.CreatedAt = DateTime.Now;
                scriptItem.IsRecorded = false;
            }

            LogService.Write("RecordingWindow: Regenerate requested");
            StartRecording();
        }

        private void Play()
        {
            if (string.IsNullOrWhiteSpace(scriptItem.AudioFilePath) || !File.Exists(scriptItem.AudioFilePath))
            {
                Status = "再生できる音声がありません";
                LogService.Write("RecordingWindow: Play blocked. audio missing");
                return;
            }

            try
            {
                StopPlayback();
                playbackReader = new AudioFileReader(scriptItem.AudioFilePath);
                playbackOutput = new WaveOutEvent();
                playbackOutput.PlaybackStopped += OnPlaybackStopped;
                playbackOutput.Init(playbackReader);
                playbackOutput.Play();
                isPlaying = true;
                Status = "再生中...";
                RaiseCommandStates();
                LogService.Write($"RecordingWindow: Play requested. file={scriptItem.AudioFilePath}");
            }
            catch (Exception ex)
            {
                Status = $"再生に失敗: {ex.Message}";
                LogService.Write("RecordingWindow: Play failed", ex);
                StopPlayback();
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            void HandlePlaybackStopped()
            {
                StopPlayback(skipStopCall: true);

                if (e.Exception is not null)
                {
                    Status = $"再生エラー: {e.Exception.Message}";
                    LogService.Write("RecordingWindow: PlaybackStopped error", e.Exception);
                    return;
                }

                Status = "再生完了";
                LogService.Write("RecordingWindow: PlaybackStopped completed");
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                HandlePlaybackStopped();
            }
            else
            {
                _ = dispatcher.BeginInvoke((Action)HandlePlaybackStopped);
            }
        }

        private void StopPlayback(bool skipStopCall = false)
        {
            try
            {
                if (playbackOutput is not null)
                {
                    playbackOutput.PlaybackStopped -= OnPlaybackStopped;
                    if (!skipStopCall && playbackOutput.PlaybackState == PlaybackState.Playing)
                        playbackOutput.Stop();
                    playbackOutput.Dispose();
                    playbackOutput = null;
                }

                if (playbackReader is not null)
                {
                    playbackReader.Dispose();
                    playbackReader = null;
                }
            }
            finally
            {
                if (isPlaying)
                {
                    isPlaying = false;
                    RaiseCommandStates();
                }
            }
        }

        private async void AddToTimeline()
        {
            try
            {
                LogService.Write($"RecordingWindow: AddToTimeline requested. textLength={scriptItem.Text?.Length ?? 0}, audio={scriptItem.AudioFilePath}");
                await voiceTimelineInsertService.InsertAsync(scriptItem);
                Status = "タイムラインへ追加しました。";
                LogService.Write("RecordingWindow: AddToTimeline completed");
            }
            catch (Exception ex)
            {
                Status = $"タイムライン追加に失敗しました: {ex.Message}";
                LogService.Write("RecordingWindow: AddToTimeline failed", ex);
            }
        }

        private static TimeSpan GetAudioDuration(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new WaveFileReader(stream);
            return reader.TotalTime;
        }

        private void OnRecordingDataAvailable(object? sender, Models.RecordingDataEventArgs e)
        {
            CurrentVolume = e.Volume;
        }

        private void OnRecordingStateChanged(object? sender, EventArgs e)
        {
            RaiseCommandStates();
        }

        private void RaiseCommandStates()
        {
            if (!commandsReady)
                return;
            StartRecordingCommand?.RaiseCanExecuteChanged();
            StopRecordingCommand?.RaiseCanExecuteChanged();
            AddToTimelineCommand?.RaiseCanExecuteChanged();
            RegenerateCommand?.RaiseCanExecuteChanged();
            PlayCommand?.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

