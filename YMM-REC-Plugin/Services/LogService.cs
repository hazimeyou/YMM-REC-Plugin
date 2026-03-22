using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace YMM_REC_Plugin.Services
{
    public static class LogService
    {
        private static readonly object SyncRoot = new();
        private static readonly string LogDirectory = GetLogDirectory();
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "YMM-REC-Plugin.log");
        private static bool initialized;

        static LogService()
        {
            InitializeLogFile();
        }

        public static void Write(string message)
        {
            WriteInternal(message, null);
        }

        public static void Write(string message, Exception exception)
        {
            WriteInternal(message, exception);
        }

        private static void WriteInternal(string message, Exception? exception)
        {
            try
            {
                EnsureInitialized();
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var builder = new StringBuilder();
                builder.Append('[').Append(timestamp).Append("] ").Append(message);
                if (exception is not null)
                {
                    builder.Append(" | ").Append(exception.GetType().Name).Append(": ").Append(exception.Message);
                }
                builder.AppendLine();

                lock (SyncRoot)
                {
                    Directory.CreateDirectory(LogDirectory);
                    File.AppendAllText(LogFilePath, builder.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            lock (SyncRoot)
            {
                if (initialized)
                    return;

                InitializeLogFile();
            }
        }

        private static void InitializeLogFile()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                File.WriteAllText(LogFilePath, string.Empty, Encoding.UTF8);
                initialized = true;
            }
            catch
            {
            }
        }

        private static string GetLogDirectory()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(assemblyDirectory, "Logs");
        }
    }
}
