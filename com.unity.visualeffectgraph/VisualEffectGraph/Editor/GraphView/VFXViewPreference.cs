
using UnityEngine;
using UnityEditor;

public class VFXViewPreference : MonoBehaviour
{
    private static bool prefsLoaded = false;
    public static bool displayExperimentalOperator { get; private set; }

    [PreferenceItem("VFX")]
    public static void PreferencesGUI()
    {
        var experimentalOperator = "displayExperimentalOperatorKey";
        if (!prefsLoaded)
        {
            displayExperimentalOperator = EditorPrefs.GetBool(experimentalOperator, false);
            prefsLoaded = true;
        }

        displayExperimentalOperator = EditorGUILayout.Toggle("Allow experimental operator", displayExperimentalOperator);

        if (GUI.changed)
        {
            EditorPrefs.SetBool(experimentalOperator, displayExperimentalOperator);
        }
    }
}