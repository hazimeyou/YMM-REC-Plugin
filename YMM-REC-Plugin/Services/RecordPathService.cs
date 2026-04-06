using System;
using System.IO;
using System.Reflection;
using NAudio.Wave;

namespace YMM_REC_Plugin.Services
{
    public class RecordPathService
    {
        private const int SampleRate = 48000;
        private const int BitDepth = 16;
        private const int Channels = 1;

        public string GetRecordsDirectory()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("プラグインフォルダーを取得できません。");

            var recordsDirectory = Path.Combine(assemblyDirectory, "Records");
            Directory.CreateDirectory(recordsDirectory);
            LogService.Write($"RecordPathService: RecordsDirectory={recordsDirectory}");
            return recordsDirectory;
        }

        public string CreateRecordFilePath()
        {
            var recordsDirectory = GetRecordsDirectory();
            var fileName = $"Record_{DateTime.Now:yyyyMMdd_HHmmss}";
            var filePath = Path.Combine(recordsDirectory, $"{fileName}.wav");
            var sequence = 1;

            while (File.Exists(filePath))
            {
                filePath = Path.Combine(recordsDirectory, $"{fileName}_{sequence:000}.wav");
                sequence++;
            }

            LogService.Write($"RecordPathService: CreateRecordFilePath={filePath}");
            return filePath;
        }

        public string GetOrCreateSilentWavPath(TimeSpan duration)
        {
            var recordsDirectory = GetRecordsDirectory();
            var seconds = Math.Max(1, (int)Math.Round(duration.TotalSeconds, MidpointRounding.AwayFromZero));
            var filePath = Path.Combine(recordsDirectory, $"Silent_{seconds}s.wav");
            if (File.Exists(filePath))
                return filePath;

            try
            {
                LogService.Write($"RecordPathService: CreateSilentWav file={filePath}, seconds={seconds}");
                var format = new WaveFormat(SampleRate, BitDepth, Channels);
                var bytesPerSecond = format.AverageBytesPerSecond;
                var totalBytes = (long)seconds * bytesPerSecond;
                var buffer = new byte[bytesPerSecond];

                using var writer = new WaveFileWriter(filePath, format);
                var remaining = totalBytes;
                while (remaining > 0)
                {
                    var toWrite = (int)Math.Min(buffer.Length, remaining);
                    writer.Write(buffer, 0, toWrite);
                    remaining -= toWrite;
                }
                writer.Flush();
            }
            catch (Exception ex)
            {
                LogService.Write("RecordPathService: CreateSilentWav failed", ex);
            }

            return filePath;
        }
    }
}
