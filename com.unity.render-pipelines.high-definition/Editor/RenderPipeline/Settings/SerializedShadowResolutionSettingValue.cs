using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public class SerializedShadowResolutionSettingValue
    {
        public SerializedProperty level;
        public SerializedProperty useOverride;
        public SerializedProperty @override;

        public SerializedShadowResolutionSettingValue(SerializedProperty property)
        {
            level = property.FindPropertyRelative("m_Level");
            useOverride = property.FindPropertyRelative("m_UseOverride");
            @override = property.FindPropertyRelative("m_Override");
        }
    }

    public static class SerializedShadowResolutionSettingValueUI
    {
        private static readonly GUIContent[] k_Options = new[]
        {
            new GUIContent("Low"),
            new GUIContent("Medium"),
            new GUIContent("High"),
            new GUIContent("Ultra"),
        };

        public interface IValueGetter<T>
        {
            string sourceDescription { get; }
            T GetValue(int level);
        }

        public struct NoopGetter<T> : IValueGetter<T>
        {
            public string sourceDescription => string.Empty;
            public T GetValue(int level) => default;
        }

        public struct FromScalableSetting: IValueGetter<int>
        {
            private ShadowResolutionSetting m_Value;
            private HDRenderPipelineAsset m_Source;

            public FromScalableSetting(
                ShadowResolutionSetting value,
                HDRenderPipelineAsset source)
            {
                m_Value = value;
                m_Source = source;
            }

            public string sourceDescription => m_Source != null ? m_Source.name : string.Empty;
            public int GetValue(int level) => m_Value != null ? m_Value[level] : default;
        }

        static Rect DoGUILayout(SerializedShadowResolutionSettingValue self, GUIContent label)
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

            var (level, useOverride) = LevelFieldGUI(enumRect, self.level.intValue, self.useOverride.boolValue);
            self.useOverride.boolValue = useOverride;
                self.level.intValue = level;

            // Return the rect fo user can render the field there
            var fieldRect = new Rect(contentRect);
            fieldRect.x = enumRect.x + enumRect.width + 2 - k_EnumOffset;
            fieldRect.width = contentRect.width - (fieldRect.x - enumRect.x) + k_EnumOffset;

            return fieldRect;
        }

        public static (int level, bool useOverride) LevelFieldGUI(Rect rect, int level, bool useOverride)
        {
            var enumValue = useOverride ? 0 : level + 1;
            var levelCount = 4; // TODO: the number of level defined in the setting
            var options = new GUIContent[levelCount + 1];
            options[0] = new GUIContent("Custom");
            Array.Copy(k_Options, 0, options, 1, levelCount);
            var newValue = EditorGUI.Popup(rect, GUIContent.none, enumValue, options);

            return (newValue - 1, newValue == 0);
        }

        public static void LevelAndIntGUILayout<T>(this SerializedShadowResolutionSettingValue self, GUIContent label, T @default)
            where T: struct, IValueGetter<int>
        {
            var fieldRect = DoGUILayout(self, label);
            if (self.useOverride.boolValue)
                self.@override.intValue = EditorGUI.IntField(fieldRect, self.@override.intValue);
            else
                EditorGUI.LabelField(fieldRect, $"{@default.GetValue(self.level.intValue)} ({@default.sourceDescription})");
        }

        public static void LevelAndIntGUILayout(this SerializedShadowResolutionSettingValue self, GUIContent label)
        {
            LevelAndIntGUILayout(self, label, new NoopGetter<int>());
        }
    }
}
