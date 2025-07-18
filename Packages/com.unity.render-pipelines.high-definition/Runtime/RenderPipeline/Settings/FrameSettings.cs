using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Helper to handle Deferred or Forward but not both</summary>
    public enum LitShaderMode
    {
        /// <summary>Lit shader uses forward rendering.</summary>
        Forward,
        /// <summary>Lit shader uses deferred rendering.</summary>
        Deferred
    }

    /// <summary>
    /// Defines how the LODBias value is computed.
    /// </summary>
    public enum LODBiasMode
    {
        /// <summary>Use the current quality settings value.</summary>
        FromQualitySettings,
        /// <summary>Scale the current quality settings value.</summary>
        ScaleQualitySettings,
        /// <summary>Set the current quality settings value.</summary>
        OverrideQualitySettings,
    }

    /// <summary>
    /// Defines how the MaximumLOD is computed.
    /// </summary>
    public enum MaximumLODLevelMode
    {
        /// <summary>Use the current quality settings value.</summary>
        FromQualitySettings,
        /// <summary>Offset the current quality settings value.</summary>
        OffsetQualitySettings,
        /// <summary>Set the current quality settings value.</summary>
        OverrideQualitySettings,
    }

    /// <summary>
    /// Defines how the SssSampleBudget is computed.
    /// </summary>
    public enum SssQualityMode
    {
        /// <summary>Use a quality settings value.</summary>
        FromQualitySettings,
        /// <summary>Use a custom value.</summary>
        OverrideQualitySettings,
    }

    /// <summary>
    /// Defines the level of MSAA for the camera.
    /// </summary>
    public enum MSAAMode
    {
        /// <summary>No MSAA.</summary>
        None = MSAASamples.None,
        /// <summary>MSAA 2X.</summary>
        MSAA2X = MSAASamples.MSAA2x,
        /// <summary>MSAA 4X.</summary>
        MSAA4X = MSAASamples.MSAA4x,
        /// <summary>MSAA 8X.</summary>
        MSAA8X = MSAASamples.MSAA8x,
        /// <summary>Uses MSAA mode from the current HDRP asset.</summary>
        FromHDRPAsset,
    }

    /* ////// HOW TO ADD FRAME SETTINGS //////
     *
     * 1 - Add an entry in the FrameSettingsField enum with a bit that is not used.
     *     If the type is non boolean, also add a field in FrameSettings (see lodBias).
     *     Note: running unit test NoDoubleBitIndex will also give you available bit indexes.
     *
     * 2 - Add a FrameSettingsFieldAttribute to it. (Inspector UI and DebugMenu are generated from this.)
     *     i   - Give the groupIndex that correspond the area you want it displayed in the interface.
     *     ii  - Change is label by filling either autoname or displayedName.
     *     iii - Add the tooltip into tooltip.
     *     iv  - If this is not a boolean you want, you can either show it as:
     *               - 2 choice enum popup: use type: FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup, targetType: typeof(EnumType) (enum with only two value 0 and 1).
     *               - custom: FrameSettingsFieldAttribute.DisplayType.Others (can be combined with targetType).
     *     v   - Add indentation and disable state by filling positiveDependencies and/or negativeDependencies.
     *     vi  - If you want it not be sorted by its bit index, use customOrderInGroup to restart numeration at this element.
     *           You certainly need to also use customOrderInGroup on element that should appear after.
     *
     * 3 - The default value should be set for:
     *         - FrameSettings.defaultCamera
     *         - FrameSettings.defaultRealtimeReflectionProbe
     *         - FrameSettings.defaultCustomOrBakeReflectionProbe
     *     For a boolean data, its default is false. If you want it to true, add the enum value in the BitArray.
     *
     * 4 - Fill FrameSettings.Sanitize to amend the rules on supported feature.
     *
     * 5 - If there is additional rules that can disable the aspect of the field, amend the display in FrameSettingsUI.Drawer with a AmmendInfo on the OverridableFrameSettingsArea.
     *     Usually this happens if you have additional support condition or if you have a non boolean frame settings.
     *
     * 6 - If the added FrameSettings have a default value that is not the C# default value, add a migration step in HDRPAsset to upgrade it with the right value.
     *
     * /////////////////////////////////////// */
    /// <summary>
    /// Collection of settings used for rendering the frame.
    /// </summary>
    public enum FrameSettingsField
    {
        /// <summary>No Frame Settings.</summary>
        None = -1,

        //rendering settings (group 0)
        /// <summary>Specifies the Lit Shader Mode for Cameras using these Frame Settings use to render the Scene.</summary>
        [FrameSettingsField(0, autoName: LitShaderMode, type: FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup, targetType: typeof(LitShaderMode), customOrderInGroup: 0, tooltip: "Specifies the Lit Shader Mode for Cameras using these Frame Settings use to render the Scene (Depends on \"Lit Shader Mode\" in current HDRP Asset).")]
        LitShaderMode = 0,
        /// <summary>When enabled, HDRP processes a full depth prepass for Cameras using these Frame Settings. Set Lit Shader Mode to Deferred to access this option.</summary>
        [FrameSettingsField(0, displayedName: "Full Depth Prepass within Deferred", positiveDependencies: new[] { LitShaderMode }, tooltip: "When enabled, HDRP processes a full depth prepass (All meshes are sent) for Cameras using these Frame Settings. Set Lit Shader Mode to Deferred to access this option.")]
        DepthPrepassWithDeferredRendering = 1,
        /// <summary>When enabled, HDRP clear GBuffers for Cameras using these Frame Settings. Set Lit Shader Mode to Deferred to access this option.</summary>
        [FrameSettingsField(0, displayedName: "Clear GBuffers", positiveDependencies: new[] { LitShaderMode }, customOrderInGroup: 0, tooltip: "When enabled, HDRP clear GBuffers for Cameras using these Frame Settings. Set Lit Shader Mode to Deferred to access this option.")]
        ClearGBuffers = 5,
        /// <summary>When enabled, Cameras using these Frame Settings calculate MSAA when they render the Scene. Set Lit Shader Mode to Forward to access this option.</summary>
        [Obsolete("#from(2021.2)")]
        MSAA = 31,
        /// <summary>Specify the level of MSAA used when rendering the Scene. Set Lit Shader Mode to Forward to access this option.</summary>
        [FrameSettingsField(0, displayedName: "MSAA Within Forward", type: FrameSettingsFieldAttribute.DisplayType.Others, targetType: typeof(MSAAMode), customOrderInGroup: 1, tooltip: "Specifies the MSAA mode for Cameras using these Frame Settings. Set Lit Shader Mode to Forward to access this option. Note that MSAA is disabled when using ray tracing.")]
        MSAAMode = 4,
        /// <summary>When enabled, Cameras using these Frame Settings use Alpha To Mask. Activate MSAA to access this option.</summary>
        [Obsolete("#from(2022.1)")]
        [FrameSettingsField(0, displayedName: "Alpha To Mask", tooltip: "When enabled, Cameras using these Frame Settings use Alpha To Mask. Activate MSAA to access this option.")]
        AlphaToMask = 56,
        /// <summary>When enabled, Cameras using these Frame Settings render opaque GameObjects.</summary>
        [FrameSettingsField(0, autoName: OpaqueObjects, customOrderInGroup: 2, tooltip: "When enabled, Cameras using these Frame Settings render opaque GameObjects.")]
        OpaqueObjects = 2,

        /// <summary>When enabled, Cameras using these Frame Settings render Transparent GameObjects.</summary>
        [FrameSettingsField(0, autoName: TransparentObjects, customOrderInGroup: 3, tooltip: "When enabled, Cameras using these Frame Settings render Transparent GameObjects.")]
        TransparentObjects = 3,
        /// <summary>When enabled, HDRP processes a transparent prepass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: TransparentPrepass, positiveDependencies: new[] { TransparentObjects }, customOrderInGroup: 3, tooltip: "When enabled, HDRP processes a transparent prepass for Cameras using these Frame Settings.")]
        TransparentPrepass = 8,
        /// <summary>When enabled, HDRP processes a transparent postpass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: TransparentPostpass, positiveDependencies: new[] { TransparentObjects }, customOrderInGroup: 3, tooltip: "When enabled, HDRP processes a transparent postpass for Cameras using these Frame Settings.")]
        TransparentPostpass = 9,
        /// <summary>When enabled, HDRP processes a transparent pass in a lower resolution for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, displayedName: "Low Resolution Transparent", positiveDependencies: new[] { TransparentObjects }, customOrderInGroup: 3, tooltip: "When enabled, HDRP processes a transparent pass in a lower resolution for Cameras using these Frame Settings.")]
        LowResTransparent = 18,
        /// <summary>When enabled, HDRP processes a refraction render pass for Cameras using these Frame Settings. This add a resolve of ColorBuffer after the drawing of opaque materials to be use for Refraction effect during transparent pass.</summary>
        [FrameSettingsField(0, autoName: Refraction, positiveDependencies: new[] { TransparentObjects }, customOrderInGroup: 4, tooltip: "When enabled, HDRP processes a refraction render pass for Cameras using these Frame Settings. This add a resolve of ColorBuffer after the drawing of opaque materials to be use for Refraction effect during transparent pass.")]
        Refraction = 13,
        //NOTE: Obsoletes must follow the proper enums, otherwise the compiler occludes them.
        /// <summary>When enabled, HDRP processes a refraction render pass for Cameras using these Frame Settings. This add a resolve of ColorBuffer after the drawing of opaque materials to be use for Refraction effect during transparent pass.</summary>
        [Obsolete("#from(2021.1)")]
        RoughRefraction = 13,
        /// <summary>When enabled, Cameras using these Frame Settings render water surfaces.</summary>
        [FrameSettingsField(0, autoName: Water, positiveDependencies: new[] { TransparentObjects, Refraction }, customOrderInGroup: 4, tooltip: "When enabled, Cameras using these Frame Settings render water surfaces.")]
        Water = 99,
        /// <summary>When enabled, Cameras using these Frame Settings will support water deformers.</summary>
        [FrameSettingsField(0, autoName: WaterDecals, positiveDependencies: new[] { TransparentObjects, Refraction, Water }, customOrderInGroup: 4, tooltip: "When enabled, Cameras using these Frame Settings will support water decals.")]
        WaterDecals = 102,
        /// <summary>When enabled, Cameras using these Frame Settings will support water excluders.</summary>
        [FrameSettingsField(0, autoName: WaterExclusion, positiveDependencies: new[] { TransparentObjects, Refraction, Water }, customOrderInGroup: 4, tooltip: "When enabled, Cameras using these Frame Settings will support water excluders.")]
        WaterExclusion = 101,

        /// <summary>When enabled, Cameras using these Frame Settings allow generation of Texture of Thickness per GameObject.layer.</summary>
        [FrameSettingsField(0, displayedName: "Compute Thickness", customOrderInGroup: 5, tooltip: "When enabled, Cameras using these Frame Settings compute Texture of thickness per Layer.")]
        ComputeThickness = 119,
        /// <summary>When enabled, HDRP processes a decal render pass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: Decals, customOrderInGroup: 6, tooltip: "When enabled, HDRP processes a decal render pass for Cameras using these Frame Settings.")]
        Decals = 12,
        /// <summary>When enabled, Cameras that use these Frame Settings make use of DecalLayers.</summary>
        [FrameSettingsField(0, autoName: DecalLayers, customOrderInGroup: 6, positiveDependencies: new[] { Decals }, tooltip: "When enabled, Cameras that use these Frame Settings make use of DecalLayers (Depends on \"Decal Layers\" in current HDRP Asset).")]
        DecalLayers = 96,
        /// <summary>When enabled, Cameras that use these Frame Settings produce a buffer containing the Rendering Layer Mask of rendered meshes.</summary>
        [FrameSettingsField(0, autoName: RenderingLayerMaskBuffer, customOrderInGroup: 7, tooltip: "When enabled, Cameras that use these Frame Settings produce a buffer containing the Rendering Layer Mask of rendered meshes.")]
        RenderingLayerMaskBuffer = 50,
        /// <summary>When enabled, HDRP updates ray tracing for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, displayedName: "Ray Tracing", customOrderInGroup: 10, tooltip: "When enabled, HDRP updates ray tracing for Cameras using these Frame Settings (Depends on \"Realtime RayTracing\" in current HDRP Asset).")]
        RayTracing = 92,
        /// <summary>When enabled, HDRP will include visual effects in the ray tracing acceleration structure.</summary>
        [FrameSettingsField(0, displayedName: "RaytracingVFX", positiveDependencies: new[] { RayTracing }, customOrderInGroup: 10, tooltip: "When enabled, HDRP will include visual effects in the ray tracing acceleration structure.")]
        RaytracingVFX = 100,
        /// <summary>When enabled, HDRP renders custom passes contained in CustomPassVolume components.</summary>
        [FrameSettingsField(0, autoName: CustomPass, customOrderInGroup: 11, tooltip: "When enabled, HDRP renders custom passes contained in CustomPassVolume components.")]
        CustomPass = 6,
        /// <summary>When enabled, HDRP renders custom passes contained in CustomPassVolume components.</summary>
        [FrameSettingsField(0, autoName: VariableRateShading, positiveDependencies: new[] { CustomPass }, customOrderInGroup: 12, tooltip: "When enabled, HDRP updates variable rate shading for Cameras using these Frame Settings.")]
        VariableRateShading = 7,
        /// <summary>When enabled, HDRP can use virtual texturing.</summary>
        [FrameSettingsField(0, autoName: VirtualTexturing, customOrderInGroup: 105, tooltip: "Virtual Texturing needs to be enabled first in Project Settings > Player > Other Settings > Virtual Texturing.")]
        VirtualTexturing = 68,
        /// <summary>When enabled, HDRP renders line topology with high quality anti-aliasing and transparency.</summary>
        [FrameSettingsField(0, autoName: HighQualityLineRendering, customOrderInGroup: 109, tooltip: "When enabled, Cameras using these Frame Settings render line topology with high quality anti-aliasing and transparency.")]
        HighQualityLineRendering = 103,
        /// <summary>When enabled, HDRP accounts for asymmetry in the projection matrix when evaluating the view direction based on pixel coordinates.</summary>
        [FrameSettingsField(0, displayedName: "Asymmetric Projection", customOrderInGroup: 107, tooltip: "When enabled HDRP will account for asymmetric projection when evaluating the view direction based on pixel coordinates.")]
        AsymmetricProjection = 78,
        /// <summary>When enabled, HDRP evaluates post effects using transformed screen space coordinates, this allows post effects to be compatible with Cluster Display for example.</summary>
        [FrameSettingsField(0, displayedName: "Screen Coordinates Override", customOrderInGroup: 108, tooltip: "When enabled HDRP will use Screen Coordinates Override for post processing and custom passes. This allows post effects to be compatible with Cluster Display for example.")]
        ScreenCoordOverride = 77,

        /// <summary>When enabled, HDRP processes a motion vector pass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: MotionVectors, customOrderInGroup: 12, tooltip: "When enabled, HDRP processes a motion vector pass for Cameras using these Frame Settings (Depends on \"Motion Vectors\" in current HDRP Asset).")]
        MotionVectors = 10,
        /// <summary>When enabled, HDRP processes an object motion vector pass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, displayedName: "Opaque Object Motion", positiveDependencies: new[] { MotionVectors }, customOrderInGroup: 13, tooltip: "When enabled, HDRP processes an object motion vector pass for Cameras using these Frame Settings.")]
        ObjectMotionVectors = 11,
        /// <summary>When enabled, transparent GameObjects use Motion Vectors. You must also enable TransparentWritesVelocity for Materials that you want to use motion vectors with.</summary>
        [FrameSettingsField(0, displayedName: "Transparent Object Motion", positiveDependencies: new[] { MotionVectors }, customOrderInGroup: 14, tooltip: "When enabled, transparent GameObjects use Motion Vectors. You must also enable TransparentWritesVelocity for Materials that you want to use motion vectors with.")]
        TransparentsWriteMotionVector = 16,

        /// <summary>When enabled, HDRP processes a distortion render pass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: Distortion, customOrderInGroup: 16, tooltip: "When enabled, HDRP processes a distortion render pass for Cameras using these Frame Settings (Depends on \"Distortion\" in current HDRP Asset).")]
        Distortion = 14,
        /// <summary>When enabled, HDRP processes a distortion render pass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: RoughDistortion, customOrderInGroup: 17, positiveDependencies: new[] { Distortion }, tooltip: "When enabled, HDRP processes a distortion render pass for Cameras using these Frame Settings (Depends on \"Distortion\" in current HDRP Asset).")]
        RoughDistortion = 67,
        /// <summary>When enabled, HDRP processes a post-processing render pass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, displayedName: "Post-process", customOrderInGroup: 18, tooltip: "When enabled, HDRP processes a post-processing render pass for Cameras using these Frame Settings.")]
        Postprocess = 15,
        /// <summary>When enabled on a Camera, HDRP renders user-written post processes.</summary>
        [FrameSettingsField(0, displayedName: "Custom Post-process", positiveDependencies: new[] { Postprocess }, customOrderInGroup: 19, tooltip: "When enabled on a Camera, HDRP renders user-written post processes.")]
        CustomPostProcess = 39,
        /// <summary>When enabled, HDRP replace NaN values with black pixels for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, displayedName: "Stop NaN", positiveDependencies: new[] { Postprocess }, customOrderInGroup: 19, tooltip: "When enabled, HDRP replace NaN values with black pixels for Cameras using these Frame Settings.")]
        StopNaN = 80,
        /// <summary>When enabled, HDRP adds depth of field to Cameras affected by a Volume containing the Depth Of Field override.</summary>
        [FrameSettingsField(0, autoName: DepthOfField, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 19, tooltip: "When enabled, HDRP adds depth of field to Cameras affected by a Volume containing the Depth Of Field override.")]
        DepthOfField = 81,
        /// <summary>When enabled, HDRP adds motion blur to Cameras affected by a Volume containing the Blur override.</summary>
        [FrameSettingsField(0, autoName: MotionBlur, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 19, tooltip: "When enabled, HDRP adds motion blur to Cameras affected by a Volume containing the Blur override.")]
        MotionBlur = 82,
        /// <summary>When enabled, HDRP adds panini projection to Cameras affected by a Volume containing the Panini Projection override.</summary>
        [FrameSettingsField(0, autoName: PaniniProjection, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 19, tooltip: "When enabled, HDRP adds panini projection to Cameras affected by a Volume containing the Panini Projection override.")]
        PaniniProjection = 83,
        /// <summary>When enabled, HDRP adds bloom to Cameras affected by a Volume containing the Bloom override.</summary>
        [FrameSettingsField(0, autoName: Bloom, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 19, tooltip: "When enabled, HDRP adds bloom to Cameras affected by a Volume containing the Bloom override.")]
        Bloom = 84,
        /// <summary>When enabled, HDRP adds Screen Space lens flare post process to Cameras affected by a Volume containing the Screen Space Lens Flare override.</summary>
        [FrameSettingsField(0, displayedName: "Screen Space Lens Flare", positiveDependencies: new[] { Postprocess, Bloom }, customOrderInGroup: 19, tooltip: "When enabled, HDRP adds Screen Space Lens Flare post process to Cameras affected by a Volume containing the Screen Space Lens Flare override.")]
        LensFlareScreenSpace = 104,
        /// <summary>When enabled, HDRP adds lens flare to Cameras.</summary>
        [FrameSettingsField(0, autoName: LensFlareDataDriven, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP adds lens flare to Cameras.")]
        LensFlareDataDriven = 97,
        /// <summary>When enabled, HDRP adds lens distortion to Cameras affected by a Volume containing the Lens Distortion override.</summary>
        [FrameSettingsField(0, autoName: LensDistortion, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP adds lens distortion to Cameras affected by a Volume containing the Lens Distortion override.")]
        LensDistortion = 85,
        /// <summary>When enabled, HDRP adds chromatic aberration to Cameras affected by a Volume containing the Chromatic Aberration override.</summary>
        [FrameSettingsField(0, autoName: ChromaticAberration, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP adds chromatic aberration to Cameras affected by a Volume containing the Chromatic Aberration override.")]
        ChromaticAberration = 86,
        /// <summary>When enabled, HDRP adds vignette to Cameras affected by a Volume containing the Vignette override.</summary>
        [FrameSettingsField(0, autoName: Vignette, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP adds vignette to Cameras affected by a Volume containing the Vignette override.")]
        Vignette = 87,
        /// <summary>When enabled, HDRP processes color grading for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: ColorGrading, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP processes color grading for Cameras using these Frame Settings.")]
        ColorGrading = 88,
        /// <summary>When enabled, HDRP processes tonemapping for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: Tonemapping, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP processes tonemapping for Cameras using these Frame Settings.")]
        Tonemapping = 93,
        /// <summary>When enabled, HDRP adds film grain to Cameras affected by a Volume containing the Film Grain override.</summary>
        [FrameSettingsField(0, autoName: FilmGrain, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP adds film grain to Cameras affected by a Volume containing the Film Grain override.")]
        FilmGrain = 89,
        /// <summary>When enabled, HDRP processes dithering for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, autoName: Dithering, positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP processes dithering for Cameras using these Frame Settings.")]
        Dithering = 90,
        /// <summary>When enabled, HDRP processes anti-aliasing for camera using these Frame Settings.</summary>
        [FrameSettingsField(0, displayedName: "Anti-aliasing", positiveDependencies: new[] { Postprocess }, customOrderInGroup: 20, tooltip: "When enabled, HDRP processes anti-aliasing for camera using these Frame Settings.")]
        Antialiasing = 91,
        /// <summary>When enabled, HDRP processes a post-processing render pass for Cameras using these Frame Settings.</summary>
        [FrameSettingsField(0, displayedName: "After Post-process", customOrderInGroup: 21, tooltip: "When enabled, HDRP processes a post-processing render pass for Cameras using these Frame Settings.")]
        AfterPostprocess = 17,
        /// <summary>When enabled, Cameras that don't use TAA process a depth test for Materials in the AfterPostProcess rendering pass.</summary>
        [FrameSettingsField(0, displayedName: "Depth Test", positiveDependencies: new[] { AfterPostprocess }, customOrderInGroup: 21, tooltip: "When enabled, Cameras that don't use TAA process a depth test for Materials in the AfterPostProcess rendering pass.")]
        ZTestAfterPostProcessTAA = 19,

        // true <=> Fixed, false <=> FromQualitySettings (default)
        /// <summary>Specifies the Level Of Detail Mode for Cameras using these Frame Settings use to render the Scene. Scale will allow to add a scale factor while Override will allow to set a specific value.</summary>
        [FrameSettingsField(0, autoName: LODBiasMode, type: FrameSettingsFieldAttribute.DisplayType.Others, targetType: typeof(LODBiasMode), customOrderInGroup: 100, tooltip: "Specifies the Level Of Detail Mode for Cameras using these Frame Settings use to render the Scene. Scale will allow to add a scale factor while Override will allow to set a specific value.")]
        LODBiasMode = 60,
        /// <summary>Set the LOD Bias with the value in <see cref="FrameSettings.lodBias"/>.</summary>
        [FrameSettingsField(0, autoName: LODBias, type: FrameSettingsFieldAttribute.DisplayType.Others, positiveDependencies: new[] { LODBiasMode }, tooltip: "Sets the Level Of Detail Bias or the Scale on it.")]
        LODBias = 61,
        /// <summary>The quality level to use when fetching the value from the quality settings.</summary>
        [FrameSettingsField(0, displayedName: "Tier Level", type: FrameSettingsFieldAttribute.DisplayType.Others, customOrderInGroup: 100, positiveDependencies: new[] { LODBiasMode }, tooltip: "The quality level to use when fetching the value from the quality settings.")]
        LODBiasQualityLevel = 64,
        // true <=> Fixed, false <=> FromQualitySettings (default)
        /// <summary>Specifies the Maximum Level Of Detail Mode for Cameras using these Frame Settings to use to render the Scene. Offset allows you to add an offset factor while Override allows you to set a specific value.</summary>
        [FrameSettingsField(0, autoName: MaximumLODLevelMode, type: FrameSettingsFieldAttribute.DisplayType.Others, targetType: typeof(MaximumLODLevelMode), tooltip: "Specifies the Maximum Level Of Detail Mode for Cameras using these Frame Settings to use to render the Scene. Offset allows you to add an offset factor while Override allows you to set a specific value.")]
        MaximumLODLevelMode = 62,
        /// <summary>Set the LOD Bias with the value in <see cref="FrameSettings.maximumLODLevel"/>.</summary>
        [FrameSettingsField(0, autoName: MaximumLODLevel, type: FrameSettingsFieldAttribute.DisplayType.Others, positiveDependencies: new[] { MaximumLODLevelMode }, tooltip: "Sets the Maximum Level Of Detail Level or the Offset on it.")]
        MaximumLODLevel = 63,
        /// <summary>The quality level to use when fetching the value from the quality settings.</summary>
        [FrameSettingsField(0, displayedName: "Tier Level", type: FrameSettingsFieldAttribute.DisplayType.Others, customOrderInGroup: 102, positiveDependencies: new[] { MaximumLODLevelMode }, tooltip: "The quality level to use when fetching the value from the quality settings.")]
        MaximumLODLevelQualityLevel = 65,
        /// <summary>The quality level to use when fetching the value from the quality settings.</summary>
        [FrameSettingsField(0, autoName: MaterialQualityLevel, type: FrameSettingsFieldAttribute.DisplayType.Others, tooltip: "The material quality level to use.")]
        MaterialQualityLevel = 66,

        //lighting settings (group 1)
        /// <summary>When enabled, Cameras using these Frame Settings render shadows.</summary>
        [FrameSettingsField(1, autoName: ShadowMaps, customOrderInGroup: 1, tooltip: "When enabled, Cameras using these Frame Settings render shadows.")]
        ShadowMaps = 20,
        /// <summary>When enabled, Cameras using these Frame Settings render Contact Shadows.</summary>
        [FrameSettingsField(1, autoName: ContactShadows, tooltip: "When enabled, Cameras using these Frame Settings render Contact Shadows")]
        ContactShadows = 21,
        /// <summary>When enabled, Cameras using these Frame Settings render Screen Space Shadows.</summary>
        [FrameSettingsField(1, autoName: ScreenSpaceShadows, customOrderInGroup: 23, tooltip: "When enabled, Cameras using these Frame Settings render Screen Space Shadows (Depends on \"Screen Space Shadows\" in current HDRP Asset). Note that Screen Space Shadows are disabled when MSAA is enabled.")]
        ScreenSpaceShadows = 34,
        /// <summary>When enabled, Cameras using these Frame Settings render shadows from Shadow Masks.</summary>
        [FrameSettingsField(1, autoName: Shadowmask, customOrderInGroup: 24, tooltip: "When enabled, Cameras using these Frame Settings render shadows from Shadow Masks (Depends on \"Shadowmask\" in current HDRP Asset).")]
        Shadowmask = 22,
        /// <summary>When enabled, Cameras using these Frame Settings calculate Screen Space Reflections.</summary>
        [FrameSettingsField(1, displayedName: "Screen Space Reflection", tooltip: "When enabled, Cameras using these Frame Settings calculate Screen Space Reflections (Depends on \"Screen Space Reflection\" in current HDRP Asset). Note that Screen Space Reflections are disabled when MSAA is enabled.")]
        SSR = 23,
        /// <summary>When enabled, Cameras using these Frame Settings calculate Screen Space Reflections on transparent objects.</summary>
        [FrameSettingsField(1, displayedName: "Transparents", customOrderInGroup: 25, positiveDependencies: new[] { SSR }, tooltip: "When enabled, Cameras using these Frame Settings calculate Screen Space Reflections on transparent objects.")]
        TransparentSSR = 94,
        /// <summary>When enabled, Cameras using these Frame Settings calculate Screen Space Ambient Occlusion.</summary>
        [FrameSettingsField(1, displayedName: "Screen Space Ambient Occlusion", tooltip: "When enabled, Cameras using these Frame Settings calculate Screen Space Ambient Occlusion (Depends on \"Screen Space Ambient Occlusion\" in current HDRP Asset).")]
        SSAO = 24,
        /// <summary>When enabled, Cameras using these Frame Settings calculate Screen Space Global Illumination.</summary>
        [FrameSettingsField(1, displayedName: "Screen Space Global Illumination", customOrderInGroup: 25, tooltip: "When enabled, Cameras using these Frame Settings calculate Screen Space Global Illumination (Depends on \"Screen Space Global Illumination\" in current HDRP Asset).")]
        SSGI = 95,
        /// <summary>When enabled, Cameras using these Frame Settings render subsurface scattering (SSS) effects for GameObjects that use a SSS Material.</summary>
        [FrameSettingsField(1, customOrderInGroup: 46, autoName: SubsurfaceScattering,
            tooltip: "When enabled, Cameras using these Frame Settings render subsurface scattering (SSS) effects for GameObjects that use a SSS Material (Depends on \"Subsurface Scattering\" in current HDRP Asset).")]
        SubsurfaceScattering = 46,
        /// <summary>Configures the sample budget of the Subsurface Scattering algorithm using Quality Levels. You can either pick from one of the existing values in the Quality Settings, or request a custom number of samples.</summary>
        [FrameSettingsField(1, customOrderInGroup: 47, displayedName: "Tier Mode", positiveDependencies: new[] { SubsurfaceScattering }, type: FrameSettingsFieldAttribute.DisplayType.Others, targetType: typeof(SssQualityMode),
            tooltip: "Configures the way the sample budget of the Subsurface Scattering algorithm is determined. You can either pick from one of the existing values in the Quality Settings, or request a custom number of samples.")]
        SssQualityMode = 47,
        /// <summary>Sets the Quality Level of the Subsurface Scattering algorithm.</summary>
        [FrameSettingsField(1, customOrderInGroup: 48, displayedName: "Tier Level", positiveDependencies: new[] { SubsurfaceScattering }, type: FrameSettingsFieldAttribute.DisplayType.Others,
            tooltip: "Sets the Quality Level of the Subsurface Scattering algorithm.")]
        SssQualityLevel = 48,
        /// <summary>Sets the custom sample budget of the Subsurface Scattering algorithm.</summary>
        [FrameSettingsField(1, customOrderInGroup: 49, displayedName: "Custom Sample Budget", positiveDependencies: new[] { SubsurfaceScattering }, type: FrameSettingsFieldAttribute.DisplayType.Others,
            tooltip: "Sets the custom sample budget of the Subsurface Scattering algorithm.")]
        SssCustomSampleBudget = 49,
        /// <summary>Sets the custom number of downsample steps used by the Subsurface Scattering algorithm.</summary>
        [FrameSettingsField(1, customOrderInGroup: 50, displayedName: "Custom Downsample Level", positiveDependencies: new[] { SubsurfaceScattering }, type: FrameSettingsFieldAttribute.DisplayType.Others,
            tooltip: "Sets the custom number of downsample steps done to the source irradance textrure before it is used by the Subsurface Scattering algorithm. Higher value will improve performance, but might lower quality.")]
        SssCustomDownsampleSteps = 51,
        /// <summary>When enabled, Cameras using these Frame Settings calculate Volumetric Clouds.</summary>
        [FrameSettingsField(1, autoName: VolumetricClouds, customOrderInGroup: 50, tooltip: "When enabled, Cameras using these Frame Settings calculate Volumetric Clouds.")]
        VolumetricClouds = 79,
        /// <summary>When enabled, Cameras using these Frame Settings calculate Volumetric Clouds at full resolution when evaluating the sky texture.</summary>
        [FrameSettingsField(1, autoName: FullResolutionCloudsForSky, customOrderInGroup: 52, positiveDependencies: new[] { VolumetricClouds }, tooltip: "When enabled, Cameras using these Frame Settings calculate Volumetric Clouds at full resolution when evaluating the sky texture.")]
        FullResolutionCloudsForSky = 98,

        /// <summary>When enabled, Cameras using these Frame Settings render subsurface scattering (SSS) Materials with an added transmission effect (only if you enable Transmission on the SSS Material in the Material's Inspector).</summary>
        [FrameSettingsField(1, autoName: Transmission, tooltip: "When enabled, Cameras using these Frame Settings render subsurface scattering (SSS) Materials with an added transmission effect (only if you enable Transmission on the SSS Material in the Material's Inspector).")]
        Transmission = 26,
        /// <summary>When enabled, Cameras using these Frame Settings render fog effects.</summary>
        [FrameSettingsField(1, displayedName: "Fog", tooltip: "When enabled, Cameras using these Frame Settings render fog effects.")]
        AtmosphericScattering = 27,
        /// <summary>When enabled, Cameras using these Frame Settings render volumetric effects such as volumetric fog and lighting.</summary>
        [FrameSettingsField(1, displayedName: "Volumetric Fog", positiveDependencies: new[] { AtmosphericScattering }, tooltip: "When enabled, Cameras using these Frame Settings render volumetric effects such as volumetric fog and lighting (Depends on \"Volumetrics\" in current HDRP Asset).")]
        Volumetrics = 28,
        /// <summary>When enabled, Cameras using these Frame Settings use several previous frames to calculate volumetric effects which increases their overall quality at run time.</summary>
        [FrameSettingsField(1, displayedName: "Reprojection", positiveDependencies: new[] { AtmosphericScattering, Volumetrics }, tooltip: "When enabled, Cameras using these Frame Settings use several previous frames to calculate volumetric effects which increases their overall quality at run time.")]
        ReprojectionForVolumetrics = 29,
        /// <summary>When enabled, Cameras that use these Frame Settings make use of LightLayers.</summary>
        [FrameSettingsField(1, autoName: LightLayers, tooltip: "When enabled, Cameras that use these Frame Settings make use of LightLayers (Depends on \"Light Layers\" in current HDRP Asset).")]
        LightLayers = 30,
        /// <summary>When enabled, Cameras that use these Frame Settings use exposure values defined in relevant components.</summary>
        [FrameSettingsField(1, autoName: ExposureControl, customOrderInGroup: 33, tooltip: "When enabled, Cameras that use these Frame Settings use exposure values defined in relevant components.")]
        ExposureControl = 32,
        /// <summary>When enabled, Cameras that use these Frame Settings calculate reflection from Reflection Probes.</summary>
        [FrameSettingsField(1, autoName: ReflectionProbe, tooltip: "When enabled, Cameras that use these Frame Settings calculate reflection from Reflection Probes.")]
        ReflectionProbe = 33,
        /// <summary>When enabled, Cameras that use these Frame Settings calculate reflection from Planar Reflection Probes.</summary>
        [FrameSettingsField(1, displayedName: "Planar Reflection Probe", customOrderInGroup: 36, tooltip: "When enabled, Cameras that use these Frame Settings calculate reflection from Planar Reflection Probes.")]
        PlanarProbe = 35,
        /// <summary>When enabled, Cameras that use these Frame Settings render Materials with base color as diffuse. This is a useful Frame Setting to use for real-time Reflection Probes because it renders metals as diffuse Materials to stop them appearing black when Unity can't calculate several bounces of specular lighting.</summary>
        [FrameSettingsField(1, displayedName: "Metallic Indirect Fallback", tooltip: "When enabled, Cameras that use these Frame Settings render Materials with base color as diffuse. This is a useful Frame Setting to use for real-time Reflection Probes because it renders metals as diffuse Materials to stop them appearing black when Unity can't calculate several bounces of specular lighting.")]
        ReplaceDiffuseForIndirect = 36,
        /// <summary>When enabled, the Sky affects specular lighting for Cameras that use these Frame Settings.</summary>
        [FrameSettingsField(1, autoName: SkyReflection, tooltip: "When enabled, the Sky affects specular lighting for Cameras that use these Frame Settings.")]
        SkyReflection = 37,
        /// <summary>When enabled, Cameras that use these Frame Settings render Direct Specular lighting. This is a useful Frame Setting to use for baked Reflection Probes to remove view dependent lighting.</summary>
        [FrameSettingsField(1, autoName: DirectSpecularLighting, tooltip: "When enabled, Cameras that use these Frame Settings render Direct Specular lighting. This is a useful Frame Setting to use for baked Reflection Probes to remove view dependent lighting.")]
        DirectSpecularLighting = 38,
        /// <summary>When enabled, HDRP uses probe volumes for baked lighting.</summary>
        [FrameSettingsField(1, customOrderInGroup: 3, displayedName: "Adaptive Probe Volumes", tooltip: "Enable Adaptive Probe Volumes for rendering and debug visualisations. Enabling this feature causes HDRP to process Adaptive Probe Volumes for this Camera/Reflection Probe.")]
        AdaptiveProbeVolume = 127,
        /// <summary>When enabled, HDRP uses probe volumes to normalize the data sampled from reflection probes so they better match the lighting at the sampling location.</summary>
        [FrameSettingsField(1, customOrderInGroup: 4, displayedName: "Normalize Reflection Probes", positiveDependencies: new[] { AdaptiveProbeVolume })]
        NormalizeReflectionProbeWithProbeVolume = 126,

        //async settings (group 2)
        /// <summary>When enabled, HDRP executes certain Compute Shader commands in parallel. This only has an effect if the target platform supports async compute.</summary>
        [FrameSettingsField(2, displayedName: "Asynchronous Execution", tooltip: "When enabled, HDRP executes certain Compute Shader commands in parallel. This is only supported on DX12 and Vulkan. If Asynchronous execution is disabled or not supported the effects will fallback on a synchronous version.")]
        AsyncCompute = 40,
        /// <summary>When enabled, HDRP builds the Light List asynchronously.</summary>
        [FrameSettingsField(2, displayedName: "Light List", positiveDependencies: new[] { AsyncCompute }, tooltip: "When enabled, HDRP builds the Light List asynchronously.")]
        LightListAsync = 41,
        /// <summary>When enabled, HDRP calculates screen space reflection asynchronously.</summary>
        [FrameSettingsField(2, displayedName: "SS Reflection", positiveDependencies: new[] { AsyncCompute }, tooltip: "When enabled, HDRP calculates screen space reflection asynchronously.")]
        SSRAsync = 42,
        /// <summary>When enabled, HDRP calculates screen space ambient occlusion asynchronously.</summary>
        [FrameSettingsField(2, displayedName: "SS Ambient Occlusion", positiveDependencies: new[] { AsyncCompute }, tooltip: "When enabled, HDRP calculates screen space ambient occlusion asynchronously.")]
        SSAOAsync = 43,
        /// <summary>When enabled, HDRP calculates Contact Shadows asynchronously.</summary>
        [FrameSettingsField(2, displayedName: "Contact Shadows", positiveDependencies: new[] { AsyncCompute }, tooltip: "When enabled, HDRP calculates Contact Shadows asynchronously.")]
        ContactShadowsAsync = 44,
        /// <summary>When enabled, HDRP calculates volumetric voxelization asynchronously.</summary>
        [FrameSettingsField(2, displayedName: "Volume Voxelizations", positiveDependencies: new[] { AsyncCompute }, tooltip: "When enabled, HDRP calculates volumetric voxelization asynchronously.")]
        VolumeVoxelizationsAsync = 45,
        /// <summary>When enabled, HDRP calculates High Quality Lines partially asynchronously.</summary>
        [FrameSettingsField(2, displayedName: "High Quality Line Rendering", positiveDependencies: new[] { AsyncCompute }, tooltip: "When enabled, HDRP calculates High Quality Lines partially asynchronously")]
        HighQualityLinesAsync = 52,

        //lightLoop settings (group 3)
        /// <summary>When enabled, HDRP uses FPTL for forward opaque.</summary>
        [FrameSettingsField(3, autoName: FPTLForForwardOpaque, tooltip: "When enabled, HDRP uses FPTL for forward opaque.")]
        FPTLForForwardOpaque = 120,
        /// <summary>When enabled, HDRP uses a big tile prepass for light visibility.</summary>
        [FrameSettingsField(3, autoName: BigTilePrepass, tooltip: "When enabled, HDRP uses a big tile prepass for light visibility.")]
        BigTilePrepass = 121,
        /// <summary>When enabled, HDRP uses light variant classification to compute lighting.</summary>
        [FrameSettingsField(3, autoName: ComputeLightVariants, tooltip: "When enabled, HDRP uses light variant classification to compute lighting.")]
        ComputeLightVariants = 124,
        /// <summary>When enabled, HDRP uses material variant classification to compute lighting.</summary>
        [FrameSettingsField(3, autoName: ComputeMaterialVariants, tooltip: "When enabled, HDRP uses material variant classification to compute lighting.")]
        ComputeMaterialVariants = 125,

        //only 128 booleans saved. For more, change the BitArray used
    }

    /// <summary>BitField that state which element is overridden.</summary>
    [Serializable]
    [DebuggerDisplay("{mask.humanizedData}")]
    public struct FrameSettingsOverrideMask
    {
        /// <summary>Gets the underlying BitArray HDRP uses to store the override mask and thus specific which field is overridden or not.
        /// Note: BitArray128 is implements IBitArray and therefore has the scripting API described below. It is recomended to use the interface as the exact BitArray con evolve from one version of the package to another as the we need more capacity here.
        /// </summary>
        [SerializeField]
        public BitArray128 mask;
    }

    /// <summary>Per renderer and per frame settings.</summary>
    [Serializable]
    [DebuggerDisplay("{bitDatas.humanizedData}")]
    [DebuggerTypeProxy(typeof(FrameSettingsDebugView))]
    partial struct FrameSettings
    {
        // Each time you add data in the framesettings. Attempt to add boolean one only if possible.
        // BitArray is quick in computation and take not a lot of space. It can contains only boolean value.
        // If anyone wants more than 128 bit, the BitArray256 already exist. Just replace this one with it should be enough.
        // For more, you should write one using previous as exemple.
        [SerializeField]
        internal BitArray128 bitDatas;

        /// <summary>
        /// If <c>lodBiasMode</c> is <c>LODBiasMode.Fixed</c>, then this value overwrites <c>QualitySettings.lodBias</c>.
        /// If <c>lodBiasMode</c> is <c>LODBiasMode.ScaleQualitySettings</c>, then this value scales <c>QualitySettings.lodBias</c>.
        /// </summary>
        public float lodBias;
        /// <summary>Specifies how HDRP calculates <c>QualitySettings.lodBias</c>.</summary>
        public LODBiasMode lodBiasMode;
        /// <summary>The quality level the rendering component uses when it fetches the quality setting value.</summary>
        public int lodBiasQualityLevel;
        /// <summary>
        /// If <c>maximumLODLevelMode</c> is <c>MaximumLODLevelMode.FromQualitySettings</c>, then this value overwrites <c>QualitySettings.maximumLODLevel</c>
        /// If <c>maximumLODLevelMode</c> is <c>MaximumLODLevelMode.OffsetQualitySettings</c>, then this value offsets <c>QualitySettings.maximumLODLevel</c>
        /// </summary>
        public int maximumLODLevel;
        /// <summary>Specifies how HDRP calculates <c>QualitySettings.maximumLODLevel</c>.</summary>
        public MaximumLODLevelMode maximumLODLevelMode;
        /// <summary>The maximum quality level the rendering component uses when it fetches the quality setting value.</summary>
        public int maximumLODLevelQualityLevel;

        /// <summary>Stores SssQualityMode on disk.</summary>
        public SssQualityMode sssQualityMode;
        /// <summary>Stores SssQualityLevel on disk.</summary>
        public int sssQualityLevel;
        /// <summary>Stores SssCustomSampleBudget on disk.</summary>
        public int sssCustomSampleBudget;
        /// <summary>Stores SssCustomDownsampleSteps on disk.</summary>
        public int sssCustomDownsampleSteps;

        /// <summary>Stores MSAA Mode on disk.</summary>
        public MSAAMode msaaMode;

        /// <summary>The actual value used by the Subsurface Scattering algorithm. Updated every frame.</summary>
        internal int sssResolvedSampleBudget;
        internal int sssResolvedDownsampleSteps;

        /// <summary>
        /// The material quality level this rendering component uses.
        /// If <c>materialQuality == 0</c>, the rendering component uses the material quality from the current quality settings in the HDRP Asset.
        /// </summary>
        public MaterialQuality materialQuality;

        /// <summary>Specifies the rendering path this rendering component uses. Here you can use the <c>LitShaderMode</c> enum to specify whether the rendering component uses forward or deferred rendering.</summary>
        public LitShaderMode litShaderMode
        {
            get => bitDatas[(uint)FrameSettingsField.LitShaderMode] ? LitShaderMode.Deferred : LitShaderMode.Forward;
            set => bitDatas[(uint)FrameSettingsField.LitShaderMode] = value == LitShaderMode.Deferred;
        }

        /// <summary>Gets the stored override value for the passed in Frame Setting. Use this to access boolean values.</summary>
        /// <param name="field">Requested field.</param>
        /// <returns>True if the field is enabled.</returns>
        public bool IsEnabled(FrameSettingsField field) => bitDatas[(uint)field];
        /// <summary>Sets the stored override value for the passed in Frame Setting. Use this to access boolean values.</summary>
        /// <param name="field">Requested field.</param>
        /// <param name="value">State to set to the field.</param>
        public void SetEnabled(FrameSettingsField field, bool value) => bitDatas[(uint)field] = value;

        /// <summary>
        /// Calculates the LOD bias value to use.
        /// </summary>
        /// <param name="hdrp">The HDRP Assets to use</param>
        /// <returns>The LOD Bias to use</returns>
        public float GetResolvedLODBias(HDRenderPipelineAsset hdrp)
        {
            var source = hdrp.currentPlatformRenderPipelineSettings.lodBias;
            switch (lodBiasMode)
            {
                case LODBiasMode.FromQualitySettings: return source[lodBiasQualityLevel];
                case LODBiasMode.OverrideQualitySettings: return lodBias;
                case LODBiasMode.ScaleQualitySettings: return lodBias * source[lodBiasQualityLevel];
                default: throw new ArgumentOutOfRangeException(nameof(lodBiasMode));
            }
        }

        /// <summary>
        /// Calculates the Maximum LOD level to use.
        /// </summary>
        /// <param name="hdrp">The HDRP Asset to use</param>
        /// <returns>The Maximum LOD level to use.</returns>
        public int GetResolvedMaximumLODLevel(HDRenderPipelineAsset hdrp)
        {
            var source = hdrp.currentPlatformRenderPipelineSettings.maximumLODLevel;
            switch (maximumLODLevelMode)
            {
                case MaximumLODLevelMode.FromQualitySettings: return source[maximumLODLevelQualityLevel];
                case MaximumLODLevelMode.OffsetQualitySettings: return source[maximumLODLevelQualityLevel] + maximumLODLevel;
                case MaximumLODLevelMode.OverrideQualitySettings: return maximumLODLevel;
                default: throw new ArgumentOutOfRangeException(nameof(maximumLODLevelMode));
            }
        }

        /// <summary>
        /// Returns the sample budget of the Subsurface Scattering algorithm.
        /// </summary>
        /// <param name="hdrp">The HDRP Asset to use.</param>
        /// <returns>The sample budget of the Subsurface Scattering algorithm.</returns>
        public int GetResolvedSssSampleBudget(HDRenderPipelineAsset hdrp)
        {
            var source = hdrp.currentPlatformRenderPipelineSettings.sssSampleBudget;
            switch (sssQualityMode)
            {
                case SssQualityMode.FromQualitySettings: return source[sssQualityLevel];
                case SssQualityMode.OverrideQualitySettings: return sssCustomSampleBudget;
                default: throw new ArgumentOutOfRangeException(nameof(sssCustomSampleBudget));
            }
        }

        /// <summary>
        /// Returns the number downsample steps that will be performed on the source
        /// irradiance texture before the main Subsurface algorithm executes.
        /// </summary>
        /// <param name="hdrp">The HDRP Asset to use.</param>
        /// <returns>The number downsample steps.</returns>
        public int GetResolvedSssDownsampleSteps(HDRenderPipelineAsset hdrp)
        {
            var source = hdrp.currentPlatformRenderPipelineSettings.sssDownsampleSteps;
            switch (sssQualityMode)
            {
                case SssQualityMode.FromQualitySettings: return source[sssQualityLevel];
                case SssQualityMode.OverrideQualitySettings: return sssCustomDownsampleSteps;
                default: throw new ArgumentOutOfRangeException(nameof(sssCustomDownsampleSteps));
            }
        }

        /// <summary>
        /// Calculates the Maximum LOD level to use.
        /// </summary>
        /// <param name="hdrp">The HDRP Asset to use</param>
        /// <returns>The Maximum LOD level to use.</returns>
        public MSAASamples GetResolvedMSAAMode(HDRenderPipelineAsset hdrp)
        {
            if (msaaMode == MSAAMode.FromHDRPAsset)
                return hdrp.currentPlatformRenderPipelineSettings.msaaSampleCount;
            else
                return (MSAASamples)msaaMode;
        }

        // followings are helper for engine.
        internal bool fptl => litShaderMode == LitShaderMode.Deferred || bitDatas[(int)FrameSettingsField.FPTLForForwardOpaque];
        internal float specularGlobalDimmer => bitDatas[(int)FrameSettingsField.DirectSpecularLighting] ? 1f : 0f;

        // When render graph debug is active, we need async information to be accurate even if not supported. Actual execution will be disabled down the line.
        bool asyncEnabled => (SystemInfo.supportsAsyncCompute || RenderGraph.isRenderGraphViewerActive) && bitDatas[(int)FrameSettingsField.AsyncCompute];

        internal bool BuildLightListRunsAsync() => asyncEnabled && bitDatas[(int)FrameSettingsField.LightListAsync];
        internal bool SSRRunsAsync() => asyncEnabled && bitDatas[(int)FrameSettingsField.SSRAsync];
        internal bool SSAORunsAsync() => asyncEnabled && bitDatas[(int)FrameSettingsField.SSAOAsync];
        internal bool ContactShadowsRunsAsync() => asyncEnabled && bitDatas[(int)FrameSettingsField.ContactShadowsAsync];
        internal bool VolumeVoxelizationRunsAsync() => asyncEnabled && bitDatas[(int)FrameSettingsField.VolumeVoxelizationsAsync];
        internal bool HighQualityLinesRunsAsync() => SystemInfo.supportsAsyncCompute && bitDatas[(int)FrameSettingsField.AsyncCompute] && bitDatas[(uint)FrameSettingsField.HighQualityLinesAsync];

        /// <summary>Construct and initialize a <see cref="FrameSettings"/></summary>
        /// <returns>A new <see cref="FrameSettings"/> initialized</returns>
        public static FrameSettings Create()
        {
            var res = new FrameSettings();
            //Initialize default values that are not the C# defaults
            res.msaaMode = MSAAMode.None;
            return res;
        }

        /// <summary>Override a frameSettings according to a mask.</summary>
        /// <param name="overriddenFrameSettings">Overrided FrameSettings. Must contains default data before attempting the override.</param>
        /// <param name="overridingFrameSettings">The FrameSettings data we will use for overriding.</param>
        /// <param name="frameSettingsOverideMask">The mask to use for overriding (1 means override this field).</param>
        internal static void Override(ref FrameSettings overriddenFrameSettings, FrameSettings overridingFrameSettings, FrameSettingsOverrideMask frameSettingsOverideMask)
        {
            //quick override of all booleans
            overriddenFrameSettings.bitDatas = (overridingFrameSettings.bitDatas & frameSettingsOverideMask.mask) | (~frameSettingsOverideMask.mask & overriddenFrameSettings.bitDatas);

            //other overrides
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.SssQualityMode])
                overriddenFrameSettings.sssQualityMode = overridingFrameSettings.sssQualityMode;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.SssQualityLevel])
                overriddenFrameSettings.sssQualityLevel = overridingFrameSettings.sssQualityLevel;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.SssCustomSampleBudget])
                overriddenFrameSettings.sssCustomSampleBudget = overridingFrameSettings.sssCustomSampleBudget;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.SssCustomDownsampleSteps])
                overriddenFrameSettings.sssCustomDownsampleSteps = overridingFrameSettings.sssCustomDownsampleSteps;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.LODBias])
                overriddenFrameSettings.lodBias = overridingFrameSettings.lodBias;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.LODBiasMode])
                overriddenFrameSettings.lodBiasMode = overridingFrameSettings.lodBiasMode;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.LODBiasQualityLevel])
                overriddenFrameSettings.lodBiasQualityLevel = overridingFrameSettings.lodBiasQualityLevel;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.MaximumLODLevel])
                overriddenFrameSettings.maximumLODLevel = overridingFrameSettings.maximumLODLevel;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.MaximumLODLevelMode])
                overriddenFrameSettings.maximumLODLevelMode = overridingFrameSettings.maximumLODLevelMode;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.MaximumLODLevelQualityLevel])
                overriddenFrameSettings.maximumLODLevelQualityLevel = overridingFrameSettings.maximumLODLevelQualityLevel;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.MaterialQualityLevel])
                overriddenFrameSettings.materialQuality = overridingFrameSettings.materialQuality;
            if (frameSettingsOverideMask.mask[(uint)FrameSettingsField.MSAAMode])
                overriddenFrameSettings.msaaMode = overridingFrameSettings.msaaMode;
        }

        /// <summary>Check FrameSettings with what is supported in RenderPipelineSettings and change value in order to be compatible.</summary>
        /// <param name="sanitizedFrameSettings">The FrameSettings being cleaned.</param>
        /// <param name="camera">Camera contais some necessary information to check how to sanitize.</param>
        /// <param name="renderPipelineSettings">Contains what is supported by the engine.</param>
        internal static void Sanitize(ref FrameSettings sanitizedFrameSettings, Camera camera, RenderPipelineSettings renderPipelineSettings)
        {
            bool reflection = camera.cameraType == CameraType.Reflection;
            // We have no clear flag to identify if a reflection is a planar reflection or a reflection probe. For now, the only way to do
            // it is to check if the matrix is oblique.
            bool reflectionPlanar = GeometryUtils.IsProjectionMatrixOblique(camera.projectionMatrix);
            bool preview = HDUtils.IsRegularPreviewCamera(camera);
            bool sceneViewFog = CoreUtils.IsSceneViewFogEnabled(camera);
            bool temporalAccumulationAllowed = !reflection || (reflection && reflectionPlanar);

            switch (renderPipelineSettings.supportedLitShaderMode)
            {
                case RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly:
                    sanitizedFrameSettings.litShaderMode = LitShaderMode.Forward;
                    break;
                case RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly:
                    sanitizedFrameSettings.litShaderMode = LitShaderMode.Deferred;
                    break;
                case RenderPipelineSettings.SupportedLitShaderMode.Both:
                    //nothing to do: keep previous value
                    break;
            }

            bool notPreview = !preview;
            bool transparentObjects = sanitizedFrameSettings.bitDatas[(int)FrameSettingsField.TransparentObjects];
            bool opaqueObjects = sanitizedFrameSettings.bitDatas[(int)FrameSettingsField.OpaqueObjects];

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ShadowMaps] &= notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Shadowmask] &= renderPipelineSettings.supportShadowMask && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ContactShadows] &= notPreview;
            bool pipelineSupportsRayTracing = HDRenderPipeline.PipelineSupportsRayTracing(renderPipelineSettings);
            // Ray tracing effects are not allowed on reflection probes due to the accumulation process.
            bool rayTracingActive = sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.RayTracing] &= pipelineSupportsRayTracing && notPreview && temporalAccumulationAllowed;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.RaytracingVFX] &= rayTracingActive;

            //MSAA only supported in forward and when not using ray tracing or water.
            if (sanitizedFrameSettings.litShaderMode != LitShaderMode.Forward || pipelineSupportsRayTracing || renderPipelineSettings.supportWater)
                sanitizedFrameSettings.msaaMode = MSAAMode.None;
            bool msaa = sanitizedFrameSettings.msaaMode == MSAAMode.FromHDRPAsset ? renderPipelineSettings.msaaSampleCount != MSAASamples.None : sanitizedFrameSettings.msaaMode != MSAAMode.None;

            // Screen space shadows are not compatible with MSAA
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ScreenSpaceShadows] &= renderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows && opaqueObjects & !msaa;

            // No recursive reflections
            bool ssr = sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.SSR] &= renderPipelineSettings.supportSSR && !msaa && notPreview && temporalAccumulationAllowed;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.TransparentSSR] &= ssr && renderPipelineSettings.supportSSRTransparent && transparentObjects && renderPipelineSettings.supportTransparentDepthPrepass;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Refraction] &= transparentObjects && notPreview;
            // Because the camera is shared between the faces of the reflection probes, we cannot allow effects that rely on the accumulation process
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.SSAO] &= renderPipelineSettings.supportSSAO && notPreview && opaqueObjects && temporalAccumulationAllowed;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.SSGI] &= renderPipelineSettings.supportSSGI && notPreview && opaqueObjects && temporalAccumulationAllowed;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.SubsurfaceScattering] &= renderPipelineSettings.supportSubsurfaceScattering;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.VolumetricClouds] &= renderPipelineSettings.supportVolumetricClouds && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.FullResolutionCloudsForSky] &= sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.VolumetricClouds];

            bool water = sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Water] &= sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Refraction] && renderPipelineSettings.supportWater && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.WaterDecals] &= water && renderPipelineSettings.supportWaterDecals;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.WaterExclusion] &= water && renderPipelineSettings.supportWaterExclusion;

            // Disable Lens Flares if they are unchecked in the HDRP Assets
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.LensFlareScreenSpace] &= sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Bloom] && sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.LensFlareScreenSpace] && renderPipelineSettings.supportScreenSpaceLensFlare;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.LensFlareDataDriven] &= sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.LensFlareDataDriven] && renderPipelineSettings.supportDataDrivenLensFlare;

            // We must take care of the scene view fog flags in the editor
            bool atmosphericScattering = sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.AtmosphericScattering] &= sceneViewFog && notPreview;

            // Volumetric are disabled if there is no atmospheric scattering
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Volumetrics] &= renderPipelineSettings.supportVolumetrics && atmosphericScattering; //&& notPreview induced by atmospheric scattering
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ReprojectionForVolumetrics] &= notPreview && temporalAccumulationAllowed;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.RenderingLayerMaskBuffer] &= renderPipelineSettings.renderingLayerMaskBuffer && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.LightLayers] &= renderPipelineSettings.supportLightLayers && notPreview;
            // We allow the user to enable exposure control on planar reflections, but not on reflection probes.
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ExposureControl] &= (!reflection || (reflectionPlanar && reflection)) && notPreview;

            // Planar and real time cubemap doesn't need post process and render in FP16
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Postprocess] &= !reflection && notPreview;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.TransparentPrepass] &= renderPipelineSettings.supportTransparentDepthPrepass && notPreview && transparentObjects;

            bool motionVector = sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.MotionVectors] &= renderPipelineSettings.supportMotionVectors && notPreview;

            // Object motion vector are disabled if motion vector are disabled
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ObjectMotionVectors] &= motionVector && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.TransparentsWriteMotionVector] &= motionVector && notPreview;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Decals] &= renderPipelineSettings.supportDecals && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.DecalLayers] &= renderPipelineSettings.supportDecalLayers && sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Decals];
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.TransparentPostpass] &= renderPipelineSettings.supportTransparentDepthPostpass && notPreview && transparentObjects;
            bool distortion = sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.Distortion] &= renderPipelineSettings.supportDistortion && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.RoughDistortion] &= distortion && notPreview;


            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.LowResTransparent] &= renderPipelineSettings.lowresTransparentSettings.enabled && transparentObjects;

            bool isAsyncEnabled = sanitizedFrameSettings.asyncEnabled;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.LightListAsync] &= isAsyncEnabled;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.SSRAsync] &= isAsyncEnabled;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.SSAOAsync] &= isAsyncEnabled;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ContactShadowsAsync] &= (isAsyncEnabled && !rayTracingActive);
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.VolumeVoxelizationsAsync] &= isAsyncEnabled;
			sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.HighQualityLinesAsync] &= isAsyncEnabled;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.CustomPass] &= renderPipelineSettings.supportCustomPass;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.CustomPass] &= camera.cameraType != CameraType.Preview;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.VariableRateShading] &= renderPipelineSettings.supportCustomPass;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.VariableRateShading] &= renderPipelineSettings.supportVariableRateShading;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.VariableRateShading] &= camera.cameraType == CameraType.Game;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.CustomPostProcess] &= camera.cameraType != CameraType.Preview;

            // Deferred opaque are always using Fptl. Forward opaque can use Fptl or Cluster, transparent use cluster.
            // When MSAA is enabled we disable Fptl as it become expensive compare to cluster
            // In HD, MSAA is only supported for forward only rendering, no MSAA in deferred mode (for code complexity reasons)
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.FPTLForForwardOpaque] &= !msaa;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.AdaptiveProbeVolume] &= renderPipelineSettings.supportProbeVolume && notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.NormalizeReflectionProbeWithProbeVolume] &= renderPipelineSettings.supportProbeVolume;

            // We disable reflection probes and planar reflections in regular preview rendering for two reasons.
            // - Performance: Realtime reflection are 99% not necessary in previews
            // - Static lighting consistency: When rendering a planar probe from a preview camera it may induce a recomputing of the static lighting
            //   but with the preview lights which are different from the ones in the scene and will change the result inducing flickering.
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.ReflectionProbe] &= notPreview;
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.PlanarProbe] &= notPreview;

            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.SubsurfaceScattering] &= opaqueObjects;

