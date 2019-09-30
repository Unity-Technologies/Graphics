using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public class SerializedScalableSettingValue
    {
        public SerializedProperty level;
        public SerializedProperty useOverride;
        public SerializedProperty @override;

        public SerializedScalableSettingValue(SerializedProperty property)
        {
            level = property.FindPropertyRelative("m_Level");
            useOverride = property.FindPropertyRelative("m_UseOverride");
            @override = property.FindPropertyRelative("m_Override");
        }
    }

    public static class SerializedScalableSettingValueUI
    {
        public interface IValueGetter<T>
        {
            string sourceDescription { get; }
            T GetValue(ScalableSetting.Level level);
        }

        public struct NoopGetter<T> : IValueGetter<T>
        {
            public string sourceDescription => string.Empty;
            public T GetValue(ScalableSetting.Level level) => default;
        }

        public struct FromScalableSetting<T>: IValueGetter<T>
        {
            private ScalableSetting<T> m_Value;
            private HDRenderPipelineAsset m_Source;

            public FromScalableSetting(
                ScalableSetting<T> value,
                HDRenderPipelineAsset source)
            {
                m_Value = value;
                m_Source = source;
            }

            public string sourceDescription => m_Source != null ? m_Source.name : string.Empty;
            public T GetValue(ScalableSetting.Level level) => m_Value != null ? m_Value[level] : default;
        }

        private static readonly GUIContent[] k_LevelOptions =
        {
            new GUIContent("Low"),
            new GUIContent("Medium"),
            new GUIContent("High"),
            new GUIContent("Custom"),
        };

        static Rect DoGUILayout(SerializedScalableSettingValue self, GUIContent label)
        {
            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, 0, EditorGUIUtility.singleLineHeight);

            var contentRect = EditorGUI.PrefixLabel(rect, label);

            // Render the enum popup
            const int k_EnumWidth = 70;
            // Magic number??
            const int k_EnumOffset = 30;
            var enumRect = new Rect(contentRect);
            enumRect.x -= k_EnumOffset;
            enumRect.width = k_EnumWidth + k_EnumOffset;

            var (level, isOverride) =
                LevelFieldGUI(enumRect, GUIContent.none, (ScalableSetting.Level)self.level.intValue, self.useOverride.boolValue);
            self.useOverride.boolValue = isOverride;
            if (!self.useOverride.boolValue)
                self.level.intValue = (int)level;

            // Return the rect fo user can render the field there
            var fieldRect = new Rect(contentRect);
            fieldRect.x = enumRect.x + enumRect.width + 2 - k_EnumOffset;
            fieldRect.width = contentRect.width - (fieldRect.x - enumRect.x) + k_EnumOffset;

            return fieldRect;
        }

        public static (ScalableSetting.Level, bool) LevelFieldGUI(Rect rect, GUIContent label, ScalableSetting.Level level, bool useOverride)
        {
            var enumValue = useOverride ? k_LevelOptions.Length - 1 : (int)level;
            var newEnumValues = EditorGUI.Popup(rect, GUIContent.none, enumValue, k_LevelOptions);
            var isOverride = newEnumValues == k_LevelOptions.Length - 1;
            return (isOverride ? level : (ScalableSetting.Level) newEnumValues, isOverride);
        }

        public static void LevelAndIntGUILayout<T>(this SerializedScalableSettingValue self, GUIContent label, T @default)
            where T: struct, IValueGetter<int>
        {
            var fieldRect = DoGUILayout(self, label);
            if (self.useOverride.boolValue)
                self.@override.intValue = EditorGUI.IntField(fieldRect, self.@override.intValue);
            else
                EditorGUI.LabelField(fieldRect, $"{@default.GetValue((ScalableSetting.Level)self.level.intValue)} ({@default.sourceDescription})");
        }

        public static void LevelAndIntGUILayout(this SerializedScalableSettingValue self, GUIContent label)
        {
            LevelAndIntGUILayout(self, label, new NoopGetter<int>());
        }

        public static void LevelAndToggleGUILayout<T>(this SerializedScalableSettingValue self, GUIContent label, T @default)
            where T: struct, IValueGetter<bool>
        {
            var fieldRect = DoGUILayout(self, label);
            if (self.useOverride.boolValue)
                self.@override.boolValue = EditorGUI.Toggle(fieldRect, self.@override.boolValue);
            else
            {
                var enabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUI.Toggle(fieldRect, @default.GetValue((ScalableSetting.Level)self.level.intValue));
                fieldRect.x += 25;
                fieldRect.width -= 25;
                EditorGUI.LabelField(fieldRect, $"({@default.sourceDescription})");
                GUI.enabled = enabled;
            }
        }

        public static void LevelAndToggleGUILayout(this SerializedScalableSettingValue self, GUIContent label)
        {
            LevelAndToggleGUILayout(self, label, new NoopGetter<bool>());
        }
    }
}
