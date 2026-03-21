using System;

namespace YMM_REC_Plugin.Models
{
    public class RecordingDataEventArgs : EventArgs
    {
        public RecordingDataEventArgs(double volume)
        {
            Volume = volume;
        }

        public double Volume { get; }
    }
}
