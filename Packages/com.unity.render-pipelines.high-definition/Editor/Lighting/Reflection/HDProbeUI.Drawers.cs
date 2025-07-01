using System;
using System.Linq;
using UnityEditorInternal;
using System.Reflection;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDProbeUI
    {
        [Flags]
        internal enum ToolBar
        {
            None = 0,
            InfluenceShape = 1 << 0,
            Blend = 1 << 1,
            NormalBlend = 1 << 2,
            CapturePosition = 1 << 3,
            MirrorPosition = 1 << 4,
            MirrorRotation = 1 << 5
        }

        internal interface IProbeUISettingsProvider
        {
            ProbeSettingsOverride displayedCaptureSettings { get; }
            ProbeSettingsOverride displayedAdvancedCaptureSettings { get; }
            ProbeSettingsOverride displayedCustomSettings { get; }
            Type customTextureType { get; }
        }

        // Constants
        internal const EditMode.SceneViewEditMode EditBaseShape = (EditMode.SceneViewEditMode)100;
        internal const EditMode.SceneViewEditMode EditInfluenceShape = (EditMode.SceneViewEditMode)101;
        internal const EditMode.SceneViewEditMode EditInfluenceNormalShape = (EditMode.SceneViewEditMode)102;
        internal const EditMode.SceneViewEditMode EditCapturePosition = (EditMode.SceneViewEditMode)103;
        internal const EditMode.SceneViewEditMode EditMirrorPosition = (EditMode.SceneViewEditMode)104;
        internal const EditMode.SceneViewEditMode EditMirrorRotation = (EditMode.SceneViewEditMode)105;
        //Note: EditMode.SceneViewEditMode.ReflectionProbeOrigin is still used
        //by legacy reflection probe and have its own mecanism that we don't want

        // Probe Setting Mode cache
        static readonly GUIContent[] k_ModeContents = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_ModeValues = { (int)ProbeSettings.Mode.Baked, (int)ProbeSettings.Mode.Custom, (int)ProbeSettings.Mode.Realtime };

        internal struct Drawer<TProvider>
            where TProvider : struct, IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
        {
            // Drawers
            public static void DrawPrimarySettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();

#if !ENABLE_BAKED_PLANAR
                if (serialized is SerializedPlanarReflectionProbe)
                {
                    serialized.probeSettings.mode.intValue = (int)ProbeSettings.Mode.Realtime;
                }
                else
                {
#endif

                    // Probe Mode
                    EditorGUILayout.IntPopup(serialized.probeSettings.mode, k_ModeContents, k_ModeValues, k_BakeTypeContent);

#if !ENABLE_BAKED_PLANAR
                }

#endif

                switch ((ProbeSettings.Mode)serialized.probeSettings.mode.intValue)
                {
                    case ProbeSettings.Mode.Realtime:
                    {
                        EditorGUILayout.PropertyField(serialized.probeSettings.realtimeMode);
                        if ((ProbeSettings.ProbeType)serialized.probeSettings.type.intValue == ProbeSettings.ProbeType.ReflectionProbe)
                            EditorGUILayout.PropertyField(serialized.probeSettings.timeSlicing, k_TimeSlicingContent);
                        break;
                    }
                    case ProbeSettings.Mode.Custom:
                    {
                        Rect lineRect = EditorGUILayout.GetControlRect(true, 64);
                        EditorGUI.BeginProperty(lineRect, k_CustomTextureContent, serialized.customTexture);
                        {
                            EditorGUI.BeginChangeCheck();
                            var customTexture = EditorGUI.ObjectField(lineRect, k_CustomTextureContent, serialized.customTexture.objectReferenceValue, provider.customTextureType, false);
                            if (EditorGUI.EndChangeCheck())
                                serialized.customTexture.objectReferenceValue = customTexture;
                        }
                        EditorGUI.EndProperty();
                        break;
                    }
                }
            }

            public static void DrawCaptureSettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(serialized.probeSettings, owner, provider.displayedCaptureSettings);
            }

            public static void DrawCaptureSettingsAdditionalProperties(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(serialized.probeSettings, owner, provider.displayedAdvancedCaptureSettings);
            }

            public static void DrawCustomSettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(serialized.probeSettings, owner, provider.displayedCustomSettings);
            }

            public static void DrawInfluenceSettings(SerializedHDProbe serialized, Editor owner)
            {
                InfluenceVolumeUI.Draw<TProvider>(serialized.probeSettings.influence, owner);
            }

            public static void DrawProjectionSettings(SerializedHDProbe serialized, Editor owner)
            {
                EditorGUILayout.PropertyField(serialized.proxyVolume, k_ProxyVolumeContent);

                if (serialized.target.proxyVolume == null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serialized.probeSettings.proxyUseInfluenceVolumeAsProxyVolume);
                    if (EditorGUI.EndChangeCheck())
                        serialized.Apply();
                }

                if (serialized.proxyVolume.objectReferenceValue != null)
                {
                    var proxy = (ReflectionProxyVolumeComponent)serialized.proxyVolume.objectReferenceValue;
                    if (proxy.proxyVolume.shape != serialized.probeSettings.influence.shape.GetEnumValue<ProxyShape>()
                        && proxy.proxyVolume.shape != ProxyShape.Infinite)
                        EditorGUILayout.HelpBox(
                            k_ProxyInfluenceShapeMismatchHelpBoxText,
                            MessageType.Error,
                            true
                        );
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        serialized.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue ? k_NoProxyHelpBoxText : k_NoProxyInfiniteHelpBoxText,
                        MessageType.Info,
                        true
                    );
                }

                // Don't display distanceBasedRoughness if the projection is infinite (as in this case we force distanceBasedRoughness to be 0 in code)
                if (owner is HDReflectionProbeEditor && !(serialized.proxyVolume.objectReferenceValue == null && serialized.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue == false))
                    EditorGUILayout.PropertyField(serialized.probeSettings.distanceBasedRoughness, EditorGUIUtility.TrTextContent("Distance Based Roughness", "When enabled, HDRP uses the assigned Proxy Volume to calculate distance based roughness for reflections. This produces more physically-accurate results if the Proxy Volume closely matches the environment."));
            }

            static readonly string[] k_BakeCustomOptionText = { "Bake as new Cubemap..." };
            static readonly string[] k_BakeButtonsText = { "Bake All Reflection Probes" };
            public static void DrawBakeButton(SerializedHDProbe serialized, Editor owner)
            {
                // Disable baking of multiple probes with different modes
                if (serialized.probeSettings.mode.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox(
                        "Baking is not possible when selecting probe with different modes",
                        MessageType.Info
                    );
                    return;
                }


                //Display a warning for the user if we are not in the quality setting with the highest resolution setting for reflection probes
                if (!IsHighestSettingForCubeResolution())
                {
                    EditorGUILayout.HelpBox(
                        "You are currently not in the highest quality setting, if you bake now the reflection probe resolution will be lower than it should be for higher quality levels.",
                        MessageType.Warning);
                }

                // Check if current mode support baking
                var mode = (ProbeSettings.Mode)serialized.probeSettings.mode.intValue;
                var doesModeSupportBaking = mode == ProbeSettings.Mode.Custom || mode == ProbeSettings.Mode.Baked;
                if (!doesModeSupportBaking)
                    return;

                var probeType = (ProbeSettings.ProbeType)serialized.probeSettings.type.intValue;

                // Check if all scene are saved to a file (requirement to bake probes)
                foreach (var target in serialized.serializedObject.targetObjects)
                {
                    var comp = (Component)target;
                    var go = comp.gameObject;
                    var scene = go.scene;
                    if (string.IsNullOrEmpty(scene.path))
                    {
                        EditorGUILayout.HelpBox(
                            "Baking is possible only if all open scenes are saved on disk. " +
                            "Please save the scenes before baking.",
                            MessageType.Info
                        );
                        return;
                    }
                }

                switch (mode)
                {
                    case ProbeSettings.Mode.Custom:
                    {
                        if (serialized.target.IsTurnedOff())
                        {
                            EditorGUILayout.HelpBox("The Resolution of this Probe has been set to Off, it therefore cannot be baked", MessageType.Info);
                        }
                        else
                        {
                            if (ButtonWithDropdownList(
                                    EditorGUIUtility.TrTextContent(
                                        "Bake",
                                        "Bakes Probe's texture, overwriting the existing texture asset (if any)."
                                    ),
                                    k_BakeCustomOptionText,
                                    data =>
                                    {
                                        switch ((int)data)
                                        {
                                            case 0:
                                                RenderInCustomAsset(serialized.target, false);
                                                break;
                                        }
                                    }))
                            {
                                RenderInCustomAsset(serialized.target, true);
                            }
                        }

                        break;
                    }
                    case ProbeSettings.Mode.Baked:
                    {
#pragma warning disable 618
                        if (UnityEditor.Lightmapping.giWorkflowMode
                            != UnityEditor.Lightmapping.GIWorkflowMode.OnDemand)
                        {
                            EditorGUILayout.HelpBox("Baking of this probe is automatic because this probe's type is 'Baked' and the Lighting window is using 'Auto Baking'. The texture created is stored in the GI cache.", MessageType.Info);
                            break;
                        }
#pragma warning restore 618
                        GUI.enabled = serialized.target.enabled;

                        if (serialized.target.IsTurnedOff())
                        {
                            EditorGUILayout.HelpBox("The Resolution of this Probe has been set to Off, it therefore cannot be baked", MessageType.Info);
                        }
                        else
                        {

                            // Bake button in non-continous mode
                            if (ButtonWithDropdownList(
                                    EditorGUIUtility.TrTextContent("Bake"),
                                    k_BakeButtonsText,
                                    data =>
                                    {
                                        if ((int)data == 0)
                                        {
                                            var system = ScriptableBakedReflectionSystemSettings.system;
                                            system.BakeAllReflectionProbes();
                                        }
                                    },
                                    GUILayout.ExpandWidth(true)))
                            {
                                if (HDBakedReflectionSystem.AreAllOpenedSceneSaved())
                                {
                                    HDBakedReflectionSystem.BakeProbes(serialized.serializedObject.targetObjects
                                    .OfType<HDProbe>().ToArray());
                                    GUIUtility.ExitGUI();
                                }
                                else
                                {
                                    Debug.LogError($"Opened scenes are not saved. Please save before baking.");
                                }
                            }

                            GUI.enabled = true;

                            var staticLightingSky = SkyManager.GetStaticLightingSky();
                            if (staticLightingSky != null && staticLightingSky.profile != null)
                            {
                                var skyType = staticLightingSky.staticLightingSkyUniqueID == 0
                                    ? "no Sky"
                                    : SkyManager.skyTypesDict[staticLightingSky.staticLightingSkyUniqueID].Name
                                        .ToString();
                                var cloudType = staticLightingSky.staticLightingCloudsUniqueID == 0
                                    ? "no Clouds"
                                    : SkyManager.cloudTypesDict[staticLightingSky.staticLightingCloudsUniqueID].Name
                                        .ToString();
                                EditorGUILayout.HelpBox(
                                    $"Static Lighting Sky uses {skyType} and {cloudType} of profile {staticLightingSky.profile.name}.",
                                    MessageType.Info);
                            }
                        }

                        break;
                    }
                    case ProbeSettings.Mode.Realtime:
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            public static void DrawSHNormalizationStatus(SerializedHDProbe serialized, Editor owner)
            {
                if (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.lightProbeSystem != RenderPipelineSettings.LightProbeSystem.AdaptiveProbeVolumes)
                    return;

                const string kResolution = " Please ensure that probe positions are valid (not inside static geometry) then bake lighting to regenerate data.";
                const string kMixedMode = "Unable to show normalization data validity when selecting probes with different modes.";
                const string kMixedValidity = "Baked reflection probe normalization data is partially invalid." + kResolution;
                const string kValid = "Baked reflection probe normalization data is valid.";
                const string kInvalid = "Baked reflection probe normalization data is invalid." + kResolution;

                var spMode = serialized.serializedObject.FindProperty("m_ProbeSettings.mode");
                var spValid = serialized.serializedObject.FindProperty("m_HasValidSHForNormalization");

                if (spMode.intValue != (int)ProbeSettings.Mode.Baked)
                    return;

                EditorGUILayout.Space();

                if (spMode.hasMultipleDifferentValues)
                    EditorGUILayout.HelpBox(kMixedMode, MessageType.Info);
                else if (spValid.hasMultipleDifferentValues)
                    EditorGUILayout.HelpBox(kMixedValidity, MessageType.Warning);
                else
                    EditorGUILayout.HelpBox(spValid.boolValue ? kValid : kInvalid, spValid.boolValue ? MessageType.Info : MessageType.Warning);
            }

            static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
            static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
            {
                return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
            }

            static void RenderInCustomAsset(HDProbe probe, bool useExistingCustomAsset)
            {
                var provider = new TProvider();

                string assetPath = null;
                if (useExistingCustomAsset && probe.customTexture != null && !probe.customTexture.Equals(null))
                    assetPath = AssetDatabase.GetAssetPath(probe.customTexture);

                if (string.IsNullOrEmpty(assetPath))
                {
                    assetPath = EditorUtility.SaveFilePanelInProject(
                        "Save custom capture",
                        probe.name, "exr",
                        "Save custom capture");
                }

                if (!string.IsNullOrEmpty(assetPath))
                {
                    var target = (RenderTexture)HDProbeSystem.CreateRenderTargetForMode(
                        probe, ProbeSettings.Mode.Custom
                    );

                    HDBakedReflectionSystem.RenderAndWriteToFile(
                        probe, assetPath, target,
                        out var cameraSettings, out var cameraPositionSettings
                    );
                    AssetDatabase.ImportAsset(assetPath);
                    HDBakedReflectionSystem.ImportAssetAt(probe, assetPath);
                    CoreUtils.Destroy(target);

                    var assetTarget = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
                    probe.SetTexture(ProbeSettings.Mode.Custom, assetTarget);
                    probe.SetRenderData(ProbeSettings.Mode.Custom, new HDProbe.RenderData(cameraSettings, cameraPositionSettings));
                    EditorUtility.SetDirty(probe);
                }
            }
        }

        static internal void Drawer_DifferentShapeError(SerializedHDProbe serialized, Editor owner)
        {
            var proxy = serialized.proxyVolume.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null
                && proxy.proxyVolume.shape != serialized.probeSettings.influence.shape.GetEnumValue<ProxyShape>()
                && proxy.proxyVolume.shape != ProxyShape.Infinite)
            {
                EditorGUILayout.HelpBox(
                    k_ProxyInfluenceShapeMismatchHelpBoxText,
                    MessageType.Error,
                    true
                );
            }
        }

        static internal bool IsHighestSettingForCubeResolution()
        {
            HDRenderPipelineAsset currentAsset = (QualitySettings.renderPipeline as HDRenderPipelineAsset);

            if(currentAsset == null)
            {
                return true;
            }

            CubeReflectionResolution highestTierInCurrent = GetHighestCubemapResolutionSetting(currentAsset.currentPlatformRenderPipelineSettings);
            CubeReflectionResolution highestResolution = highestTierInCurrent;

            //Iterate over every quality setting to check their settings for the cubemap resolution
            for (int i = 0; i < QualitySettings.count; ++i)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i) as HDRenderPipelineAsset;
                if (asset != null)
                {
                    //Iterate through reflection cube map quality tiers
                    CubeReflectionResolution highestInTier = GetHighestCubemapResolutionSetting(asset.currentPlatformRenderPipelineSettings);

                    if (highestInTier > highestResolution)
                    {
                        highestResolution = highestInTier;
                    }
                }
            }

            return highestResolution == highestTierInCurrent;
        }

        //Iterating over every CubeReflectionResolutionTier for a certain RenderPipelineSetting and return the highest value found
        static internal CubeReflectionResolution GetHighestCubemapResolutionSetting(RenderPipelineSettings settings)
        {
            CubeReflectionResolution highestTierInCurrent = CubeReflectionResolution.CubeReflectionResolution0;

            //Iterate through reflection cube map quality tiers, hardcoded to 3 tiers as they are always low, medium and high and there
            //doesnt seem to be a better way to iterate over ScalableSettings
            for (int i = 0; i < 3; i++)
            {
                var res = settings.cubeReflectionResolution[i];
                if(res > highestTierInCurrent)
                {
                    highestTierInCurrent = res;
                }
            }

            return highestTierInCurrent;
        }
    }

    [EditorTool(Description, typeof(PlanarReflectionProbe), null, (int)Mode, null)]
    internal class PlanarReflectionProbeModifyBaseShapeTool : ReflectionProbeTool
    {
        private const string Description = "Modify the base shape.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditBaseShape;
        private const string IconName = "EditShape";

        public PlanarReflectionProbeModifyBaseShapeTool() : base(Description, Mode, IconName) { }
    }

    [EditorTool(Description, typeof(PlanarReflectionProbe), null, (int)Mode, null)]
    internal class PlanarReflectionProbeModifyMirrorPositionTool : ReflectionProbeTool
    {
        private const string Description = "Change the mirror position.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditMirrorPosition;
        private const string IconName = "MoveTool";

        public PlanarReflectionProbeModifyMirrorPositionTool() : base(Description, Mode, IconName) { }
    }

    [EditorTool(Description, typeof(PlanarReflectionProbe), null, (int)Mode, null)]
    internal class PlanarReflectionProbeModifyMirrorRotationTool : ReflectionProbeTool
    {
        private const string Description = "Change the mirror rotation.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditMirrorRotation;
        private const string IconName = "RotateTool";

        public PlanarReflectionProbeModifyMirrorRotationTool() : base(Description, Mode, IconName) { }
    }

    [EditorTool(Description, typeof(PlanarReflectionProbe), null, (int)Mode, null)]
    internal class PlanarReflectionProbeModifyEditInfluenceShapeTool : ReflectionProbeTool
    {
        private const string Description = "Modify the influence volume blend distance.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditInfluenceShape;
        private const string IconName = "BlendDistance";

        public PlanarReflectionProbeModifyEditInfluenceShapeTool() : base(Description, Mode, IconName) { }
    }

    [EditorTool(Description, typeof(ReflectionProbe), null, (int)Mode, null)]
    internal class ReflectionProbeModifyBaseShapeTool : ReflectionProbeTool
    {
        private const string Description = "Modify the base shape.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditBaseShape;
        private const string IconName = "EditShape";

        public ReflectionProbeModifyBaseShapeTool() : base(Description, Mode, IconName) {}
    }

    [EditorTool(Description, typeof(ReflectionProbe), null, (int)Mode, null)]
    internal class ReflectionProbeModifyEditInfluenceShapeTool : ReflectionProbeTool
    {
        private const string Description = "Modify the influence volume blend distance.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditInfluenceShape;
        private const string IconName = "BlendDistance";

        public ReflectionProbeModifyEditInfluenceShapeTool() : base(Description, Mode, IconName) { }
    }

    [EditorTool(Description, typeof(ReflectionProbe), null, (int)Mode, null)]
    internal class ReflectionProbeModifyInfluenceNormalShapeTool : ReflectionProbeTool
    {
        private const string Description = "Modify the influence volume normal blend distance.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditInfluenceNormalShape;
        private const string IconName = "NormalBlendDistance";

        public ReflectionProbeModifyInfluenceNormalShapeTool() : base(Description, Mode, IconName) { }
    }

    [EditorTool(Description, typeof(ReflectionProbe), null, (int)Mode, null)]
    internal class ReflectionProbeModifyCapturePositionTool : ReflectionProbeTool
    {
        private const string Description = "Change the capture position.";
        private const EditMode.SceneViewEditMode Mode = HDProbeUI.EditCapturePosition;
        private const string IconName = "CapturePosition";

        protected ReflectionProbeModifyCapturePositionTool() : base(Description, Mode, IconName) { }
    }

    internal class ReflectionProbeTool : EditorTool
    {
        private readonly string _description;
        private readonly EditMode.SceneViewEditMode _mode;
        private readonly string _iconName;
        private GUIContent _iconContent;
        private bool _wasDeactivated;

        protected ReflectionProbeTool(string description, EditMode.SceneViewEditMode mode, string iconName)
        {
            _description = description;
            _mode = mode;
            _iconName = iconName;
        }

        public override GUIContent toolbarIcon => _iconContent;
        public override void OnWillBeDeactivated() => _wasDeactivated = true;
        public override void OnToolGUI(EditorWindow window)
        {
            Bounds bounds = target switch
            {
                PlanarReflectionProbe planarProbe => planarProbe.bounds,
                ReflectionProbe reflectionProbe => reflectionProbe.bounds,
                _ => default
            };
            if (bounds == default)
                return;
            if (EditMode.editMode == _mode && !_wasDeactivated)
                return;

            EditMode.ChangeEditMode(_mode, bounds);
            ToolManager.SetActiveTool(this);
            _wasDeactivated = false;
        }

        private void OnEnable() => _iconContent = EditorGUIUtility.TrIconContent(_iconName, _description);
    }
}
