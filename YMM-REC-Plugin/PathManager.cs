using System;
using System.IO;
using System.Reflection;
using System.Windows;
using YukkuriMovieMaker;
using YukkuriMovieMaker.Commons;
namespace　YMM_REC_Plugin
{
    public static class PathManager
    {
        // アプリのルートディレクトリ
        public static string AppPath => AppDirectories.AppPath;
        public static string AppDirectory => AppDirectories.AppDirectory;
        public static string UserDirectory => AppDirectories.UserDirectory;
        public static string PluginDirectory => AppDirectories.PluginDirectory;
        public static string BackupDirectory => AppDirectories.BackupDirectory;
        public static string TemporaryDirectory => AppDirectories.TemporaryDirectory;
        public static string ResourceDirectory => AppDirectories.ResourceDirectory;

    }
}
