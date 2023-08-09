using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditorInternal;

using Object = UnityEngine.Object;

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
            var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;

            // HDRP
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipelineAsset")
            {
                var apvDisabledErrorMsg = "Probe Volumes are not enabled.\nMake sure Light Probe System is set to Probe Volumes in the HDRP asset in use.";

                static int IndexOf(string[] names, string name) { for (int i = 0; i < names.Length; i++) { if (name == names[i]) return i; } return -1; }

                var k_Expandables = Type.GetType("UnityEditor.Rendering.HighDefinition.HDRenderPipelineUI+Expandable,Unity.RenderPipelines.HighDefinition.Editor");
                var probeVolume = k_Expandables.GetEnumValues().GetValue(IndexOf(k_Expandables.GetEnumNames(), "ProbeVolume"));

                var k_QualitySettingsHelpBox = Type.GetType("UnityEditor.Rendering.HighDefinition.HDEditorUtils,Unity.RenderPipelines.HighDefinition.Editor")
                    .GetMethod("QualitySettingsHelpBox", BindingFlags.Static | BindingFlags.NonPublic);

                k_QualitySettingsHelpBox.Invoke(null, new object[] { apvDisabledErrorMsg, MessageType.Error, probeVolume, "m_RenderPipelineSettings.lightProbeSystem" });
            }

            // Custom pipelines
            else
            {
                string apvDisabledErrorMsg = "Probe Volumes are not enabled by this render pipeline.";
                EditorGUILayout.HelpBox(apvDisabledErrorMsg, MessageType.Error);
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

            if (ProbeReferenceVolume._GetLightingSettingsOrDefaultsFallback.Invoke().realtimeGI)
            {
                EditorGUILayout.HelpBox("The Probe Volume feature is not supported when using Enlighten.", MessageType.Warning, wide: true);
                drawInspector = false;
            }

            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                APVDisabledHelpBox();
                drawInspector = false;
            }

            if (drawInspector)
            {
                serializedObject.Update();

                ProbeVolumeUI.Inspector.Draw(m_SerializedProbeVolume, this);
            }


            m_SerializedProbeVolume.Apply();
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

            //important: if the origin of the handle's space move along the handle,
            //handles displacement will appears as moving two time faster.
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, probeVolume.transform.rotation, Vector3.one)))
            {
                //contained must be initialized in all case
                s_ShapeBox.center = Quaternion.Inverse(probeVolume.transform.rotation) * probeVolume.transform.position;
                s_ShapeBox.size = probeVolume.size;

                s_ShapeBox.monoHandle = false;
                EditorGUI.BeginChangeCheck();
                s_ShapeBox.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new Object[] { probeVolume, probeVolume.transform }, "Change Probe Volume Bounding Box");

                    probeVolume.size = s_ShapeBox.size;
                    Vector3 delta = probeVolume.transform.rotation * s_ShapeBox.center - probeVolume.transform.position;
                    probeVolume.transform.position += delta; ;
                }
            }
        }
    }
}
