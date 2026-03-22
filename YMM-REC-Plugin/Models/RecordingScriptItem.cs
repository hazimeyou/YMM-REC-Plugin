using System;

namespace YMM_REC_Plugin.Models
{
    public class RecordingScriptItem
    {
        public string Text { get; set; } = string.Empty;
        public string AudioFilePath { get; set; } = string.Empty;
        public bool IsRecorded { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
