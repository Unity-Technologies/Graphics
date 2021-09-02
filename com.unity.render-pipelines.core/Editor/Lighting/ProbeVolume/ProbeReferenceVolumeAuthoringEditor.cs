using UnityEditor;
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeAuthoring))]
    internal class ProbeReferenceVolumeAuthoringEditor : Editor
    {
        private SerializedProperty m_Dilate;
        private SerializedProperty m_MaxDilationSampleDistance;
        private SerializedProperty m_DilationValidityThreshold;
        private SerializedProperty m_DilationIterations;
        private SerializedProperty m_DilationInvSquaredWeight;

        private SerializedProperty m_EnableVirtualOffset;
        private SerializedProperty m_VirtualOffsetGeometrySearchMultiplier;
        private SerializedProperty m_VirtualOffsetBiasOutOfGeometry;

        private SerializedProperty m_Profile;

        internal static readonly GUIContent s_ProfileAssetLabel = new GUIContent("Profile", "The asset which determines the characteristics of the probe reference volume.");

        private static bool DilationGroupEnabled;
        private static bool VirtualOffsetGroupEnabled;

        private float DilationValidityThresholdInverted;

        ProbeReferenceVolumeAuthoring actualTarget => target as ProbeReferenceVolumeAuthoring;

        private void OnEnable()
        {
            m_Profile = serializedObject.FindProperty("m_Profile");
            m_Dilate = serializedObject.FindProperty("m_EnableDilation");
            m_DilationIterations = serializedObject.FindProperty("m_DilationIterations");
            m_DilationInvSquaredWeight = serializedObject.FindProperty("m_DilationInvSquaredWeight");
            m_MaxDilationSampleDistance = serializedObject.FindProperty("m_MaxDilationSampleDistance");
            m_DilationValidityThreshold = serializedObject.FindProperty("m_DilationValidityThreshold");
            m_EnableVirtualOffset = serializedObject.FindProperty("m_EnableVirtualOffset");
            m_VirtualOffsetGeometrySearchMultiplier = serializedObject.FindProperty("m_VirtualOffsetGeometrySearchMultiplier");
            m_VirtualOffsetBiasOutOfGeometry = serializedObject.FindProperty("m_VirtualOffsetBiasOutOfGeometry");

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

                DilationGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(DilationGroupEnabled, "Dilation");
                if (DilationGroupEnabled)
                {
                    GUIContent dilateGUI = EditorGUIUtility.TrTextContent("Dilate", "Enable probe dilation. Disable only for debug purposes.");
                    m_Dilate.boolValue = EditorGUILayout.Toggle(dilateGUI, m_Dilate.boolValue);
                    EditorGUI.BeginDisabledGroup(!m_Dilate.boolValue);
                    m_MaxDilationSampleDistance.floatValue = EditorGUILayout.FloatField("Dilation Distance", m_MaxDilationSampleDistance.floatValue);
                    DilationValidityThresholdInverted = EditorGUILayout.Slider("Dilation Validity Threshold", DilationValidityThresholdInverted, 0f, 1f);
                    EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    m_DilationIterations.intValue = EditorGUILayout.IntSlider("Dilation Iteration Count", m_DilationIterations.intValue, 1, 5);
                    m_DilationInvSquaredWeight.boolValue = EditorGUILayout.Toggle("Squared Distance Weighting", m_DilationInvSquaredWeight.boolValue);
                    EditorGUI.indentLevel--;

                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("Refresh dilation"), EditorStyles.miniButton))
                    {
                        ProbeGIBaking.RevertDilation();
                        ProbeGIBaking.PerformDilation();
                    }

                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                VirtualOffsetGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(VirtualOffsetGroupEnabled, "Virtual Offset (Proof of Concept)");
                if (VirtualOffsetGroupEnabled)
                {
                    GUIContent virtualOffsetGUI = EditorGUIUtility.TrTextContent("Use Virtual Offset", "Push invalid probes out of geometry. Please note, this feature is currently a proof of concept, it is fairly slow and not optimal in quality.");
                    m_EnableVirtualOffset.boolValue = EditorGUILayout.Toggle(virtualOffsetGUI, m_EnableVirtualOffset.boolValue);
                    EditorGUI.BeginDisabledGroup(!m_EnableVirtualOffset.boolValue);
                    m_VirtualOffsetGeometrySearchMultiplier.floatValue = EditorGUILayout.FloatField(EditorGUIUtility.TrTextContent("Search multiplier", "A multiplier to be applied on the distance between two probes to derive the search distance out of geometry."), m_VirtualOffsetGeometrySearchMultiplier.floatValue);
                    m_VirtualOffsetBiasOutOfGeometry.floatValue = EditorGUILayout.FloatField(EditorGUIUtility.TrTextContent("Bias out geometry", "Determines how much a probe is pushed out of the geometry on top of the distance to closest hit."), m_VirtualOffsetBiasOutOfGeometry.floatValue);

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
            m_VirtualOffsetGeometrySearchMultiplier.floatValue = Mathf.Clamp(m_VirtualOffsetGeometrySearchMultiplier.floatValue, 0.0f, 1.0f);
        }
    }
}
