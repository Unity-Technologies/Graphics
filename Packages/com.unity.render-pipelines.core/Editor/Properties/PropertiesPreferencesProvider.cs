using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [DisplayInfo(name = "Properties", order = 100)]
    class PropertiesPreferencesProvider : ICoreRenderPipelinePreferencesProvider
    {
        class Styles
        {
            public static readonly GUIContent additionalPropertiesLabel = EditorGUIUtility.TrTextContent("Advanced Properties", "Tells Unity to show or hide Advanced Properties.");
            public static readonly GUIContent[] additionalPropertiesNames = { EditorGUIUtility.TrTextContent("All Visible"), EditorGUIUtility.TrTextContent("All Hidden") };
            public static readonly int[] additionalPropertiesValues = { 1, 0 };
        }

        static List<string> s_SearchKeywords = new() { "Additional", "Advanced", "Properties" };
        public List<string> keywords => s_SearchKeywords;

        public void PreferenceGUI()
        {
            AdvancedProperties.enabled = EditorGUILayout.IntPopup(Styles.additionalPropertiesLabel,
                AdvancedProperties.enabled ? 1 : 0, Styles.additionalPropertiesNames,
                Styles.additionalPropertiesValues) == 1;
        }
    }
}
