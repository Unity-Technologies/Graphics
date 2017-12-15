using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    using CED = CoreEditorDrawer<HDReflectionProbeEditor.UIState, HDReflectionProbeEditor.SerializedReflectionProbe>;

    partial class HDReflectionProbeEditor
    {
        #region Sections
        static readonly CED.IDrawer[] k_PrimarySection =
        {
            CED.Action(Drawer_ReflectionProbeMode),
            CED.FadeGroup((s, p, o, i) => s.GetModeFaded((ReflectionProbeMode)i),
                true,
                CED.noop,                                      // Baked
                CED.Action(Drawer_ModeSettingsRealtime),      // Realtime
                CED.Action(Drawer_ModeSettingsCustom)         // Custom
            ),
            CED.space,
            CED.Action(Drawer_InfluenceShape),
            //CED.Action(Drawer_IntensityMultiplier),
            CED.space,
            CED.Action(Drawer_Toolbar),
            CED.space
        };

        static readonly CED.IDrawer k_InfluenceVolumeSection = CED.FoldoutGroup(
            "Influence volume settings",
            (s, p, o) => p.blendDistance,
            true,
            CED.Action(Drawer_DistanceBlend),
            CED.FadeGroup(
                (s, p, o, i) => s.GetShapeFaded((ReflectionInfluenceShape)i),
                false,
                CED.Action(Drawer_InfluenceBoxSettings),      // Box
                CED.Action(Drawer_InfluenceSphereSettings)    // Sphere
            )/*,
            CED.Action(Drawer_UseSeparateProjectionVolume)*/
        );

        static readonly CED.IDrawer k_SeparateProjectionVolumeSection = CED.FadeGroup(
            (s, p, o, i) => s.useSeparateProjectionVolumeDisplay.faded,
            false,
            CED.FoldoutGroup(
                "Reprojection volume settings",
                (s, p, o) => p.useSeparateProjectionVolume,
                true,
                CED.FadeGroup(
                    (s, p, o, i) => s.GetShapeFaded((ReflectionInfluenceShape)i),
                    false,
                    CED.Action(Drawer_ProjectionBoxSettings), // Box
                    CED.Action(Drawer_ProjectionSphereSettings) // Sphere
                )
            )
        );

        static readonly CED.IDrawer k_CaptureSection = CED.FoldoutGroup(
            "Capture settings",
            (s, p, o) => p.shadowDistance,
            true,
            CED.Action(Drawer_CaptureSettings)
        );

        static readonly CED.IDrawer k_AdditionalSection = CED.FoldoutGroup(
            "Additional settings",
            (s, p, o) => p.dimmer,
            true,
            CED.Action(Drawer_AdditionalSettings)
        );

        static readonly CED.IDrawer k_BakingActions = CED.Action(Drawer_BakeActions);
        #endregion

        static void Drawer_CaptureSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            var renderPipelineAsset = (HDRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
            p.resolution.intValue = renderPipelineAsset.globalTextureSettings.reflectionCubemapSize;
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("Resolution"), CoreEditorUtils.GetContent(p.resolution.intValue.ToString()));

            EditorGUILayout.PropertyField(p.shadowDistance);
            EditorGUILayout.PropertyField(p.cullingMask);
            EditorGUILayout.PropertyField(p.useOcclusionCulling);
            EditorGUILayout.PropertyField(p.nearClip);
            EditorGUILayout.PropertyField(p.farClip);
        }

        static void Drawer_AdditionalSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.dimmer);

            if (p.so.targetObjects.Length == 1)
            {
                var probe = (ReflectionProbe)p.so.targetObject;
                if (probe.mode == ReflectionProbeMode.Custom && probe.customBakedTexture != null)
                {
                    var cubemap = probe.customBakedTexture as Cubemap;
                    if (cubemap && cubemap.mipmapCount == 1)
                        EditorGUILayout.HelpBox("No mipmaps in the cubemap, Smoothness value in Standard shader will be ignored.", MessageType.Warning);
                }
            }
        }

        static readonly string[] k_BakeCustomOptionText = { "Bake as new Cubemap..." };
        static readonly string[] k_BakeButtonsText = { "Bake All Reflection Probes" };
        static void Drawer_BakeActions(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            if (p.mode.intValue == (int)ReflectionProbeMode.Realtime)
            {
                EditorGUILayout.HelpBox("Baking of this reflection probe should be initiated from the scripting API because the type is 'Realtime'", MessageType.Info);

                if (!QualitySettings.realtimeReflectionProbes)
                    EditorGUILayout.HelpBox("Realtime reflection probes are disabled in Quality Settings", MessageType.Warning);
                return;
            }

            if (p.mode.intValue == (int)ReflectionProbeMode.Baked && UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.OnDemand)
            {
                EditorGUILayout.HelpBox("Baking of this reflection probe is automatic because this probe's type is 'Baked' and the Lighting window is using 'Auto Baking'. The cubemap created is stored in the GI cache.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            switch ((ReflectionProbeMode)p.mode.intValue)
            {
                case ReflectionProbeMode.Custom:
                {
                    if (ButtonWithDropdownList(
                        CoreEditorUtils.GetContent("Bake|Bakes Reflection Probe's cubemap, overwriting the existing cubemap texture asset (if any)."), k_BakeCustomOptionText, 
                        data =>
                        {
                            var mode = (int)data;

                            var probe = p.so.targetObject as ReflectionProbe;
                            if (mode == 0)
                            {
                                BakeCustomReflectionProbe(probe, false, true);
                                ResetProbeSceneTextureInMaterial(probe);
                            }
                        },
                        GUILayout.ExpandWidth(true)))
                    {
                        var probe = (ReflectionProbe)p.so.targetObject;
                        BakeCustomReflectionProbe(probe, true, true);
                        ResetProbeSceneTextureInMaterial(probe);
                        GUIUtility.ExitGUI();
                        }
                    break;
                }

                case ReflectionProbeMode.Baked:
                    {
                        GUI.enabled = ((ReflectionProbe)p.so.targetObject).enabled;

                        // Bake button in non-continous mode
                        if (ButtonWithDropdownList(
                            CoreEditorUtils.GetContent("Bake"),
                            k_BakeButtonsText,
                            data =>
                            {
                                var mode = (int)data;
                                if (mode == 0)
                                {
                                    BakeAllReflectionProbesSnapshots();
                                    ResetAllProbeSceneTextureInMaterial();
                                }
                            },
                            GUILayout.ExpandWidth(true)))
                        {
                            var probe = (ReflectionProbe)p.so.targetObject;
                            BakeReflectionProbeSnapshot(probe);
                            ResetProbeSceneTextureInMaterial(probe);
                            GUIUtility.ExitGUI();
                        }

                        GUI.enabled = true;
                        break;
                    }

                case ReflectionProbeMode.Realtime:
                    // Not showing bake button in realtime
                    break;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        #region Influence Volume
        static void Drawer_DistanceBlend(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.Slider(p.blendDistance, 0, CalculateMaxBlendDistance(s, p, owner), CoreEditorUtils.GetContent("Blend Distance|Area around the probe where it is blended with other probes. Only used in deferred probes."));
            EditorGUI.BeginChangeCheck();
        }

        static void Drawer_InfluenceBoxSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p.boxSize, CoreEditorUtils.GetContent("Box Size|The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object."));
            EditorGUILayout.PropertyField(p.boxOffset, CoreEditorUtils.GetContent("Box Offset|The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object."));
            EditorGUILayout.PropertyField(p.boxProjection, CoreEditorUtils.GetContent("Box Projection|Box projection causes reflections to appear to change based on the object's position within the probe's box, while still using a single probe as the source of the reflection. This works well for reflections on objects that are moving through enclosed spaces such as corridors and rooms. Setting box projection to False and the cubemap reflection will be treated as coming from infinitely far away. Note that this feature can be globally disabled from Graphics Settings -> Tier Settings"));

            if (EditorGUI.EndChangeCheck())
            {
                var center = p.boxOffset.vector3Value;
                var size = p.boxSize.vector3Value;
                if (ValidateAABB((ReflectionProbe)p.so.targetObject, ref center, ref size))
                {
                    p.boxOffset.vector3Value = center;
                    p.boxSize.vector3Value = size;
                }
            }
        }

        static void Drawer_InfluenceSphereSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.influenceSphereRadius, CoreEditorUtils.GetContent("Radius"));
        }
        #endregion

        #region Projection Volume
        static void Drawer_UseSeparateProjectionVolume(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.useSeparateProjectionVolume);
            s.useSeparateProjectionVolumeDisplay.target = p.useSeparateProjectionVolume.boolValue;
        }

        static void Drawer_ProjectionBoxSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.boxReprojectionVolumeSize);
            EditorGUILayout.PropertyField(p.boxReprojectionVolumeCenter);
        }

        static void Drawer_ProjectionSphereSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.sphereReprojectionVolumeRadius);
        }
        #endregion

        #region Field Drawers
        static readonly GUIContent[] k_Content_ReflectionProbeMode = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_Content_ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        static void Drawer_ReflectionProbeMode(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.mode.hasMultipleDifferentValues;
            EditorGUILayout.IntPopup(p.mode, k_Content_ReflectionProbeMode, k_Content_ReflectionProbeModeValues, CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting)."));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                s.SetModeTarget(p.mode.intValue);
                foreach (var targetObject in p.so.targetObjects)
                    ResetProbeSceneTextureInMaterial((ReflectionProbe)targetObject);
            }
        }

        static void Drawer_InfluenceShape(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.influenceShape.hasMultipleDifferentValues;
            EditorGUILayout.PropertyField(p.influenceShape, CoreEditorUtils.GetContent("Shape"));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                s.SetShapeTarget(p.influenceShape.intValue);
        }

        static void Drawer_IntensityMultiplier(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.intensityMultiplier, CoreEditorUtils.GetContent("Intensity"));
        }
        #endregion

        #region Toolbar
        static readonly EditMode.SceneViewEditMode[] k_Toolbar_SceneViewEditModes = 
        {
            EditMode.SceneViewEditMode.ReflectionProbeBox,
            //EditMode.SceneViewEditMode.GridBox,
            EditMode.SceneViewEditMode.ReflectionProbeOrigin
        };
        static GUIContent[] s_Toolbar_Contents = null;
        static GUIContent[] toolbar_Contents
        {
            get
            {
                return s_Toolbar_Contents ?? (s_Toolbar_Contents = new []
                {
                    EditorGUIUtility.IconContent("EditCollider", "|Modify the influence volume of the reflection probe."),
                    //EditorGUIUtility.IconContent("PreMatCube", "|Modify the projection volume of the reflection probe."),
                    EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects.")
                });
            }
        }
        static void Drawer_Toolbar(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            if (p.so.targetObjects.Length > 1)
                return;

            // Show the master tool selector
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            var oldEditMode = EditMode.editMode;

            Func<Bounds> getBounds = () =>
            {
                var bounds = new Bounds();
                foreach (var targetObject in p.so.targetObjects)
                {
                    var rp = (ReflectionProbe)targetObject;
                    var b = rp.bounds;
                    bounds.Encapsulate(b);
                }
                return bounds;
            };

            EditMode.DoInspectorToolbar(k_Toolbar_SceneViewEditModes, toolbar_Contents, getBounds, owner);

            //if (GUILayout.Button(EditorGUIUtility.IconContent("Navigation", "|Fit the reflection probe volume to the surrounding colliders.")))
            //    s.AddOperation(Operation.FitVolumeToSurroundings);

            if (oldEditMode != EditMode.editMode)
            {
                switch (EditMode.editMode)
                {
                    case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                        s.AddOperation(Operation.UpdateOldLocalSpace);
                        break;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Mode Specific Settings
        static void Drawer_ModeSettingsCustom(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderDynamicObjects, CoreEditorUtils.GetContent("Dynamic Objects|If enabled dynamic objects are also rendered into the cubemap"));

            p.customBakedTexture.objectReferenceValue = EditorGUILayout.ObjectField(CoreEditorUtils.GetContent("Cubemap"), p.customBakedTexture.objectReferenceValue, typeof(Cubemap), false);
        }

        static void Drawer_ModeSettingsRealtime(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.refreshMode, CoreEditorUtils.GetContent("Refresh Mode|Controls how this probe refreshes in the Player"));
            EditorGUILayout.PropertyField(p.timeSlicingMode, CoreEditorUtils.GetContent("Time Slicing|If enabled this probe will update over several frames, to help reduce the impact on the frame rate"));
        }
        #endregion

        static float CalculateMaxBlendDistance(UIState s, SerializedReflectionProbe p, Editor o)
        {
            var shape = (ReflectionInfluenceShape)p.influenceShape.intValue;
            switch (shape)
            {
                case ReflectionInfluenceShape.Sphere:
                    return p.influenceSphereRadius.floatValue * 0.5f;
                default:
                case ReflectionInfluenceShape.Box:
                {
                    var size = p.boxSize.vector3Value;
                    var v = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
                    return v * 0.5f;
                }
            }
        }

        static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new [] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
        static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
        }
    }
}
