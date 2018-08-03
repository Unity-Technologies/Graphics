using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<HDReflectionProbeUI, SerializedHDReflectionProbe>;
    using _ = CoreEditorUtils;

    internal partial class HDReflectionProbeUI
    {
        static HDReflectionProbeUI()
        {
            Inspector = new[]
            {
                SectionPrimarySettings,
                SectionProxyVolumeSettings,
                CED.Select(
                        (s, d, o) => s.influenceVolume,
                        (s, d, o) => d.influenceVolume,
                        InfluenceVolumeUI.SectionFoldoutShape
                        ),
                SectionInfluenceProxyMismatch,
                SectionCaptureSettings,
                SectionAdditionalSettings,
                ButtonBake
            };
        }

        public static readonly CED.IDrawer[] Inspector;

        public static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
                CED.Action(Drawer_Toolbar),
                CED.space,
                CED.Action(Drawer_ReflectionProbeMode),
                CED.space,
                CED.FadeGroup((s, p, o, i) => s.IsSectionExpandedMode((ReflectionProbeMode)i),
                    FadeOption.Indent,
                    CED.noop,                                  // Baked
                    CED.Action(Drawer_ModeSettingsRealtime),  // Realtime
                    CED.Action(Drawer_ModeSettingsCustom)     // Custom
                    )
                );

        public static readonly CED.IDrawer SectionProxyVolumeSettings = CED.FoldoutGroup(
                "Proxy Volume",
                (s, p, o) => s.isSectionExpandedProxyVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_ProxyVolume),
                CED.space,
                CED.Action(Drawer_ProjectionSettings)
                );

        public static readonly CED.IDrawer SectionInfluenceVolumeSettings = CED.FoldoutGroup(
                "Influence Volume",
                (s, p, o) => s.isSectionExpandedInfluenceVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_InfluenceAdvancedSwitch),
                CED.space,
                CED.Action(Drawer_InfluenceShape),
                CED.space,
                CED.Action(Drawer_InfluenceAreas)
                );

        public static readonly CED.IDrawer SectionInfluenceProxyMismatch = CED.Action(Drawer_InfluenceProxyMissmatch);

        public static readonly CED.IDrawer SectionCaptureSettings = CED.FoldoutGroup(
                "Capture Settings",
                (s, p, o) => s.isSectionExpandedCaptureSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_CaptureSettings)
                );

        public static readonly CED.IDrawer SectionAdditionalSettings = CED.FoldoutGroup(
                "Artistic Settings",
                (s, p, o) => s.isSectionExpandedAdditional,
                FoldoutOption.Indent,
                CED.Action(Drawer_AdditionalSettings)
                );

        public static readonly CED.IDrawer ButtonBake = CED.Action(Drawer_BakeActions);

        static void Drawer_CaptureSettings(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            var renderPipelineAsset = (HDRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
            p.resolution.intValue = (int)renderPipelineAsset.GetRenderPipelineSettings().lightLoopSettings.reflectionCubemapSize;
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("Resolution"), CoreEditorUtils.GetContent(p.resolution.intValue.ToString()));

            EditorGUILayout.PropertyField(p.shadowDistance, CoreEditorUtils.GetContent("Shadow Distance"));
            EditorGUILayout.PropertyField(p.cullingMask, CoreEditorUtils.GetContent("Culling Mask"));
            EditorGUILayout.PropertyField(p.useOcclusionCulling, CoreEditorUtils.GetContent("Use Occlusion Culling"));
            EditorGUILayout.PropertyField(p.nearClip, CoreEditorUtils.GetContent("Near Clip"));
            EditorGUILayout.PropertyField(p.farClip, CoreEditorUtils.GetContent("Far Clip"));
        }

        static void Drawer_AdditionalSettings(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.weight, CoreEditorUtils.GetContent("Influence Volume Weight|Blending weight to use while interpolating between influence volume. (Reminder: Sky is an Influence Volume too)."));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p.multiplier, CoreEditorUtils.GetContent("Multiplier|Tweeking option to enhance reflection."));
            if (EditorGUI.EndChangeCheck())
                p.multiplier.floatValue = Mathf.Max(0.0f, p.multiplier.floatValue);

            if (p.so.targetObjects.Length == 1)
            {
                var probe = p.target;
                if (probe.mode == ReflectionProbeMode.Custom && probe.customBakedTexture != null)
                {
                    var cubemap = probe.customBakedTexture as Cubemap;
                    if (cubemap && cubemap.mipmapCount == 1)
                        EditorGUILayout.HelpBox("No mipmaps in the cubemap, Smoothness value in Standard shader will be ignored.", MessageType.Warning);
                }
            }
        }

        static void Drawer_BakeActions(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)p.mode.intValue, p.target);
        }

        static void Drawer_ProxyVolume(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.proxyVolumeComponent, _.GetContent("Proxy Volume"));

            if (p.proxyVolumeComponent.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                        "When no Proxy setted, Influence shape will be used as Proxy shape too.",
                        MessageType.Info,
                        true
                        );
            }
        }

        #region Influence Volume
        static void Drawer_InfluenceProxyMissmatch(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            if (p.proxyVolumeComponent.objectReferenceValue != null)
            {
                var proxy = (ReflectionProxyVolumeComponent)p.proxyVolumeComponent.objectReferenceValue;
                if ((int)proxy.proxyVolume.shape != p.influenceVolume.shape.enumValueIndex)
                    EditorGUILayout.HelpBox(
                        "Proxy volume and influence volume have different shape types, this is not supported.",
                        MessageType.Error,
                        true
                        );
            }
        }

        static void Drawer_InfluenceBoxSettings(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            bool advanced = p.influenceVolume.editorAdvancedModeEnabled.boolValue;
            var maxBlendDistance = p.boxSize.vector3Value * 0.5f;

            EditorGUILayout.BeginHorizontal();
            Drawer_AdvancedBlendDistance(
                p,
                false,
                maxBlendDistance,
                CoreEditorUtils.GetContent("Blend Distance|Area around the probe where it is blended with other probes. Only used in deferred probes.")
                );
            if (GUILayout.Button(toolbar_Contents[1], GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((advanced ? 3 : 1) * (EditorGUIUtility.singleLineHeight + 3))))
            {
                EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[1], GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Drawer_AdvancedBlendDistance(
                p,
                true,
                maxBlendDistance,
                CoreEditorUtils.GetContent("Blend Normal Distance|Area around the probe where the normals influence the probe. Only used in deferred probes.")
                );
            if (GUILayout.Button(toolbar_Contents[2], GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.MinHeight(22f), GUILayout.MaxHeight((advanced ? 3 : 1) * (EditorGUIUtility.singleLineHeight + 3))))
            {
                EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[2], GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();

            if (advanced)
            {
                CoreEditorUtils.DrawVector6(
                    CoreEditorUtils.GetContent("Face fade|Fade faces of the cubemap."),
                    p.influenceVolume.boxSideFadePositive, p.influenceVolume.boxSideFadeNegative, Vector3.zero, Vector3.one, HDReflectionProbeEditor.k_handlesColor);
            }
        }

        static void Drawer_AdvancedBlendDistance(SerializedHDReflectionProbe p, bool isNormal, Vector3 maxBlendDistance, GUIContent content)
        {
            SerializedProperty blendDistancePositive = isNormal ? p.influenceVolume.boxBlendNormalDistancePositive : p.influenceVolume.boxBlendDistancePositive;
            SerializedProperty blendDistanceNegative = isNormal ? p.influenceVolume.boxBlendNormalDistanceNegative : p.influenceVolume.boxBlendDistanceNegative;
            SerializedProperty editorAdvancedModeBlendDistancePositive = isNormal ? p.influenceVolume.editorAdvancedModeBlendNormalDistancePositive : p.influenceVolume.editorAdvancedModeBlendDistancePositive;
            SerializedProperty editorAdvancedModeBlendDistanceNegative = isNormal ? p.influenceVolume.editorAdvancedModeBlendNormalDistanceNegative : p.influenceVolume.editorAdvancedModeBlendDistanceNegative;
            SerializedProperty editorSimplifiedModeBlendDistance = isNormal ? p.influenceVolume.editorSimplifiedModeBlendNormalDistance : p.influenceVolume.editorSimplifiedModeBlendDistance;
            Vector3 bdp = blendDistancePositive.vector3Value;
            Vector3 bdn = blendDistanceNegative.vector3Value;

            EditorGUILayout.BeginVertical();

            if (p.influenceVolume.editorAdvancedModeEnabled.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                CoreEditorUtils.DrawVector6(
                    content,
                    editorAdvancedModeBlendDistancePositive, editorAdvancedModeBlendDistanceNegative, Vector3.zero, maxBlendDistance, HDReflectionProbeEditor.k_handlesColor);
                if(EditorGUI.EndChangeCheck())
                {
                    blendDistancePositive.vector3Value = editorAdvancedModeBlendDistancePositive.vector3Value;
                    blendDistanceNegative.vector3Value = editorAdvancedModeBlendDistanceNegative.vector3Value;
                    p.Apply();
                }
            }
            else
            {
                float distance = editorSimplifiedModeBlendDistance.floatValue;
                EditorGUI.BeginChangeCheck();
                distance = EditorGUILayout.FloatField(content, distance);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 decal = Vector3.one * distance;
                    bdp.x = Mathf.Clamp(decal.x, 0f, maxBlendDistance.x);
                    bdp.y = Mathf.Clamp(decal.y, 0f, maxBlendDistance.y);
                    bdp.z = Mathf.Clamp(decal.z, 0f, maxBlendDistance.z);
                    bdn.x = Mathf.Clamp(decal.x, 0f, maxBlendDistance.x);
                    bdn.y = Mathf.Clamp(decal.y, 0f, maxBlendDistance.y);
                    bdn.z = Mathf.Clamp(decal.z, 0f, maxBlendDistance.z);
                    blendDistancePositive.vector3Value = bdp;
                    blendDistanceNegative.vector3Value = bdn;
                    editorSimplifiedModeBlendDistance.floatValue = distance;
                    p.Apply();
                }
            }

            GUILayout.EndVertical();
        }

        static void Drawer_InfluenceSphereSettings(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            var maxBlendDistance = p.influenceVolume.sphereRadius.floatValue;

            var blendDistance = p.influenceVolume.boxBlendDistancePositive.vector3Value.x;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.influenceVolume.boxBlendDistancePositive.hasMultipleDifferentValues;
            blendDistance = EditorGUILayout.Slider(CoreEditorUtils.GetContent("Blend Distance|Area around the probe where it is blended with other probes. Only used in deferred probes."), blendDistance, 0, maxBlendDistance);
            if (EditorGUI.EndChangeCheck())
            {
                p.influenceVolume.boxBlendDistancePositive.vector3Value = Vector3.one * blendDistance;
                p.influenceVolume.boxBlendDistanceNegative.vector3Value = Vector3.one * blendDistance;
            }
            if (GUILayout.Button(toolbar_Contents[1], GUILayout.Width(28f), GUILayout.Height(EditorGUIUtility.singleLineHeight + 3)))
            {
                EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[1], GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();

            var blendNormalDistance = p.influenceVolume.boxBlendNormalDistancePositive.vector3Value.x;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.influenceVolume.boxBlendNormalDistancePositive.hasMultipleDifferentValues;
            blendNormalDistance = EditorGUILayout.Slider(CoreEditorUtils.GetContent("Blend Normal Distance|Area around the probe where the normals influence the probe. Only used in deferred probes."), blendNormalDistance, 0, maxBlendDistance);
            if (EditorGUI.EndChangeCheck())
            {
                p.influenceVolume.boxBlendNormalDistancePositive.vector3Value = Vector3.one * blendNormalDistance;
                p.influenceVolume.boxBlendNormalDistanceNegative.vector3Value = Vector3.one * blendNormalDistance;
            }
            if (GUILayout.Button(toolbar_Contents[2], GUILayout.Width(28f), GUILayout.Height(EditorGUIUtility.singleLineHeight + 3)))
            {
                EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[2], GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.showMixedValue = false;
        }

        #endregion

        #region Field Drawers
        static readonly GUIContent[] k_Content_ReflectionProbeMode = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_Content_ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        static void Drawer_ReflectionProbeMode(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.mode.hasMultipleDifferentValues;
            EditorGUILayout.IntPopup(p.mode, k_Content_ReflectionProbeMode, k_Content_ReflectionProbeModeValues, CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting)."));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                s.SetModeTarget(p.mode.intValue);
            }
        }

        static void Drawer_InfluenceAdvancedSwitch(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = p.influenceVolume.editorAdvancedModeEnabled.boolValue;
                advanced = !GUILayout.Toggle(!advanced, CoreEditorUtils.GetContent("Normal|Normal parameters mode (only change for box shape)."), EditorStyles.miniButtonLeft, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                advanced = GUILayout.Toggle(advanced, CoreEditorUtils.GetContent("Advanced|Advanced parameters mode (only change for box shape)."), EditorStyles.miniButtonRight, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                s.alternativeBoxBlendHandle.allHandleControledByOne = s.alternativeBoxBlendNormalHandle.allHandleControledByOne = !advanced;
                if (p.influenceVolume.editorAdvancedModeEnabled.boolValue ^ advanced)
                {
                    p.influenceVolume.editorAdvancedModeEnabled.boolValue = advanced;
                    if (advanced)
                    {
                        p.influenceVolume.boxBlendDistancePositive.vector3Value = p.influenceVolume.editorAdvancedModeBlendDistancePositive.vector3Value;
                        p.influenceVolume.boxBlendDistanceNegative.vector3Value = p.influenceVolume.editorAdvancedModeBlendDistanceNegative.vector3Value;
                        p.influenceVolume.boxBlendNormalDistancePositive.vector3Value = p.influenceVolume.editorAdvancedModeBlendNormalDistancePositive.vector3Value;
                        p.influenceVolume.boxBlendNormalDistanceNegative.vector3Value = p.influenceVolume.editorAdvancedModeBlendNormalDistanceNegative.vector3Value;
                    }
                    else
                    {
                        p.influenceVolume.boxBlendDistanceNegative.vector3Value = p.influenceVolume.boxBlendDistancePositive.vector3Value = Vector3.one * p.influenceVolume.editorSimplifiedModeBlendDistance.floatValue;
                        p.influenceVolume.boxBlendNormalDistanceNegative.vector3Value = p.influenceVolume.boxBlendNormalDistancePositive.vector3Value = Vector3.one * p.influenceVolume.editorSimplifiedModeBlendNormalDistance.floatValue;
                    }
                    p.Apply();
                }
            }
        }

        static void Drawer_InfluenceShape(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.influenceVolume.shape.hasMultipleDifferentValues;
            EditorGUILayout.PropertyField(p.influenceVolume.shape, CoreEditorUtils.GetContent("Shape"));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                s.SetShapeTarget(p.influenceVolume.shape.intValue);

            switch ((InfluenceShape)p.influenceVolume.shape.enumValueIndex)
            {
                case InfluenceShape.Box:
                    Drawer_InfluenceShapeBoxSettings(s, p, owner);
                    break;
                case InfluenceShape.Sphere:
                    Drawer_InfluenceShapeSphereSettings(s, p, owner);
                    break;
            }

        }

        static void Drawer_InfluenceShapeBoxSettings(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(p.boxSize, CoreEditorUtils.GetContent("Box Size|The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object."));
            if (GUILayout.Button(toolbar_Contents[0], GUILayout.Width(28f), GUILayout.Height(EditorGUIUtility.singleLineHeight + 3)))
            {
                EditMode.ChangeEditMode(EditMode.SceneViewEditMode.ReflectionProbeBox, GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(p.boxOffset, CoreEditorUtils.GetContent("Box Offset|The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object."));
            if (GUILayout.Button(toolbar_Contents[3], GUILayout.Width(28f), GUILayout.Height(EditorGUIUtility.singleLineHeight + 3)))
            {
                EditMode.ChangeEditMode(EditMode.SceneViewEditMode.ReflectionProbeOrigin, GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                var center = p.boxOffset.vector3Value;
                var size = p.boxSize.vector3Value;
                if (HDReflectionProbeEditorUtility.ValidateAABB(p.target, ref center, ref size))
                {
                    //clamp to contains object center instead of resizing
                    Vector3 projector = (center - p.boxOffset.vector3Value).normalized;
                    p.boxOffset.vector3Value = center + Mathf.Abs(Vector3.Dot((p.boxSize.vector3Value - size) * .5f, projector)) * projector;
                }
            }
        }

        static void Drawer_InfluenceShapeSphereSettings(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(p.influenceVolume.sphereRadius, CoreEditorUtils.GetContent("Radius"));
            if (GUILayout.Button(toolbar_Contents[0], GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.Height(EditorGUIUtility.singleLineHeight + 3)))
            {
                EditMode.ChangeEditMode(EditMode.SceneViewEditMode.ReflectionProbeBox, GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(p.boxOffset, CoreEditorUtils.GetContent("Sphere Offset|The center of the sphere in which the reflections will be applied to objects. The value is relative to the position of the Game Object."));
            if (GUILayout.Button(toolbar_Contents[3], GUILayout.ExpandHeight(true), GUILayout.Width(28f), GUILayout.Height(EditorGUIUtility.singleLineHeight + 3)))
            {
                EditMode.ChangeEditMode(EditMode.SceneViewEditMode.ReflectionProbeOrigin, GetBoundsGetter(p)(), owner);
            }
            EditorGUILayout.EndHorizontal();
        }

        static void Drawer_InfluenceAreas(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            if (s.IsSectionExpandedShape(InfluenceShape.Box).value)
            {
                Drawer_InfluenceBoxSettings(s, p, owner);
            }
            if (s.IsSectionExpandedShape(InfluenceShape.Sphere).value)
            {
                Drawer_InfluenceSphereSettings(s, p, owner);
            }
        }

        static void Drawer_ProjectionSettings(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.boxProjection, CoreEditorUtils.GetContent("Parallax Correction|Parallax Correction causes reflections to appear to change based on the object's position within the probe's box, while still using a single probe as the source of the reflection. This works well for reflections on objects that are moving through enclosed spaces such as corridors and rooms. Setting Parallax Correction to False and the cubemap reflection will be treated as coming from infinitely far away. Note that this feature can be globally disabled from Graphics Settings -> Tier Settings"));
        }

        static void Drawer_IntensityMultiplier(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.intensityMultiplier, CoreEditorUtils.GetContent("Intensity"));
        }

        #endregion

        #region Toolbar
        static readonly EditMode.SceneViewEditMode[] k_Toolbar_SceneViewEditModes =
        {
            EditMode.SceneViewEditMode.ReflectionProbeBox,
            EditMode.SceneViewEditMode.GridBox,
            EditMode.SceneViewEditMode.Collider,
            EditMode.SceneViewEditMode.ReflectionProbeOrigin
        };
        static GUIContent[] s_Toolbar_Contents = null;
        static GUIContent[] toolbar_Contents
        {
            get
            {
                return s_Toolbar_Contents ?? (s_Toolbar_Contents = new[]
                {
                    EditorGUIUtility.IconContent("EditCollider", "|Modify the extents of the reflection probe. (SHIFT+1)"),
                    EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume of the reflection probe. (SHIFT+2)"),
                    EditorGUIUtility.IconContent("SceneViewOrtho", "|Modify the influence normal volume of the reflection probe. (SHIFT+3)"),
                    EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects.")
                });
            }
        }
        static void Drawer_Toolbar(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            if (p.so.targetObjects.Length > 1)
                return;

            // Show the master tool selector
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            var oldEditMode = EditMode.editMode;

            EditMode.DoInspectorToolbar(k_Toolbar_SceneViewEditModes, toolbar_Contents, GetBoundsGetter(p), owner);

            //if (GUILayout.Button(EditorGUIUtility.IconContent("Navigation", "|Fit the reflection probe volume to the surrounding colliders.")))
            //    s.AddOperation(Operation.FitVolumeToSurroundings);

            if (oldEditMode != EditMode.editMode)
            {
                switch (EditMode.editMode)
                {
                    case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                        s.UpdateOldLocalSpace(p.target);
                        break;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static public void Drawer_ToolBarButton(int buttonIndex, Editor owner, params GUILayoutOption[] styles)
        {
            if (GUILayout.Button(toolbar_Contents[buttonIndex], styles))
            {
                EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[buttonIndex], GetBoundsGetter(owner)(), owner);
            }
        }

        static Func<Bounds> GetBoundsGetter(SerializedHDReflectionProbe p)
        {
            return () =>
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
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in o.targets)
                    {
                        var rp = targetObject.transform;
                        var b = rp.position;
                        bounds.Encapsulate(b);
                    }
                    return bounds;
                };
        }

        static readonly KeyCode[] k_ShortCutKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
        };
        public static void DoShortcutKey(SerializedHDReflectionProbe p, Editor o)
        {
            var evt = Event.current;
            if (evt.type != EventType.KeyDown || !evt.shift)
                return;

            for (var i = 0; i < k_ShortCutKeys.Length; ++i)
            {
                if (evt.keyCode == k_ShortCutKeys[i])
                {
                    var mode = EditMode.editMode == k_Toolbar_SceneViewEditModes[i]
                        ? EditMode.SceneViewEditMode.None
                        : k_Toolbar_SceneViewEditModes[i];
                    EditMode.ChangeEditMode(mode, GetBoundsGetter(p)(), o);
                    evt.Use();
                    break;
                }
            }
        }

        #endregion

        #region Mode Specific Settings
        static void Drawer_ModeSettingsCustom(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderDynamicObjects, CoreEditorUtils.GetContent("Dynamic Objects|If enabled dynamic objects are also rendered into the cubemap"));

            EditorGUI.showMixedValue = p.customBakedTexture.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var customBakedTexture = EditorGUILayout.ObjectField(CoreEditorUtils.GetContent("Cubemap"), p.customBakedTexture.objectReferenceValue, typeof(Cubemap), false);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                p.customBakedTexture.objectReferenceValue = customBakedTexture;
        }

        static void Drawer_ModeSettingsRealtime(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.refreshMode, CoreEditorUtils.GetContent("Refresh Mode|Controls how this probe refreshes in the Player"));
            EditorGUILayout.PropertyField(p.timeSlicingMode, CoreEditorUtils.GetContent("Time Slicing|If enabled this probe will update over several frames, to help reduce the impact on the frame rate"));
        }

        #endregion

        static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
        static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
        }
    }
}
