using UnityEditor;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    [CustomPropertyDrawer(typeof(ProbeVolumeBakingProcessSettings))]
    class ProbeVolumeBakingProcessSettingsDrawer : PropertyDrawer
    {
        // static readonly string dilationEditorFoldoutKey = "APV_Dilation_Foldout";
        // static readonly string virtualOffsetEditorFoldoutKey = "APV_VirtualOffset_Foldout";

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dilationSettings = property.FindPropertyRelative("dilationSettings");
            var virtualOffsetSettings = property.FindPropertyRelative("virtualOffsetSettings");
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // bool dilationFoldout = EditorPrefs.GetBool(dilationEditorFoldoutKey, false);

            // sceneBakingSettings.dilationSettings.dilate

            var m_MaxDilationSampleDistance = dilationSettings.FindPropertyRelative("dilationDistance");
            var m_DilationValidityThreshold = dilationSettings.FindPropertyRelative("dilationValidityThreshold");
            float DilationValidityThresholdInverted = 1f - m_DilationValidityThreshold.floatValue;
            var m_DilationIterations = dilationSettings.FindPropertyRelative("dilationIterations");
            var m_DilationInvSquaredWeight = dilationSettings.FindPropertyRelative("squaredDistWeighting");

            // dilationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(dilationFoldout, "Dilation");
            // bool dilationFoldout = EditorPrefs.SetBool(dilationEditorFoldoutKey, dilationFoldout);
            EditorGUILayout.LabelField("Dilation Settings", EditorStyles.boldLabel);
            // if (dilationFoldout)
            {
                // GUIContent dilateGUI = EditorGUIUtility.TrTextContent("Dilate", "Enable probe dilation. Disable only for debug purposes.");
                // m_Dilate.boolValue = EditorGUILayout.Toggle(dilateGUI, m_Dilate.boolValue);
                // EditorGUI.BeginDisabledGroup(!m_Dilate.boolValue);
                m_MaxDilationSampleDistance.floatValue = EditorGUILayout.FloatField("Dilation Distance", m_MaxDilationSampleDistance.floatValue);
                DilationValidityThresholdInverted = EditorGUILayout.Slider("Dilation Validity Threshold", DilationValidityThresholdInverted, 0f, 1f);
                EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                m_DilationIterations.intValue = EditorGUILayout.IntSlider("Dilation Iteration Count", m_DilationIterations.intValue, 1, 5);
                m_DilationInvSquaredWeight.boolValue = EditorGUILayout.Toggle("Squared Distance Weighting", m_DilationInvSquaredWeight.boolValue);
                EditorGUI.indentLevel--;

                // if (GUILayout.Button(EditorGUIUtility.TrTextContent("Refresh dilation"), EditorStyles.miniButton))
                // {
                //     ProbeGIBaking.RevertDilation();
                //     ProbeGIBaking.PerformDilation();
                // }

                m_MaxDilationSampleDistance.floatValue = Mathf.Max(m_MaxDilationSampleDistance.floatValue, 0);
                m_DilationValidityThreshold.floatValue = 1f - DilationValidityThresholdInverted;

                // EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            var m_EnableVirtualOffset = virtualOffsetSettings.FindPropertyRelative("useVirtualOffset");
            var m_VirtualOffsetGeometrySearchMultiplier = virtualOffsetSettings.FindPropertyRelative("searchMultiplier");
            var m_VirtualOffsetBiasOutOfGeometry = virtualOffsetSettings.FindPropertyRelative("outOfGeoOffset");

            EditorGUILayout.LabelField("Virtual Offset Settings", EditorStyles.boldLabel);
            // VirtualOffsetGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(VirtualOffsetGroupEnabled, "Virtual Offset (Proof of Concept)");
            // if (VirtualOffsetGroupEnabled)
            // {
            GUIContent virtualOffsetGUI = EditorGUIUtility.TrTextContent("Use Virtual Offset", "Push invalid probes out of geometry. Please note, this feature is currently a proof of concept, it is fairly slow and not optimal in quality.");
            m_EnableVirtualOffset.boolValue = EditorGUILayout.Toggle(virtualOffsetGUI, m_EnableVirtualOffset.boolValue);
            EditorGUI.BeginDisabledGroup(!m_EnableVirtualOffset.boolValue);
            m_VirtualOffsetGeometrySearchMultiplier.floatValue = EditorGUILayout.FloatField(EditorGUIUtility.TrTextContent("Search multiplier", "A multiplier to be applied on the distance between two probes to derive the search distance out of geometry."), m_VirtualOffsetGeometrySearchMultiplier.floatValue);
            m_VirtualOffsetBiasOutOfGeometry.floatValue = EditorGUILayout.FloatField(EditorGUIUtility.TrTextContent("Bias out geometry", "Determines how much a probe is pushed out of the geometry on top of the distance to closest hit."), m_VirtualOffsetBiasOutOfGeometry.floatValue);

            EditorGUI.EndDisabledGroup();
            // }
            // EditorGUILayout.EndFoldoutHeaderGroup();

            m_VirtualOffsetGeometrySearchMultiplier.floatValue = Mathf.Clamp(m_VirtualOffsetGeometrySearchMultiplier.floatValue, 0.0f, 1.0f);
            EditorGUI.EndProperty();
        }
    }
}
