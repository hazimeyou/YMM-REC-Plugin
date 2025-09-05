using YMM_REC_Plugin;
using YukkuriMovieMaker.Plugin;

namespace YMM_REC_Plugin
{
    public class MyToolPlugin : IToolPlugin
    {
        public string Name => "録音プラグイン";
        public Type ViewModelType => typeof(ToolViewModel);
        public Type ViewType => typeof(ToolView);
    }
}