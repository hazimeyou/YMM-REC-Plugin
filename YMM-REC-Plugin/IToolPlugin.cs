using System;
using System.Threading.Tasks;
using YMM_REC_Plugin.Services;
using YukkuriMovieMaker.Plugin;

namespace YMM_REC_Plugin
{
    public class MyToolPlugin : IToolPlugin
    {
        static MyToolPlugin()
        {
            LogService.Write("MyToolPlugin: static constructor");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    LogService.Write("UnhandledException", ex);
                else
                    LogService.Write("UnhandledException: non-Exception object");
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                LogService.Write("UnobservedTaskException", e.Exception);
            };
        }

        public string Name => "録音プラグイン";
        public Type ViewModelType => typeof(ToolViewModel);
        public Type ViewType => typeof(ToolView);
    }
}
