using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEditorInternal;
using System;
using RuntimeSRPPreferences = UnityEngine.Rendering.CoreRenderPipelinePreferences;

namespace UnityEditor.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeTouchupVolume>;

    internal class ProbeTouchupColorPreferences
    {
        internal static Func<Color> GetColorPrefProbeVolumeGizmoColor;
        internal static Color s_ProbeTouchupVolumeGizmoColorDefault = new Color32(222, 132, 144, 45);

        static ProbeTouchupColorPreferences()
        {
            GetColorPrefProbeVolumeGizmoColor = RuntimeSRPPreferences.RegisterPreferenceColor("Scene/Probe Touchup Volume Gizmo", s_ProbeTouchupVolumeGizmoColorDefault);
        }

    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeTouchupVolume))]
    internal class ProbeTouchupVolumeEditor : Editor
    {
        internal static class Styles
        {
            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is independent of the Transform's Scale.");
            internal static readonly GUIContent s_IntensityScale = new GUIContent("Probe Intensity Scale", "A scale to be applied to all probes that fall within this probe touchup volume.");
            internal static readonly GUIContent s_InvalidateProbes = new GUIContent("Invalidate Probes", "Set all probes falling within this probe touchup volume to invalid.");
            internal static readonly GUIContent s_OverrideDilationThreshold = new GUIContent("Override Dilation Validity Threshold", "Whether to override the dilation validity threshold used for probes falling within this probe touch-up volume.");
            internal static readonly GUIContent s_OverriddenDilationThreshold = new GUIContent("Dilation Validity Threshold", "The dilation validity threshold to use for this probe volume.");
            internal static readonly GUIContent s_TouchupHeader = EditorGUIUtility.TrTextContent("Touchup Controls");

            internal static readonly Color k_GizmoColorBase = ProbeTouchupColorPreferences.s_ProbeTouchupVolumeGizmoColorDefault;

            internal static readonly Color[] k_BaseHandlesColor = new Color[]
            {
                ProbeTouchupColorPreferences.s_ProbeTouchupVolumeGizmoColorDefault,
                ProbeTouchupColorPreferences.s_ProbeTouchupVolumeGizmoColorDefault,
                ProbeTouchupColorPreferences.s_ProbeTouchupVolumeGizmoColorDefault,
                ProbeTouchupColorPreferences.s_ProbeTouchupVolumeGizmoColorDefault,
                ProbeTouchupColorPreferences.s_ProbeTouchupVolumeGizmoColorDefault,
                ProbeTouchupColorPreferences.s_ProbeTouchupVolumeGizmoColorDefault
            };
        }

        static class ProbeTouchupVolumeUI
        {
            public static readonly CED.IDrawer Inspector = null;

            enum AdditionalProperties
            {
                Touchup = 1 << 0,
            }
            enum Expandable
            {
                Touchup = 1 << 0,
            }

            readonly static ExpandedState<Expandable, ProbeTouchupVolume> k_ExpandedState = new ExpandedState<Expandable, ProbeTouchupVolume>(Expandable.Touchup);
            readonly static AdditionalPropertiesState<AdditionalProperties, ProbeTouchupVolume> k_AdditionalPropertiesState = new AdditionalPropertiesState<AdditionalProperties, ProbeTouchupVolume>(0);

            public static void RegisterEditor(ProbeTouchupVolumeEditor editor)
            {
                k_AdditionalPropertiesState.RegisterEditor(editor);
            }

            public static void UnregisterEditor(ProbeTouchupVolumeEditor editor)
            {
                k_AdditionalPropertiesState.UnregisterEditor(editor);
            }

            [SetAdditionalPropertiesVisibility]
            public static void SetAdditionalPropertiesVisibility(bool value)
            {
                if (value)
                    k_AdditionalPropertiesState.ShowAll();
                else
                    k_AdditionalPropertiesState.HideAll();
            }

            public static void DrawTouchupAdditionalContent(SerializedProbeTouchupVolume serialized, Editor owner)
            {
                using (new EditorGUI.DisabledScope(serialized.invalidateProbes.boolValue))
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.HelpBox("Changing the intensity of probe data is a delicate operation that can lead to inconsistencies in the lighting, hence the feature is to be used sparingly.", MessageType.Info, wide: true);
                        EditorGUILayout.PropertyField(serialized.intensityScale, Styles.s_IntensityScale);
                    }
                }
            }

            public static void DrawTouchupContent(SerializedProbeTouchupVolume serialized, Editor owner)
            {
                EditorGUILayout.PropertyField(serialized.invalidateProbes, Styles.s_InvalidateProbes);

                using (new EditorGUI.DisabledScope(serialized.invalidateProbes.boolValue))
                {
                    EditorGUILayout.PropertyField(serialized.overrideDilationThreshold, Styles.s_OverrideDilationThreshold);
                    using (new EditorGUI.DisabledScope(!serialized.overrideDilationThreshold.boolValue))
                        EditorGUILayout.PropertyField(serialized.overriddenDilationThreshold, Styles.s_OverriddenDilationThreshold);
                }
            }


            static ProbeTouchupVolumeUI()
            {
                Inspector = CED.Group(
                CED.AdditionalPropertiesFoldoutGroup(Styles.s_TouchupHeader, Expandable.Touchup, k_ExpandedState, AdditionalProperties.Touchup, k_AdditionalPropertiesState,
                CED.Group((serialized, owner) => DrawTouchupContent(serialized, owner)), DrawTouchupAdditionalContent));
            }
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
                    ProbeVolumeEditor.APVDisabledHelpBox();
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

                ProbeTouchupVolumeUI.Inspector.Draw(m_SerializedTouchupVolume, this);
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
                s_ShapeBox.SetBaseColor(ProbeTouchupColorPreferences.GetColorPrefProbeVolumeGizmoColor());
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
                    Undo.RecordObjects(new UnityEngine.Object[] { touchupVolume, touchupVolume.transform }, "Change Probe Touchup Volume Bounding Box");

                    touchupVolume.size = s_ShapeBox.size;
                    Vector3 delta = touchupVolume.transform.rotation * s_ShapeBox.center - touchupVolume.transform.position;
                    touchupVolume.transform.position += delta; ;
                }
            }
        }
    }
}
