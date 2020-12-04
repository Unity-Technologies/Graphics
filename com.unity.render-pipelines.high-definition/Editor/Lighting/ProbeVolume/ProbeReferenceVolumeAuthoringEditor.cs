#if UNITY_EDITOR

using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeAuthoring))]
    public class ProbeReferenceVolumeAuthoringEditor : Editor
    {
        private SerializedProperty m_DrawProbes;
        private SerializedProperty m_DrawBricks;
        private SerializedProperty m_DrawCells;
        private SerializedProperty m_ProbeShading;
        private SerializedProperty m_CullingDistance;
        private SerializedProperty m_Exposure;
        private SerializedProperty m_Dilate;
        private SerializedProperty m_MaxDilationSamples;
        private SerializedProperty m_MaxDilationSampleDistance;
        private SerializedProperty m_DilationValidityThreshold;
        private SerializedProperty m_GreedyDilation;
        private SerializedProperty m_VolumeAsset;

        private SerializedProperty m_Profile;

        internal static readonly GUIContent s_DataAssetLabel = new GUIContent("Data asset", "The asset which serializes all probe related data in this volume.");
        internal static readonly GUIContent s_ProfileAssetLabel = new GUIContent("Profile", "The asset which determines the characteristics of the probe reference volume.");


        private string[] SizeModes = { "Length", "Density" };
        private string[] ProbeShadingModes = { "Size", "SH", "Validity" };

        private static bool ShadingGroupEnabled;
        private static bool DebugVisualizationGroupEnabled;
        private static bool DilationGroupEnabled;

        private float DilationValidityThresholdInverted;

        ProbeReferenceVolumeAuthoring actualTarget => target as ProbeReferenceVolumeAuthoring;

        private void OnEnable()
        {
            // TODO: TMP NAMING for profile.
            m_Profile = serializedObject.FindProperty("m_Profile");
            m_DrawProbes = serializedObject.FindProperty("DrawProbes");
            m_DrawBricks = serializedObject.FindProperty("DrawBricks");
            m_DrawCells = serializedObject.FindProperty("DrawCells");
            m_ProbeShading = serializedObject.FindProperty("ProbeShading");
            m_CullingDistance = serializedObject.FindProperty("CullingDistance");
            m_Exposure = serializedObject.FindProperty("Exposure");
            m_Dilate = serializedObject.FindProperty("Dilate");
            m_MaxDilationSamples = serializedObject.FindProperty("MaxDilationSamples");
            m_MaxDilationSampleDistance = serializedObject.FindProperty("MaxDilationSampleDistance");
            m_DilationValidityThreshold = serializedObject.FindProperty("DilationValidityThreshold");
            m_GreedyDilation = serializedObject.FindProperty("GreedyDilation");
            m_VolumeAsset = serializedObject.FindProperty("VolumeAsset");

            DilationValidityThresholdInverted = 1f - m_DilationValidityThreshold.floatValue;
			ProbeGIBaking.Init();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var probeReferenceVolumes = FindObjectsOfType<ProbeReferenceVolumeAuthoring>();
            bool foundInconsistency = false;
            if (probeReferenceVolumes.Length > 1)
            {
                foreach (var o1 in probeReferenceVolumes)
                {
                    foreach (var o2 in probeReferenceVolumes)
                    {
                        if (!o1.m_Profile.IsEquivalent(o2.m_Profile) && !foundInconsistency)
                        {
                            EditorGUILayout.HelpBox("Multiple Probe Reference Volume components are loaded, but they have different profiles. "
                                + "This is unsupported, please make sure all loaded Probe Reference Volume have the same profile or profiles with equal values.", MessageType.Error, wide: true);
                            foundInconsistency = true;
                        }
                        if (foundInconsistency) break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();

            // The layout system breaks alignment when mixing inspector fields with custom layout'd
            // fields, do the layout manually instead
            int buttonWidth = 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = EditorGUILayout.GetControlRect();
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth, lineRect.height);
            var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            GUIContent guiContent = EditorGUIUtility.TrTextContent("Profile", "A reference to a profile asset.");
            EditorGUI.PrefixLabel(labelRect, guiContent);

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.BeginProperty(fieldRect, GUIContent.none, m_Profile);

                m_Profile.objectReferenceValue = (ProbeReferenceVolumeProfile)EditorGUI.ObjectField(fieldRect, m_Profile.objectReferenceValue, typeof(ProbeReferenceVolumeProfile), false);

                EditorGUI.EndProperty();
            }

            if (GUI.Button(buttonNewRect, EditorGUIUtility.TrTextContent("New", "Create a new profile."), EditorStyles.miniButton))
            {
                // By default, try to put assets in a folder next to the currently active
                // scene file. If the user isn't a scene, put them in root instead.
                var targetName = actualTarget.name;
                var scene = actualTarget.gameObject.scene;
                var asset = ProbeReferenceVolumeAuthoring.CreateReferenceVolumeProfile(scene, targetName);
                m_Profile.objectReferenceValue = asset;
            }

            m_VolumeAsset.objectReferenceValue = EditorGUILayout.ObjectField(s_DataAssetLabel, m_VolumeAsset.objectReferenceValue, typeof(ProbeVolumeAsset), false);

            DebugVisualizationGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(DebugVisualizationGroupEnabled, "Debug Visualization");
            if (DebugVisualizationGroupEnabled)
            {
                m_DrawCells.boolValue = EditorGUILayout.Toggle("Draw Cells", m_DrawCells.boolValue);
                m_DrawBricks.boolValue = EditorGUILayout.Toggle("Draw Bricks", m_DrawBricks.boolValue);
                m_DrawProbes.boolValue = EditorGUILayout.Toggle("Draw Probes", m_DrawProbes.boolValue);
                EditorGUI.BeginDisabledGroup(!m_DrawProbes.boolValue);
                m_ProbeShading.enumValueIndex = EditorGUILayout.Popup("Probe Shading Mode", m_ProbeShading.enumValueIndex, ProbeShadingModes);
                EditorGUI.BeginDisabledGroup(m_ProbeShading.enumValueIndex != 1);
                m_Exposure.floatValue = EditorGUILayout.FloatField("Probe exposure", m_Exposure.floatValue);
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
                m_CullingDistance.floatValue = EditorGUILayout.FloatField("Culling Distance", m_CullingDistance.floatValue);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            DilationGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(DilationGroupEnabled, "Dilation");
            if (DilationGroupEnabled)
            {
                m_Dilate.boolValue = EditorGUILayout.Toggle("Dilate", m_Dilate.boolValue);
                EditorGUI.BeginDisabledGroup(!m_Dilate.boolValue);
                m_MaxDilationSamples.intValue = EditorGUILayout.IntField("Max Dilation Samples", m_MaxDilationSamples.intValue);
                m_MaxDilationSampleDistance.floatValue = EditorGUILayout.FloatField("Max Dilation Sample Distance", m_MaxDilationSampleDistance.floatValue);
                DilationValidityThresholdInverted = EditorGUILayout.Slider("Dilation Validity Threshold", DilationValidityThresholdInverted, 0f, 1f);
                m_GreedyDilation.boolValue = EditorGUILayout.Toggle("Greedy Dilation", m_GreedyDilation.boolValue);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (EditorGUI.EndChangeCheck())
            {
                Constrain();
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void Constrain()
        {         
            m_CullingDistance.floatValue = Mathf.Max(m_CullingDistance.floatValue, 0);
            m_MaxDilationSamples.intValue = Mathf.Max(m_MaxDilationSamples.intValue, 0);
            m_MaxDilationSampleDistance.floatValue = Mathf.Max(m_MaxDilationSampleDistance.floatValue, 0);
            m_DilationValidityThreshold.floatValue = 1f - DilationValidityThresholdInverted;
        }
    }
}

#endif
