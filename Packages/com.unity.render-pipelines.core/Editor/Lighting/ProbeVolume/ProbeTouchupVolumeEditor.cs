using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditorInternal;

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
            GetColorPrefProbeVolumeGizmoColor = RuntimeSRPPreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Probe Adjustment Volume Gizmo", s_ProbeTouchupVolumeGizmoColorDefault);
        }

    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeTouchupVolume))]
    internal class ProbeTouchupVolumeEditor : Editor
    {
        internal static class Styles
        {
            internal static readonly GUIContent s_RotateToolIcon = EditorGUIUtility.TrIconContent("RotateTool", "The virtual offset direction for probes falling in this volume.");
            internal static readonly GUIContent s_VolumeHeader = EditorGUIUtility.TrTextContent("Influence Volume");
            internal static readonly GUIContent s_TouchupHeader = EditorGUIUtility.TrTextContent("Probe Volume Overrides");

            internal static readonly GUIContent s_Mode = new GUIContent("Mode", "Choose which type of adjustment to apply to probes covered by this volume.");
            internal static readonly GUIContent s_DilationThreshold = new GUIContent("Dilation Validity Threshold", "Override the Dilation Validity Threshold for probes covered by this Probe Adjustment Volume. Higher values increase the chance of probes being considered invalid.");
            internal static readonly GUIContent s_UpdateValidity = new GUIContent("Update Probe Validity", "Invalidate probes covered by this Probe Adjustment Volume.");
            internal static readonly GUIContent s_VODirection = new GUIContent("Direction", "Rotate the axis along which probes will be pushed when applying Virtual Offset.");
            internal static readonly GUIContent s_VODistance = new GUIContent("Distance", "Determines how far probes are pushed in the direction of the Virtual Offset.");

            internal static readonly EditMode.SceneViewEditMode VirtualOffsetEditMode = (EditMode.SceneViewEditMode)110;

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

        static internal bool Button(GUIContent content)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space(15 * EditorGUI.indentLevel, false);
            bool value = GUILayout.Button(content, EditorStyles.miniButton);
            GUILayout.EndHorizontal();
            return value;
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
                Volume = 1 << 0,
                Touchup = 1 << 1,
            }

            readonly static ExpandedState<Expandable, ProbeTouchupVolume> k_ExpandedState = new ExpandedState<Expandable, ProbeTouchupVolume>(Expandable.Volume | Expandable.Touchup);
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

            public static void DrawVolumeContent(SerializedProbeTouchupVolume serialized, Editor owner)
            {
                EditorGUILayout.PropertyField(serialized.shape);
                EditorGUILayout.PropertyField(serialized.shape.intValue == 0 ? serialized.size : serialized.radius);

            }

            static T[] RemoveAt<T>(T[] values, int index)
            {
                var list = new List<T>(values);
                list.RemoveAt(index);
                return list.ToArray();
            }
            static GUIContent[] CastArray(string[] values)
            {
                var result = new GUIContent[values.Length];
                for (int i = 0; i < values.Length; i++)
                    result[i] = new GUIContent(ObjectNames.NicifyVariableName(values[i]));
                return result;
            }

            public static void DrawTouchupContent(SerializedProbeTouchupVolume serialized, Editor owner)
            {
                ProbeTouchupVolume ptv = (serialized.serializedObject.targetObject as ProbeTouchupVolume);

                var bakingSet = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(ptv.gameObject.scene);
                bool useVirtualOffset = bakingSet != null ? bakingSet.settings.virtualOffsetSettings.useVirtualOffset : false;

                var hiddenMode = (int)ProbeTouchupVolume.Mode.IntensityScale;
                var availableValues = (int[])Enum.GetValues(typeof(ProbeTouchupVolume.Mode));
                var availableModes = CastArray(Enum.GetNames(typeof(ProbeTouchupVolume.Mode)));
                if (!k_AdditionalPropertiesState[AdditionalProperties.Touchup] && serialized.mode.intValue != hiddenMode)
                {
                    int idx = Array.IndexOf(availableValues, hiddenMode);
                    availableValues = RemoveAt(availableValues, idx);
                    availableModes = RemoveAt(availableModes, idx);
                }

                EditorGUI.BeginChangeCheck();
                int newValue = EditorGUILayout.IntPopup(Styles.s_Mode, serialized.mode.intValue, availableModes, availableValues);
                if (EditorGUI.EndChangeCheck())
                    serialized.mode.intValue = newValue;

                if (serialized.mode.intValue == (int)ProbeTouchupVolume.Mode.OverrideValidityThreshold)
                {
                    EditorGUILayout.PropertyField(serialized.overriddenDilationThreshold, Styles.s_DilationThreshold);
                }
                else if (serialized.mode.intValue == (int)ProbeTouchupVolume.Mode.ApplyVirtualOffset)
                {
                    EditorGUI.BeginDisabledGroup(!useVirtualOffset);
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(serialized.virtualOffsetRotation, Styles.s_VODirection);

                    var editMode = Styles.VirtualOffsetEditMode;
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Toggle(editMode == EditMode.editMode, Styles.s_RotateToolIcon, EditorStyles.miniButton, GUILayout.Width(28f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditMode.SceneViewEditMode targetMode = EditMode.editMode == editMode ? EditMode.SceneViewEditMode.None : editMode;
                        EditMode.ChangeEditMode(targetMode, GetBounds(serialized, owner), owner);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(serialized.virtualOffsetDistance, Styles.s_VODistance);
                    EditorGUI.EndDisabledGroup();

                    if (!useVirtualOffset)
                    {
                        CoreEditorUtils.DrawFixMeBox("Apply Virtual Offset can be used only if Virtual Offset is enabled for the Baking Set.", MessageType.Warning, "Open", () =>
                        {
                            ProbeVolumeLightingTab.OpenBakingSet(bakingSet);
                        });
                    }
                }
                else if (serialized.mode.intValue == (int)ProbeTouchupVolume.Mode.OverrideVirtualOffsetSettings)
                {
                    EditorGUI.BeginDisabledGroup(!useVirtualOffset);
                    EditorGUILayout.PropertyField(serialized.geometryBias);
                    EditorGUILayout.PropertyField(serialized.rayOriginBias);
                    EditorGUI.EndDisabledGroup();

                    if (!useVirtualOffset)
                    {
                        EditorGUILayout.HelpBox("Override Virtual Offset can be used only if Virtual Offset is enabled for the Baking Set.", MessageType.Warning);
                    }

                }
                else if (serialized.mode.intValue == (int)ProbeTouchupVolume.Mode.InvalidateProbes)
                {
                    if (Button(Styles.s_UpdateValidity))
                    {
                        ProbeGIBaking.RecomputeValidityAfterBake();
                    }
                }
                else if (serialized.mode.intValue == (int)ProbeTouchupVolume.Mode.IntensityScale)
                {
                    EditorGUILayout.HelpBox("Overriding the intensity of probes can break the physical plausibility of lighting. This may result in unwanted visual inconsistencies.", MessageType.Info, wide: true);
                    EditorGUILayout.PropertyField(serialized.intensityScale);
                }
            }

            internal static Bounds GetBounds(SerializedProbeTouchupVolume serialized, Editor owner)
            {
                var position = ((Component)owner.target).transform.position;
                if (serialized.shape.intValue == (int)ProbeTouchupVolume.Shape.Box)
                    return new Bounds(position, serialized.size.vector3Value);
                if (serialized.shape.intValue == (int)ProbeTouchupVolume.Shape.Box)
                    return new Bounds(position, serialized.radius.floatValue * Vector3.up);
                return default;
            }

            public static void DrawTouchupAdditionalContent(SerializedProbeTouchupVolume serialized, Editor owner)
            {
            }


            static ProbeTouchupVolumeUI()
            {
                Inspector = CED.Group(
                    CED.FoldoutGroup(Styles.s_VolumeHeader, Expandable.Volume, k_ExpandedState,
                        (serialized, owner) => DrawVolumeContent(serialized, owner)),
                    CED.AdditionalPropertiesFoldoutGroup(Styles.s_TouchupHeader, Expandable.Touchup, k_ExpandedState, AdditionalProperties.Touchup, k_AdditionalPropertiesState,
                        CED.Group((serialized, owner) => DrawTouchupContent(serialized, owner)), DrawTouchupAdditionalContent)
                );
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

        static HierarchicalSphere _ShapeSphere;
        static HierarchicalSphere s_ShapeSphere
        {
            get
            {
                if (_ShapeSphere == null)
                    _ShapeSphere = new HierarchicalSphere(Styles.k_GizmoColorBase);
                return _ShapeSphere;
            }
        }

        protected void OnEnable()
        {
            m_SerializedTouchupVolume = new SerializedProbeTouchupVolume(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                ProbeVolumeEditor.APVDisabledHelpBox();
                return;
            }

            serializedObject.Update();
            ProbeTouchupVolumeUI.Inspector.Draw(m_SerializedTouchupVolume, this);
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeTouchupVolume touchupVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(touchupVolume.transform.position, touchupVolume.transform.rotation, Vector3.one)))
            {
                if (touchupVolume.shape == ProbeTouchupVolume.Shape.Box)
                {
                    s_ShapeBox.center = Vector3.zero;
                    s_ShapeBox.size = touchupVolume.size;
                    s_ShapeBox.SetBaseColor(ProbeTouchupColorPreferences.GetColorPrefProbeVolumeGizmoColor());
                    s_ShapeBox.DrawHull(true);
                }
                else if (touchupVolume.shape == ProbeTouchupVolume.Shape.Sphere)
                {
                    s_ShapeSphere.center = Vector3.zero;
                    s_ShapeSphere.radius = touchupVolume.radius;
                    s_ShapeSphere.DrawHull(true);
                }

                if (touchupVolume.mode == ProbeTouchupVolume.Mode.ApplyVirtualOffset)
                {
                    ArrowHandle(0, Quaternion.Euler(touchupVolume.virtualOffsetRotation), touchupVolume.virtualOffsetDistance);
                }
            }
        }

        static void ArrowHandle(int controlID, Quaternion rotation, float distance)
        {
            Handles.matrix *= Matrix4x4.Rotate(rotation);

            float thickness = Handles.lineThickness;
            float coneSize = .05f;

            var linePos = (distance - coneSize) * Vector3.forward;
            var conePos = (distance - coneSize * 0.5f) * Vector3.forward;

            if (distance < coneSize)
            {
                conePos = coneSize * 0.5f * Vector3.forward;
                Handles.matrix *= Matrix4x4.Scale(new Vector3(1, 1, distance / coneSize));
            }

            Handles.DrawLine(Vector3.zero, linePos, thickness);
            Handles.ConeHandleCap(controlID, conePos, Quaternion.identity, coneSize, EventType.Repaint);
        }

        protected void OnSceneGUI()
        {
            ProbeTouchupVolume touchupVolume = target as ProbeTouchupVolume;

            var position = Quaternion.Inverse(touchupVolume.transform.rotation) * touchupVolume.transform.position;

            //important: if the origin of the handle's space move along the handle,
            //handles displacement will appears as moving two time faster.
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, touchupVolume.transform.rotation, Vector3.one)))
            {
                if (touchupVolume.shape == ProbeTouchupVolume.Shape.Box)
                {
                    //contained must be initialized in all case
                    s_ShapeBox.center = position;
                    s_ShapeBox.size = touchupVolume.size;

                    s_ShapeBox.monoHandle = false;
                    EditorGUI.BeginChangeCheck();
                    s_ShapeBox.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { touchupVolume, touchupVolume.transform }, "Change Adjustment Volume Bounding Box");

                        touchupVolume.size = s_ShapeBox.size;
                        Vector3 delta = touchupVolume.transform.rotation * s_ShapeBox.center - touchupVolume.transform.position;
                        touchupVolume.transform.position += delta; ;
                    }
                }
                else if (touchupVolume.shape == ProbeTouchupVolume.Shape.Sphere)
                {
                    s_ShapeSphere.center = position;
                    s_ShapeSphere.radius = touchupVolume.radius;

                    EditorGUI.BeginChangeCheck();
                    s_ShapeSphere.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(touchupVolume, "Change Adjustment Volume Radius");
                        touchupVolume.radius = s_ShapeSphere.radius;
                    }
                }

                if (touchupVolume.mode == ProbeTouchupVolume.Mode.ApplyVirtualOffset && EditMode.editMode == Styles.VirtualOffsetEditMode)
                {
                    EditorGUI.BeginChangeCheck();
                    Quaternion rotation = Handles.RotationHandle(Quaternion.Euler(touchupVolume.virtualOffsetRotation), position);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(touchupVolume, "Change Virtual Offset Direction");
                        touchupVolume.virtualOffsetRotation = rotation.eulerAngles;
                    }
                }
            }
        }
    }
}