#if !ENABLE_VIRTUALTEXTURES
            sanitizedFrameSettings.bitDatas[(uint)FrameSettingsField.VirtualTexturing] = false;
#endif
        }

        /// <summary>Aggregation is default with override of the renderer then sanitized depending on supported features of hdrpasset.</summary>
        /// <param name="aggregatedFrameSettings">The aggregated FrameSettings result.</param>
        /// <param name="camera">The camera rendering.</param>
        /// <param name="additionalData">Additional data of the camera rendering.</param>
        /// <param name="hdrpAsset">HDRenderPipelineAsset contening default FrameSettings.</param>
        internal static void AggregateFrameSettings(RenderingPathFrameSettings defaultRenderingPathFrameSettings, ref FrameSettings aggregatedFrameSettings, Camera camera,
            HDAdditionalCameraData additionalData, HDRenderPipelineAsset hdrpAsset)
        {
            var type = additionalData != null ? additionalData.defaultFrameSettings : FrameSettingsRenderType.Camera;
            AggregateFrameSettings(
                ref aggregatedFrameSettings,
                camera,
                additionalData,
                ref defaultRenderingPathFrameSettings.GetDefaultFrameSettings(type), //fallback on Camera for SceneCamera and PreviewCamera
                hdrpAsset.currentPlatformRenderPipelineSettings);
        }

        // Note: this version is the one tested as there is issue getting HDRenderPipelineAsset in batchmode in unit test framework currently.
        /// <summary>Aggregation is default with override of the renderer then sanitized depending on supported features of hdrpasset.</summary>
        /// <param name="aggregatedFrameSettings">The aggregated FrameSettings result.</param>
        /// <param name="camera">The camera rendering.</param>
        /// <param name="additionalData">Additional data of the camera rendering.</param>
        /// <param name="defaultFrameSettings">Base framesettings to copy prior any override.</param>
        /// <param name="supportedFeatures">Currently supported feature for the sanitization pass.</param>
        internal static void AggregateFrameSettings(ref FrameSettings aggregatedFrameSettings, Camera camera, HDAdditionalCameraData additionalData, ref FrameSettings defaultFrameSettings, RenderPipelineSettings supportedFeatures)
        {
            aggregatedFrameSettings = defaultFrameSettings; //fallback on Camera for SceneCamera and PreviewCamera
            if (additionalData != null && additionalData.customRenderingSettings)
                Override(ref aggregatedFrameSettings, additionalData.renderingPathCustomFrameSettings, additionalData.renderingPathCustomFrameSettingsOverrideMask);
            Sanitize(ref aggregatedFrameSettings, camera, supportedFeatures);
        }

        /// <summary>
        /// Equality operator between two FrameSettings. Return `true` if equivalent. (comparison of content).
        /// </summary>
        /// <param name="a">First frame settings.</param>
        /// <param name="b">Second frame settings.</param>
        /// <returns>True if both settings are equal.</returns>
        public static bool operator ==(FrameSettings a, FrameSettings b)
            => a.bitDatas == b.bitDatas
            && a.sssQualityMode == b.sssQualityMode
            && a.sssQualityLevel == b.sssQualityLevel
            && a.sssCustomSampleBudget == b.sssCustomSampleBudget
            && a.sssCustomDownsampleSteps == b.sssCustomDownsampleSteps
            && a.lodBias == b.lodBias
            && a.lodBiasMode == b.lodBiasMode
            && a.lodBiasQualityLevel == b.lodBiasQualityLevel
            && a.maximumLODLevel == b.maximumLODLevel
            && a.maximumLODLevelMode == b.maximumLODLevelMode
            && a.maximumLODLevelQualityLevel == b.maximumLODLevelQualityLevel
            && a.materialQuality == b.materialQuality
            && a.msaaMode == b.msaaMode;

        /// <summary>
        /// Inequality operator between two FrameSettings. Return `true` if different. (comparison of content).
        /// </summary>
        /// <param name="a">First frame settings.</param>
        /// <param name="b">Second frame settings.</param>
        /// <returns>True if settings are not equal.</returns>
        public static bool operator !=(FrameSettings a, FrameSettings b) => !(a == b);

        /// <summary>
        /// Equality operator between two FrameSettings. Return `true` if equivalent. (comparison of content).
        /// </summary>
        /// <param name="obj">Frame Settings to compare to.</param>
        /// <returns>True if both settings are equal.</returns>
        public override bool Equals(object obj)
            => (obj is FrameSettings)
            && bitDatas.Equals(((FrameSettings)obj).bitDatas)
            && sssQualityMode.Equals(((FrameSettings)obj).sssQualityMode)
            && sssQualityLevel.Equals(((FrameSettings)obj).sssQualityLevel)
            && sssCustomSampleBudget.Equals(((FrameSettings)obj).sssCustomSampleBudget)
            && sssCustomDownsampleSteps.Equals(((FrameSettings)obj).sssCustomDownsampleSteps)
            && lodBias.Equals(((FrameSettings)obj).lodBias)
            && lodBiasMode.Equals(((FrameSettings)obj).lodBiasMode)
            && lodBiasQualityLevel.Equals(((FrameSettings)obj).lodBiasQualityLevel)
            && maximumLODLevel.Equals(((FrameSettings)obj).maximumLODLevel)
            && maximumLODLevelMode.Equals(((FrameSettings)obj).maximumLODLevelMode)
            && maximumLODLevelQualityLevel.Equals(((FrameSettings)obj).maximumLODLevelQualityLevel)
            && materialQuality.Equals(((FrameSettings)obj).materialQuality)
            && msaaMode.Equals(((FrameSettings)obj).msaaMode);


        /// <summary>
        /// Returns the hash code of this object.
        /// </summary>
        /// <returns>Hash code of the frame settings.</returns>
        public override int GetHashCode()
        {
            var hashCode = 1474027755;

            hashCode = hashCode * -1521134295 + bitDatas.GetHashCode();
            hashCode = hashCode * -1521134295 + sssQualityMode.GetHashCode();
            hashCode = hashCode * -1521134295 + sssQualityLevel.GetHashCode();
            hashCode = hashCode * -1521134295 + sssCustomSampleBudget.GetHashCode();
            hashCode = hashCode * -1521134295 + sssCustomDownsampleSteps.GetHashCode();
            hashCode = hashCode * -1521134295 + lodBias.GetHashCode();
            hashCode = hashCode * -1521134295 + lodBiasMode.GetHashCode();
            hashCode = hashCode * -1521134295 + lodBiasQualityLevel.GetHashCode();
            hashCode = hashCode * -1521134295 + maximumLODLevel.GetHashCode();
            hashCode = hashCode * -1521134295 + maximumLODLevelMode.GetHashCode();
            hashCode = hashCode * -1521134295 + maximumLODLevelQualityLevel.GetHashCode();
            hashCode = hashCode * -1521134295 + materialQuality.GetHashCode();
            hashCode = hashCode * -1521134295 + msaaMode.GetHashCode();

            return hashCode;
        }

        #region DebuggerDisplay

        [DebuggerDisplay("{m_Value}", Name = "{m_Label,nq}")]
        internal class DebuggerEntry
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            string m_Label;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            object m_Value;

            public DebuggerEntry(string label, object value)
            {
                m_Label = label;
                m_Value = value;
            }
        }

        [DebuggerDisplay("", Name = "{m_GroupName,nq}")]
        internal class DebuggerGroup
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            string m_GroupName;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public DebuggerEntry[] m_Entries;

            public DebuggerGroup(string groupName, DebuggerEntry[] entries)
            {
                m_GroupName = groupName;
                m_Entries = entries;
            }
        }

        internal class FrameSettingsDebugView
        {
            const int numberOfNonBitValues = 2;

            FrameSettings m_FrameSettings;

            public FrameSettingsDebugView(FrameSettings frameSettings)
                => m_FrameSettings = frameSettings;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public DebuggerGroup[] Keys
            {
                get
                {
                    // the following cannot really be cached as this class is reconstructed at each code step while debugging
                    Array bitValues = Enum.GetValues(typeof(FrameSettingsField));
                    int bitsLength = bitValues.Length;

                    var attributes = new Dictionary<FrameSettingsField, FrameSettingsFieldAttribute>();
                    var groups = new List<DebuggerGroup>();

                    Dictionary<FrameSettingsField, string> frameSettingsEnumNameMap = FrameSettingsFieldAttribute.GetEnumNameMap();
                    Type type = typeof(FrameSettingsField);
                    var noAttribute = new List<FrameSettingsField>();
                    foreach (FrameSettingsField enumVal in frameSettingsEnumNameMap.Keys)
                    {
                        attributes[enumVal] = type.GetField(frameSettingsEnumNameMap[enumVal]).GetCustomAttribute<FrameSettingsFieldAttribute>();
                        if (attributes[enumVal] == null)
                            noAttribute.Add(enumVal);
                    }

                    var groupIndexes = attributes.Values.Where(a => a != null).Select(a => a.group).Distinct();
                    foreach (int groupIndex in groupIndexes)
                        groups.Add(new DebuggerGroup(FrameSettingsHistory.foldoutNames[groupIndex], attributes?.Where(pair => pair.Value?.group == groupIndex)?.OrderBy(pair => pair.Value.orderInGroup).Select(kvp => new DebuggerEntry(Enum.GetName(typeof(FrameSettingsField), kvp.Key), m_FrameSettings.bitDatas[(uint)kvp.Key])).ToArray()));

                    groups.Add(new DebuggerGroup("Bits without attribute", noAttribute.Where(fs => fs != FrameSettingsField.None)?.Select(fs => new DebuggerEntry(Enum.GetName(typeof(FrameSettingsField), fs), m_FrameSettings.bitDatas[(uint)fs])).ToArray()));

                    groups.Add(new DebuggerGroup("Non Bit data", new DebuggerEntry[]
                    {
                        new DebuggerEntry("sssQualityMode", m_FrameSettings.sssQualityMode),
                        new DebuggerEntry("sssQualityLevel", m_FrameSettings.sssQualityLevel),
                        new DebuggerEntry("sssCustomSampleBudget", m_FrameSettings.sssCustomSampleBudget),
                        new DebuggerEntry("sssCustomDownSampleSteps", m_FrameSettings.sssCustomDownsampleSteps),
                        new DebuggerEntry("lodBias", m_FrameSettings.lodBias),
                        new DebuggerEntry("lodBiasMode", m_FrameSettings.lodBiasMode),
                        new DebuggerEntry("lodBiasQualityLevel", m_FrameSettings.lodBiasQualityLevel),
                        new DebuggerEntry("maximumLODLevel", m_FrameSettings.maximumLODLevel),
                        new DebuggerEntry("maximumLODLevelMode", m_FrameSettings.maximumLODLevelMode),
                        new DebuggerEntry("maximumLODLevelQualityLevel", m_FrameSettings.maximumLODLevelQualityLevel),
                        new DebuggerEntry("materialQuality", m_FrameSettings.materialQuality),
                        new DebuggerEntry("msaaMode", m_FrameSettings.msaaMode),
                    }));

                    return groups.ToArray();
                }
            }
        }

        #endregion
    }

    //Keep it internal for now. We need to update the whole system of FrameSettings in future versions
    /// <summary>
    /// Use this attribute to specify path to a FrameSettingsOverrideMask to use when drawing Inspectors
    /// </summary>
    /// <example>
    ///     public class FrameSettingsHandler : MonoBehaviour
    ///     {
    ///         [UseOverrideMask(nameof(m_FrameSettingsOverride))]
    ///         [SerializeField] FrameSettings m_FrameSettings = FrameSettings.Create();
    ///         [SerializeField, HideInInspector] FrameSettingsOverrideMask m_FrameSettingsOverride;
    ///     }
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    class UseOverrideMaskAttribute : Attribute
    {
#if UNITY_EDITOR
        /// <summary>Path to look for mask</summary>
        public readonly string pathToOverrideMask;
        public readonly FrameSettingsRenderType defaultValuesToUse;
#endif
        /// <summary> Constructor </summary>
        /// <param name="pathToOverrideMask">Path to look for mask</param>
        public UseOverrideMaskAttribute(string pathToOverrideMask, FrameSettingsRenderType defaultValuesToUse)
        {
#if UNITY_EDITOR
            this.pathToOverrideMask = pathToOverrideMask;
            this.defaultValuesToUse = defaultValuesToUse;
#endif
        }
    }
}
