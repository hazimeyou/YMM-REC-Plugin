using System;

namespace YMM_REC_Plugin.Models
{
    public class RecordedFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public DateTime CreatedAt { get; set; }
        public long DataLength { get; set; }
    }
}
