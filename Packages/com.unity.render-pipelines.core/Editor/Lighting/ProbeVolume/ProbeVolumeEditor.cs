using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[assembly: InternalsVisibleTo("SRPSmoke.Editor.Tests")]

namespace UnityEditor.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeVolume))]
    internal class ProbeVolumeEditor : Editor
    {
        SerializedProbeVolume m_SerializedProbeVolume;
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;

        static HierarchicalBox _ShapeBox;
        static HierarchicalBox s_ShapeBox
        {
            get
            {
                if (_ShapeBox == null)
                    _ShapeBox = new HierarchicalBox(ProbeVolumeUI.Styles.k_GizmoColorBase, ProbeVolumeUI.Styles.k_BaseHandlesColor);
                return _ShapeBox;
            }
        }

        protected void OnEnable()
        {
            m_SerializedProbeVolume = new SerializedProbeVolume(serializedObject);
        }

        internal static void APVDisabledHelpBox()
        {
            var renderPipelineAssetType = GraphicsSettings.currentRenderPipelineAssetType;
            switch (renderPipelineAssetType)
            {
                case { Name: "HDRenderPipelineAsset" }:
                {
                    var lightingGroup = GetHDRPLightingGroup();
                    var probeVolume = GetHDRPProbeVolumeEnum();
                    var qualitySettingsHelpBox = GetHDRPQualitySettingsHelpBox();

                    qualitySettingsHelpBox.Invoke(null, new[]
                    {
                        "The current HDRP Asset does not support Adaptive Probe Volumes.", MessageType.Warning, lightingGroup, probeVolume, "m_RenderPipelineSettings.lightProbeSystem"
                    });
                    break;
                }
                case { Name: "UniversalRenderPipelineAsset" }:
                {
                    var qualitySettingsHelpBox = GetURPQualitySettingsHelpBox();
                    var lightingValue = GetURPLightingGroup();

                    qualitySettingsHelpBox.Invoke(null, new[]
                    {
                        "The current URP Asset does not support Adaptive Probe Volumes.", MessageType.Warning, lightingValue, "m_LightProbeSystem"
                    });
                    break;
                }
                default:
                {
                    EditorGUILayout.HelpBox("The current SRP does not support Adaptive Probe Volumes.", MessageType.Warning);
                    break;
                }
            }
        }

        internal static object GetHDRPLightingGroup()
        {
            var expandableGroup = Type.GetType("UnityEditor.Rendering.HighDefinition.HDRenderPipelineUI+ExpandableGroup,Unity.RenderPipelines.HighDefinition.Editor");
            return expandableGroup.GetEnumValues().GetValue(IndexOf(expandableGroup.GetEnumNames(), "Lighting"));
        }

        internal static object GetHDRPProbeVolumeEnum()
        {
            var lightingSection = Type.GetType("UnityEditor.Rendering.HighDefinition.HDRenderPipelineUI+ExpandableLighting,Unity.RenderPipelines.HighDefinition.Editor");
            return lightingSection.GetEnumValues().GetValue(IndexOf(lightingSection.GetEnumNames(), "ProbeVolume"));
        }

        internal static MethodInfo GetHDRPQualitySettingsHelpBox()
        {
            return Type.GetType("UnityEditor.Rendering.HighDefinition.HDEditorUtils,Unity.RenderPipelines.HighDefinition.Editor")
                .GetMethod("QualitySettingsHelpBoxForReflection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        internal static MethodInfo GetURPQualitySettingsHelpBox()
        {
            return Type.GetType("UnityEditor.Rendering.Universal.EditorUtils,Unity.RenderPipelines.Universal.Editor")
                .GetMethod("QualitySettingsHelpBox", BindingFlags.Static | BindingFlags.NonPublic);
        }

        internal static object GetURPLightingGroup()
        {
            var lightingSection = Type.GetType("UnityEditor.Rendering.Universal.UniversalRenderPipelineAssetUI+Expandable,Unity.RenderPipelines.Universal.Editor");
            return lightingSection.GetEnumValues().GetValue(IndexOf(lightingSection.GetEnumNames(), "Lighting"));
        }

        internal static int IndexOf(string[] names, string name)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (name == names[i])
                    return i;
            }
            return -1;
        }

        internal static void FrameSettingDisabledHelpBox()
        {
            var renderPipelineAssetType = GraphicsSettings.currentRenderPipelineAssetType;

            // HDRP only
            if (renderPipelineAssetType != null && renderPipelineAssetType.Name == "HDRenderPipelineAsset")
            {
                static int IndexOf(string[] names, string name) { for (int i = 0; i < names.Length; i++) { if (name == names[i]) return i; } return -1; }

                var frameSettingsField = Type.GetType("UnityEngine.Rendering.HighDefinition.FrameSettingsField,Unity.RenderPipelines.HighDefinition.Runtime");
                var apvFrameSetting = frameSettingsField.GetEnumValues().GetValue(IndexOf(frameSettingsField.GetEnumNames(), "AdaptiveProbeVolume"));

                var ensureFrameSetting = Type.GetType("UnityEditor.Rendering.HighDefinition.HDEditorUtils,Unity.RenderPipelines.HighDefinition.Editor")
                    .GetMethod("EnsureFrameSetting", BindingFlags.Static | BindingFlags.NonPublic);

                ensureFrameSetting.Invoke(null, new[] { apvFrameSetting });
            }
        }

        public override void OnInspectorGUI()
        {
            ProbeVolume probeVolume = target as ProbeVolume;

            bool hasChanges = false;
            if (probeVolume.cachedTransform != probeVolume.gameObject.transform.worldToLocalMatrix)
            {
                hasChanges = true;
            }

            if (probeVolume.cachedHashCode != probeVolume.GetHashCode())
            {
                hasChanges = true;
            }

            probeVolume.mightNeedRebaking = hasChanges;

            bool drawInspector = true;

#pragma warning disable 618
            if (ProbeVolumeLightingTab.GetLightingSettings().realtimeGI)
#pragma warning restore 618
            {
                EditorGUILayout.HelpBox("Adaptive Probe Volumes are not supported when using Realtime Global Illumination(Enlighten).", MessageType.Warning, wide: true);
                drawInspector = false;
            }

            if (!ProbeVolumeGlobalSettingsStripper.ProbeVolumeSupportedForBuild())
            {
                APVDisabledHelpBox();
                drawInspector = false;
            }

            if (drawInspector)
            {
                ProbeVolumeEditor.FrameSettingDisabledHelpBox();

                serializedObject.Update();
                ProbeVolumeUI.k_Inspector.Draw(m_SerializedProbeVolume, this);
                m_SerializedProbeVolume.Apply();
            }
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeVolume probeVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(probeVolume.transform.position, probeVolume.transform.rotation, Vector3.one)))
            {
                // Bounding box.
                s_ShapeBox.center = Vector3.zero;
                s_ShapeBox.size = probeVolume.size;
                s_ShapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }
        }

        protected void OnSceneGUI()
        {
            ProbeVolume probeVolume = target as ProbeVolume;
            if (probeVolume.mode != ProbeVolume.Mode.Local)
                return;

            // important: if the origin of the handle's space move along the handle,
            // handles displacement will appears as moving two time faster.
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, probeVolume.transform.rotation, Vector3.one)))
            {
                // contained must be initialized in all case
                s_ShapeBox.center = Quaternion.Inverse(probeVolume.transform.rotation) * probeVolume.transform.position;
                s_ShapeBox.size = probeVolume.size;

                s_ShapeBox.monoHandle = false;
                EditorGUI.BeginChangeCheck();
                s_ShapeBox.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new Object[] { probeVolume, probeVolume.transform }, "Change Adaptive Probe Volume Bounding Box");

                    probeVolume.size = s_ShapeBox.size;
                    Vector3 delta = probeVolume.transform.rotation * s_ShapeBox.center - probeVolume.transform.position;
                    probeVolume.transform.position += delta; ;
                }
            }
        }

        [MenuItem("CONTEXT/ProbeVolume/Rendering Debugger...")]
        internal static void AddProbeVolumeContextMenu()
        {
            ProbeVolumeLightingTab.OpenProbeVolumeDebugPanel(null, null, 0);
        }
    }
}
