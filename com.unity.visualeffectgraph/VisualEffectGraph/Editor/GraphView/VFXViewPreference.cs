using UnityEngine;
using UnityEditor;

public static class VFXViewPreference
{
    private static bool m_Loaded = false;
    private static bool m_DisplayExperimentalOperator = false;
    private static bool m_DisplayExtraDebugInfo = false;


    public static bool displayExperimentalOperator
    {
        get
        {
            LoadIfNeeded();
            return m_DisplayExperimentalOperator;
        }
    }

    public static bool displayExtraDebugInfo
    {
        get
        {
            LoadIfNeeded();
            return m_DisplayExtraDebugInfo;
        }
    }

    public const string experimentalOperatorKey = "VFX.displayExperimentalOperatorKey";
    public const string extraDebugInfoKey = "VFX.ExtraDebugInfo";

    private static void LoadIfNeeded()
    {
        if (!m_Loaded)
        {
            m_DisplayExperimentalOperator = EditorPrefs.GetBool(experimentalOperatorKey, false);
            m_DisplayExtraDebugInfo = EditorPrefs.GetBool(extraDebugInfoKey, false);

            m_Loaded = true;
        }
    }

    [PreferenceItem("Visual Effects")]
    public static void PreferencesGUI()
    {
        LoadIfNeeded();
        m_DisplayExperimentalOperator = EditorGUILayout.Toggle("Experimental Operators/Blocks", m_DisplayExperimentalOperator);
        m_DisplayExtraDebugInfo = EditorGUILayout.Toggle("Show Additional DebugInfo", m_DisplayExtraDebugInfo);

        if (GUI.changed)
        {
            EditorPrefs.SetBool(experimentalOperatorKey, m_DisplayExperimentalOperator);
            EditorPrefs.SetBool(extraDebugInfoKey, m_DisplayExtraDebugInfo);
        }
    }
}
