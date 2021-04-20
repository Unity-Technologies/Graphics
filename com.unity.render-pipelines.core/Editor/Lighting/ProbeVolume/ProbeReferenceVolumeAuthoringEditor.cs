#if UNITY_EDITOR

using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeAuthoring))]
    internal class ProbeReferenceVolumeAuthoringEditor : Editor
    {
        // debug gizmo data
        class CellInstancedDebugProbes
        {
            public List<Matrix4x4[]> probeBuffers;
            public List<MaterialPropertyBlock> props;
            public List<int[]> probeMaps;
            public Hash128 cellHash;
            public Vector3 cellPosition;
        }

        private SerializedProperty m_DrawProbes;
        private SerializedProperty m_DrawBricks;
        private SerializedProperty m_DrawCells;
        private SerializedProperty m_ProbeShading;
        private SerializedProperty m_CullingDistance;
        private SerializedProperty m_ExposureCompensation;
        private SerializedProperty m_Dilate;
        private SerializedProperty m_MaxDilationSamples;
        private SerializedProperty m_MaxDilationSampleDistance;
        private SerializedProperty m_DilationValidityThreshold;
        private SerializedProperty m_GreedyDilation;
        private SerializedProperty m_VolumeAsset;

        private SerializedProperty m_Profile;

        internal static readonly GUIContent s_DataAssetLabel = new GUIContent("Data asset", "The asset which serializes all probe related data in this volume.");
        internal static readonly GUIContent s_ProfileAssetLabel = new GUIContent("Profile", "The asset which determines the characteristics of the probe reference volume.");

        private string[] ProbeShadingModes = { "Size", "SH", "Validity" };

        private static bool DebugVisualizationGroupEnabled;
        private static bool DilationGroupEnabled;

        private float DilationValidityThresholdInverted;

        ProbeReferenceVolumeAuthoring actualTarget => target as ProbeReferenceVolumeAuthoring;

        // Debug Properties
        Mesh debugMesh;
        Material debugMaterial;
        const int probesPerBatch = 1023;
        List<CellInstancedDebugProbes> cellDebugData = new List<CellInstancedDebugProbes>();

        //Once the onRenderPipelineTypeChanged event is made public, we won't need the following:
        static EventInfo onRenderPipelineTypeChanged = typeof(RenderPipelineManager).GetEvent("activeRenderPipelineTypeChanged", BindingFlags.NonPublic | BindingFlags.Static);
        static MethodInfo addHandler = onRenderPipelineTypeChanged.GetAddMethod(nonPublic: true);
        static MethodInfo removeHandler = onRenderPipelineTypeChanged.GetAddMethod(nonPublic: true);

        private void OnEnable()
        {
            m_Profile = serializedObject.FindProperty("m_Profile");
            m_DrawProbes = serializedObject.FindProperty("m_DrawProbes");
            m_DrawBricks = serializedObject.FindProperty("m_DrawBricks");
            m_DrawCells = serializedObject.FindProperty("m_DrawCells");
            m_ProbeShading = serializedObject.FindProperty("m_ProbeShading");
            m_CullingDistance = serializedObject.FindProperty("m_CullingDistance");
            m_ExposureCompensation = serializedObject.FindProperty("m_Exposure");
            m_Dilate = serializedObject.FindProperty("m_Dilate");
            m_MaxDilationSamples = serializedObject.FindProperty("m_MaxDilationSamples");
            m_MaxDilationSampleDistance = serializedObject.FindProperty("m_MaxDilationSampleDistance");
            m_DilationValidityThreshold = serializedObject.FindProperty("m_DilationValidityThreshold");
            m_GreedyDilation = serializedObject.FindProperty("m_GreedyDilation");
            m_VolumeAsset = serializedObject.FindProperty("volumeAsset");

            DilationValidityThresholdInverted = 1f - m_DilationValidityThreshold.floatValue;

            // Update debug material in case the current render pipeline has a custom one
            CheckInit();
            addHandler.Invoke(null, new Action[] { UpdateDebugMaterial });
        }

        void OnDisable()
        {
            removeHandler.Invoke(null, new Action[] { UpdateDebugMaterial });
        }

        public override void OnInspectorGUI()
        {
            var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipelineAsset")
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
                            if (!o1.profile.IsEquivalent(o2.profile) && !foundInconsistency)
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
                    m_ExposureCompensation.floatValue = EditorGUILayout.FloatField("Probe Exposure Compensation", m_ExposureCompensation.floatValue);
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
            else
            {
                EditorGUILayout.HelpBox("Probe Volume is not a supported feature by this SRP.", MessageType.Error, wide: true);
            }
        }

        private void Constrain()
        {
            m_CullingDistance.floatValue = Mathf.Max(m_CullingDistance.floatValue, 0);
            m_MaxDilationSamples.intValue = Mathf.Max(m_MaxDilationSamples.intValue, 0);
            m_MaxDilationSampleDistance.floatValue = Mathf.Max(m_MaxDilationSampleDistance.floatValue, 0);
            m_DilationValidityThreshold.floatValue = 1f - DilationValidityThresholdInverted;
        }

        private void CheckInit()
        {
            if (debugMesh == null || debugMaterial == null)
            {
                // Load debug mesh, material
                debugMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Packages/com.unity.render-pipelines.core/Editor/Resources/DebugProbe.fbx");
                UpdateDebugMaterial();
            }
        }

        void UpdateDebugMaterial()
        {
            Shader debugShader = Shader.Find("Hidden/InstancedProbeShader");
            var srpAsset = QualitySettings.renderPipeline ?? GraphicsSettings.renderPipelineAsset;
            if (srpAsset is IOverrideCoreEditorResources overrideResources)
                debugShader = overrideResources.GetProbeVolumeProbeShader();

            debugMaterial = new Material(debugShader) { enableInstancing = true };
        }

        public void OnSceneGUI()
        {
            if (Event.current.type == EventType.Layout)
                DrawProbeGizmos();
        }

        void DrawProbeGizmos()
        {
            if (m_DrawProbes.boolValue)
            {
                // TODO: Update data on ref vol changes
                if (cellDebugData.Count == 0)
                    CreateInstancedProbes();

                // Debug data has not been loaded yet.
                if (debugMesh == null || debugMaterial == null)
                    return;

                foreach (var debug in cellDebugData)
                {
                    if (actualTarget.ShouldCull(debug.cellPosition, ProbeReferenceVolume.instance.GetTransform().posWS))
                        continue;

                    for (int i = 0; i < debug.probeBuffers.Count; ++i)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        var props = debug.props[i];
                        props.SetInt("_ShadingMode", m_ProbeShading.intValue);
                        props.SetFloat("_ExposureCompensation", -m_ExposureCompensation.floatValue);
                        props.SetFloat("_ProbeSize", Gizmos.probeSize * 100);

                        var debugCam = SceneView.lastActiveSceneView.camera;
                        Graphics.DrawMeshInstanced(debugMesh, 0, debugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, debugCam, LightProbeUsage.Off, null);
                    }
                }
            }
        }

        void CreateInstancedProbes()
        {
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                if (cell.sh == null || cell.sh.Length == 0)
                    continue;

                float largestBrickSize = cell.bricks.Count == 0 ? 0 : cell.bricks[0].subdivisionLevel;

                List<Matrix4x4[]> probeBuffers = new List<Matrix4x4[]>();
                List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
                List<int[]> probeMaps = new List<int[]>();

                // Batch probes for instanced rendering
                for (int brickSize = 0; brickSize < largestBrickSize + 1; brickSize++)
                {
                    List<Matrix4x4> probeBuffer = new List<Matrix4x4>();
                    List<int> probeMap = new List<int>();

                    for (int i = 0; i < cell.probePositions.Length; i++)
                    {
                        // Skip probes which aren't of current brick size
                        if (cell.bricks[i / 64].subdivisionLevel == brickSize)
                        {
                            probeBuffer.Add(Matrix4x4.TRS(cell.probePositions[i], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                            probeMap.Add(i);
                        }

                        // Batch limit reached or out of probes
                        if (probeBuffer.Count >= probesPerBatch || i == cell.probePositions.Length - 1)
                        {
                            MaterialPropertyBlock prop = new MaterialPropertyBlock();
                            float gradient = largestBrickSize == 0 ? 1 : brickSize / largestBrickSize;
                            prop.SetColor("_Color", Color.Lerp(Color.red, Color.green, gradient));
                            props.Add(prop);

                            probeBuffers.Add(probeBuffer.ToArray());
                            probeBuffer = new List<Matrix4x4>();
                            probeMaps.Add(probeMap.ToArray());
                            probeMap = new List<int>();
                        }
                    }
                }

                var debugData = new CellInstancedDebugProbes();
                debugData.probeBuffers = probeBuffers;
                debugData.props = props;
                debugData.probeMaps = probeMaps;
                debugData.cellPosition = cell.position;

                Vector4[][] shBuffer = new Vector4[4][];
                for (int i = 0; i < shBuffer.Length; i++)
                    shBuffer[i] = new Vector4[probesPerBatch];

                Vector4[] validityColors = new Vector4[probesPerBatch];

                for (int batchIndex = 0; batchIndex < debugData.probeMaps.Count; batchIndex++)
                {
                    for (int indexInBatch = 0; indexInBatch < debugData.probeMaps[batchIndex].Length; indexInBatch++)
                    {
                        int probeIdx = debugData.probeMaps[batchIndex][indexInBatch];

                        shBuffer[0][indexInBatch] = new Vector4(cell.sh[probeIdx][0, 3], cell.sh[probeIdx][0, 1], cell.sh[probeIdx][0, 2], cell.sh[probeIdx][0, 0]);
                        shBuffer[1][indexInBatch] = new Vector4(cell.sh[probeIdx][1, 3], cell.sh[probeIdx][1, 1], cell.sh[probeIdx][1, 2], cell.sh[probeIdx][1, 0]);
                        shBuffer[2][indexInBatch] = new Vector4(cell.sh[probeIdx][2, 3], cell.sh[probeIdx][2, 1], cell.sh[probeIdx][2, 2], cell.sh[probeIdx][2, 0]);

                        validityColors[indexInBatch] = Color.Lerp(Color.green, Color.red, cell.validity[probeIdx]);
                    }

                    debugData.props[batchIndex].SetVectorArray("_R", shBuffer[0]);
                    debugData.props[batchIndex].SetVectorArray("_G", shBuffer[1]);
                    debugData.props[batchIndex].SetVectorArray("_B", shBuffer[2]);

                    debugData.props[batchIndex].SetVectorArray("_Validity", validityColors);
                }

                cellDebugData.Add(debugData);
            }
        }
    }
}

#endif
