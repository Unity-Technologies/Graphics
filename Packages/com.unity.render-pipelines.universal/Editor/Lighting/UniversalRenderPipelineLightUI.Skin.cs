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
            public static readonly GUIContent ShadowNormalBias = L10n.TextContent("Normal", "Determines the bias this Light applies along the normal of surfaces it illuminates. This is ignored for point lights.", null, null);
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
            public static readonly GUIContent VolumetricsHeader = EditorGUIUtility.TrTextContent("Volumetrics", "Settings that control how this light interacts with volumetric fog.");
            public static readonly GUIContent VolumetricEnable = EditorGUIUtility.TrTextContent("Enable", "When enabled, this light interacts with volumetric fog.");
            public static readonly GUIContent VolumetricMultiplier = EditorGUIUtility.TrTextContent("Multiplier", "Controls the intensity of the scattered volumetric lighting.");
            public static readonly GUIContent VolumetricShadowDimmer = EditorGUIUtility.TrTextContent("Shadow Dimmer", "Dims the volumetric shadows this light casts. Set to 0 to skip shadow sampling in volumetric fog.");
#endif

            public static readonly GUIContent LightCookieSize = L10n.TextContent("Cookie Size", "Controls the size of the cookie mask currently assigned to the light.", null, null);
            public static readonly GUIContent LightCookieOffset = L10n.TextContent("Cookie Offset", "Controls the offset of the cookie mask currently assigned to the light.", null, null);
            /// <summary>Title with "Rendering Layer"</summary>
            public static readonly GUIContent RenderingLayers = L10n.TextContent("Rendering Layers", "Select the Rendering Layers that the Light affects. This Light affects objects where at least one Rendering Layer value matches.", null, null);
            public static readonly GUIContent RenderingLayersHelpBox = EditorGUIUtility.TrTextContentWithIcon($"Rendering Layers are disabled by default because they have a small GPU performance cost. Enable them in the active Universal Render Pipeline Asset, under Lighting -> Use Rendering Layers.", CoreEditorStyles.GetMessageTypeIcon(MessageType.Info));
        }
    }
}
