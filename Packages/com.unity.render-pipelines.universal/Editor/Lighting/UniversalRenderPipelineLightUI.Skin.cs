using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    partial class UniversalRenderPipelineLightUI
    {
        internal static class Styles
        {
            public static readonly GUIContent Type = L10n.TextContent("Type", "Specifies the current type of light. Possible types are Directional, Spot, Point, and Area lights.", null, null);

            public static readonly GUIContent AreaLightShapeContent = L10n.TextContent("Shape", "Specifies the shape of the Area light. Possible types are Rectangle and Disc.", null, null);
            public static readonly GUIContent[] LightTypeTitles = { L10n.TextContent("Spot", null, null, null), L10n.TextContent("Directional", null, null, null), L10n.TextContent("Point", null, null, null), L10n.TextContent("Area (baked only)", null, null, null) };
            public static readonly int[] LightTypeValues = { (int)LightType.Spot, (int)LightType.Directional, (int)LightType.Point, (int)LightType.Rectangle };

            public static readonly GUIContent[] AreaLightShapeTitles = { L10n.TextContent("Rectangle", null, null, null), L10n.TextContent("Disc", null, null, null) };
            public static readonly int[] AreaLightShapeValues = { (int)LightType.Rectangle, (int)LightType.Disc };

            public static readonly GUIContent SpotAngle = L10n.TextContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.", null, null);

            public static readonly GUIContent BakingWarning = L10n.TextContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.", null, null, null);
            public static readonly GUIContent DisabledLightWarning = L10n.TextContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.", null, null, null);
            public static readonly GUIContent SunSourceWarning = L10n.TextContent("This light is set as the current Sun Source, which requires a directional light. Go to the Lighting Window's Environment settings to edit the Sun Source.", null, null, null);
            public static readonly GUIContent CullingMask = L10n.TextContent("Culling Mask", "Specifies which lights are culled per camera. To control exclude certain lights affecting certain objects, use Rendering Layers instead, which is supported across all rendering paths.", null, null);
            public static readonly GUIContent CullingMaskWarning = L10n.TextContent("Culling Mask should be used to control which lights are culled per camera. If you want to exclude certain lights from affecting certain objects, use Rendering Layers on the Light, and Rendering Layer Mask on the Mesh Renderer.", null, null, null);

            public static readonly GUIContent ShadowRealtimeSettings = L10n.TextContent("Realtime Shadows", "Settings for realtime direct shadows.", null, null);
            public static readonly GUIContent ShadowStrength = L10n.TextContent("Strength", "Controls how dark the shadows cast by the light will be.", null, null);
            public static readonly GUIContent ShadowNearPlane = L10n.TextContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.", null, null);
            public static readonly GUIContent ShadowNormalBiasCaster = L10n.TextContent("Normal", "Determines the bias this Light applies along the normal of surfaces it illuminates when calculating shadows. This is ignored for point lights. The normal bias mode can be changed in Project Settings > Graphics > URP.", null, null);
            public static readonly GUIContent ShadowNormalBiasReceiver = L10n.TextContent("Normal", "Determines the distance by which this Light offsets the shadow-map sampling position along the surface normal of the shadow receiver. This also applies to point lights. The normal bias mode can be changed in Project Settings > Graphics > URP.", null, null);
            public static readonly GUIContent ShadowDepthBias = L10n.TextContent("Depth Bias", "Determines the bias at which shadows are pushed away from the shadow-casting Game Object along the line from the Light. The depth bias mode can be changed in Project Settings > Graphics > URP.", null, null);
            public static readonly GUIContent ShadowSlopeScaleDepthBias = L10n.TextContent("Slope-Scale Depth Bias", "Determines the slope-scaled depth bias this Light applies while rendering the shadow map: surfaces at a grazing angle to the light receive a proportionally larger depth offset. The depth bias mode can be changed in Project Settings > Graphics > URP.", null, null);
            public static readonly GUIContent ShadowInfo = L10n.TextContent("Unity might reduce the Light's shadow resolution to ensure that shadow maps fit in the shadow atlas. Consider this when selecting the the size of the shadow atlas, the shadow resolution of Lights, the number of Lights in your scene and whether you use soft shadows.", null, null, null);

            // Resolution (default or custom)
            public static readonly GUIContent ShadowResolution = L10n.TextContent("Resolution", $"Sets the rendered resolution of the shadow maps. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage. Rounded to the next power of two, and clamped to be at least {UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution}.", null, null);
            public static readonly int[] ShadowResolutionDefaultValues =
            {
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom,
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow,
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium,
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh
            };
            public static readonly GUIContent[] ShadowResolutionDefaultOptions =
            {
                L10n.TextContent("Custom", null, null, null),
                UniversalRenderPipelineAssetUI.Styles.additionalLightsShadowResolutionTierNames[0],
                UniversalRenderPipelineAssetUI.Styles.additionalLightsShadowResolutionTierNames[1],
                UniversalRenderPipelineAssetUI.Styles.additionalLightsShadowResolutionTierNames[2],
            };

            public static GUIContent SoftShadowQuality = L10n.TextContent("Soft Shadows Quality", "Controls the filtering quality of soft shadows. Higher quality has lower performance.", null, null);

            // PCSS
            public static GUIContent PCSSHeader = L10n.TextContent("Soft Shadows", "Settings for Percentage-Closer Soft Shadows (PCSS), which widen the penumbra with the distance between the caster and the receiver. These settings apply when PCSS is enabled in the URP Asset and the resolved soft shadow quality is High.", null, null);
            public static GUIContent PCSSAngularDiameter = L10n.TextContent("Angular Diameter", "Controls the angular diameter of the light in degrees, which sets how wide the PCSS penumbra grows with distance. The sun is roughly 0.5 degrees.", null, null);
            public static GUIContent PCSSSoftnessScale = L10n.TextContent("Softness Scale", "Controls the multiplier applied to the PCSS penumbra size, so shadows can be softened without changing the size of the light.", null, null);
            public static GUIContent PCSSSampleQuality = L10n.TextContent("PCSS Samples", "Controls how many shadow map samples PCSS takes per pixel. Higher quality reduces noise but has lower performance. Lower settings rely on temporal anti-aliasing to hide noise. Select Custom to set the sample counts and tuning values yourself.", null, null);
            public static GUIContent PCSSBlockerSampleCount = L10n.TextContent("Blocker Sample Count", "Controls the number of samples the PCSS blocker search takes. Higher values reduce noise but have lower performance.", null, null);
            public static GUIContent PCSSFilterSampleCount = L10n.TextContent("Filter Sample Count", "Controls the number of samples the PCSS filtering phase takes. Higher values reduce noise but have lower performance.", null, null);
            public static GUIContent PCSSMaxPenumbraSize = L10n.TextContent("Max Penumbra Size", "Controls the world-space upper bound on the PCSS penumbra size. This acts as a safety clamp.", null, null);
            public static GUIContent PCSSMaxSamplingDistance = L10n.TextContent("Max Sampling Distance", "Controls the world-space distance, at most, that PCSS moves the depth each sample compares against towards the light. This does not move the samples across the shadow map. Useful for reducing light bleeding.", null, null);
            public static GUIContent PCSSMinFilterSizeTexels = L10n.TextContent("Min Filter Size (Texels)", "Controls the smallest PCSS filter radius in shadow map texels. Useful for hiding flickering at shadow contact points.", null, null);
            public static GUIContent PCSSMinFilterSize = L10n.TextContent("Min Filter Size", "Controls the smallest PCSS filter radius. The range 0 to 1 maps to 0 to about 4 shadow map texels. Useful for hiding flickering at shadow contact points.", null, null);
            public static GUIContent PCSSMinFilterMaxAngularDiameter = L10n.TextContent("Min Filter Max Angle", "Controls the angle in degrees the minimum PCSS filter cone widens to. Values below Blocker Search Angle, or below Angular Diameter multiplied by Softness Scale, are raised to the larger of the two.", null, null);
            public static GUIContent PCSSBlockerSearchAngularDiameter = L10n.TextContent("Blocker Search Angle", "Controls the angle in degrees of the PCSS blocker search cone. Wider angles reduce light bleeding. Values below Angular Diameter multiplied by Softness Scale are raised to it.", null, null);
            public static GUIContent PCSSBlockerSamplingClumpExponent = L10n.TextContent("Blocker Clumping", "Controls how strongly the PCSS blocker search samples clump towards the center of the search cone.", null, null);

            // {0} is the URP Asset toggle label
            public static GUIContent PCSSNotEnabledInAsset = L10n.TextContent("These parameters only take effect once {0} is enabled in the Shadows section of the active URP Asset.", null, null, null);
            public static GUIContent PCSSNotListedBelowHighQuality = L10n.TextContent("These parameters only take effect once {0} is enabled in the Shadows section of the active URP Asset, which lists it once Soft Shadows Quality there is set to High.", null, null, null);

            // Bias (default or custom)
            public static GUIContent shadowBias = L10n.TextContent("Bias", "Select if the Bias should use the settings from the Pipeline Asset or Custom settings.", null, null);
            public static int[] optionDefaultValues = { 0, 1 };
            public static GUIContent[] displayedDefaultOptions =
            {
                L10n.TextContent("Custom", null, null, null),
                L10n.TextContent("Use settings from Render Pipeline Asset", null, null, null)
            };

            public static readonly GUIContent customShadowLayers = L10n.TextContent("Custom Shadow Layers", "When enabled, you can use the Layer property below to specify the layers for shadows separately to lighting. When disabled, the Light Layer property in the General section specifies the layers for both lighting and for shadows.", null, null);
            public static readonly GUIContent ShadowLayer = L10n.TextContent("Layer", "Specifies the light layer to use for shadows.", null, null);

#if VOLUMETRIC_FOG
            public static readonly GUIContent VolumetricsHeader = L10n.TextContent("Volumetrics", "Settings that control how this light interacts with volumetric fog.", null, null);
            public static readonly GUIContent VolumetricEnable = L10n.TextContent("Enable", "When enabled, this light interacts with volumetric fog.", null, null);
            public static readonly GUIContent VolumetricMultiplier = L10n.TextContent("Multiplier", "Controls the intensity of the scattered volumetric lighting.", null, null);
            public static readonly GUIContent VolumetricShadowDimmer = L10n.TextContent("Shadow Dimmer", "Dims the volumetric shadows this light casts. Set to 0 to skip shadow sampling in volumetric fog.", null, null);
#endif

            public static readonly GUIContent LightCookieSize = L10n.TextContent("Cookie Size", "Controls the size of the cookie mask currently assigned to the light.", null, null);
            public static readonly GUIContent LightCookieOffset = L10n.TextContent("Cookie Offset", "Controls the offset of the cookie mask currently assigned to the light.", null, null);
            /// <summary>Title with "Rendering Layer"</summary>
            public static readonly GUIContent RenderingLayers = L10n.TextContent("Rendering Layers", "Select the Rendering Layers that the Light affects. This Light affects objects where at least one Rendering Layer value matches.", null, null);
            public static readonly GUIContent RenderingLayersHelpBox = L10n.TextContentWithIcon("Rendering Layers are disabled by default because they have a small GPU performance cost. Enable them in the active Universal Render Pipeline Asset, under Lighting -> Use Rendering Layers.", MessageType.Info, null);
        }
    }
}
