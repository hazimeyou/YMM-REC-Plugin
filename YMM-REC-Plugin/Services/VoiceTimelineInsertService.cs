using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;
using YMM_REC_Plugin.Models;
using YMM_REC_Plugin.Voice;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace YMM_REC_Plugin.Services
{
    public class VoiceTimelineInsertService
    {
        public Task InsertAsync(RecordingScriptItem item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(item.AudioFilePath) || !File.Exists(item.AudioFilePath))
                throw new FileNotFoundException("録音済み wav が見つかりません。", item.AudioFilePath);

            LogService.Write($"VoiceTimelineInsert: start. file={item.AudioFilePath}, textLength={item.Text?.Length ?? 0}");

            var dispatcher = Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("UI Dispatcher を取得できません。");

            return dispatcher.InvokeAsync(() =>
            {
                int? selectedFrame = null;
                int? selectedLayer = null;
                var selectionService = new TimelineSelectionService();
                if (selectionService.TryGetSelectedPlacement(out var frame, out var placementLayer))
                {
                    selectedFrame = frame;
                    selectedLayer = placementLayer;
                }

                if (TryAttachToSelectedVoiceItem(item))
                {
                    LogService.Write("VoiceTimelineInsert: attached to selected VoiceItem");
                    return;
                }

                var timeline = ToolViewModel.TimelineInstance;
                if (timeline is not null)
                {
                    InsertWithTimeline(timeline, item, selectedFrame, selectedLayer);
                    LogService.Write("VoiceTimelineInsert: completed via direct timeline");
                    return;
                }

                LogService.Write("VoiceTimelineInsert: TimelineInstance null. fallback reflection path");

                var mainViewModel = Application.Current?.MainWindow?.DataContext
                    ?? throw new InvalidOperationException("MainViewModel を取得できません。");

                var fallbackTimeline = GetActiveTimeline(mainViewModel)
                    ?? throw new InvalidOperationException("タイムラインを取得できません。");

                var currentFrame = selectedFrame ?? GetCurrentFrame(fallbackTimeline);
                var length = GetLengthFrames(fallbackTimeline, item.AudioFilePath);
                var targetLayer = selectedLayer ?? 0;
                var voiceItem = CreateVoiceItemViaReflection(item, currentFrame, length, targetLayer);
                TryAddItem(fallbackTimeline, voiceItem, currentFrame, length, targetLayer);
                LogService.Write("VoiceTimelineInsert: completed via reflection");
            }).Task;
        }

        private static bool TryAttachToSelectedVoiceItem(RecordingScriptItem item)
        {
            try
            {
                var selectionService = new TimelineSelectionService();
                var selectedItems = selectionService.GetSelectedItemsSnapshot();
                foreach (var selected in selectedItems)
                {
                    LogService.Write($"VoiceTimelineInsert: evaluating selected item type={selected.GetType().FullName}");

                    if (!selected.GetType().Name.Contains("Voice", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var serifProp = selected.GetType().GetProperty("Serif", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var currentSerif = serifProp?.GetValue(selected) as string;
                    if (string.IsNullOrWhiteSpace(item.Text) && !string.IsNullOrWhiteSpace(currentSerif))
                    {
                        item.Text = currentSerif;
                        LogService.Write($"VoiceTimelineInsert: attach uses selected serif. length={item.Text.Length}");
                    }

                    var voiceParameterProp = selected.GetType().GetProperty("VoiceParameter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    object? targetForParameter = selected;

                    if (voiceParameterProp is null)
                    {
                        var characterProp = selected.GetType().GetProperty("Character", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        var character = characterProp?.GetValue(selected);
                        if (character is not null)
                        {
                            voiceParameterProp = character.GetType().GetProperty("VoiceParameter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            targetForParameter = character;
                            LogService.Write("VoiceTimelineInsert: using Character.VoiceParameter");
                            LogService.Write($"VoiceTimelineInsert: Character type={character.GetType().FullName}");
                        }
                    }

                    FieldInfo? voiceParameterField = null;
                    if (voiceParameterProp is null)
                    {
                        voiceParameterField = selected.GetType().GetField("voiceParameter", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (voiceParameterField is not null)
                        {
                            targetForParameter = selected;
                            LogService.Write("VoiceTimelineInsert: using field voiceParameter");
                        }
                    }

                    if ((voiceParameterProp is null && voiceParameterField is null) || targetForParameter is null)
                    {
                        LogService.Write("VoiceTimelineInsert: VoiceParameter property not found on selected item");
                        continue;
                    }

                    var existing = voiceParameterProp is not null
                        ? voiceParameterProp.GetValue(targetForParameter)
                        : voiceParameterField?.GetValue(targetForParameter);
                    LogService.Write($"VoiceTimelineInsert: VoiceParameter before type={existing?.GetType().FullName}, value={Summarize(existing)}");
                    var recorded = existing as RecordedVoiceParameter ?? new RecordedVoiceParameter();
                    recorded.Text = item.Text;
                    recorded.AudioFilePath = item.AudioFilePath;
                    recorded.Duration = item.Duration;
                    recorded.CreatedAt = item.CreatedAt;

                    if (voiceParameterProp is not null)
                    {
                        if (voiceParameterProp.CanWrite)
                        {
                            voiceParameterProp.SetValue(targetForParameter, recorded);
                        }
                        else
                        {
                            LogService.Write("VoiceTimelineInsert: VoiceParameter property not writable");
                        }
                    }

                    if (voiceParameterField is not null)
                    {
                        voiceParameterField.SetValue(targetForParameter, recorded);
                    }
                    LogService.Write($"VoiceTimelineInsert: VoiceParameter after type={recorded.GetType().FullName}, audio={recorded.AudioFilePath}");

                    if (serifProp is not null && serifProp.CanWrite && !string.IsNullOrWhiteSpace(item.Text))
                        serifProp.SetValue(selected, item.Text);

                    var isVoiceChangedProp = selected.GetType().GetProperty("IsVoiceChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (isVoiceChangedProp?.CanWrite == true)
                        isVoiceChangedProp.SetValue(selected, true);

                    LogService.Write($"VoiceTimelineInsert: attached parameter. audio={recorded.AudioFilePath}, textLength={recorded.Text.Length}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogService.Write("VoiceTimelineInsert: attach failed", ex);
            }

            return false;
        }

        private static string Summarize(object? value)
        {
            if (value is null)
                return "null";
            if (value is string s)
                return $"\"{s}\" (len={s.Length})";
            return value.ToString() ?? value.GetType().FullName ?? "<unknown>";
        }

        private static void InsertWithTimeline(Timeline timeline, RecordingScriptItem item, int? selectedFrame, int? selectedLayer)
        {
            var frame = selectedFrame ?? timeline.CurrentFrame;
            var layer = selectedLayer ?? 0;
            var length = GetLengthFrames(timeline, item.AudioFilePath);
            var voiceItem = CreateVoiceItem(item, frame, length, layer);

            var added = timeline.TryAddItems(new IItem[] { voiceItem }, voiceItem.Frame, voiceItem.Layer);
            if (!added)
                throw new InvalidOperationException("タイムラインへの追加に失敗しました。");

            LogService.Write($"VoiceTimelineInsert: TryAddItems success. frame={voiceItem.Frame}, layer={voiceItem.Layer}, length={voiceItem.Length}");
        }

        private static VoiceItem CreateVoiceItem(RecordingScriptItem item, int frame, int length, int layer)
        {
            var parameter = new RecordedVoiceParameter
            {
                Text = item.Text,
                AudioFilePath = item.AudioFilePath,
                Duration = item.Duration,
                CreatedAt = item.CreatedAt
            };
            LogService.Write($"VoiceTimelineInsert: CreateVoiceItem. textLength={item.Text?.Length ?? 0}, audio={item.AudioFilePath}");

            var character = new Character
            {
                Voice = RecordedVoiceSpeaker.Description,
                VoiceParameter = parameter.Clone()
            };

            var voiceItem = new VoiceItem(character)
            {
                Serif = item.Text,
                VoiceParameter = parameter,
                Frame = frame,
                Layer = layer,
                Length = length
            };

            return voiceItem;
        }

        private static TimeSpan GetAudioDuration(string filePath)
        {
            using var reader = new WaveFileReader(filePath);
            return reader.TotalTime;
        }

        private static int GetLengthFrames(object timeline, string filePath)
        {
            var fps = GetTimelineFps(timeline, fallbackFps: 60.0);
            var durationSeconds = GetAudioDuration(filePath).TotalSeconds;
            var frames = (int)Math.Round(durationSeconds * fps, MidpointRounding.AwayFromZero);
            return Math.Max(1, frames);
        }

        private static double GetTimelineFps(object timeline, double fallbackFps)
        {
            try
            {
                var videoInfoProperty = timeline.GetType().GetProperty("VideoInfo", BindingFlags.Public | BindingFlags.Instance);
                var videoInfo = videoInfoProperty?.GetValue(timeline);
                if (videoInfo is not null)
                {
                    var fpsFromVideoInfo = GetPropertyDouble(videoInfo, "FPS");
                    if (fpsFromVideoInfo > 0)
                        return fpsFromVideoInfo;
                }

                var fps = GetPropertyDouble(timeline, "FPS");
                if (fps > 0)
                    return fps;

                fps = GetPropertyDouble(timeline, "FrameRate");
                if (fps > 0)
                    return fps;
            }
            catch
            {
            }

            return fallbackFps;
        }

        private static double GetPropertyDouble(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
                return 0;

            var value = property.GetValue(instance);
            return value switch
            {
                double d => d,
                float f => f,
                decimal m => (double)m,
                int i => i,
                long l => l,
                string s when double.TryParse(s, out var parsed) => parsed,
                _ => 0
            };
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

        private static object CreateVoiceItemViaReflection(RecordingScriptItem item, int frame, int length, int layer)
        {
            var voiceItemType = Type.GetType("YukkuriMovieMaker.Project.Items.VoiceItem, YukkuriMovieMaker")
                ?? throw new InvalidOperationException("VoiceItem を取得できません。");

            var parameter = new RecordedVoiceParameter
            {
                Text = item.Text,
                AudioFilePath = item.AudioFilePath,
                Duration = item.Duration,
                CreatedAt = item.CreatedAt
            };
            LogService.Write($"VoiceTimelineInsert: CreateVoiceItemViaReflection. textLength={item.Text?.Length ?? 0}, audio={item.AudioFilePath}");

            var characterType = Type.GetType("YukkuriMovieMaker.Project.Character, YukkuriMovieMaker")
                ?? throw new InvalidOperationException("Character を取得できません。");
            var character = Activator.CreateInstance(characterType)
                ?? throw new InvalidOperationException("Character を作成できません。");

            var voiceDescriptionType = Type.GetType("YukkuriMovieMaker.Plugin.Voice.VoiceDescription, YukkuriMovieMaker.Plugin")
                ?? throw new InvalidOperationException("VoiceDescription を取得できません。");
            var speaker = RecordedVoiceSpeaker.Instance;
            var voiceDescription = Activator.CreateInstance(voiceDescriptionType, speaker)
                ?? throw new InvalidOperationException("VoiceDescription を作成できません。");
            var apiProp = voiceDescriptionType.GetProperty("API");
            if (apiProp?.CanWrite == true)
                apiProp.SetValue(voiceDescription, RecordedVoiceSpeaker.ApiName);
            var argProp = voiceDescriptionType.GetProperty("Arg");
            if (argProp?.CanWrite == true)
                argProp.SetValue(voiceDescription, RecordedVoiceSpeaker.SpeakerId);

            characterType.GetProperty("Voice")?.SetValue(character, voiceDescription);
            characterType.GetProperty("VoiceParameter")?.SetValue(character, parameter.Clone());

            var voiceItem = Activator.CreateInstance(voiceItemType, character)
                ?? throw new InvalidOperationException("VoiceItem を作成できません。");

            voiceItemType.GetProperty("Serif")?.SetValue(voiceItem, item.Text);
            voiceItemType.GetProperty("VoiceParameter")?.SetValue(voiceItem, parameter);
            voiceItemType.GetProperty("Frame")?.SetValue(voiceItem, frame);
            voiceItemType.GetProperty("Layer")?.SetValue(voiceItem, layer);
            voiceItemType.GetProperty("Length")?.SetValue(voiceItem, length);
            var voiceLengthProp = voiceItemType.GetProperty("VoiceLength");
            if (voiceLengthProp?.CanWrite == true)
                voiceLengthProp.SetValue(voiceItem, item.Duration ?? GetAudioDuration(item.AudioFilePath));

            return voiceItem;
        }

        private static void TryAddItem(object timeline, object voiceItem, int frame, int length, int layer)
        {
            var timelineType = timeline.GetType();
            var itemInterfaceType = timelineType.Assembly.GetType("YukkuriMovieMaker.Project.Items.IItem")
                ?? throw new InvalidOperationException("IItem を取得できません。");

            var itemArray = Array.CreateInstance(itemInterfaceType, 1);
            itemArray.SetValue(voiceItem, 0);

            var tryAddItems = timelineType.GetMethod(
                "TryAddItems",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { itemArray.GetType(), typeof(int), typeof(int) },
                modifiers: null);

            if (tryAddItems is not null)
            {
                var added = (bool)tryAddItems.Invoke(timeline, new object[] { itemArray, frame, layer })!;
                if (!added)
                    throw new InvalidOperationException("タイムラインへの追加に失敗しました。");
                return;
            }

            var addItems = timelineType.GetMethod("AddItems", BindingFlags.Instance | BindingFlags.Public);
            if (addItems is null)
                throw new InvalidOperationException("タイムライン追加メソッドを取得できません。");

            addItems.Invoke(timeline, new object[] { itemArray });
        }
    }
}
