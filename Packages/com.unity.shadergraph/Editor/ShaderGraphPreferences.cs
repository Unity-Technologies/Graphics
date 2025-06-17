using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    internal class LabelWidthScope : GUI.Scope
    {
        float m_previewLabelWidth;
        internal LabelWidthScope(int labelPadding = 10, int labelWidth = 251)
        {
            m_previewLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
            GUILayout.BeginHorizontal();
            GUILayout.Space(labelPadding);
            GUILayout.BeginVertical();
        }

        protected override void CloseScope()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = m_previewLabelWidth;
        }
    }

    static class ShaderGraphPreferences
    {
        static class Keys
        {
            internal const string variantLimit = "UnityEditor.ShaderGraph.VariantLimit";
            internal const string autoAddRemoveBlocks = "UnityEditor.ShaderGraph.AutoAddRemoveBlocks";
            internal const string allowDeprecatedBehaviors = "UnityEditor.ShaderGraph.AllowDeprecatedBehaviors";
        }

        static bool m_Loaded = false;
        internal delegate void PreferenceChangedDelegate();

        internal static PreferenceChangedDelegate onVariantLimitChanged;
        static int m_previewVariantLimit = 2048;

        internal static PreferenceChangedDelegate onAllowDeprecatedChanged;
        internal static int previewVariantLimit
        {
            get { return m_previewVariantLimit; }
            set
            {
                if (onVariantLimitChanged != null)
                    onVariantLimitChanged();
                TrySave(ref m_previewVariantLimit, value, Keys.variantLimit);
            }
        }

        static bool m_AutoAddRemoveBlocks = true;
        internal static bool autoAddRemoveBlocks
        {
            get => m_AutoAddRemoveBlocks;
            set => TrySave(ref m_AutoAddRemoveBlocks, value, Keys.autoAddRemoveBlocks);
        }

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

            using (var scope = new LabelWidthScope(10, 300))
            {
                var actualLimit = ShaderGraphProjectSettings.instance.shaderVariantLimit;
                var willPreviewVariantBeIgnored = ShaderGraphPreferences.previewVariantLimit > actualLimit;

                var variantLimitLabel = willPreviewVariantBeIgnored
                    ? new GUIContent("Preview Variant Limit", EditorGUIUtility.IconContent("console.infoicon").image, $"The Preview Variant Limit is higher than the Shader Variant Limit in Project Settings: {actualLimit}. The Preview Variant Limit will be ignored.")
                    : new GUIContent("Preview Variant Limit");

                EditorGUI.BeginChangeCheck();
                var variantLimitValue = EditorGUILayout.DelayedIntField(variantLimitLabel, previewVariantLimit);
                variantLimitValue = Mathf.Max(0, variantLimitValue);
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
            }
        }

        static void Load()
        {
            m_previewVariantLimit = EditorPrefs.GetInt(Keys.variantLimit, 128);
            m_AutoAddRemoveBlocks = EditorPrefs.GetBool(Keys.autoAddRemoveBlocks, true);
            m_AllowDeprecatedBehaviors = EditorPrefs.GetBool(Keys.allowDeprecatedBehaviors, false);

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
