using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditorInternal;

using RuntimeSRPPreferences = UnityEngine.Rendering.CoreRenderPipelinePreferences;

namespace UnityEditor.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeAdjustmentVolume>;

    internal class ProbeAdjustmentColorPreferences
    {
        internal static Func<Color> GetColorPrefProbeVolumeGizmoColor;
        internal static Color s_ProbeAdjustmentVolumeGizmoColorDefault = new Color32(222, 132, 144, 45);

        static ProbeAdjustmentColorPreferences()
        {
            GetColorPrefProbeVolumeGizmoColor = RuntimeSRPPreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Probe Adjustment Volume Gizmo", s_ProbeAdjustmentVolumeGizmoColorDefault);
        }

    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeAdjustmentVolume))]
    internal class ProbeAdjustmentVolumeEditor : Editor
    {
        internal static class Styles
        {
            internal static readonly GUIContent s_VORotateTool = EditorGUIUtility.TrIconContent("RotateTool", "The virtual offset direction for probes falling in this volume.");
            internal static readonly GUIContent s_SORotateTool = EditorGUIUtility.TrIconContent("RotateTool", "The direction used to sample the ambient probe for probes falling in this volume.");
            internal static readonly GUIContent s_VolumeHeader = EditorGUIUtility.TrTextContent("Influence Volume");
            internal static readonly GUIContent s_AdjustmentHeader = EditorGUIUtility.TrTextContent("Probe Volume Overrides");

            internal static readonly GUIContent s_Mode = new GUIContent("Mode", "Choose which type of adjustment to apply to probes covered by this volume.");
            internal static readonly GUIContent s_DilationThreshold = new GUIContent("Dilation Validity Threshold", "Override the Dilation Validity Threshold for probes covered by this Probe Adjustment Volume. Higher values increase the chance of probes being considered invalid.");
            internal static readonly GUIContent virtualOffsetThreshold = new GUIContent("Validity Threshold", "Override the Virtual Offset Validity Threshold for probes covered by this Probe Adjustment Volume. Higher values increase the chance of probes being considered invalid.");
            internal static readonly GUIContent s_VODirection = new GUIContent("Rotation", "Rotate the axis along which probes will be pushed when applying Virtual Offset.");
            internal static readonly GUIContent s_VODistance = new GUIContent("Distance", "Determines how far probes are pushed in the direction of the Virtual Offset.");
            internal static readonly GUIContent renderingLayerMaskOperation = new GUIContent("Operation", "The operation to combine the Rendering Layer Mask set by this adjustment volume with the Rendering Layer Mask of the probes covered by this volume.");
            internal static readonly GUIContent renderingLayerMask = new GUIContent("Rendering Layer Mask", "Sets the Rendering Layer Mask to be combined with the Rendering Layer Mask of the probes covered by this volume.");
            internal static readonly GUIContent s_PreviewLighting = new GUIContent("Preview Probe Adjustments", "Quickly preview the effect of adjustments on probes covered by this volume.");

            internal static readonly GUIContent skyOcclusionSampleCount = new GUIContent("Sample Count", "Controls the number of samples per probe for sky occlusion baking.");
            internal static readonly GUIContent skyOcclusionMaxBounces = new GUIContent("Max Bounces", "Controls the number of bounces per light path for sky occlusion baking.");

            internal static readonly string s_AdjustmentVolumeChangedMessage = "This Adjustment Volume has never been baked, or has changed since the last bake. Re-bake Probe Volumes to ensure lighting data is valid.";

            internal static readonly EditMode.SceneViewEditMode VirtualOffsetEditMode = (EditMode.SceneViewEditMode)110;
            internal static readonly EditMode.SceneViewEditMode SkyDirectionEditMode = (EditMode.SceneViewEditMode)110;

            internal static readonly Color k_GizmoColorBase = ProbeAdjustmentColorPreferences.s_ProbeAdjustmentVolumeGizmoColorDefault;

            internal static readonly Color[] k_BaseHandlesColor = new Color[]
            {
                ProbeAdjustmentColorPreferences.s_ProbeAdjustmentVolumeGizmoColorDefault,
                ProbeAdjustmentColorPreferences.s_ProbeAdjustmentVolumeGizmoColorDefault,
                ProbeAdjustmentColorPreferences.s_ProbeAdjustmentVolumeGizmoColorDefault,
                ProbeAdjustmentColorPreferences.s_ProbeAdjustmentVolumeGizmoColorDefault,
                ProbeAdjustmentColorPreferences.s_ProbeAdjustmentVolumeGizmoColorDefault,
                ProbeAdjustmentColorPreferences.s_ProbeAdjustmentVolumeGizmoColorDefault
            };
        }

        static class ProbeAdjustmentVolumeUI
        {
            public static readonly CED.IDrawer Inspector = null;

            enum AdditionalProperties
            {
                Adjustments = 1 << 0,
            }
            enum Expandable
            {
                Volume = 1 << 0,
                Adjustments = 1 << 1,
            }

            readonly static ExpandedState<Expandable, ProbeAdjustmentVolume> k_ExpandedState = new ExpandedState<Expandable, ProbeAdjustmentVolume>(Expandable.Volume | Expandable.Adjustments);
            readonly static AdditionalPropertiesState<AdditionalProperties, ProbeAdjustmentVolume> k_AdditionalPropertiesState = new AdditionalPropertiesState<AdditionalProperties, ProbeAdjustmentVolume>(0);

            public static void DrawVolumeContent(SerializedProbeAdjustmentVolume serialized, Editor owner)
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

            public static void DrawAdjustmentContent(SerializedProbeAdjustmentVolume serialized, Editor owner)
            {
                ProbeAdjustmentVolume ptv = (owner.target as ProbeAdjustmentVolume);

                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(ptv.gameObject.scene);
                bool useVirtualOffset = bakingSet != null ? bakingSet.settings.virtualOffsetSettings.useVirtualOffset : false;
                bool useSkyOcclusion = bakingSet != null ? bakingSet.skyOcclusion : false;

                var hiddenMode = (int)ProbeAdjustmentVolume.Mode.IntensityScale;
                var availableValues = (int[])Enum.GetValues(typeof(ProbeAdjustmentVolume.Mode));
                var availableModes = CastArray(Enum.GetNames(typeof(ProbeAdjustmentVolume.Mode)));
                if (!k_AdditionalPropertiesState[AdditionalProperties.Adjustments] && serialized.mode.intValue != hiddenMode)
                {
                    int idx = Array.IndexOf(availableValues, hiddenMode);
                    availableValues = RemoveAt(availableValues, idx);
                    availableModes = RemoveAt(availableModes, idx);
                }

                EditorGUI.BeginChangeCheck();
                int newValue = EditorGUILayout.IntPopup(Styles.s_Mode, serialized.mode.intValue, availableModes, availableValues);
                if (EditorGUI.EndChangeCheck())
                    serialized.mode.intValue = newValue;

                if (serialized.mode.intValue == (int)ProbeAdjustmentVolume.Mode.OverrideValidityThreshold)
                {
                    EditorGUILayout.PropertyField(serialized.overriddenDilationThreshold, Styles.s_DilationThreshold);
                }
                else if (serialized.mode.intValue == (int)ProbeAdjustmentVolume.Mode.ApplyVirtualOffset)
                {
                    EditorGUI.BeginDisabledGroup(!useVirtualOffset);
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(serialized.virtualOffsetRotation, Styles.s_VODirection);

                    var editMode = Styles.VirtualOffsetEditMode;
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Toggle(editMode == EditMode.editMode, Styles.s_VORotateTool, EditorStyles.miniButton, GUILayout.Width(28f));
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
                else if (serialized.mode.intValue == (int)ProbeAdjustmentVolume.Mode.OverrideVirtualOffsetSettings)
                {
                    EditorGUI.BeginDisabledGroup(!useVirtualOffset);
                    EditorGUILayout.PropertyField(serialized.virtualOffsetThreshold, Styles.virtualOffsetThreshold);
                    EditorGUILayout.PropertyField(serialized.geometryBias);
                    EditorGUILayout.PropertyField(serialized.rayOriginBias);
                    EditorGUI.EndDisabledGroup();

                    if (!useVirtualOffset)
                    {
                        CoreEditorUtils.DrawFixMeBox("Override Virtual Offset can be used only if Virtual Offset is enabled for the Baking Set.", MessageType.Warning, "Open", () =>
                        {
                            ProbeVolumeLightingTab.OpenBakingSet(bakingSet);
                        });
                    }
                }
                else if (serialized.mode.intValue == (int)ProbeAdjustmentVolume.Mode.OverrideSampleCount)
                {
                    EditorGUILayout.LabelField("Probes", EditorStyles.miniBoldLabel);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serialized.directSampleCount);
                        EditorGUILayout.PropertyField(serialized.indirectSampleCount);
                        EditorGUILayout.PropertyField(serialized.sampleCountMultiplier);
                        EditorGUILayout.PropertyField(serialized.maxBounces);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Sky Occlusion", EditorStyles.miniBoldLabel);
                    using (new EditorGUI.DisabledGroupScope(bakingSet != null && !bakingSet.skyOcclusion))
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serialized.skyOcclusionSampleCount, Styles.skyOcclusionSampleCount);
                        EditorGUILayout.PropertyField(serialized.skyOcclusionMaxBounces, Styles.skyOcclusionMaxBounces);
                    }
                }
                else if (serialized.mode.intValue == (int)ProbeAdjustmentVolume.Mode.IntensityScale)
                {
                    EditorGUILayout.PropertyField(serialized.intensityScale);
                    EditorGUILayout.HelpBox("Overriding the intensity of probes can break the physical plausibility of lighting. This may result in unwanted visual inconsistencies.", MessageType.Info, wide: true);
                }
				else if (serialized.mode.intValue == (int)ProbeAdjustmentVolume.Mode.OverrideSkyDirection)
                {
                    if (!SupportedRenderingFeatures.active.skyOcclusion)
                    {
                        EditorGUILayout.HelpBox("Sky Occlusion is not supported by this Render Pipeline.", MessageType.Warning);
                        return;
                    }

                    var editMode = Styles.SkyDirectionEditMode;

                    EditorGUILayout.BeginHorizontal();
                    using (new EditorGUI.DisabledScope(!SupportedRenderingFeatures.active.skyOcclusion))
                    {
                        using (new EditorGUI.DisabledScope(editMode == EditMode.editMode))
                            EditorGUILayout.PropertyField(serialized.skyDirection);

                        EditorGUI.BeginChangeCheck();
                        GUILayout.Toggle(editMode == EditMode.editMode, Styles.s_SORotateTool, EditorStyles.miniButton, GUILayout.Width(28f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditMode.SceneViewEditMode targetMode = EditMode.editMode == editMode ? EditMode.SceneViewEditMode.None : editMode;
                            EditMode.ChangeEditMode(targetMode, GetBounds(serialized, owner), owner);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (!useSkyOcclusion)
                    {
                        CoreEditorUtils.DrawFixMeBox("Overriding Sky Direction can be used only if Sky Occlusion is enabled for the Baking Set.", MessageType.Warning, "Open", () =>
                        {
                            ProbeVolumeLightingTab.OpenBakingSet(bakingSet);
                        });
                    }
                }
                else if (serialized.mode.intValue == (int)ProbeAdjustmentVolume.Mode.OverrideRenderingLayerMask)
                {
                    if (bakingSet != null && !bakingSet.useRenderingLayers)
                    {
                        CoreEditorUtils.DrawFixMeBox("Override Rendering Layer can be used only if Rendering Layers are enabled for the Baking Set.", MessageType.Warning, "Open", () =>
                        {
                            ProbeVolumeLightingTab.OpenBakingSet(bakingSet);
                        });
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serialized.renderingLayerMaskOperation, Styles.renderingLayerMaskOperation);

                        string[] options;
                        if (bakingSet != null)
                        {
                            options = new string[bakingSet.renderingLayerMasks.Length];
                            for (int i = 0; i < bakingSet.renderingLayerMasks.Length; i++)
                                options[i] = bakingSet.renderingLayerMasks[i].name;
                        }
                        else
                        {
                            options = new string[APVDefinitions.probeMaxRegionCount];
                            for (int i = 0; i < APVDefinitions.probeMaxRegionCount; i++)
                                options[i] = "Mask " + (i + 1);
                        }

                        EditorGUI.BeginChangeCheck();
                        int newMask = EditorGUILayout.MaskField(Styles.renderingLayerMask, serialized.renderingLayerMask.intValue, options);
                        if (EditorGUI.EndChangeCheck())
                             serialized.renderingLayerMask.uintValue = (uint)newMask;
                    }
                }
            }

            static void DrawBakingHelpers(SerializedProbeAdjustmentVolume p, Editor owner)
            {
                ProbeAdjustmentVolume ptv = owner.target as ProbeAdjustmentVolume;
                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(ptv.gameObject.scene);

                if (owner.targets.Length == 1)
                {
                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledScope(Lightmapping.isRunning || bakingSet == null))
                    {
                        using (new EditorGUI.DisabledScope(AdaptiveProbeVolumes.isRunning))
                            if (GUILayout.Button(Styles.s_PreviewLighting))
                                AdaptiveProbeVolumes.BakeAdjustmentVolume(bakingSet, ptv);

                        ProbeVolumeLightingTab.BakeAPVButton();
                    }
                }

                if (ptv.cachedHashCode != ptv.GetHashCode())
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(Styles.s_AdjustmentVolumeChangedMessage, MessageType.Warning);
                }
            }

            internal static Bounds GetBounds(SerializedProbeAdjustmentVolume serialized, Editor owner)
            {
                var position = ((Component)owner.target).transform.position;
                if (serialized.shape.intValue == (int)ProbeAdjustmentVolume.Shape.Box)
                    return new Bounds(position, serialized.size.vector3Value);
                if (serialized.shape.intValue == (int)ProbeAdjustmentVolume.Shape.Box)
                    return new Bounds(position, serialized.radius.floatValue * Vector3.up);
                return default;
            }

            public static void DrawAdditionalContent(SerializedProbeAdjustmentVolume serialized, Editor owner)
            {
            }

            static ProbeAdjustmentVolumeUI()
            {
                Inspector = CED.Group(
                    CED.FoldoutGroup(Styles.s_VolumeHeader, Expandable.Volume, k_ExpandedState,
                        (serialized, owner) => DrawVolumeContent(serialized, owner)),
                    CED.AdditionalPropertiesFoldoutGroup(Styles.s_AdjustmentHeader, Expandable.Adjustments, k_ExpandedState, AdditionalProperties.Adjustments, k_AdditionalPropertiesState,
                        CED.Group((serialized, owner) => DrawAdjustmentContent(serialized, owner)), DrawAdditionalContent),
                    CED.Group(null, GroupOption.None, DrawBakingHelpers)
                );
            }
        }


        SerializedProbeAdjustmentVolume m_SerializedAdjustmentVolume;
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
            m_SerializedAdjustmentVolume = new SerializedProbeAdjustmentVolume(serializedObject);
            ProbeVolumeDebug.s_ActiveAdjustmentVolumes++;
        }

        protected void OnDisable()
        {
            ProbeVolumeDebug.s_ActiveAdjustmentVolumes--;
        }

        public override void OnInspectorGUI()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                ProbeVolumeEditor.APVDisabledHelpBox();
                return;
            }

            ProbeVolumeEditor.FrameSettingDisabledHelpBox();

            serializedObject.Update();
            ProbeAdjustmentVolumeUI.Inspector.Draw(m_SerializedAdjustmentVolume, this);
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeAdjustmentVolume adjustmentVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(adjustmentVolume.transform.position, adjustmentVolume.transform.rotation, Vector3.one)))
            {
                if (adjustmentVolume.shape == ProbeAdjustmentVolume.Shape.Box)
                {
                    s_ShapeBox.center = Vector3.zero;
                    s_ShapeBox.size = adjustmentVolume.size;
                    s_ShapeBox.SetBaseColor(ProbeAdjustmentColorPreferences.GetColorPrefProbeVolumeGizmoColor());
                    s_ShapeBox.DrawHull(true);
                }
                else if (adjustmentVolume.shape == ProbeAdjustmentVolume.Shape.Sphere)
                {
                    s_ShapeSphere.center = Vector3.zero;
                    s_ShapeSphere.radius = adjustmentVolume.radius;
                    s_ShapeSphere.DrawHull(true);
                }

                if (adjustmentVolume.mode == ProbeAdjustmentVolume.Mode.ApplyVirtualOffset)
                {
                    ArrowHandle(0, Quaternion.Euler(adjustmentVolume.virtualOffsetRotation), adjustmentVolume.virtualOffsetDistance);
                }

            }
            using (new Handles.DrawingScope(Matrix4x4.TRS(adjustmentVolume.transform.position, Quaternion.identity, Vector3.one)))
            {
                if (adjustmentVolume.mode == ProbeAdjustmentVolume.Mode.OverrideSkyDirection)
                {
                    var editMode = Styles.SkyDirectionEditMode;
                    if (editMode != EditMode.editMode)
                    {
                        var quat = Quaternion.FromToRotation(Vector3.forward, adjustmentVolume.skyDirection);
                        adjustmentVolume.skyShadingDirectionRotation = quat.eulerAngles;
                    }

                    ArrowHandle(0, Quaternion.Euler(adjustmentVolume.skyShadingDirectionRotation), 1.0f);
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
            ProbeAdjustmentVolume adjustmentVolume = target as ProbeAdjustmentVolume;

            var position = Quaternion.Inverse(adjustmentVolume.transform.rotation) * adjustmentVolume.transform.position;

            //important: if the origin of the handle's space move along the handle,
            //handles displacement will appears as moving two time faster.
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, adjustmentVolume.transform.rotation, Vector3.one)))
            {
                if (adjustmentVolume.shape == ProbeAdjustmentVolume.Shape.Box)
                {
                    //contained must be initialized in all case
                    s_ShapeBox.center = position;
                    s_ShapeBox.size = adjustmentVolume.size;

                    s_ShapeBox.monoHandle = false;
                    EditorGUI.BeginChangeCheck();
                    s_ShapeBox.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { adjustmentVolume, adjustmentVolume.transform }, "Change Adjustment Volume Bounding Box");

                        adjustmentVolume.size = s_ShapeBox.size;
                        Vector3 delta = adjustmentVolume.transform.rotation * s_ShapeBox.center - adjustmentVolume.transform.position;
                        adjustmentVolume.transform.position += delta; ;
                    }
                }
                else if (adjustmentVolume.shape == ProbeAdjustmentVolume.Shape.Sphere)
                {
                    s_ShapeSphere.center = position;
                    s_ShapeSphere.radius = adjustmentVolume.radius;

                    EditorGUI.BeginChangeCheck();
                    s_ShapeSphere.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(adjustmentVolume, "Change Adjustment Volume Radius");
                        adjustmentVolume.radius = s_ShapeSphere.radius;
                    }
                }

                if (adjustmentVolume.mode == ProbeAdjustmentVolume.Mode.ApplyVirtualOffset && EditMode.editMode == Styles.VirtualOffsetEditMode)
                {
                    EditorGUI.BeginChangeCheck();
                    Quaternion rotation = Handles.RotationHandle(Quaternion.Euler(adjustmentVolume.virtualOffsetRotation), position);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(adjustmentVolume, "Change Virtual Offset Direction");
                        adjustmentVolume.virtualOffsetRotation = rotation.eulerAngles;
                    }
                }
            }
            if (adjustmentVolume.mode == ProbeAdjustmentVolume.Mode.OverrideSkyDirection && EditMode.editMode == Styles.SkyDirectionEditMode)
            {
                EditorGUI.BeginChangeCheck();

                Quaternion rotation = Handles.RotationHandle(Quaternion.Euler(adjustmentVolume.skyShadingDirectionRotation), adjustmentVolume.transform.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(adjustmentVolume, "Change Sky Shading Direction");
                    adjustmentVolume.skyShadingDirectionRotation = rotation.eulerAngles;
                    adjustmentVolume.skyDirection = rotation * Vector3.forward;
                    adjustmentVolume.skyDirection.Normalize();
                }
            }
        }

        [MenuItem("CONTEXT/ProbeAdjustmentVolume/Rendering Debugger...")]
        internal static void AddAdjustmentVolumeContextMenu()
        {
            ProbeVolumeLightingTab.OpenProbeVolumeDebugPanel(null, null, 0);
        }
    }
}
