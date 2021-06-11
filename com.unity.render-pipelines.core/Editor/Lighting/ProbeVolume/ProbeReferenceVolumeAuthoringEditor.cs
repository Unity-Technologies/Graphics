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
        [InitializeOnLoad]
        class RealtimeProbeSubdivisionDebug
        {
            static RealtimeProbeSubdivisionDebug()
            {
                EditorApplication.update -= UpdateRealtimeSubdivisionDebug;
                EditorApplication.update += UpdateRealtimeSubdivisionDebug;
            }

            static void UpdateRealtimeSubdivisionDebug()
            {
                if (ProbeReferenceVolume.instance.debugDisplay.realtimeSubdivision)
                {
                    var probeVolumeAuthoring = FindObjectOfType<ProbeReferenceVolumeAuthoring>();

                    if (probeVolumeAuthoring == null || !probeVolumeAuthoring.isActiveAndEnabled)
                        return;

                    var ctx = ProbeGIBaking.PrepareProbeSubdivisionContext(probeVolumeAuthoring);

                    // Cull all the cells that are not visible (we don't need them for realtime debug)
                    ctx.cells.RemoveAll(c => {
                        return probeVolumeAuthoring.ShouldCullCell(c.position);
                    });

                    Camera activeCamera = Camera.current ?? SceneView.lastActiveSceneView.camera;

                    if (activeCamera != null)
                    {
                        var cameraPos = activeCamera.transform.position;
                        ctx.cells.Sort((c1, c2) => {
                            c1.volume.CalculateCenterAndSize(out var c1Center, out var _);
                            float c1Distance = Vector3.Distance(cameraPos, c1Center);

                            c2.volume.CalculateCenterAndSize(out var c2Center, out var _);
                            float c2Distance = Vector3.Distance(cameraPos, c2Center);

                            return c1Distance.CompareTo(c2Distance);
                        });
                    }

                    ProbeGIBaking.BakeBricks(ctx);
                }
            }
        }

        private SerializedProperty m_Dilate;
        private SerializedProperty m_MaxDilationSampleDistance;
        private SerializedProperty m_DilationValidityThreshold;
        private SerializedProperty m_DilationIterations;
        private SerializedProperty m_VolumeAsset;

        private SerializedProperty m_Profile;

        internal static readonly GUIContent s_DataAssetLabel = new GUIContent("Data asset", "The asset which serializes all probe related data in this volume.");
        internal static readonly GUIContent s_ProfileAssetLabel = new GUIContent("Profile", "The asset which determines the characteristics of the probe reference volume.");

        private static bool DilationGroupEnabled;

        private float DilationValidityThresholdInverted;

        ProbeReferenceVolumeAuthoring actualTarget => target as ProbeReferenceVolumeAuthoring;

        private void OnEnable()
        {
            m_Profile = serializedObject.FindProperty("m_Profile");
            m_Dilate = serializedObject.FindProperty("m_EnableDilation");
            m_DilationIterations = serializedObject.FindProperty("m_DilationIterations");
            m_MaxDilationSampleDistance = serializedObject.FindProperty("m_MaxDilationSampleDistance");
            m_DilationValidityThreshold = serializedObject.FindProperty("m_DilationValidityThreshold");
            m_VolumeAsset = serializedObject.FindProperty("volumeAsset");

            DilationValidityThresholdInverted = 1f - m_DilationValidityThreshold.floatValue;
        }

        public override void OnInspectorGUI()
        {
            var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipelineAsset")
            {
                serializedObject.Update();

                if (!ProbeReferenceVolume.instance.isInitialized)
                {
                    EditorGUILayout.HelpBox("The probe volumes feature is disabled. The feature needs to be enabled in the HDRP Settings and on the used HDRP asset.", MessageType.Warning, wide: true);
                    return;
                }

                var probeReferenceVolumes = FindObjectsOfType<ProbeReferenceVolumeAuthoring>();
                bool mismatchedProfile = false;
                if (probeReferenceVolumes.Length > 1)
                {
                    foreach (var o1 in probeReferenceVolumes)
                    {
                        foreach (var o2 in probeReferenceVolumes)
                        {
                            if (!o1.profile.IsEquivalent(o2.profile))
                            {
                                mismatchedProfile = true;
                            }
                        }
                    }

                    if (mismatchedProfile)
                    {
                        EditorGUILayout.HelpBox("Multiple Probe Reference Volume components are loaded, but they have different profiles. "
                            + "This is unsupported, please make sure all loaded Probe Reference Volume have the same profile or profiles with equal values.", MessageType.Error, wide: true);
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

                DilationGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(DilationGroupEnabled, "Dilation");
                if (DilationGroupEnabled)
                {
                    GUIContent dilateGUI = EditorGUIUtility.TrTextContent("Dilate", "Enable probe dilation. Disable only for debug purposes.");
                    m_Dilate.boolValue = EditorGUILayout.Toggle(dilateGUI, m_Dilate.boolValue);
                    EditorGUI.BeginDisabledGroup(!m_Dilate.boolValue);
                    m_MaxDilationSampleDistance.floatValue = EditorGUILayout.FloatField("Dilation Distance", m_MaxDilationSampleDistance.floatValue);
                    DilationValidityThresholdInverted = EditorGUILayout.Slider("Dilation Validity Threshold", DilationValidityThresholdInverted, 0f, 1f);
                    m_DilationIterations.intValue = EditorGUILayout.IntSlider("Dilation Iteration Count", m_DilationIterations.intValue, 1, 5);
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
            m_MaxDilationSampleDistance.floatValue = Mathf.Max(m_MaxDilationSampleDistance.floatValue, 0);
            m_DilationValidityThreshold.floatValue = 1f - DilationValidityThresholdInverted;
        }
    }
}
