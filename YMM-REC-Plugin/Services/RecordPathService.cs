using System;
using System.IO;
using System.Reflection;

namespace YMM_REC_Plugin.Services
{
    public class RecordPathService
    {
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
    }
}
