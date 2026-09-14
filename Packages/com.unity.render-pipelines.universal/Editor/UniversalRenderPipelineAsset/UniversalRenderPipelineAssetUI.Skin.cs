using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineAssetUI
    {
        internal static class Styles
        {
            // Groups
            public static GUIContent renderingSettingsText = L10n.TextContent("Rendering", "Settings that control the core part of the pipeline rendered frame.", null, null);
            public static GUIContent qualitySettingsText = L10n.TextContent("Quality", "Settings that control the quality level of the Render pipeline, improving performance and graphics quality.", null, null);
            public static GUIContent lightingSettingsText = L10n.TextContent("Lighting", "Settings that affect the lighting in the Scene", null, null);
            public static GUIContent shadowSettingsText = L10n.TextContent("Shadows", "Settings that configure how shadows look and behave, and can be used to balance between the visual quality and performance of shadows.", null, null);
            public static GUIContent postProcessingSettingsText = L10n.TextContent("Post-processing", "Settings that allow for fine tuning of post-processing effects in the Scene when this Render Pipeline Asset is in use.", null, null);
            public static GUIContent volumeSettingsText = L10n.TextContent("Volumes", "Settings related to usage of Volume Components.", null, null);
            public static GUIContent adaptivePerformanceText = L10n.TextContent("Adaptive Performance", null, null, null);

            // Rendering
            public static GUIContent rendererHeaderText = L10n.TextContent("Renderer List", "Lists all the renderers available to this Render Pipeline Asset.", null, null);
            public static GUIContent rendererDefaultText = L10n.TextContent("Default", "This renderer is currently the default for the render pipeline.", null, null);
            public static GUIContent rendererSetDefaultText = L10n.TextContent("Set Default", "Makes this renderer the default for the render pipeline.", null, null);
            public static GUIContent rendererSettingsText = L10n.IconContent("_Menu", "Opens settings for this renderer.", null);
            public static GUIContent requireDepthTextureText = L10n.TextContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.", null, null);
            public static GUIContent requireOpaqueTextureText = L10n.TextContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.", null, null);
            public static GUIContent opaqueDownsamplingText = L10n.TextContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture", null, null);
            public static GUIContent supportsTerrainHolesText = L10n.TextContent("Terrain Holes", "When disabled, Universal Rendering Pipeline removes all Terrain hole Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static GUIContent srpBatcher = L10n.TextContent("SRP Batcher", "If enabled, the render pipeline uses the SRP batcher.", null, null);
            public static GUIContent storeActionsOptimizationText = L10n.TextContent("Store Actions", "Sets the store actions policy on tile based GPUs. Affects render targets memory usage and will impact performance.", null, null);
            public static GUIContent dynamicBatching = L10n.TextContent("Dynamic Batching", "If enabled, the render pipeline will batch drawcalls with few triangles together by copying their vertex buffers into a shared buffer on a per-frame basis.", null, null);
            public static GUIContent warningDynamicBatching = L10n.TextContent("Dynamic Batching has been removed due to limited performance benefits on modern hardware. This option no longer has any effect. Use SRP Batcher or GPU Instancing instead.", null, null, null);

            // Quality
            public static GUIContent hdrText = L10n.TextContent("HDR", "Controls the global HDR settings.", null, null);
            public static GUIContent hdrColorBufferPrecisionText = L10n.TextContent("HDR Precision", "Controls the precision of the camera color buffer in HDR rendering. 32-bits is the default. 64-bits can reduce banding artifacts at the cost of memory and performance.", null, null);
            public static GUIContent msaaText = L10n.TextContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.", null, null);
            public static GUIContent renderScaleText = L10n.TextContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution.", null, null);
            public static GUIContent upscalingFilterText = L10n.TextContent("Upscaling Filter", "Controls the type of filter used for upscaling when render scale is lower than 1.0.", null, null);
            public static GUIContent fsrOverrideSharpness = L10n.TextContent("Override FSR Sharpness", "Overrides the FSR sharpness value for the render pipeline asset.", null, null);
            public static GUIContent fsrSharpnessText = L10n.TextContent("FSR Sharpness", "Controls the intensity of the sharpening filter used by FidelityFX Super Resolution.", null, null);
            public static GUIContent enableLODCrossFadeText = L10n.TextContent("LOD Cross Fade", "Controls whether LOD Cross Fade enabled or disabled.", null, null);
            public static GUIContent lodCrossFadeDitheringTypeText = L10n.TextContent("LOD Cross Fade Dithering Type", "Controls the LOD Cross Fade Dithering Type that will be used to draw Renderer LOD when LODGroup has CrossFade Fade Mode selected.", null, null);
            public static GUIContent shEvalModeText = L10n.TextContent("SH Evaluation Mode", "Defines the Spherical Harmonic (SH) lighting evaluation type (per vertex, per pixel, or mixed).", null, null);
            public static readonly string xrUpscalingInfo = L10n.Tr("When targeting an XR device, the XR runtime or compositor may provide anti-aliasing and upscaling. In that case this URP upscaler can be redundant or ignored - review your XR provider settings to configure anti-aliasing and upscaling.", null);
            public static readonly string xrUpscalingInfoButton = L10n.Tr("Open Project Validation", null);
            public static GUIContent lightFalloffModeText = L10n.TextContent("Light Falloff Mode", "How light intensity falls off with distance. Use Linear to match the Built-in Render Pipeline.", null, null);
            public static readonly string stpMobilePlatformWarning = L10n.Tr("STP is selected for use on a mobile platform. STP is only supported on modern compute-capable hardware and its performance overhead may make it impractical on lower-end devices.", null);
#if ENABLE_UPSCALER_FRAMEWORK
            public static GUIContent scalingModeText = L10n.TextContent("Scaling Mode", "Whether the camera renders below, at, or above the display resolution. This sets the available Render Scale range.", null, null);
            public static readonly string renderScaleQualityModeInfo = L10n.Tr("Render Scale is ignored because this upscaler's Resolution Mode is set to Quality Mode, which controls the render resolution. Set it to Custom Scaling to drive the render resolution from Render Scale.", null);
            public static readonly string upscalingNoUpscalersWarning = L10n.Tr("Scaling is set to Upscaling but the priority list is empty. No upscaling will be performed.", null);
            public static readonly string upscalerPriorityMultiSelectWarning = L10n.Tr("Upscaler Priority can't be edited while multiple Render Pipeline Assets are selected.", null);
            public static GUIContent upscalerPriorityHeaderText = L10n.TextContent("Upscaler Priority", "The upscalers to use, from highest to lowest priority. The first upscaler supported by the target platform is used.", null, null);
            public static GUIContent openPackageManagerText = L10n.TextContent("Open Package Manager", null, null, null);
            public static GUIContent upscalerPlatformFallbackIcon = L10n.IconContent("BuildSettings.Editor.Small", null, null);
            public static GUIContent upscalerNotRegisteredWarning = L10n.IconContent("console.warnicon", "This upscaler isn't available for the active build target or the package that provides it isn't installed.", null);
            public static GUIContent upscalerGraphicsAPIBadge = L10n.IconContent("console.warnicon.sml", null, null);
            public static readonly string supportedGraphicsAPIs = L10n.Tr("Supported: {0}", null);
            public static readonly string unsupportedGraphicsAPIs = L10n.Tr("Unsupported: {0}", null);
#endif

            // Main light
            public static GUIContent mainLightRenderingModeText = L10n.TextContent("Main Light", "Main light is the brightest directional light.", null, null);
            public static GUIContent supportsMainLightShadowsText = L10n.TextContent("Cast Shadows", "If enabled, the main light can be a shadow casting light.", null, null);
            public static GUIContent mainLightShadowmapResolutionText = L10n.TextContent("Shadow Resolution", "Resolution of the main light shadowmap texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the maximum shadows atlas resolution.", null, null);

            // Probe volumes
            public static readonly GUIContent lightProbeSystemContent = L10n.TextContent("Light Probe System", "What system to use for Light Probes.", null, null);
            public static readonly GUIContent probeVolumeMemoryBudget = L10n.TextContent("Memory Budget", "Determines the width and height of the 3D textures used to store lighting data from probes. Depth is fixed.", null, null);
            public static readonly GUIContent probeVolumeBlendingMemoryBudget = L10n.TextContent("Blending Memory Budget", "Determines the width and height of the 3D textures used to store light scenario blending data from probes. Depth is fixed.", null, null);
            public static readonly GUIContent supportProbeVolumeGPUStreaming = L10n.TextContent("Enable GPU Streaming", "Enable streaming of Cells for Adaptive Probe Volumes.", null, null);
            public static readonly GUIContent supportProbeVolumeDiskStreaming = L10n.TextContent("Enable Disk Streaming", "Enable streaming of Cells from disk for Adaptive Probe Volumes.", null, null);
            public static readonly GUIContent supportProbeVolumeScenarios = L10n.TextContent("Enable Lighting Scenarios", "Enable Lighting Scenario Baking for Adaptive Probe Volumes.", null, null);
            public static readonly GUIContent supportProbeVolumeScenarioBlending = L10n.TextContent("Enable Lighting Scenario Blending", "Enable Lighting Scenario Blending for Adaptive Probe Volumes.\nNote: Lighting Scenario Blending requires Compute Shader support.", null, null);
            public static readonly GUIContent probeVolumeSHBands = L10n.TextContent("SH Bands", "The number of Spherical Harmonic bands used by Adaptive Probe Volumes to store lighting data. Choosing L2 provides better quality but with higher memory and runtime costs.", null, null);

            // Additional lights
            public static GUIContent addditionalLightsRenderingModeText = L10n.TextContent("Additional Lights", "Additional lights support.", null, null);
            public static GUIContent perObjectLimit = L10n.TextContent("Per Object Limit", "Maximum amount of additional lights. These lights are sorted and culled per-object.", null, null);
            public static GUIContent supportsAdditionalShadowsText = L10n.TextContent("Cast Shadows", "If enabled, shadows will be supported for spot and point lights.", null, null);
            public static GUIContent additionalLightsShadowmapResolution = L10n.TextContent("Shadow Atlas Resolution", "All additional lights are packed into a single shadowmap atlas. This setting controls the atlas size.", null, null);
            public static GUIContent additionalLightsShadowResolutionTiers = L10n.TextContent("Shadow Resolution Tiers", $"Additional Lights Shadow Resolution Tiers. Rounded to the next power of two, and clamped to be at least {UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution}.", null, null);
            public static GUIContent[] additionalLightsShadowResolutionTierNames =
            {
                new("Low"),
                new("Medium"),
                new("High")
            };
            public static GUIContent additionalLightsCookieResolution = L10n.TextContent("Cookie Atlas Resolution", "All additional lights are packed into a single cookie atlas. This setting controls the atlas size.", null, null);
            public static GUIContent additionalLightsCookieFormat = L10n.TextContent("Cookie Atlas Format", "All additional lights are packed into a single cookie atlas. This setting controls the atlas format.", null, null);

            // Reflection Probes
            public static GUIContent reflectionProbesSettingsText = L10n.TextContent("Reflection Probes", null, null, null);
            public static GUIContent reflectionProbeBlendingText = L10n.TextContent("Probe Blending", "If enabled smooth transitions will be created between reflection probes.", null, null);
            public static GUIContent reflectionProbeAtlasText = L10n.TextContent("Probe Atlas Blending", "If enabled, reflection probes will be added to the Forward Plus data grid and combined into a single atlas texture. The atlas is used by default when both Forward Plus and the GPU Resident Drawer are used.", null, null);
            public static GUIContent reflectionProbeBoxProjectionText = L10n.TextContent("Box Projection", "If enabled reflections appear based on the object’s position within the probe’s box, while still using a single probe as the source of the reflection.", null, null);
            public static GUIContent reflectionProbeBlendingGpuResidentDrawerWarningText = L10n.TextContent("Probe Atlas Blending is currently force enabled because GPUResidentDrawer is in use. GPUResidentDrawer currently only supports Reflection Probes via Probe Atlas Blending.", null, null, null);

            // Additional lighting settings
            public static GUIContent mixedLightingSupportLabel = L10n.TextContent("Mixed Lighting", "Makes the render pipeline include mixed-lighting Shader Variants in the build.", null, null);
            public static GUIContent useRenderingLayers = L10n.TextContent("Use Rendering Layers", "When enabled, rendering layers are used to select which lights an object is affected by. When using the Deferred rendering path, this option allocates an extra render target.", null, null);
            public static GUIContent supportsLightCookies = L10n.TextContent("Light Cookies", "Makes the render pipeline include light cookies Shader Variants in the build.", null, null);

            // Shadow settings
            public static GUIContent shadowWorkingUnitText = L10n.TextContent("Working Unit", "The unit in which Unity measures the shadow cascade distances. The exception is Max Distance, which will still be in meters.", null, null);
            public static GUIContent shadowDistanceText = L10n.TextContent("Max Distance", "Maximum shadow rendering distance.", null, null);
            public static GUIContent shadowCascadesText = L10n.TextContent("Cascade Count", "Number of cascade splits used for directional shadows.", null, null);
            public static GUIContent shadowDepthBias = L10n.TextContent("Depth Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts. The depth bias mode can be changed in Project Settings > Graphics > URP.", null, null);
            public static GUIContent shadowSlopeScaleDepthBias = L10n.TextContent("Slope-Scale Depth Bias", "Controls the slope-scaled depth bias applied while rendering the shadow map: surfaces at a grazing angle to the light receive a proportionally larger depth offset. Useful for avoiding false self-shadowing artifacts. The depth bias mode can be changed in Project Settings > Graphics > URP.", null, null);
            public static GUIContent shadowNormalBias = L10n.TextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.", null, null);
            public static GUIContent supportsSoftShadows = L10n.TextContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.", null, null);
            public static GUIContent conservativeEnclosingSphere = L10n.TextContent("Conservative Enclosing Sphere", "Enable this option to improve shadow frustum culling and prevent Unity from excessively culling shadows in the corners of the shadow cascades. Disable this option only for compatibility purposes of existing projects created in previous Unity versions.", null, null);

            public static GUIContent softShadowsQuality = L10n.TextContent("Quality", "Default shadow quality setting for Lights.", null, null);
            public static GUIContent[] softShadowsQualityAssetOptions =
            {
                L10n.TextContent(nameof(SoftShadowQuality.Low), null, null, null),
                L10n.TextContent(nameof(SoftShadowQuality.Medium), null, null, null),
                L10n.TextContent(nameof(SoftShadowQuality.High), null, null, null)
            };
            public static int[] softShadowsQualityAssetValues =  { (int)SoftShadowQuality.Low, (int)SoftShadowQuality.Medium, (int)SoftShadowQuality.High };

            // Post-processing
            public static GUIContent colorGradingMode = L10n.TextContent("Grading Mode", "Defines how color grading will be applied. Operators will react differently depending on the mode.", null, null);
            public static GUIContent colorGradingLutSize = L10n.TextContent("LUT size", "Sets the size of the internal and external color grading lookup textures (LUTs).", null, null);
            public static GUIContent allowPostProcessAlphaOutput = L10n.TextContent("Alpha Processing", "When enabled, post-processing outputs alpha channel if available. Alpha 0 preserves the original color. Otherwise post-processing applies to the alpha channel as well. Results may vary depending on the effect.", null, null);
            public static GUIContent useFastSRGBLinearConversion = L10n.TextContent("Fast sRGB/Linear conversions", "Use faster, but less accurate approximation functions when converting between the sRGB and Linear color spaces.", null, null);
            public static GUIContent supportDataDrivenLensFlare = L10n.TextContent("Data Driven Lens Flare", "When enabled, URP allocates shader variants and memory for Data Driven Lens Flare effect.", null, null);
            public static GUIContent supportScreenSpaceLensFlare = L10n.TextContent("Screen Space Lens Flare", "When enabled, URP allocates shader variants and memory for Screen Space Lens Flare effect.", null, null);
            public static string alphaOutputWarning = "Camera back-buffer format does not support alpha channel. Final output will be opaque.";
            public static string colorGradingModeWarning = "HDR rendering is required to use the high dynamic range color grading mode. The low dynamic range will be used instead.";
            public static string colorGradingModeWithHDROutput = "With the current configuration, Unity uses the HDR color grading mode when outputting to an HDR display.";
            public static string colorGradingModeSpecInfo = "The high dynamic range color grading mode works best on platforms that support floating point textures.";
            public static string colorGradingLutSizeWarning = "The minimal recommended LUT size for the high dynamic range color grading mode is 32. Using lower values will potentially result in color banding and posterization effects.";

            // Volumes
            public static GUIContent volumeFrameworkUpdateMode = L10n.TextContent("Volume Update Mode", "Select how Unity updates Volumes: every frame or when triggered via scripting. In the Editor, Unity updates Volumes every frame when not in the Play mode.", null, null);
            public static GUIContent volumeProfileLabel = L10n.TextContent("Volume Profile", "Settings that will override the values defined in the Default Volume Profile set in the Render Pipeline Global settings. Local Volumes inside scenes may override these settings further.", null, null);
            public static System.Lazy<GUIStyle> volumeProfileContextMenuStyle = new(() => new GUIStyle(CoreEditorStyles.contextMenuStyle) { margin = new RectOffset(0, 1, 3, 0) });

            // GPU Resident Drawer
            public static GUIContent gpuResidentDrawerMode = L10n.TextContent("GPU Resident Drawer", "Enables draw submission through the GPU Resident Drawer, which can improve CPU performance.", null, null);
            public static GUIContent smallMeshScreenPercentage = L10n.TextContent("Small-Mesh Screen-Percentage", "Default minimum screen percentage (0-20%) gpu-driven Renderers can cover before getting culled. If a Renderer is part of a LODGroup, this will be ignored.", null, null);
            public static GUIContent gpuResidentDrawerEnableOcclusionCullingInCameras = L10n.TextContent("GPU Occlusion Culling", "Enables GPU occlusion culling in Game and SceneView cameras.", null, null);
            public static string shadowSmallMeshPctToolTip = "Default per-cascade minimum screen percentage (0-50%) gpu-driven Renderers can cover before getting shadows culled. If a Renderer is part of a LODGroup, this will be ignored.";
            public static readonly GUIContent shadowSmallMeshPct1 = EditorGUIUtility.TrTextContent("Cascade 1 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip);
            public static readonly GUIContent shadowSmallMeshPct2 = EditorGUIUtility.TrTextContent("Cascade 2 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip);
            public static readonly GUIContent shadowSmallMeshPct3 = EditorGUIUtility.TrTextContent("Cascade 3 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip);
            public static readonly GUIContent shadowSmallMeshPct4 = EditorGUIUtility.TrTextContent("Cascade 4 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip);

            // Adaptive performance settings
            public static GUIContent useAdaptivePerformance = L10n.TextContent("Use adaptive performance", "Allows Adaptive Performance to adjust rendering quality during runtime", null, null);

            // Renderer List Messages
            public static GUIContent rendererListDefaultMessage =
                L10n.TextContent("Cannot remove Default Renderer",
                    "Removal of the Default Renderer is not allowed. To remove, set another Renderer to be the new Default and then remove.", null, null);

            public static GUIContent rendererMissingDefaultMessage =
                L10n.TextContent("Missing Default Renderer\nThere is no default renderer assigned, so Unity can’t perform any rendering. Set another renderer to be the new Default, or assign a renderer to the Default slot.", null, null, null);
            public static GUIContent rendererMissingMessage =
                L10n.TextContent("Missing Renderer(s)\nOne or more renderers are either missing or unassigned.  Switching to these renderers at runtime can cause issues.", null, null, null);
            public static GUIContent lightlayersUnsupportedMessage =
                L10n.TextContent("Some Graphics API(s) in the Player Graphics APIs list are incompatible with Light Layers.  Switching to these Graphics APIs at runtime can cause issues: ", null, null, null);
            public static GUIContent rendererUnsupportedAPIMessage =
                L10n.TextContent("Some Renderer(s) in the Renderer List are incompatible with the Player Graphics APIs list.  Switching to these renderers at runtime can cause issues.\n\n", null, null, null);
            public static GUIContent webGL2GpuResidentDrawerErrorMessage =
                L10n.TextContent("GPU Resident Drawer is not supported with the WebGL2 Graphics API.", null, null, null);
            public static GUIContent brgShaderStrippingErrorMessage =
                L10n.TextContent("\"BatchRendererGroup Variants\" setting must be \"Keep All\". To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".", null, null, null);
            public static GUIContent staticBatchingInfoMessage =
                L10n.TextContent("Static Batching is not recommended when using GPU draw submission modes, performance may improve if Static Batching is disabled in Player Settings.", null, null, null);
            public static readonly string lightModeErrorFormatter = L10n.Tr("Rendering Path must be set to Forward+ or Deferred+ for correct lighting and reflections. Renderers to change: {0}.", null);
            public static GUIContent renderGraphNotEnabledErrorMessage =
                L10n.TextContent("Render Graph must be enabled to use occlusion culling.", null, null, null);
            public static GUIContent stencilLodCrossFadeWarningMessage =
                L10n.TextContent("LOD Cross Fade with stencil dithering is not compatible with stencil override in Renderer.", null, null, null);
            
            public static readonly string formatterTileOnlyMode = L10n.Tr("'{0}' will be skipped because it is incompatible with the enabled 'Tile-Only Mode'. Affected renderers: {1}.", null);
            public static readonly string tileOnlyModeMaybeMessage = L10n.Tr("'{0}' might be skipped when 'Tile-Only Mode' is enabled. Renderer Features can provide this functionality, so behavior can vary. Affected renderers: {1}.", null);
            public static readonly string msaaTileOnlyInfo = L10n.Tr("'{0}' is supported in 'Tile-Only Mode'. However, in the Editor and on platforms where the back buffer does not support MSAA, URP forces MSAA to None to keep data in Tile Memory. Affected renderers: {1}.", null);
            public static readonly string msaaTileOnlyDeferredWarning = L10n.Tr("'{0}' is incompatible with 'Tile-Only Mode' + Deferred rendering. On platforms with MSAA back buffer support, the renderer will silently fall back to Forward(+) at runtime. On other platforms, MSAA is dropped to 1. Set MSAA to Disabled to keep Deferred consistently, or switch the renderer to Forward(+). Affected renderers: {1}.", null);
            public static readonly string suffixWhenDifferentPositionTileOnlyMode = L10n.Tr(" (different position)", null);

            // Dropdown menu options
            public static string[] mainLightOptions = { "Disabled", "Per Pixel" };
            public static string[] volumeFrameworkUpdateOptions = { "Every Frame", "Via Scripting" };
        }
    }
}
