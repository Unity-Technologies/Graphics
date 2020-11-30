#if UNITY_EDITOR

using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeAuthoring))]
    public class ProbeReferenceVolumeAuthoringEditor : Editor
    {
        private SerializedProperty CellSize;
        private SerializedProperty SizeMode;
        private SerializedProperty BrickSize;
        private SerializedProperty MaxSubdivision;
        private SerializedProperty DrawProbes;
        private SerializedProperty DrawBricks;
        private SerializedProperty DrawCells;
        private SerializedProperty ProbeShading;
        private SerializedProperty CullingDistance;
        private SerializedProperty Exposure;
        private SerializedProperty NormalBias;
        private SerializedProperty Dilate;
        private SerializedProperty MaxDilationSamples;
        private SerializedProperty MaxDilationSampleDistance;
        private SerializedProperty DilationValidityThreshold;
        private SerializedProperty GreedyDilation;
        private SerializedProperty VolumeAsset;

        internal static readonly GUIContent s_DataAssetLabel = new GUIContent("Data asset", "The asset which serializes all probe related data in this volume.");


        private string[] SizeModes = { "Length", "Density" };
        private string[] ProbeShadingModes = { "Size", "SH", "Validity" };

        private static bool VolumeGroupEnabled;
        private static bool ShadingGroupEnabled;
        private static bool DebugVisualizationGroupEnabled;
        private static bool DilationGroupEnabled;

        private float DilationValidityThresholdInverted;

        private void OnEnable()
        {
            CellSize = serializedObject.FindProperty("CellSize");
            SizeMode = serializedObject.FindProperty("SizeMode");
            BrickSize = serializedObject.FindProperty("BrickSize");
            MaxSubdivision = serializedObject.FindProperty("MaxSubdivision");
            DrawProbes = serializedObject.FindProperty("DrawProbes");
            DrawBricks = serializedObject.FindProperty("DrawBricks");
            DrawCells = serializedObject.FindProperty("DrawCells");
            ProbeShading = serializedObject.FindProperty("ProbeShading");
            CullingDistance = serializedObject.FindProperty("CullingDistance");
            Exposure = serializedObject.FindProperty("Exposure");
            NormalBias = serializedObject.FindProperty("NormalBias");
            Dilate = serializedObject.FindProperty("Dilate");
            MaxDilationSamples = serializedObject.FindProperty("MaxDilationSamples");
            MaxDilationSampleDistance = serializedObject.FindProperty("MaxDilationSampleDistance");
            DilationValidityThreshold = serializedObject.FindProperty("DilationValidityThreshold");
            GreedyDilation = serializedObject.FindProperty("GreedyDilation");
            VolumeAsset = serializedObject.FindProperty("VolumeAsset");

            DilationValidityThresholdInverted = 1f - DilationValidityThreshold.floatValue;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var probeReferenceVolumes = FindObjectsOfType<ProbeReferenceVolumeAuthoring>();
            if (probeReferenceVolumes.Length > 1)
            {
                var s = "List of game objects with a Probe Reference Volume component:\n \n";
                foreach (var o in probeReferenceVolumes)
                {
                    s += " - " + o.name + "\n";
                }
                EditorGUILayout.HelpBox("Multiple Probe Reference Volume components are in the scene. " +
                    "This is not supported and could lead to faulty results; please remove one.\n\n" + s, MessageType.Error, wide: true);
            }

            EditorGUI.BeginChangeCheck();

            VolumeGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(VolumeGroupEnabled, "Volume");
            if (VolumeGroupEnabled)
            {
                CellSize.intValue = EditorGUILayout.IntField("Cell Size", CellSize.intValue);
                SizeMode.enumValueIndex = EditorGUILayout.Popup("Brick Size Mode", SizeMode.enumValueIndex, SizeModes);
                BrickSize.floatValue = EditorGUILayout.FloatField("Brick Size", BrickSize.floatValue);
                MaxSubdivision.intValue = EditorGUILayout.IntField("Max Subdivision Level", MaxSubdivision.intValue);
                VolumeAsset.objectReferenceValue = EditorGUILayout.ObjectField(s_DataAssetLabel, VolumeAsset.objectReferenceValue, typeof(ProbeVolumeAsset), false);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            ShadingGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(ShadingGroupEnabled, "Shading");
            if (ShadingGroupEnabled)
            {
                NormalBias.floatValue = EditorGUILayout.FloatField("Normal Bias", NormalBias.floatValue);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            DebugVisualizationGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(DebugVisualizationGroupEnabled, "Debug Visualization");
            if (DebugVisualizationGroupEnabled)
            {
                DrawCells.boolValue = EditorGUILayout.Toggle("Draw Cells", DrawCells.boolValue);
                DrawBricks.boolValue = EditorGUILayout.Toggle("Draw Bricks", DrawBricks.boolValue);
                DrawProbes.boolValue = EditorGUILayout.Toggle("Draw Probes", DrawProbes.boolValue);
                EditorGUI.BeginDisabledGroup(!DrawProbes.boolValue);
                ProbeShading.enumValueIndex = EditorGUILayout.Popup("Probe Shading Mode", ProbeShading.enumValueIndex, ProbeShadingModes);
                EditorGUI.BeginDisabledGroup(ProbeShading.enumValueIndex != 1);
                Exposure.floatValue = EditorGUILayout.FloatField("Probe exposure", Exposure.floatValue);
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
                CullingDistance.floatValue = EditorGUILayout.FloatField("Culling Distance", CullingDistance.floatValue);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            DilationGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(DilationGroupEnabled, "Dilation");
            if (DilationGroupEnabled)
            {
                Dilate.boolValue = EditorGUILayout.Toggle("Dilate", Dilate.boolValue);
                EditorGUI.BeginDisabledGroup(!Dilate.boolValue);
                MaxDilationSamples.intValue = EditorGUILayout.IntField("Max Dilation Samples", MaxDilationSamples.intValue);
                MaxDilationSampleDistance.floatValue = EditorGUILayout.FloatField("Max Dilation Sample Distance", MaxDilationSampleDistance.floatValue);
                DilationValidityThresholdInverted = EditorGUILayout.Slider("Dilation Validity Threshold", DilationValidityThresholdInverted, 0f, 1f);
                GreedyDilation.boolValue = EditorGUILayout.Toggle("Greedy Dilation", GreedyDilation.boolValue);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (GUILayout.Button("Run Probe Placement"))
            {
                ProbeGIBaking.RunPlacement();
            }

            if (EditorGUI.EndChangeCheck())
            {
                Constrain();
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void Constrain()
        {         
            CellSize.intValue = Mathf.Max(CellSize.intValue, 1);
            BrickSize.floatValue = Mathf.Max(BrickSize.floatValue, 1);
            MaxSubdivision.intValue = Mathf.Clamp(MaxSubdivision.intValue, 0, 15);
            CullingDistance.floatValue = Mathf.Max(CullingDistance.floatValue, 0);
            NormalBias.floatValue = Mathf.Max(NormalBias.floatValue, 0);
            MaxDilationSamples.intValue = Mathf.Max(MaxDilationSamples.intValue, 0);
            MaxDilationSampleDistance.floatValue = Mathf.Max(MaxDilationSampleDistance.floatValue, 0);
            DilationValidityThreshold.floatValue = 1f - DilationValidityThresholdInverted;
        }
    }
}

#endif
