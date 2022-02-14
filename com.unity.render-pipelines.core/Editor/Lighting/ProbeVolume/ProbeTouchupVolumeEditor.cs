using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEditorInternal;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeTouchupVolume))]
    internal class ProbeTouchupVolumeEditor : Editor
    {
        internal static class Styles
        {
            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is independent of the Transform's Scale.");
            internal static readonly GUIContent s_IntensityScale = new GUIContent("Probe Intensity Scale", "A scale to be applied to all probes that fall within this probe touchup volume.");
            internal static readonly GUIContent s_InvalidateProbes = new GUIContent("Invalidate Probes", "Set all probes falling within this probe touchup volume to invalid.");
            internal static readonly GUIContent s_OverrideDilationThreshold = new GUIContent("Override Dilation Threshold", "Whether to override the dilation threshold used for probes falling within this probe touch-up volume.");
            internal static readonly GUIContent s_OverriddenDilationThreshold = new GUIContent("Dilation Threshold", "The dilation threshold to use for this probe volume.");

            internal static readonly Color k_GizmoColorBase = new Color32(222, 132, 144, 255);

            internal static readonly Color[] k_BaseHandlesColor = new Color[]
            {
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase
            };
        }


        SerializedProbeTouchupVolume m_SerializedTouchupVolume;
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;

        static HierarchicalBox _ShapeBox;
        static HierarchicalBox s_ShapeBox
        {
            get
            {
                if (_ShapeBox == null)
                    _ShapeBox = new HierarchicalBox(Styles.k_GizmoColorBase, Styles.k_BaseHandlesColor);
                return _ShapeBox;
            }
        }

        protected void OnEnable()
        {
            m_SerializedTouchupVolume = new SerializedProbeTouchupVolume(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipelineAsset")
            {
                serializedObject.Update();

                if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
                {
                    EditorGUILayout.HelpBox("The probe volumes feature is disabled. The feature needs to be enabled in the HDRP Settings and on the used HDRP asset.", MessageType.Warning, wide: true);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SerializedTouchupVolume.size, Styles.s_Size);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 tmpClamp = m_SerializedTouchupVolume.size.vector3Value;
                    tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                    tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                    tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                    m_SerializedTouchupVolume.size.vector3Value = tmpClamp;
                }


                EditorGUILayout.PropertyField(m_SerializedTouchupVolume.invalidateProbes, Styles.s_InvalidateProbes);

                using (new EditorGUI.DisabledScope(m_SerializedTouchupVolume.invalidateProbes.boolValue))
                {
                    EditorGUILayout.PropertyField(m_SerializedTouchupVolume.overrideDilationThreshold, Styles.s_OverrideDilationThreshold);
                    using (new EditorGUI.DisabledScope(!m_SerializedTouchupVolume.overrideDilationThreshold.boolValue))
                        EditorGUILayout.PropertyField(m_SerializedTouchupVolume.overriddenDilationThreshold, Styles.s_OverriddenDilationThreshold);
                }
                // TODO: This is a very dangerous thing to expose, so for now we don't show. Keeping here in case we find it necessary.
                //EditorGUI.BeginDisabledGroup(m_SerializedTouchupVolume.invalidateProbes.boolValue);
                //EditorGUILayout.PropertyField(m_SerializedTouchupVolume.intensityScale, Styles.s_IntensityScale);
                //EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("Probe Volumes is not a supported feature by this SRP.", MessageType.Error, wide: true);
            }

            m_SerializedTouchupVolume.Apply();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeTouchupVolume touchupVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(touchupVolume.transform.position, touchupVolume.transform.rotation, Vector3.one)))
            {
                // Bounding box.
                s_ShapeBox.center = Vector3.zero;
                s_ShapeBox.size = touchupVolume.size;
                s_ShapeBox.baseColor = new Color(0.75f, 0.2f, 0.18f);
                s_ShapeBox.DrawHull(true);
            }
        }

        protected void OnSceneGUI()
        {
            ProbeTouchupVolume touchupVolume = target as ProbeTouchupVolume;

            //important: if the origin of the handle's space move along the handle,
            //handles displacement will appears as moving two time faster.
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, touchupVolume.transform.rotation, Vector3.one)))
            {
                //contained must be initialized in all case
                s_ShapeBox.center = Quaternion.Inverse(touchupVolume.transform.rotation) * touchupVolume.transform.position;
                s_ShapeBox.size = touchupVolume.size;

                s_ShapeBox.monoHandle = false;
                EditorGUI.BeginChangeCheck();
                s_ShapeBox.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new Object[] { touchupVolume, touchupVolume.transform }, "Change Probe Touchup Volume Bounding Box");

                    touchupVolume.size = s_ShapeBox.size;
                    Vector3 delta = touchupVolume.transform.rotation * s_ShapeBox.center - touchupVolume.transform.position;
                    touchupVolume.transform.position += delta; ;
                }
            }
        }
    }
}
