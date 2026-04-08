using System;
using System.ComponentModel;
using System.Threading.Tasks;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin.Voice;

namespace YMM_REC_Plugin.Voice
{
    public class NoVoiceResource : IVoiceResource
    {
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public string Name => "録音プラグイン";
        public string Terms => string.Empty;
        public bool IsDownloaded => true;
        public string FileSize => string.Empty;

        public event EventHandler? DownloadStarted
        {
            add { }
            remove { }
        }

        public Task DownloadAsync(ProgressMessage progress) => Task.CompletedTask;

        public Task<bool> HasUpdateAsync() => Task.FromResult(false);
    }
}
