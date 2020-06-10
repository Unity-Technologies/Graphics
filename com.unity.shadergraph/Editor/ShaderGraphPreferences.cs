using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphPreferences
    {
        static class Keys
        {
            internal const string variantLimit = "UnityEditor.ShaderGraph.VariantLimit";
            internal const string autoAddRemoveBlocks = "UnityEditor.ShaderGraph.AutoAddRemoveBlocks";
        }

        static bool m_Loaded = false;
        internal delegate void PreferenceChangedDelegate();

        internal static PreferenceChangedDelegate onVariantLimitChanged;
        static int m_VariantLimit = 128;
        internal static int variantLimit
        {
            get { return m_VariantLimit; }
            set 
            {
                if(onVariantLimitChanged != null)
                    onVariantLimitChanged();
                TrySave(ref m_VariantLimit, value, Keys.variantLimit); 
            }
        }

        static bool m_AutoAddRemoveBlocks = true;
        internal static bool autoAddRemoveBlocks
        {
            get => m_AutoAddRemoveBlocks;
            set => TrySave(ref m_AutoAddRemoveBlocks, value, Keys.autoAddRemoveBlocks);
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

            EditorGUI.BeginChangeCheck ();
            var variantLimitValue = EditorGUILayout.DelayedIntField("Shader Variant Limit", variantLimit);
            if (EditorGUI.EndChangeCheck ()) 
            {
                variantLimit = variantLimitValue;
            }

            EditorGUI.BeginChangeCheck ();
            var autoAddRemoveBlocksValue = EditorGUILayout.Toggle("Automatically Add and Remove Block Nodes", autoAddRemoveBlocks);
            if (EditorGUI.EndChangeCheck ()) 
            {
                autoAddRemoveBlocks = autoAddRemoveBlocksValue;
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        static void Load()
        {
            m_VariantLimit = EditorPrefs.GetInt(Keys.variantLimit, 128);
            m_AutoAddRemoveBlocks = EditorPrefs.GetBool(Keys.autoAddRemoveBlocks, true);

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
