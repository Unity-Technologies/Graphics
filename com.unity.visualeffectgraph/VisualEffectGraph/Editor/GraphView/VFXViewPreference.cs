using UnityEngine;
using UnityEditor;

public static class VFXViewPreference
{
    private static bool m_Loaded = false;
    private static bool m_DisplayExperimentalOperator = false;

    public static bool displayExperimentalOperator
    {
        get
        {
            LoadIfNeeded();
            return m_DisplayExperimentalOperator;
        }
    }

    private static readonly string experimentalOperatorKey = "displayExperimentalOperatorKey";

    private static void LoadIfNeeded()
    {
        if (!m_Loaded)
        {
            m_DisplayExperimentalOperator = EditorPrefs.GetBool(experimentalOperatorKey, false);
            m_Loaded = true;
        }
    }

    [PreferenceItem("VFX")]
    public static void PreferencesGUI()
    {
        LoadIfNeeded();
        m_DisplayExperimentalOperator = EditorGUILayout.Toggle("Allow experimental operator", m_DisplayExperimentalOperator);

        if (GUI.changed)
        {
            EditorPrefs.SetBool(experimentalOperatorKey, m_DisplayExperimentalOperator);
        }
    }
}
