using System.ComponentModel;
using System.Threading.Tasks;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.ItemEditor;
using YukkuriMovieMaker.Plugin.Voice;
using YukkuriMovieMaker.UndoRedo;

namespace YMM_REC_Plugin.Voice
{
    public class RecordedVoicePronounce : IVoicePronounce, IEditable, INotifyPropertyChanged, IUndoRedoable
    {
        public LipSyncFrame[] LipSyncFrames { get; set; } = [];

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<UndoRedoEventArgs>? UndoRedoCommandCreated
        {
            add { }
            remove { }
        }

        public IVoicePronounce Clone()
        {
            return new RecordedVoicePronounce
            {
                LipSyncFrames = (LipSyncFrame[])LipSyncFrames.Clone()
            };
        }

        public void BeginEdit()
        {
        }

        public ValueTask EndEditAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
