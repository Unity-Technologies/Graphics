using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphPreferences
    {
        static class Keys
        {
            internal const string variantLimit = "UnityEditor.ShaderGraph.VariantLimit";
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

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck ();
            var variantLimitValue = EditorGUILayout.DelayedIntField("Shader Variant Limit", variantLimit);
            if (EditorGUI.EndChangeCheck ()) 
            {
                variantLimit = variantLimitValue;
            }
        }

        static void Load()
        {
            m_VariantLimit = EditorPrefs.GetInt(Keys.variantLimit, 128);

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
