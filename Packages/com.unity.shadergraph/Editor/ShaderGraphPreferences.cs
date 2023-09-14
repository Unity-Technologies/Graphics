using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphPreferences
    {
        static class Keys
        {
            internal const string variantLimit = "UnityEditor.ShaderGraph.VariantLimit";
            internal const string autoAddRemoveBlocks = "UnityEditor.ShaderGraph.AutoAddRemoveBlocks";
            internal const string allowDeprecatedBehaviors = "UnityEditor.ShaderGraph.AllowDeprecatedBehaviors";
            internal const string zoomStepSize = "UnityEditor.ShaderGraph.ZoomStepSize";
        }

        static bool m_Loaded = false;
        internal delegate void PreferenceChangedDelegate();

        internal static PreferenceChangedDelegate onVariantLimitChanged;
        static int m_PreviewVariantLimit = 128;
        internal static int previewVariantLimit
        {
            get { return m_PreviewVariantLimit; }
            set
            {
                if (onVariantLimitChanged != null)
                    onVariantLimitChanged();
                TrySave(ref m_PreviewVariantLimit, value, Keys.variantLimit);
            }
        }

        static bool m_AutoAddRemoveBlocks = true;
        internal static bool autoAddRemoveBlocks
        {
            get => m_AutoAddRemoveBlocks;
            set => TrySave(ref m_AutoAddRemoveBlocks, value, Keys.autoAddRemoveBlocks);
        }

        internal static PreferenceChangedDelegate onAllowDeprecatedChanged;
        static bool m_AllowDeprecatedBehaviors = false;
        internal static bool allowDeprecatedBehaviors
        {
            get => m_AllowDeprecatedBehaviors;
            set
            {
                TrySave(ref m_AllowDeprecatedBehaviors, value, Keys.allowDeprecatedBehaviors);
                if (onAllowDeprecatedChanged != null)
                {
                    onAllowDeprecatedChanged();
                }
            }
        }

        internal static PreferenceChangedDelegate onZoomStepSizeChanged;
        const float defaultZoomStepSize = 0.5f;
        static float m_ZoomStepSize = defaultZoomStepSize;
        internal static float zoomStepSize
        {
            get => m_ZoomStepSize;
            set
            {
                TrySave(ref m_ZoomStepSize, value, Keys.zoomStepSize);
                if (onZoomStepSizeChanged != null)
                {
                    onZoomStepSizeChanged();
                }
            }
        }


        static ShaderGraphPreferences()
        {
            Load();
        }

        [SettingsProvider]
        static SettingsProvider PreferenceGUI()
        {
            return new SettingsProvider("Preferences/Shader Graph", SettingsScope.User)
            {
                guiHandler = searchContext => OpenGUI()
            };
        }

        static void OpenGUI()
        {
            if (!m_Loaded)
                Load();

            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 256;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            var actualLimit = ShaderGraphProjectSettings.instance.shaderVariantLimit;
            var willPreviewVariantBeIgnored = ShaderGraphPreferences.previewVariantLimit > actualLimit;

            var variantLimitLabel = willPreviewVariantBeIgnored
                ? new GUIContent("Preview Variant Limit", EditorGUIUtility.IconContent("console.infoicon").image, $"The Preview Variant Limit is higher than the Shader Variant Limit in Project Settings: {actualLimit}. The Preview Variant Limit will be ignored.")
                : new GUIContent("Preview Variant Limit");

            var variantLimitValue = EditorGUILayout.DelayedIntField(variantLimitLabel, previewVariantLimit);
            if (EditorGUI.EndChangeCheck())
            {
                previewVariantLimit = variantLimitValue;
            }

            EditorGUI.BeginChangeCheck();
            var autoAddRemoveBlocksValue = EditorGUILayout.Toggle("Automatically Add and Remove Block Nodes", autoAddRemoveBlocks);
            if (EditorGUI.EndChangeCheck())
            {
                autoAddRemoveBlocks = autoAddRemoveBlocksValue;
            }

            EditorGUI.BeginChangeCheck();
            var allowDeprecatedBehaviorsValue = EditorGUILayout.Toggle("Enable Deprecated Nodes", allowDeprecatedBehaviors);
            if (EditorGUI.EndChangeCheck())
            {
                allowDeprecatedBehaviors = allowDeprecatedBehaviorsValue;
            }

            EditorGUI.BeginChangeCheck();
            var zoomStepSizeValue = EditorGUILayout.Slider(new GUIContent("Zoom Step Size", $"Default is 0.5"), zoomStepSize, 0.0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                zoomStepSize = zoomStepSizeValue;
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        static void Load()
        {
            m_PreviewVariantLimit = EditorPrefs.GetInt(Keys.variantLimit, 128);
            m_AutoAddRemoveBlocks = EditorPrefs.GetBool(Keys.autoAddRemoveBlocks, true);
            m_AllowDeprecatedBehaviors = EditorPrefs.GetBool(Keys.allowDeprecatedBehaviors, false);
            m_ZoomStepSize = EditorPrefs.GetFloat(Keys.zoomStepSize, defaultZoomStepSize);
            m_Loaded = true;
        }

        static void TrySave<T>(ref T field, T newValue, string key)
        {
            if (field.Equals(newValue))
                return;

            if (typeof(T) == typeof(float))
                EditorPrefs.SetFloat(key, (float)(object)newValue);
            else if (typeof(T) == typeof(int))
                EditorPrefs.SetInt(key, (int)(object)newValue);
            else if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(key, (bool)(object)newValue);
            else if (typeof(T) == typeof(string))
                EditorPrefs.SetString(key, (string)(object)newValue);

            field = newValue;
        }
    }
}
