using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace YMM_REC_Plugin.Services
{
    public class TimelineSelectionService
    {
        public string? TryGetSelectedSerif()
        {
            var items = GetSelectedItemsSnapshot();
            foreach (var item in items)
            {
                var serif = GetStringProperty(item, "Serif")
                            ?? GetStringProperty(item, "Text");
                if (!string.IsNullOrWhiteSpace(serif))
                {
                    LogService.Debug($"TimelineSelection: found serif. type={item.GetType().FullName}, length={serif.Length}");
                    return serif;
                }
            }

            LogService.Debug("TimelineSelection: no serif found in selected items");
            return null;
        }

        public bool TryGetSelectedPlacement(out int frame, out int layer)
        {
            frame = 0;
            layer = 0;

            var items = GetSelectedItemsSnapshot();
            foreach (var item in items)
            {
                if (TryGetIntProperty(item, "Frame", out var foundFrame))
                {
                    frame = foundFrame;
                    if (!TryGetIntProperty(item, "Layer", out layer))
                        layer = 0;

                    LogService.Debug($"TimelineSelection: selected placement frame={frame}, layer={layer}");
                    return true;
                }
            }

            LogService.Debug("TimelineSelection: no placement found in selected items");
            return false;
        }

        public IReadOnlyList<object> GetSelectedItemsSnapshot()
        {
            var result = new List<object>();

            try
            {
                var timeline = ToolViewModel.TimelineInstance;
                if (timeline is not null)
                {
                    LogService.Debug($"TimelineSelection: collecting from TimelineInstance type={timeline.GetType().FullName}");
                    CollectFromObject(timeline, result);
                }
                else
                {
                    LogService.Debug("TimelineSelection: TimelineInstance null. fallback to MainViewModel");
                    var mainViewModel = Application.Current?.MainWindow?.DataContext;
                    if (mainViewModel is not null)
                    {
                        LogService.Debug($"TimelineSelection: collecting from MainViewModel type={mainViewModel.GetType().FullName}");
                        CollectFromObject(mainViewModel, result);
                        var activeTimelineViewModel = mainViewModel.GetType()
                            .GetProperty("ActiveTimelineViewModel", BindingFlags.Instance | BindingFlags.Public)
                            ?.GetValue(mainViewModel);
                        if (activeTimelineViewModel is not null)
                        {
                            LogService.Debug($"TimelineSelection: collecting from ActiveTimelineViewModel type={activeTimelineViewModel.GetType().FullName}");
                            CollectFromObject(activeTimelineViewModel, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Write("TimelineSelection: exception while collecting selected items", ex);
            }

            if (LogService.IsDebugEnabled)
            {
                foreach (var item in result)
                {
                    DumpObjectShape("TimelineSelection: selected item", item);
                }
                LogService.Debug($"TimelineSelection: selected item count={result.Count}");
            }
            return result;
        }

        private static void CollectFromObject(object source, List<object> result)
        {
            foreach (var name in new[]
                     {
                         "SelectedItems",
                         "SelectedItem",
                         "SelectedTimelineItems",
                         "SelectedItemViewModels",
                         "SelectedElements",
                         "SelectedClips"
                     })
            {
                var property = source.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property is null)
                    continue;

                var value = property.GetValue(source);
                if (value is null)
                    continue;

                LogService.Debug($"TimelineSelection: found selection property {name} on {source.GetType().FullName}");

                if (value is IEnumerable enumerable && value is not string)
                {
                    foreach (var item in enumerable)
                    {
                        if (item is null)
                            continue;

                        result.Add(UnwrapItem(item));
                    }
                }
                else
                {
                    result.Add(UnwrapItem(value));
                }
            }
        }

        private static object UnwrapItem(object item)
        {
            var itemProperty = item.GetType().GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemProperty?.GetValue(item) is object inner)
                return inner;

            var modelProperty = item.GetType().GetProperty("Model", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (modelProperty?.GetValue(item) is object model)
                return model;

            return item;
        }

        private static string? GetStringProperty(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetValue(instance) is string value)
                return value;

            return null;
        }

        private static bool TryGetIntProperty(object instance, string propertyName, out int value)
        {
            value = 0;
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null)
            {
                var raw = property.GetValue(instance);
                if (TryConvertToInt(raw, out value))
                    return true;
            }

            var field = instance.GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
            {
                var raw = field.GetValue(instance);
                if (TryConvertToInt(raw, out value))
                    return true;
            }

            return false;
        }

        private static bool TryConvertToInt(object? raw, out int value)
        {
            value = 0;
            if (raw is null)
                return false;

            switch (raw)
            {
                case int i:
                    value = i;
                    return true;
                case long l:
                    value = (int)l;
                    return true;
                case short s:
                    value = s;
                    return true;
                case byte b:
                    value = b;
                    return true;
                case string str when int.TryParse(str, out var parsed):
                    value = parsed;
                    return true;
                default:
                    return false;
            }
        }

        private static void DumpObjectShape(string prefix, object instance)
        {
            var type = instance.GetType();
            LogService.Debug($"{prefix}: type={type.FullName}");

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                var valueSummary = TryFormatValue(prop, instance);
                LogService.Debug($"{prefix}: prop {prop.Name} ({prop.PropertyType.FullName}) = {valueSummary}");
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var valueSummary = TryFormatValue(field, instance);
                LogService.Debug($"{prefix}: field {field.Name} ({field.FieldType.FullName}) = {valueSummary}");
            }
        }

        private static string TryFormatValue(PropertyInfo prop, object instance)
        {
            try
            {
                var value = prop.GetValue(instance);
                return SummarizeValue(value);
            }
            catch (Exception ex)
            {
                return $"<error {ex.GetType().Name}: {ex.Message}>";
            }
        }

        private static string TryFormatValue(FieldInfo field, object instance)
        {
            try
            {
                var value = field.GetValue(instance);
                return SummarizeValue(value);
            }
            catch (Exception ex)
            {
                return $"<error {ex.GetType().Name}: {ex.Message}>";
            }
        }

        private static string SummarizeValue(object? value)
        {
            if (value is null)
                return "null";

            if (value is string s)
                return $"\"{s}\" (len={s.Length})";

            if (value is System.Collections.IEnumerable && value is not string)
                return $"[{value.GetType().FullName}]";

            return value.ToString() ?? value.GetType().FullName ?? "<unknown>";
        }
    }
}
