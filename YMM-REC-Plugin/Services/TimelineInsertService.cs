using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using YMM_REC_Plugin.Models;

namespace YMM_REC_Plugin.Services
{
    public class TimelineInsertService
    {
        public Task InsertAsync(RecordedFileInfo recordedFile)
        {
            if (recordedFile is null)
                throw new ArgumentNullException(nameof(recordedFile));

            if (!File.Exists(recordedFile.FilePath))
                throw new FileNotFoundException("保存済み wav ファイルが見つかりません。", recordedFile.FilePath);

            var dispatcher = Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("UI Dispatcher を取得できません。");

            return dispatcher.InvokeAsync(() =>
            {
                var mainViewModel = Application.Current?.MainWindow?.DataContext
                    ?? throw new InvalidOperationException("MainViewModel を取得できません。");

                var timeline = GetActiveTimeline(mainViewModel)
                    ?? throw new InvalidOperationException("タイムラインを取得できません。");

                var currentFrame = GetCurrentFrame(timeline);
                var audioItem = CreateAudioItem(recordedFile.FilePath, currentFrame);
                TryAddItem(timeline, audioItem, currentFrame);
            }).Task;
        }

        private static object? GetActiveTimeline(object mainViewModel)
        {
            var mainViewModelType = mainViewModel.GetType();

            var activeTimelineViewModel = mainViewModelType
                .GetProperty("ActiveTimelineViewModel", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(mainViewModel);

            if (activeTimelineViewModel is not null)
            {
                var timelineField = activeTimelineViewModel.GetType()
                    .GetField("timeline", BindingFlags.Instance | BindingFlags.NonPublic);

                if (timelineField?.GetValue(activeTimelineViewModel) is { } timeline)
                    return timeline;
            }

            var modelField = mainViewModelType.GetField("model", BindingFlags.Instance | BindingFlags.NonPublic);
            var model = modelField?.GetValue(mainViewModel);
            return model?.GetType()
                .GetProperty("Timeline", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(model);
        }

        private static int GetCurrentFrame(object timeline)
        {
            return (int)(timeline.GetType()
                .GetProperty("CurrentFrame", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(timeline)
                ?? throw new InvalidOperationException("現在フレームを取得できません。"));
        }

        private static object CreateAudioItem(string filePath, int frame)
        {
            var audioItemType = Type.GetType("YukkuriMovieMaker.Project.Items.AudioItem, YukkuriMovieMaker")
                ?? throw new InvalidOperationException("AudioItem 型を取得できません。");

            object audioItem;

            var stringCtor = audioItemType.GetConstructor(new[] { typeof(string) });
            if (stringCtor is not null)
            {
                audioItem = stringCtor.Invoke(new object[] { filePath });
            }
            else
            {
                audioItem = Activator.CreateInstance(audioItemType)
                    ?? throw new InvalidOperationException("AudioItem を生成できません。");
                audioItemType.GetProperty("FilePath")?.SetValue(audioItem, filePath);
            }

            audioItemType.GetProperty("Frame")?.SetValue(audioItem, frame);
            audioItemType.GetProperty("Layer")?.SetValue(audioItem, 0);
            return audioItem;
        }

        private static void TryAddItem(object timeline, object audioItem, int frame)
        {
            var timelineType = timeline.GetType();
            var itemInterfaceType = timelineType.Assembly.GetType("YukkuriMovieMaker.Project.Items.IItem")
                ?? throw new InvalidOperationException("IItem 型を取得できません。");

            var itemArray = Array.CreateInstance(itemInterfaceType, 1);
            itemArray.SetValue(audioItem, 0);

            var tryAddItems = timelineType.GetMethod(
                "TryAddItems",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { itemArray.GetType(), typeof(int), typeof(int), typeof(bool) },
                modifiers: null);

            if (tryAddItems is not null)
            {
                var added = (bool)tryAddItems.Invoke(timeline, new object[] { itemArray, frame, 0, false })!;
                if (!added)
                    throw new InvalidOperationException("タイムラインへの音声追加に失敗しました。");
                return;
            }

            var addItems = timelineType.GetMethod("AddItems", BindingFlags.Instance | BindingFlags.Public);
            if (addItems is null)
                throw new InvalidOperationException("タイムライン追加メソッドを取得できません。");

            addItems.Invoke(timeline, new object[] { itemArray });
        }
    }
}
