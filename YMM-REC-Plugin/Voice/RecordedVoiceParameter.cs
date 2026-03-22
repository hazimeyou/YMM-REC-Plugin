using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YMM_REC_Plugin.Services;
using YukkuriMovieMaker.Plugin.Voice;

namespace YMM_REC_Plugin.Voice
{
    public class RecordedVoiceParameter : VoiceParameterBase, IVoiceParameter
    {
        private string text = string.Empty;
        private string audioFilePath = string.Empty;

        [Display(Name = "セリフ", Description = "読み上げテキスト")]
        public string Text
        {
            get => text;
            set
            {
                text = value ?? string.Empty;
                LogService.Write($"RecordedVoiceParameter: Text set. length={text.Length}");
            }
        }

        [Display(Name = "録音ファイル", Description = "録音済みwavファイルのパス")]
        [ReadOnly(true)]
        public string AudioFilePath
        {
            get => audioFilePath;
            set
            {
                audioFilePath = value ?? string.Empty;
                var exists = string.IsNullOrWhiteSpace(audioFilePath) ? "empty" : (System.IO.File.Exists(audioFilePath) ? "exists" : "missing");
                LogService.Write($"RecordedVoiceParameter: AudioFilePath set. value={audioFilePath}, status={exists}");
            }
        }

        public TimeSpan? Duration { get; set; }
        public DateTime? CreatedAt { get; set; }

        public IVoiceParameter Clone()
        {
            return new RecordedVoiceParameter
            {
                Text = Text,
                AudioFilePath = AudioFilePath,
                Duration = Duration,
                CreatedAt = CreatedAt
            };
        }
    }
}
