using System.Collections.Generic;
using System.Threading.Tasks;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Update;
using YukkuriMovieMaker.Plugin.Voice;

namespace YMM_REC_Plugin.Voice
{
    public class RecordedVoicePlugin : IVoicePlugin
    {
        static RecordedVoicePlugin()
        {
            Services.LogService.Write("RecordedVoicePlugin: static constructor");
        }

        public string Name => "録音プラグイン";
        public PluginDetailsAttribute Details => new PluginDetailsAttribute
        {
            AuthorName = "YMM-REC-Plugin",
            ContentId = "YMM-REC-RecordedVoice"
        };
        public IPluginUpdater? Updater => null;

        public IEnumerable<IVoiceSpeaker> Voices => new[] { RecordedVoiceSpeaker.Instance };
        public bool CanUpdateVoices => false;
        public bool IsVoicesCached => true;

        public Task UpdateVoicesAsync() => Task.CompletedTask;
    }
}
