using UnityEditor;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    [CustomPropertyDrawer(typeof(ProbeVolumeBakingProcessSettings))]
    class ProbeVolumeBakingProcessSettingsDrawer : PropertyDrawer
    {
        static class Styles
        {
            // TODO_FCC: tooltips
            public static GUIContent dilationDistance = new GUIContent("Dilation Distance", "TODO");
            public static GUIContent dilationValidityDistance = new GUIContent("Dilation Validity Threshold", "TODO");
            public static GUIContent dilationIterationCount = new GUIContent("Dilation Iteration Count", "TODO");
            public static GUIContent dilationSquaredDistanceWeighting = new GUIContent("Squared Distance Weighting", "TODO");
            public static GUIContent useVirtualOffset = EditorGUIUtility.TrTextContent("Use Virtual Offset", "Push invalid probes out of geometry. Please note, this feature is currently a proof of concept, it is fairly slow and not optimal in quality.");
            public static GUIContent virtualOffsetSearchMultiplier = EditorGUIUtility.TrTextContent("Search multiplier", "A multiplier to be applied on the distance between two probes to derive the search distance out of geometry.");
            public static GUIContent virtualOffsetBiasOutGeometry = EditorGUIUtility.TrTextContent("Bias out geometry", "Determines how much a probe is pushed out of the geometry on top of the distance to closest hit.");

            public static string dilationSettingsTitle = "Dilation Settings";
            public static string advancedTitle = "Advanced";
            public static string virtualOffsetSettingsTitle = "Virtual Offset Settings";
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dilationSettings = property.FindPropertyRelative("dilationSettings");
            var virtualOffsetSettings = property.FindPropertyRelative("virtualOffsetSettings");

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            property.serializedObject.Update();

            DrawDilationSettings(dilationSettings);
            DrawVirtualOffsetSettings(virtualOffsetSettings);
            EditorGUI.EndProperty();

            property.serializedObject.ApplyModifiedProperties();
        }

        void DrawDilationSettings(SerializedProperty dilationSettings)
        {
            var m_MaxDilationSampleDistance = dilationSettings.FindPropertyRelative("dilationDistance");
            var m_DilationValidityThreshold = dilationSettings.FindPropertyRelative("dilationValidityThreshold");
            float DilationValidityThresholdInverted = 1f - m_DilationValidityThreshold.floatValue;
            var m_DilationIterations = dilationSettings.FindPropertyRelative("dilationIterations");
            var m_DilationInvSquaredWeight = dilationSettings.FindPropertyRelative("squaredDistWeighting");

            EditorGUILayout.LabelField(Styles.dilationSettingsTitle, EditorStyles.boldLabel);

            m_MaxDilationSampleDistance.floatValue = Mathf.Max(EditorGUILayout.FloatField(Styles.dilationDistance, m_MaxDilationSampleDistance.floatValue), 0);
            DilationValidityThresholdInverted = EditorGUILayout.Slider(Styles.dilationValidityDistance, DilationValidityThresholdInverted, 0f, 1f);
            m_DilationValidityThreshold.floatValue = 1f - DilationValidityThresholdInverted;

            EditorGUILayout.LabelField(Styles.advancedTitle, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_DilationIterations.intValue = EditorGUILayout.IntSlider(Styles.dilationIterationCount, m_DilationIterations.intValue, 1, 5);
            m_DilationInvSquaredWeight.boolValue = EditorGUILayout.Toggle(Styles.dilationSquaredDistanceWeighting, m_DilationInvSquaredWeight.boolValue);
            EditorGUI.indentLevel--;

            // if (GUILayout.Button(EditorGUIUtility.TrTextContent("Refresh dilation"), EditorStyles.miniButton))
            // {
            //     ProbeGIBaking.RevertDilation();
            //     ProbeGIBaking.PerformDilation();
            // }
        }

        void DrawVirtualOffsetSettings(SerializedProperty virtualOffsetSettings)
        {

            var m_EnableVirtualOffset = virtualOffsetSettings.FindPropertyRelative("useVirtualOffset");
            var m_VirtualOffsetGeometrySearchMultiplier = virtualOffsetSettings.FindPropertyRelative("searchMultiplier");
            var m_VirtualOffsetBiasOutOfGeometry = virtualOffsetSettings.FindPropertyRelative("outOfGeoOffset");

            EditorGUILayout.LabelField(Styles.virtualOffsetSettingsTitle, EditorStyles.boldLabel);
            m_EnableVirtualOffset.boolValue = EditorGUILayout.Toggle(Styles.useVirtualOffset, m_EnableVirtualOffset.boolValue);
            EditorGUI.BeginDisabledGroup(!m_EnableVirtualOffset.boolValue);
            m_VirtualOffsetGeometrySearchMultiplier.floatValue = Mathf.Clamp01(EditorGUILayout.FloatField(Styles.virtualOffsetSearchMultiplier, m_VirtualOffsetGeometrySearchMultiplier.floatValue));
            m_VirtualOffsetBiasOutOfGeometry.floatValue = EditorGUILayout.FloatField(Styles.virtualOffsetBiasOutGeometry, m_VirtualOffsetBiasOutOfGeometry.floatValue);

            EditorGUI.EndDisabledGroup();

        }
    }
}
