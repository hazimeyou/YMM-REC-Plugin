using System;
using System.IO;
using System.Threading.Tasks;
using YMM_REC_Plugin.Services;
using YukkuriMovieMaker.Plugin.Voice;

namespace YMM_REC_Plugin.Voice
{
    public class RecordedVoiceSpeaker : IVoiceSpeaker
    {
        public const string ApiName = "YMM-REC-RecordedVoice";
        public const string SpeakerId = "RecordedVoiceSpeaker";

        public static RecordedVoiceSpeaker Instance { get; } = new RecordedVoiceSpeaker();
        public static VoiceDescription Description { get; } = new VoiceDescription(Instance);

        static RecordedVoiceSpeaker()
        {
            LogService.Write("RecordedVoiceSpeaker: static constructor");
        }

        public string EngineName => "録音プラグイン";
        public string SpeakerName => "録音プラグイン";
        public string API => ApiName;
        public string ID => SpeakerId;
        public bool IsVoiceDataCachingRequired => false;
        public SupportedTextFormat Format => SupportedTextFormat.Text;
        public IVoiceLicense License => new NoVoiceLicense();
        public IVoiceResource Resource => new NoVoiceResource();
        public string SpeakerAuthor => string.Empty;
        public string SpeakerContentId => string.Empty;
        public string EngineAuthor => string.Empty;
        public string EngineContentId => string.Empty;

        public Task<string> ConvertKanjiToYomiAsync(string text, IVoiceParameter parameter)
        {
            return Task.FromResult(text);
        }

        public Task<IVoicePronounce?> CreateVoiceAsync(string text, IVoicePronounce? pronounce, IVoiceParameter? parameter, string outputFilePath)
        {
            LogService.Write($"RecordedVoiceSpeaker: CreateVoiceAsync start. output={outputFilePath}, textLength={text?.Length ?? 0}");

            try
            {
                if (parameter is not RecordedVoiceParameter recorded)
                    throw new InvalidOperationException("録音パラメータが取得できません。");

                LogService.Write($"RecordedVoiceSpeaker: parameter type={recorded.GetType().FullName}, AudioFilePath={recorded.AudioFilePath}");

                if (string.IsNullOrWhiteSpace(recorded.AudioFilePath))
                    throw new InvalidOperationException("録音済み wav のパスが空です。");

                if (!File.Exists(recorded.AudioFilePath))
                    throw new FileNotFoundException("録音済み wav が見つかりません。", recorded.AudioFilePath);

                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath) ?? ".");
                File.Copy(recorded.AudioFilePath, outputFilePath, overwrite: true);

                LogService.Write($"RecordedVoiceSpeaker: CreateVoiceAsync completed. source={recorded.AudioFilePath}");

                var result = pronounce ?? new RecordedVoicePronounce();
                return Task.FromResult<IVoicePronounce?>(result);
            }
            catch (Exception ex)
            {
                LogService.Write("RecordedVoiceSpeaker: CreateVoiceAsync failed", ex);
                throw;
            }
        }

        public IVoiceParameter CreateVoiceParameter()
        {
            LogService.Write("RecordedVoiceSpeaker: CreateVoiceParameter");
            var parameter = new RecordedVoiceParameter();
            var silentPath = new RecordPathService().GetOrCreateSilentWavPath(TimeSpan.FromSeconds(5));
            if (!string.IsNullOrWhiteSpace(silentPath))
            {
                parameter.AudioFilePath = silentPath;
                parameter.Duration = TimeSpan.FromSeconds(5);
                parameter.CreatedAt = DateTime.Now;
            }
            return parameter;
        }

        public bool IsMatch(string api, string id)
        {
            return string.Equals(api, API, StringComparison.Ordinal) && string.Equals(id, ID, StringComparison.Ordinal);
        }

        public IVoiceParameter MigrateParameter(IVoiceParameter parameter)
        {
            LogService.Write($"RecordedVoiceSpeaker: MigrateParameter. type={parameter?.GetType().FullName}");
            if (parameter is RecordedVoiceParameter recorded)
            {
                LogService.Write($"RecordedVoiceSpeaker: MigrateParameter recorded. audio={recorded.AudioFilePath}");
                if (string.IsNullOrWhiteSpace(recorded.AudioFilePath) || !File.Exists(recorded.AudioFilePath))
                {
                    var silentPath = new RecordPathService().GetOrCreateSilentWavPath(TimeSpan.FromSeconds(5));
                    if (!string.IsNullOrWhiteSpace(silentPath))
                    {
                        recorded.AudioFilePath = silentPath;
                        recorded.Duration ??= TimeSpan.FromSeconds(5);
                        recorded.CreatedAt ??= DateTime.Now;
                        LogService.Write($"RecordedVoiceSpeaker: MigrateParameter linked silent wav. audio={recorded.AudioFilePath}");
                    }
                }
                return recorded;
            }

            return CreateVoiceParameter();
        }
    }
}
