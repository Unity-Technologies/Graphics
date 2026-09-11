using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HighDefinition.RenderPipelineSettings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDRenderPipelineUI
    {
        public class Styles
        {
            public static readonly GUIContent renderingSectionTitle = L10n.TextContent("Rendering", null, null, null);
            public static readonly GUIContent lightingSectionTitle = L10n.TextContent("Lighting", null, null, null);
            public static readonly GUIContent materialSectionTitle = L10n.TextContent("Material", null, null, null);
            public static readonly GUIContent postProcessSectionTitle = L10n.TextContent("Post-processing", null, null, null);
            public static readonly GUIContent volumesSectionTitle = L10n.TextContent("Volumes", null, null, null);
            public static readonly GUIContent xrTitle = L10n.TextContent("XR", null, null, null);
            public static readonly GUIContent virtualTexturingTitle = L10n.TextContent("Virtual Texturing", "Virtual Texturing Settings. These are only available when Virtual Texturing is enabled in the Player Settings.", null, null);
            public static readonly GUIContent lightLoopSubTitle = L10n.TextContent("Lights", null, null, null);
            public static readonly GUIContent tierSubTitle = L10n.TextContent("Tier Settings", null, null, null);

            public static readonly GUIContent volumetricSubTitle = L10n.TextContent("Volumetrics", null, null, null);
            public static readonly GUIContent volumetricCloudsSubTitle = L10n.TextContent("Volumetric Clouds", null, null, null);
            public static readonly GUIContent lightProbeSubTitle = L10n.TextContent("Light Probe Lighting", null, null, null);
            public static readonly GUIContent cookiesSubTitle = L10n.TextContent("Cookies", null, null, null);
            public static readonly GUIContent reflectionsSubTitle = L10n.TextContent("Reflections", null, null, null);
            public static readonly GUIContent skySubTitle = L10n.TextContent("Sky", null, null, null);
            public static readonly GUIContent decalsSubTitle = L10n.TextContent("Decals", null, null, null);
            public static readonly GUIContent decalsMetalAndAOSubTitle = L10n.TextContent("Decals Metal And AO", null, null, null);
            public static readonly GUIContent decalResolutionSubTitle = L10n.TextContent("Transparent Texture Resolution", null, null, null);
            public static readonly GUIContent decalResolutionTiers = L10n.TextContent("Resolution Tiers", null, null, null);

            public static readonly GUIContent shadowSubTitle = L10n.TextContent("Shadows", null, null, null);

            public static readonly GUIContent punctualLightshadowSubTitle = L10n.TextContent("Punctual Light Shadows", null, null, null);
            public static readonly GUIContent directionalLightshadowSubTitle = L10n.TextContent("Directional Light Shadows", null, null, null);
            public static readonly GUIContent areaLightshadowSubTitle = L10n.TextContent("Area Light Shadows", null, null, null);

            public static readonly GUIContent shadowLightAtlasSubTitle = L10n.TextContent("Light Atlas", null, null, null);

            public static readonly GUIContent shadowResolutionTiers = L10n.TextContent("Resolution Tiers", null, null, null);

            public static readonly GUIContent dynamicResolutionSubTitle = L10n.TextContent("Dynamic resolution", null, null, null);
            public static readonly GUIContent lowResTransparencySubTitle = L10n.TextContent("Low res Transparency", null, null, null);

            public static readonly GUIContent motionBlurQualitySettings = L10n.TextContent("Motion Blur", null, null, null);
            public static readonly GUIContent bloomQualitySettings = L10n.TextContent("Bloom", null, null, null);
            public static readonly GUIContent chromaticAberrationQualitySettings = L10n.TextContent("Chromatic Aberration", null, null, null);

            public static readonly GUIContent depthOfFieldQualitySettings = L10n.TextContent("Depth Of Field", null, null, null);
            public static readonly GUIContent farBlurSubTitle = L10n.TextContent("Far Blur", null, null, null);
            public static readonly GUIContent nearBlurSubTitle = L10n.TextContent("Near Blur", null, null, null);
            public static readonly GUIContent maxRadiusQuality = L10n.TextContent("Max Radius", null, null, null);
            public static readonly GUIContent sampleCountQuality = L10n.TextContent("Sample Count", null, null, null);
            public static readonly GUIContent pbrResolutionQualityTitle = L10n.TextContent("Enable High Resolution", null, null, null);
            public static readonly GUIContent resolutionQuality = L10n.TextContent("Resolution", null, null, null);
            public static readonly GUIContent adaptiveSamplingWeight = L10n.TextContent("Adaptive Sampling Weight", null, null, null);
            public static readonly GUIContent highQualityPrefiltering = L10n.TextContent("High Quality Prefiltering", null, null, null);
            public static readonly GUIContent highQualityFiltering = L10n.TextContent("High Quality Filtering", null, null, null);
            public static readonly GUIContent dofPhysicallyBased = L10n.TextContent("Physically Based", null, null, null);
            public static readonly GUIContent limitNearBlur = L10n.TextContent("Limit Manual Range Near Blur", null, null, null);
            public static readonly GUIContent maxSamplesQuality = L10n.TextContent("Max Samples", null, null, null);

            // Lens Flares
            public static readonly GUIContent LensFlareTitle = L10n.TextContent("Lens Flares", null, null, null);

            // SSAO
            public static readonly GUIContent SSAOQualitySettingSubTitle = L10n.TextContent("Screen Space Ambient Occlusion", null, null, null);
            public static readonly GUIContent AOStepCount = L10n.TextContent("Step Count", null, null, null);
            public static readonly GUIContent AOFullRes = L10n.TextContent("Full Resolution", null, null, null);
            public static readonly GUIContent AOMaxRadiusInPixels = L10n.TextContent("Maximum Radius in Pixels", null, null, null);
            public static readonly GUIContent AODirectionCount = L10n.TextContent("Direction Count", null, null, null);
            public static readonly GUIContent AOBilateralUpsample = L10n.TextContent("Bilateral Upsample", null, null, null);

            // RTAO
            public static readonly GUIContent RTAOQualitySettingSubTitle = L10n.TextContent("Ray Traced Ambient Occlusion", null, null, null);
            public static readonly GUIContent RTAORayLength = L10n.TextContent("Max Ray Length", null, null, null);
            public static readonly GUIContent RTAOSampleCount = L10n.TextContent("Sample Count", null, null, null);
            public static readonly GUIContent RTAODenoise = L10n.TextContent("Denoise", null, null, null);
            public static readonly GUIContent RTAODenoiserRadius = L10n.TextContent("Denoiser Radius", null, null, null);

            public static readonly GUIContent contactShadowsSettingsSubTitle = L10n.TextContent("Contact Shadows", null, null, null);
            public static readonly GUIContent contactShadowsSampleCount = L10n.TextContent("Sample Count", null, null, null);

            public static readonly GUIContent SSRSettingsSubTitle = L10n.TextContent("Screen Space Reflection", null, null, null);
            public static readonly GUIContent SSRMaxRaySteps = L10n.TextContent("Max Ray Steps", null, null, null);

            // RTR
            public static readonly GUIContent RTRSettingsSubTitle = L10n.TextContent("Ray Traced Reflections (Performance)", null, null, null);
            public static readonly GUIContent RTRMinSmoothness = L10n.TextContent("Minimum Smoothness", null, null, null);
            public static readonly GUIContent RTRSmoothnessFadeStart = L10n.TextContent("Smoothness Fade Start", null, null, null);
            public static readonly GUIContent RTRRayLength = L10n.TextContent("Max Ray Length", null, null, null);
            public static readonly GUIContent RTRFullResolution = L10n.TextContent("Full Resolution", null, null, null);
            public static readonly GUIContent RTRRayMaxIterations = L10n.TextContent("Ray Max Iterations", null, null, null);
            public static readonly GUIContent RTRDenoise = L10n.TextContent("Denoise", null, null, null);
            public static readonly GUIContent RTRDenoiserRadius = L10n.TextContent("Denoiser Radius", null, null, null);
            public static readonly GUIContent RTRDenoiserAntiFlicker = L10n.TextContent("Anti Flickering Strength", null, null, null);

            // RTGI
            public static readonly GUIContent RTGISettingsSubTitle = L10n.TextContent("Ray Traced Global Illumination (Performance)", null, null, null);
            public static readonly GUIContent RTGIRayLength = L10n.TextContent("Max Ray Length", null, null, null);
            public static readonly GUIContent RTGIFullResolution = L10n.TextContent("Full Resolution", null, null, null);
            public static readonly GUIContent RTGIRaySteps = L10n.TextContent("Ray Steps", null, null, null);
            public static readonly GUIContent RTGIDenoise = L10n.TextContent("Denoise", null, null, null);
            public static readonly GUIContent RTGIHalfResDenoise = L10n.TextContent("Half Resolution Denoiser", null, null, null);
            public static readonly GUIContent RTGIDenoiserRadius = L10n.TextContent("Denoiser Radius", null, null, null);
            public static readonly GUIContent RTGISecondDenoise = L10n.TextContent("Second Denoiser Pass", null, null, null);

            // SSGI
            public static readonly GUIContent SSGISettingsSubTitle = L10n.TextContent("Screen Space Global Illumination", null, null, null);
            public static readonly GUIContent SSGIRaySteps = L10n.TextContent("Ray Steps", null, null, null);
            public static readonly GUIContent SSGIRadius = L10n.TextContent("Radius", null, null, null);
            public static readonly GUIContent SSGIClampValue = L10n.TextContent("Clamp Value", null, null, null);
            public static readonly GUIContent SSGIDenoise = L10n.TextContent("Denoise", null, null, null);
            public static readonly GUIContent SSGIHalfResDenoise = L10n.TextContent("Half Resolution Denoiser", null, null, null);
            public static readonly GUIContent SSGIDenoiserRadius = L10n.TextContent("Denoiser Radius", null, null, null);
            public static readonly GUIContent SSGISecondDenoise = L10n.TextContent("Second Denoiser Pass", null, null, null);

            // Water rendering
            public static readonly GUIContent waterSubTitle = L10n.TextContent("Water", null, null, null);
            public static readonly GUIContent supportWaterContent = L10n.TextContent("Enable", "When enabled, HDRP allocates memory for the water surfaces simulation and rendering. This allows you to use water surfaces in your Unity Project.", null, null);
            public static readonly GUIContent waterSimulationResolutionContent = L10n.TextContent("Simulation Resolution", "Specifies the resolution of the water simulation. A higher resolution increases the visual quality, but increases the cost.", null, null);
            public static readonly GUIContent supportWaterDeformationContent = L10n.TextContent("Deformation", "When enabled, HDRP allocates additional memory to support water deformation.", null, null);
            public static readonly GUIContent waterDecalAtlasSizeContent = L10n.TextContent("Decal Atlas Size", "Specifies the size of the atlas used to store texture water decals.", null, null);
            public static readonly GUIContent maximumWaterDecalCountContent = L10n.TextContent("Maximum Decal Count", "Sets the maximum amount of water decals HDRP can support.", null, null);
            public static readonly GUIContent supportWaterFoamContent = L10n.TextContent("Foam", "When enabled, HDRP allocates additional memory to support water foam.", null, null);
            public static readonly GUIContent foamAtlasSizeContent = L10n.TextContent("Foam Atlas Size", "Specifies the size of the atlas used to store texture water foam.", null, null);
            public static readonly GUIContent supportWaterExclusionContent = L10n.TextContent("Exclusion", "When enabled, HDRP allocates a stencil bit to support water excluders.", null, null);
            public static readonly GUIContent supportWaterHorizontalDeformationContent = L10n.TextContent("Horizontal Deformation", "When enabled, HDRP allocates additional memory to support water horizontal deformation. Enabling this property limits compatibility with other water system features (like scripts interactions or underwater), refer to the documentation for more details.", null, null);
            public static readonly GUIContent waterCausticsMeshResolutionContent = L10n.TextContent("Caustics Mesh Resolution", "Specifies the resolution of the mesh used to render the caustics texture. For better efficiency, this needs to be lower than the caustics resolution texture in your water surface. A higher resolution increases the number of vertices in the scene.", null, null);

            // High Quality Line Rendering
            public static readonly GUIContent highQualityLineRenderingSubTitle = L10n.TextContent("High Quality Line Rendering", null, null, null);
            public static readonly GUIContent supportHighQualityLineRenderingContent = L10n.TextContent("Enable", "When enabled, HDRP allocates memory for high quality line rendering. This allows you to render lines with high quality anti-aliasing and transparency in your Unity Project.", null, null);
            public static readonly GUIContent highQualityLineRenderingMemoryBudget = L10n.TextContent("Memory Budget", "Determines the size of graphics memory allocations for high quality line rendering.", null, null);
            // Compute Thickness
            public static readonly GUIContent computeThicknessSubTitle = L10n.TextContent("Compute Thickness", null, null, null);
            public static readonly GUIContent computeThicknessEnableContent = L10n.TextContent("Enable", "When enabled, HDRP allocates memory for the Compute Thickness pass. For each Game Object layer selected in LayerMask, the thickness of all objects in that layer is written in a buffer. This buffer can be sampled only in Shader Graph via HDSampleBuffer node with the layer index as input.", null, null);
            public static readonly GUIContent computeThicknessResolutionContent = L10n.TextContent("Resolution", "Specifies the resolution of the Compute Thickness buffers. A higher resolution increases visual quality, but increases the cost.", null, null);
            public static readonly GUIContent computeThicknessLayerContent = L10n.TextContent("Layer Mask", "Specifies the list of Game Objects layers included in the Thickness Compute pass. For each layer selected, HDRP allocated a Render Target. In VR, all layers will be computed into the same buffer.", null, null);

            // Fog
            public static readonly GUIContent FogSettingsSubTitle = L10n.TextContent("Volumetric Fog", null, null, null);
            public static readonly GUIContent FogSettingsBudget = L10n.TextContent("Volumetric Fog Budget", null, null, null);
            public static readonly GUIContent FogSettingsRatio = L10n.TextContent("Resolution Depth Ratio", null, null, null);

            public static readonly GUIContent materialQualityLevelContent = L10n.TextContent("Default Material Quality Level", "", null, null);

            public static readonly GUIContent supportShadowMaskContent = L10n.TextContent("Shadowmask", "When enabled, HDRP allocates Shader variants and memory for processing shadow masks. This allows you to use shadow masks in your Unity Project.", null, null);
            public static readonly GUIContent supportSSRContent = L10n.TextContent("Screen Space Reflection", "When enabled, HDRP allocates memory for processing screen space reflection (SSR). This allows you to use SSR in your Unity Project.", null, null);
            public static readonly GUIContent cubeResolutionTitle = L10n.TextContent("Cube Reflection Resolution Tiers", "Resolution of a cube Reflection Probe rendered from a camera. Unity represents a cube Reflection Probe in its 2d atlas with a 2d octahedral projection texture.", null, null);
            public static readonly GUIContent planarResolutionTitle = L10n.TextContent("Planar Reflection Resolution Tiers", "Resolution of a planar Reflection Probe rendered from a camera.", null, null);
            public static readonly GUIContent supportSSRTransparentContent = L10n.TextContent("Transparent", "When enabled, HDRP executes additional steps to achieve screen space reflection (SSR) on transparent objects. This feature requires both screen space reflections and transparent depth prepass to be enabled.", null, null);
            public static readonly GUIContent supportSSAOContent = L10n.TextContent("Screen Space Ambient Occlusion", "When enabled, HDRP allocates memory for processing screen space ambient occlusion (SSAO). This allows you to use SSAO in your Unity Project.", null, null);
            public static readonly GUIContent supportSSGIContent = L10n.TextContent("Screen Space Global Illumination", "When enabled, HDRP allocates memory for processing screen space global illumination (SSGI). This allows you to use SSGI in your Unity Project.", null, null);
            public static readonly GUIContent renderingLayerMaskBuffer = L10n.TextContent("Rendering Layer Mask Buffer", "When enabled, HDRP writes Rendering Layer Mask of Renderers to a buffer target that can be sampled in a shader in order to create fullscreen effects.\nThis comes with a performance and a memory cost.", null, null);
            public static readonly GUIContent supportedSSSContent = L10n.TextContent("Subsurface Scattering", "When enabled, HDRP allocates memory for processing subsurface scattering (SSS). This allows you to use SSS in your Unity Project.", null, null);
            public static readonly GUIContent subsurfaceScatteringBorderAttenuation = L10n.TextContent("Support Border Attenuation", "When enabled, the subsurface scattering takes into account the occlusion of near objects with no subsurface-scattering or a different diffusion profile.", null, null);
            public static readonly GUIContent sssSampleBudget = L10n.TextContent("Sample Budget", "Maximum number of samples the Subsurface Scattering algorithm is allowed to take.", null, null);
            public static readonly GUIContent sssDownsampleSteps = L10n.TextContent("Downsample Level", "The number of downsample steps done to the source irradance textrure before it is used by the Subsurface Scattering algorithm. Higher value will improve performance, but might lower quality.", null, null);
            public static readonly GUIContent supportVolumetricFogContent = L10n.TextContent("Volumetric Fog", "When enabled, HDRP allocates Shader variants and memory for volumetric effects. This allows you to use volumetric lighting and fog in your Unity Project.", null, null);
            public static readonly GUIContent supportVolumetricCloudsContent = L10n.TextContent("Volumetric Clouds", "When enabled, HDRP allocates memory for processing volumetric clouds. This allows you to use volumetric clouds in your Unity Project.", null, null);
            public static readonly GUIContent volumetricResolutionContent = L10n.TextContent("High Quality ", "When enabled, HDRP increases the resolution of volumetric lighting buffers. Warning: There is a high performance cost, do not enable on consoles.", null, null);
            public static readonly GUIContent supportLightLayerContent = L10n.TextContent("Light Layers", "When enabled, HDRP allocates memory for processing Light Layers. This allows you to use Light Layers in your Unity Project. For deferred rendering, this allocation includes an extra render target in memory and extra cost.", null, null);
            public static readonly GUIContent colorBufferFormatContent = L10n.TextContent("Color Buffer Format", "Specifies the format used by the scene color render target. R11G11B10 is a faster option and should have sufficient precision.", null, null);
            public static readonly GUIContent depthBufferFormatContent = L10n.TextContent("Depth Buffer Format", "Specifies the format used by the scene depth render target.", null, null);
            public static readonly GUIContent supportCustomPassContent = L10n.TextContent("Custom Pass", "When enabled, HDRP allocates a custom pass buffer. It also enable custom passes inside Custom Pass Volume components.", null, null);
            public static readonly GUIContent supportVariableRateShadingContent = L10n.TextContent("Variable Rate Shading", "When enabled, HDRP enables the usage of variable rate shading in custom passes.", null, null);
            public static readonly GUIContent customBufferFormatContent = L10n.TextContent("Custom Buffer Format", "Specifies the format used by the custom pass render target.", null, null);
            public static readonly GUIContent supportLitShaderModeContent = L10n.TextContent("Lit Shader Mode", "Specifies the rendering modes HDRP supports for Lit Shaders. HDRP removes all allocated memory and Shader variants for modes you do not specify.", null, null);
            public static readonly GUIContent MSAASampleCountContent = L10n.TextContent("Multisample Anti-aliasing Quality", "Specifies the default quality for MSAA. Set Lit Shader Mode to Forward Only or Both to use this feature.\nMSAA is not supported when water or raytracing is enabled", null, null);
            public static readonly GUIContent supportDecalContent = L10n.TextContent("Enable", "When enabled, HDRP allocates Shader variants and memory to the decals buffer and cluster decal. This allows you to use decals in your Unity Project.", null, null);
            public static readonly GUIContent supportDecalLayersContent = L10n.TextContent("Layers", "When enabled, HDRP allocates Shader variants and memory to the decals layers buffer. This allows you to use decal layers in your Unity Project.", null, null);
            public static readonly GUIContent supportSurfaceGradientContent = L10n.TextContent("Additive Normal Blending", "When enabled, HDRP uses surface gradients to preserve the affected objects normal when applying decal normals.", null, null);
            public static readonly GUIContent decalNormalFormatContent = L10n.TextContent("High Precision Normal Buffer", "When enabled, HDRP uses a high precision format for the buffer storing decal normals.", null, null);
            public static readonly GUIContent supportMotionVectorContent = L10n.TextContent("Motion Vectors", "When enabled, HDRP allocates memory for processing motion vectors which it uses for Motion Blur, TAA, and temporal re-projection of various effect like SSR.", null, null);
            public static readonly GUIContent supportRuntimeAOVAPIContent = L10n.TextContent("Runtime AOV API", "When disabled, HDRP removes all AOV API Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static readonly GUIContent supportDitheringCrossFadeContent = L10n.TextContent("Dithering Cross-fade", "When disabled, HDRP removes all dithering cross fade Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static readonly GUIContent supportTerrainHoleContent = L10n.TextContent("Terrain Hole", "When disabled, HDRP removes all Terrain hole Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static readonly GUIContent supportDistortion = L10n.TextContent("Distortion", "When disabled, HDRP removes all distortion Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static readonly GUIContent supportTransparentBackface = L10n.TextContent("Transparent Backface", "When disabled, HDRP removes all transparent backface Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static readonly GUIContent supportTransparentDepthPrepass = L10n.TextContent("Transparent Depth Prepass", "When disabled, HDRP removes all transparent depth prepass Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static readonly GUIContent supportTransparentDepthPostpass = L10n.TextContent("Transparent Depth Postpass", "When disabled, HDRP removes all transparent depth postpass Shader variants when you build for the Unity Player. This decreases build time.", null, null);
            public static readonly GUIContent supportRaytracing = L10n.TextContent("Realtime Raytracing", null, null, null);
            public static readonly GUIContent supportedRayTracingMode = L10n.TextContent("Supported Ray Tracing Mode", null, null, null);
            public static readonly GUIContent supportVFXRayTracing = L10n.TextContent("Visual Effects Ray Tracing", "When enabled, Visual Effects Outputs which have Enable Ray Tracing on will be accounted for in Ray-traced effects.", null, null);
            public static readonly GUIContent rayTracingRestrictionOnlyWarning = L10n.TextContentWithIcon("Ray tracing requires DX12 on Windows. Disable Auto Graphics API and set Direct3D 12 as the first Graphics API in Player Settings. You can use the HDRP Wizard to configure these settings.", null, CoreEditorStyles.iconWarn, null);
            public static string rayTracingNotSupportedBuildTarget = L10n.Tr("The {0} does not support Ray tracing. Consider disabling it or switch to a platform that supports it.", null);
            public static readonly GUIContent rayTracingMSAAUnsupported = L10n.TextContent("When Ray tracing is enabled in asset, MSAA is not supported. Please refer to the documentation.", null, null, null);
            public static readonly GUIContent waterMSAAUnsupported = L10n.TextContent("When Water is enabled in asset, MSAA is not supported. Please refer to the documentation.", null, null, null);
            public static readonly GUIContent maximumLODLevel = L10n.TextContent("Maximum LOD Level", null, null, null);
            public static readonly GUIContent LODBias = L10n.TextContent("LOD Bias", null, null, null);
            public static readonly GUIContent supportRuntimeDebugDisplayContentLabel = L10n.TextContent("Runtime Debug Shaders", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.", null, null);
            public static readonly GUIContent lightProbeSystemContent = L10n.TextContent("Light Probe System", "What system to use for Light Probes.", null, null);
            public static readonly GUIContent probeVolumeMemoryBudget = L10n.TextContent("Memory Budget", "Determines the width and height of the textures used to store GI data from probes. Note that the textures also has a fixed depth dimension.", null, null);
            public static readonly GUIContent probeVolumeBlendingMemoryBudget = L10n.TextContent("Scenario Blending Memory Budget", "Determines the width and height of the textures used to blend between lighting scenarios. Note that the textures also has a fixed depth dimension.", null, null);
            public static readonly GUIContent supportProbeVolumeScenarios = L10n.TextContent("Lighting Scenarios", "Support lighting scenarios for probe volume.", null, null);
            public static readonly GUIContent supportProbeVolumeScenarioBlending = L10n.TextContent("Scenario Blending", "Support lighting scenarios blending for probe volume.", null, null);
            public static readonly GUIContent supportProbeVolumeDiskStreaming = L10n.TextContent("Enable Disk Streaming", "Enable cell streamin from the disk for probe volume. Enabling this will force GPU streaming for probe volume.", null, null);
            public static readonly GUIContent supportProbeVolumeGPUStreaming = L10n.TextContent("Enable GPU Streaming", "Enable cell streaming in and out of GPU memory for the probe volume.", null, null);
            public static readonly GUIContent probeVolumeSHBands = L10n.TextContent("SH Bands", "Determines up to what SH bands the Probe Volume will use. Choosing L2 will lead to better quality, but also higher memory and runtime cost.", null, null);
            public static readonly GUIContent maxLocalVolumetricFogSizeStyle = L10n.TextContent("Max Local Fog Size", "Specifies the maximum size for the individual 3D Local Volumetric Fog texture that HDRP uses for Local Volumetric Fog. This settings will affect your memory consumption.", null, null);
            public static readonly GUIContent maxLocalVolumetricFogOnScreenStyle = L10n.TextContent("Max Local Fog On Screen", "Sets the maximum number of Local Volumetric Fog can handle on screen at once. This settings will affect your memory consumption.", null, null);
            public static readonly GUIContent supportScreenSpaceLensFlare = L10n.TextContent("Screen Space Lens Flare", "When enabled, HDRP allocates shader variants and memory for Screen Space Lens Flare effect.", null, null);
            public static readonly GUIContent supportDataDrivenLensFlare = L10n.TextContent("Data Driven Lens Flare", "When enabled, HDRP allocates shader variants and memory for Data Driven Lens Flare effect.", null, null);

            public const string cacheErrorFormat = "This configuration will lead to more than 2 GB reserved for this cache at runtime! ({0} requested) Only {1} element will be reserved instead.";
            public const string cacheInfoFormat = "Reserving {0} in memory at runtime.";
            public const string multipleDifferenteValueMessage = "Multiple different values";
            public const string rayTracingUnsupportedMessage = "The current HDRP Asset does not support Ray Tracing.";

            public static readonly GUIContent cookieSizeContent = L10n.TextContent("Cookie Size", "Specifies the maximum size for the individual 2D cookies that HDRP uses for Directional and Spot Lights.", null, null);
            public static readonly GUIContent cookieTextureArraySizeContent = L10n.TextContent("Texture Array Size", "Sets the maximum Texture Array size for the 2D cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.", null, null);
            public static readonly GUIContent pointCookieTextureArraySizeContent = L10n.TextContent("Cubemap Array Size", "Sets the maximum Texture Array size for the Cube cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.", null, null);
            public static readonly GUIContent cookieAtlasSizeContent = L10n.TextContent("2D Atlas Size", "Specifies the size of the atlas used for 2D cookies (Directional, Spot and Rectangle Lights).", null, null);
            public static readonly GUIContent cookieAtlasFormatContent = L10n.TextContent("Format", "Specifies the HDR format of the atlas used for 2D cookies. R16G16B16A16 can be use for EXR cookies (it provides more precision than R11G11B10)", null, null);
            public static readonly GUIContent cookieAtlasLastValidMipContent = L10n.TextContent("2D Atlas Last Valid Mip", "Apply border when the cookie is copied into the atlas. It avoid the cookie border to be clamped when sampling mips but can introduce some blurriness.", null, null);

            public static readonly GUIContent reflectionProbeCompressCacheContent = L10n.TextContent("Compress Baked Reflection Probes", "When enabled, HDRP compresses baked Reflection Probes to save disk space.", null, null);
            public static readonly GUIContent reflectionProbeFormatContent = L10n.TextContent("Reflection Probe Format", "Color format used for reflection and planar probes. Keep in mind that probes are not pre-exposed when selecting the format.", null, null);
            public static readonly GUIContent reflectionProbeAtlasSizeContent = L10n.TextContent("Reflection 2D Atlas Size", "Specifies the resolution of Reflection Probes Atlas.", null, null);
            public static readonly GUIContent reflectionProbeAtlasLastValidCubeMipContent = L10n.TextContent("Reflection 2D Atlas Last Valid Cube Mip", "Apply border when the cube Reflection Probe is copied into the atlas. This avoids the Reflection Probe border to be clamped when sampling mips but can introduce some blurriness.", null, null);
            public static readonly GUIContent reflectionProbeAtlasLastValidPlanarMipContent = L10n.TextContent("Reflection 2D Atlas Last Valid Planar Mip", "Apply border when the planar Reflection Probe is copied into the atlas. This avoids the Reflection Probe border to be clamped when sampling mips but can introduce some blurriness.", null, null);
            public static readonly GUIContent reflectionProbeDecreaseResToFitContent = L10n.TextContent("Decrease Reflection Probe Resolution To Fit", "When enabled, HDRP decreases a Reflection Probe resolution if the Reflection Probe doesn't fit in the cache.", null, null);

            public static readonly GUIContent supportFabricBSDFConvolutionContent = L10n.TextContent("Fabric BSDF Convolution", "When enabled, HDRP calculates a separate version of each Reflection Probe for the Fabric Shader, creating more accurate lighting effects. See the documentation for more information and limitations of this feature.", null, null);

            public static readonly GUIContent skyReflectionSizeContent = L10n.TextContent("Reflection Size", "Specifies the maximum resolution of the cube map HDRP uses to represent the sky.", null, null);
            public static readonly GUIContent skyLightingOverrideMaskContent = L10n.TextContent("Lighting Override Mask", "Specifies the layer mask HDRP uses to override sky lighting.", null, null);
            public const string skyLightingHelpBoxContent = "Be careful, Sky Lighting Override Mask is set to Everything. This is most likely a mistake as it serves no purpose.";

            public static readonly GUIContent maxDirectionalContent = L10n.TextContent("Maximum Directional on Screen", "Sets the maximum number of Directional Lights HDRP can handle on screen at once.", null, null);
            public static readonly GUIContent maxPonctualContent = L10n.TextContent("Maximum Punctual on Screen", "Sets the maximum number of Point and Spot Lights HDRP can handle on screen at once.", null, null);
            public static readonly GUIContent maxAreaContent = L10n.TextContent("Maximum Area on Screen", "Sets the maximum number of area Lights HDRP can handle on screen at once.", null, null);
            public static readonly GUIContent maxCubeProbesContent = L10n.TextContent("Maximum Cube Reflection Probes on Screen", "Sets the maximum number of Cube Reflection Probes HDRP can handle on screen at once. This value is capped to " + HDRenderPipeline.k_MaxCubeReflectionsOnScreen + " for performance reasons", null, null);
            public static readonly GUIContent maxPlanarProbesContent = L10n.TextContent("Maximum Planar Reflection Probes on Screen", "Sets the maximum number of Planar Reflection Probes HDRP can handle on screen at once.", null, null);
            public static readonly GUIContent maxDecalContent = L10n.TextContent("Maximum Clustered Decals on Screen", "Sets the maximum number of decals that can affect transparent GameObjects on screen.", null, null);
            public static readonly GUIContent maxLightPerCellContent = L10n.TextContent("Maximum Lights per Cell (Ray Tracing)", "Sets the maximum number of lights HDRP can handle in each cell of the ray tracing light cluster.", null, null);

            public static readonly GUIContent resolutionContent = L10n.TextContent("Resolution", "Specifies the resolution of the shadow Atlas.", null, null);
            public static readonly GUIContent cachedShadowAtlasResolution = L10n.TextContent("Cached Resolution", "Specifies the resolution of the shadow Atlas that contains the cached shadow maps.", null, null);
            public static readonly GUIContent directionalShadowPrecisionContent = L10n.TextContent("Precision", "Select the shadow map bit depth, this forces HDRP to use selected bit depth for shadow maps.", null, null);
            public static readonly GUIContent precisionContent = L10n.TextContent("Precision", "Select the shadow map bit depth, this forces HDRP to use selected bit depth for shadow maps.", null, null);
            public static readonly GUIContent dynamicRescaleContent = L10n.TextContent("Dynamic Rescale", "When enabled, scales the shadow map size using the screen size of the Light to leave more space for other shadows in the atlas.", null, null);
            public static readonly GUIContent maxRequestContent = L10n.TextContent("Maximum Shadows on Screen", "Sets the maximum number of shadows HDRP can handle on screen at once. See the documentation for details on how many shadows each light type casts.", null, null);
            public static readonly GUIContent maxResolutionContent = L10n.TextContent("Max Resolution", "Specifies the maximum resolution that each shadow map can have.", null, null);
            public static readonly GUIContent lowQualityContent = L10n.TextContent("Low", "Specifies the resolution of the shadows set to low quality.", null, null);
            public static readonly GUIContent mediumQualityContent = L10n.TextContent("Medium", "Specifies the resolution of the shadows set to medium quality.", null, null);
            public static readonly GUIContent highQualityContent = L10n.TextContent("High", "Specifies the resolution of the shadows set to high quality.", null, null);
            public static readonly GUIContent veryHighQualityContent = L10n.TextContent("Very High", "Specifies the resolution of the shadows set to very high quality.", null, null);
            public static readonly GUIContent allowMixedCachedCascadeShadows = L10n.TextContent("Allow Mixed Cached Shadows", "Allow mixed cached shadows for directional lights, it will incur in further memory cost.", null, null);

            public static readonly GUIContent supportContactShadows = L10n.TextContent("Contact Shadows", "Enables the support of contact shadows in HDRP.", null, null);
            public static readonly GUIContent useContactShadows = L10n.TextContent("Use Contact Shadows", "Use contact shadows for lights.", null, null);

            public static readonly GUIContent supportScreenSpaceShadows = L10n.TextContent("Screen Space Shadows", "Enables the support of screen space shadows in HDRP.", null, null);
            public static readonly GUIContent maxScreenSpaceShadowSlots = L10n.TextContent("Maximum", "Sets the maximum number of screen space shadows slots HDRP can handle on screen at once. Monochrome (Opaque/Transparent) shadows requires one slot, colored shadows requires three and area lights shadows require two.", null, null);
            public static readonly GUIContent screenSpaceShadowFormat = L10n.TextContent("Buffer Format", "Defines the format of the buffer used for screen space shadows. The buffer format can be R8G8B8A8 or R16G16B16A16.", null, null);
            public static readonly GUIContent maxShadowResolution = L10n.TextContent("Max Resolution", "Specifies the maximum resolution for any single shadow map.", null, null);

            public static readonly GUIContent drawDistanceContent = L10n.TextContent("Draw Distance", "Sets the maximum distance from the Camera at which HDRP draws Decals.", null, null);
            public static readonly GUIContent atlasWidthContent = L10n.TextContent("Atlas Width", "Sets the width of the Decal Atlas.", null, null);
            public static readonly GUIContent atlasHeightContent = L10n.TextContent("Atlas Height", "Sets the height of the Decal Atlas.", null, null);
            public static readonly GUIContent metalAndAOContent = L10n.TextContent("Metal and Ambient Occlusion Properties", "When enabled, Decals affect metal and ambient occlusion properties.", null, null);
            public static readonly GUIContent punctualFilteringQuality = L10n.TextContent("Punctual Shadow Filtering Quality", "Specifies the quality of punctual shadows. See the documentation for details on the algorithm HDRP uses for each preset.", null, null);
            public static readonly GUIContent directionalFilteringQuality = L10n.TextContent("Directional Shadow Filtering Quality", "Specifies the quality of directional shadows. See the documentation for details on the algorithm HDRP uses for each preset.", null, null);
            public static readonly GUIContent areaFilteringQuality = L10n.TextContent("Area Shadow Filtering Quality", "Specifies the quality of area shadows. See the documentation for details on the algorithm HDRP uses for each preset.", null, null);


            public static readonly GUIContent DLSSTitle = L10n.TextContent("NVIDIA Deep Learning Super Sampling (DLSS)", null, null, null);
            public static readonly GUIContent enabled = L10n.TextContent("Enable", "When enabled, HDRP dynamically lowers the resolution of render targets to reduce the workload on the GPU.", null, null);
            public static readonly GUIContent enableDLSS = L10n.TextContent("Enable DLSS", "Enables NVIDIA Deep Learning Super Sampling (DLSS).", null, null);
            public static readonly GUIContent DLSSQualitySettingContent = L10n.TextContent("DLSS Mode", "Selects a performance quality setting for NVIDIA Deep Learning Super Sampling (DLSS).", null, null);
            public static readonly GUIContent DLSSInjectionPoint = L10n.TextContent("DLSS Injection Point", "The injection point at which to apply DLSS upscaling.", null, null);
            public static readonly GUIContent defaultInjectionPoint = L10n.TextContent("Injection Point", "The injection point at which to apply the upscaling.", null, null);
            public static readonly GUIContent TAAUInjectionPoint = L10n.TextContent("TAA Upscale Injection Point", "The injection point at which to apply the upscaling.", null, null);
            public static readonly GUIContent STPInjectionPoint = L10n.TextContent("STP Injection Point", "The injection point at which to apply the upscaling.", null, null);
#if ENABLE_UPSCALER_FRAMEWORK
            public static readonly GUIContent IUpscalerInjectionPoint = L10n.TextContent("Injection Point", "The injection point at which to apply the upscaling.", null, null);
#endif
            public static readonly GUIContent DLSSUseOptimalSettingsContent = L10n.TextContent("DLSS Use Optimal Settings", "Sets the scale automatically for NVIDIA Deep Learning Super Sampling, depending on the values of quality settings. When DLSS Optimal Settings is on, the percentage settings for Dynamic Resolution Scaling are ignored.", null, null);
            public static readonly GUIContent DLSSRenderPresetsContent = L10n.TextContent("DLSS Render Presets", "DLSS will use the specified render preset for each quality value.", null, null);
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            public static readonly string[] DLSSPerfQualityLabels =
            {   // should follow enum value ordering in DLSSQuality enum
                UnityEngine.NVIDIA.DLSSQuality.MaximumPerformance.ToString(),
                UnityEngine.NVIDIA.DLSSQuality.Balanced.ToString(),
                UnityEngine.NVIDIA.DLSSQuality.MaximumQuality.ToString(),
                UnityEngine.NVIDIA.DLSSQuality.UltraPerformance.ToString(),
                UnityEngine.NVIDIA.DLSSQuality.DLAA.ToString()
            };
            public static string[][] DLSSPresetOptionsForEachPerfQuality = PopulateDLSSQualityPresetLabels();
            private static string[][] PopulateDLSSQualityPresetLabels()
            {
                int CountBits(uint bitMask) // System.Numerics.BitOperations not available
                {
                    int count = 0;
                    while (bitMask > 0)
                    {
                        count += (bitMask & 1) > 0 ? 1 : 0;
                        bitMask >>= 1;
                    }
                    return count;
                }

                System.Array perfQualities = System.Enum.GetValues(typeof(UnityEngine.NVIDIA.DLSSQuality));
                string[][] labels = new string[perfQualities.Length][];
                foreach(UnityEngine.NVIDIA.DLSSQuality quality in perfQualities)
                {
                    uint presetBitmask = UnityEngine.NVIDIA.GraphicsDevice.GetAvailableDLSSPresetsForQuality(quality);
                    int numPresets = CountBits(presetBitmask) + 1; // +1 for default option which is available to all quality enums
                    labels[(int)quality] = new string[numPresets];

                    int iWrite = 0;
                    System.Array presets = System.Enum.GetValues(typeof(UnityEngine.NVIDIA.DLSSPreset));
                    foreach(UnityEngine.NVIDIA.DLSSPreset preset in presets)
                    {
                        if (preset == UnityEngine.NVIDIA.DLSSPreset.Preset_Default)
                        {
                            labels[(int)quality][iWrite++] = "Default Preset" + " - " + UnityEngine.NVIDIA.GraphicsDevice.GetDLSSPresetExplanation(preset);
                            continue;
                        }

                        if ((presetBitmask & (uint)preset) != 0)
                        {
                            string presetName = preset.ToString().Replace('_', ' ');
                            labels[(int)quality][iWrite++] = presetName + " - " + UnityEngine.NVIDIA.GraphicsDevice.GetDLSSPresetExplanation(preset);
                        }
                    }
                }
                return labels;
            }
#endif

            public static readonly GUIContent FSR2Title = L10n.TextContent("AMD FidelityFX Super Resolution 2.0 (FSR2)", null, null, null);
            public static readonly GUIContent enableFSR2 = L10n.TextContent("Enable Fidelity FX 2.2", "Enables FidelityFX 2.0 Super Resolution (FSR2).", null, null);
            public static readonly GUIContent FSR2InjectionPoint = L10n.TextContent("FSR2 Injection Point", "The injection point at which to apply FidelityFX 2.0 Super Resolution (FSR2).", null, null);
            public static readonly GUIContent FSR2EnableSharpness = L10n.TextContent("FSR2 Enable Sharpness", "Enable an additional sharpening pass on FidelityFX 2.0 Super Resolution (FSR2).", null, null);
            public static readonly GUIContent FSR2UseOptimalSettingsContent = L10n.TextContent("FSR2 Use Optimal Settings", "Sets the scale automatically for AMD Fidelity FX 2.0 Super Resolution (FSR2), depending on the values of quality settings. When FSR2 Optimal Settings is on, the percentage settings for Dynamic Resolution Scaling are ignored.", null, null);
            public static readonly GUIContent FSR2QualitySettingContent = L10n.TextContent("FSR2 Mode", "Selects a performance quality setting for AMD FidelityFX 2.0 Super Resolution (FSR2).", null, null);
            public static readonly GUIContent FSR2Sharpness = L10n.TextContent("FSR2 Sharpness", "The sharpness value between 0 and 1, where 0 is no additional sharpness and 1 is maximum additional sharpness.", null, null);

            public static readonly GUIContent FSRTitle = L10n.TextContent("AMD FidelityFX Super Resolution 1.0 (FSR)", null, null, null);

            public static readonly GUIContent[] UpscalerInjectionPointNames =
            {
                new GUIContent("Before Post Process (Default)"),
                new GUIContent("After Depth Of Field (Low depth of field cost)"),
                new GUIContent("After Post Process (Low post process cost)")
            };
            public static readonly int[] UpscalerInjectionPointValues =
            {
                (int)DynamicResolutionHandler.UpsamplerScheduleType.BeforePost,
                (int)DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField,
                (int)DynamicResolutionHandler.UpsamplerScheduleType.AfterPost
            };

            public const string DLSSPackageLabel = "NVIDIA Deep Learning Super Sampling (DLSS) is not active in this project. To activate it, install the NVIDIA package.";

            public const string DLSSFeatureDetectedMsg = "Unity detected NVIDIA Deep Learning Super Sampling and will ignore the Fallback Upscale Filter.";
            public const string DLSSFeatureNotDetectedMsg = "Unity cannot detect NVIDIA Deep Learning Super Sampling (DLSS) and will use the Fallback Upscale Filter instead.";
            public const string DLSSIgnorePercentages = "Unity detected that NVIDIA Deep Learning Super Sampling (DLSS) is using Optimal Settings. When DLSS Optimal Settings is on, the percentage settings for Dynamic Resolution Scaling are ignored.";
            public const string DLSSWinTargetWarning = "HDRP does not support DLSS for the current build target. To enable DLSS, set your build target to Windows x86_64.";
            public const string DLSSSwitchTarget64Button = "Fix";

            public const string FSR2PackageLabel = "AMD Fidelity FX2 Super Sampling (FSR2) is not active in this project. To activate it, install the AMD package.";
            public const string FSR2WinTargetWarning = "HDRP does not support AMD Fidelity FX2 for the current build target and graphics device API. To enable FSR2, set your build target to Windows x86_64 and DirectX12.";
            public const string FSR2SwitchTarget64Button = "Fix";
            public const string FSR2FeatureDetectedMsg = "Unity detected AMD Fidelity FX 2 Super Resolution and will ignore the Fallback Upscale Filter.";
            public const string FSR2FeatureNotDetectedMsg = "Unity cannot detect AMD Fidelity FX 2 Super Resolution and will use the Fallback Upscale Filter instead.";

            public const string STPSwDrsWarningMsg = "STP cannot support dynamic resolution without hardware dynamic resolution mode. You can use the forced screen percentage feature to guarantee a fixed resoution for STP or HDRP will fall back to the next best supported upscaling filter instead.";

            public const string hwDrsUnsupportedWarningMsg = "The current graphics device does not support hardware dynamic resolution mode. HDRP will automatically fall back to software dynamic resolution mode.";

            public static readonly GUIContent fsrOverrideSharpness = L10n.TextContent("Override FSR Sharpness", "Overrides the FSR sharpness value for the render pipeline asset.", null, null);
            public static readonly GUIContent fsrSharpnessText = L10n.TextContent("FSR Sharpness", "Controls the intensity of the sharpening filter used by AMD FidelityFX Super Resolution.", null, null);

            public static readonly GUIContent maxPercentage = L10n.TextContent("Maximum Screen Percentage", "Sets the maximum screen percentage that dynamic resolution can reach.", null, null);
            public static readonly GUIContent minPercentage = L10n.TextContent("Minimum Screen Percentage", "Sets the minimum screen percentage that dynamic resolution can reach.", null, null);
            public static readonly GUIContent dynResType = L10n.TextContent("Dynamic Resolution Type", "Specifies the type of dynamic resolution that HDRP uses.", null, null);
            public static readonly GUIContent dynResTypeWarning = L10n.TextContent("The current graphics API does not support hardware dynamic resolution mode. HDRP will automatically fall back to software dynamic resolution mode.", null, null, null);
            public static readonly GUIContent useMipBias = L10n.TextContent("Use Mip Bias", "Offsets the mip bias to recover more detail. This only works if the camera is utilizing TAA.", null, null);
            public static readonly GUIContent upsampleFilter = L10n.TextContent("Default Upscale Filter", "Specifies the filter that HDRP uses for upscaling unless overwritten by API by the user.", null, null);
            public static readonly GUIContent upsampleLowResFilter = L10n.TextContent("Upscale Filter", "Specifies the filter that HDRP uses for upscaling the low-resolution transparent texture. Bilinear provides good quality at the cost of performance whereas Nearest Depth is more performant.", null, null);
            public static readonly GUIContent fallbackUpsampleFilter = L10n.TextContent("Default Fallback Upscale Filter", "Specifies the filter that HDRP uses for upscaling as a fallback if DLSS is not detected. Can be overwritten via API.", null, null);
            public static readonly GUIContent forceScreenPercentage = L10n.TextContent("Force Screen Percentage", "When enabled, HDRP uses the Forced Screen Percentage value as the screen percentage.", null, null);
            public static readonly GUIContent forcedScreenPercentage = L10n.TextContent("Forced Screen Percentage", "Sets a specific screen percentage value. HDRP forces this screen percentage for dynamic resolution.", null, null);

#if ENABLE_UPSCALER_FRAMEWORK
            // Contextual help for a framework upscaler's Resolution Mode (drawn under the dropdown in the IUpscaler section).
            public static readonly string iUpscalerQualityModeInfo = L10n.Tr("Quality Mode fixes the render resolution from the upscaler's quality preset. The Dynamic Resolution screen-percentage range is ignored for cameras using this upscaler. Switch to Custom Scaling to drive the render resolution from Dynamic Resolution.", null);
            public static readonly string iUpscalerCustomSingleResInfo = L10n.Tr("This quality preset supports a single render resolution (~{0:0}% render scale), so Dynamic Resolution is ignored (the render resolution is pinned to that recommended size).", null);
            public static readonly string iUpscalerCustomClampInfo = L10n.Tr("Dynamic Resolution drives the render resolution, clamped to this upscaler's supported range [{0:0}%..{1:0}%]. Values outside the range are clamped at runtime.", null);
            public static readonly string iUpscalerCustomMinBelowRangeWarning = L10n.Tr("Minimum Screen Percentage ({0:0}%) is below {2}'s supported minimum ({1:0}%); it will be clamped at runtime. Raise the minimum to stay within the upscaler's range.", null);
            public static readonly string iUpscalerCustomAdvisoryInfo = L10n.Tr("Dynamic Resolution drives the render resolution. Below about {0:0}% render scale this upscaler still works but quality degrades (advisory; not clamped).", null);
#endif
            public static readonly GUIContent lowResTransparencyMinimumThreshold = L10n.TextContent("Low Res Transparency Min Threshold", "The minimum percentage threshold allowed to clamp low resolution transparency. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.", null, null);
            public static readonly GUIContent lowResSSGIMinimumThreshold = L10n.TextContent("Low Res Screen Space GI Min Threshold", "The minimum percentage threshold allowed to clamp low resolution Screen Space Global Illumination. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.", null, null);
            public static readonly GUIContent lowResVolumetricCloudsMinimumThreshold = L10n.TextContent("Low Res Volumetric Clouds Min Threshold", "The minimum percentage threshold allowed to clamp tracing resolution for Volumetric Clouds. When the resolution percentage falls below this threshold, HDRP will trace the Volumetric Clouds in half res.", null, null);
            public const string lowResTransparencyThresholdDisabledMsg = "Low res transparency is currently disabled in the quality settings. \"Low Res Transparency Min Threshold\" will be ignored.";
            public static readonly GUIContent rayTracingHalfResThreshold = L10n.TextContent("Ray Tracing Half Res Threshold", "The minimum percentage threshold allowed to render ray tracing effects at half resolution. When the resolution percentage falls below this threshold, HDRP will render ray tracing effects at full resolution.", null, null);

            public static readonly GUIContent lowResTransparentEnabled = L10n.TextContent("Enable", "When enabled, materials tagged as Low Res Transparent, will be rendered in a quarter res offscreen buffer and then composited to full res.", null, null);
            public static readonly GUIContent checkerboardDepthBuffer = L10n.TextContent("Checkerboarded Depth Buffer Downsample", "When enabled, the depth buffer used for low res transparency is generated in a min/max checkerboard pattern from original full res buffer.", null, null);
            public static readonly GUIContent lowResTranspUpsample = L10n.TextContent("Upsample type", "The type of upsampling filter used to composite the low resolution transparency.", null, null);

            public static readonly GUIContent XRSinglePass = L10n.TextContent("Single Pass", "When enabled, XR views are rendered simultaneously and the render loop is processed only once. This setting will improve CPU and GPU performance but will use more GPU memory.", null, null);
            public static readonly GUIContent XROcclusionMesh = L10n.TextContent("Occlusion Mesh", "When enabled, the occlusion mesh will be rendered for each view during the depth prepass to reduce shaded fragments.", null, null);
            public static readonly GUIContent XRCameraJitter = L10n.TextContent("Camera Jitter", "When enabled, jitter will be added to the camera to provide more samples for temporal effects. This is usually not required in VR due to micro variations from the tracking.", null, null);
            public static readonly GUIContent XRMotionBlur = L10n.TextContent("Allow Motion Blur", "When enabled, motion blur can be used in XR. When this option is disabled, regardless of the settings, motion blur will be turned off when in XR.", null, null);

            public static readonly GUIContent lutSize = L10n.TextContent("Grading LUT Size", "Sets size of the internal and external color grading lookup textures (LUTs).", null, null);
            public static readonly GUIContent lutFormat = L10n.TextContent("Grading LUT Format", "Specifies the encoding format for color grading lookup textures. Lower precision formats are faster and use less memory at the expense of color precision.", null, null);
            public static readonly GUIContent bufferFormat = L10n.TextContent("Buffer Format", "Specifies the encoding format of the color buffers that are used during post processing. Lower precision formats are faster and use less memory at the expense of color precision.", null, null);

            public static readonly GUIContent volumeProfileLabel = L10n.TextContent("Volume Profile", "Settings that will override the values defined in the Default Volume Profile set in the Render Pipeline Global settings. Local Volumes inside scenes may override these settings further.", null, null);
            public static System.Lazy<GUIStyle> volumeProfileContextMenuStyle = new(() => new GUIStyle(CoreEditorStyles.contextMenuStyle) { margin = new RectOffset(0, 1, 3, 0) });

            public static readonly GUIContent[] shadowBitDepthNames = { new GUIContent("32 bit"), new GUIContent("16 bit"), new GUIContent("24 bit") };
            public static readonly int[] shadowBitDepthValues = { (int)DepthBits.Depth32, (int)DepthBits.Depth16, (int)DepthBits.Depth24 };

            public static readonly GUIContent gpuResidentDrawerMode = L10n.TextContent("GPU Resident Drawer", "Enables draw submission through the GPU Resident Drawer, which can improve CPU performance", null, null);
            public static readonly GUIContent smallMeshScreenPercentage = L10n.TextContent("Small-Mesh Screen-Percentage", "Default minimum screen percentage (0-20%) gpu-driven Renderers can cover before getting culled. If a Renderer is part of a LODGroup, this will be ignored.", null, null);
            public static readonly GUIContent enableOcclusionCullingInCameras = L10n.TextContent("GPU Occlusion Culling", "Enables GPU occlusion culling in Game and SceneView cameras.", null, null);
            public static readonly GUIContent useDepthPrepassForOccluders = L10n.TextContent("Occluders From Depth Prepass", "Always builds occluders from the depth pre-pass. If this flag is on, or the Full Depth Prepass within Deferred frame setting is enabled, or Lit Shader Mode is Forward Only, occluders are built during the depth pre-pass.  Otherwise occluders are built during the gbuffer pass.", null, null);

            public static readonly string shadowSmallMeshPctToolTip = "Default minimum shadow screen percentage (0-50%) gpu-driven Renderers can cover before getting culled. If a Renderer is part of a LODGroup, this will be ignored.";
            public static readonly GUIContent shadowSmallMeshPct1 = L10n.TextContent("Cascade 1 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip, null, null);
            public static readonly GUIContent shadowSmallMeshPct2 = L10n.TextContent("Cascade 2 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip, null, null);
            public static readonly GUIContent shadowSmallMeshPct3 = L10n.TextContent("Cascade 3 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip, null, null);
            public static readonly GUIContent shadowSmallMeshPct4 = L10n.TextContent("Cascade 4 Small-Mesh Screen-Percentage", shadowSmallMeshPctToolTip, null, null);

            public static GUIContent brgShaderStrippingErrorMessage =
                L10n.TextContent("\"BatchRendererGroup Variants\" setting must be \"Keep All\". To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".", null, null, null);
            public static GUIContent staticBatchingInfoMessage =
                L10n.TextContent("Static Batching is not recommended when using GPU draw submission modes, performance may improve if Static Batching is disabled in Player Settings.", null, null, null);

            public static readonly string featureNotRecommendedWarning = L10n.Tr("This feature is not optimized for this platform, enabling it may affect performance.", null);
            public static readonly string maxFogBudgetWarning = L10n.Tr("The maximum recommended fog budget for this platform is {0}. Lower the tier to stay under this for optimal performance.", null);
            public static readonly string minFogBudgetForDensityCutoffWarning = L10n.Tr("It is recommended to use Density cutoff when the fog budget is > {0} for this platform.", null);

            public const string memoryDrawback = "Adds GPU memory";
            public const string shaderVariantDrawback = "Adds Shader Variants";
            public const string lotShaderVariantDrawback = "Adds multiple Shader Variants";
            public const string gBufferDrawback = "Adds a GBuffer";
            public const string lotGBufferDrawback = "Adds GBuffers";
            public const string dBufferDrawback = "Adds a DBuffer";
            public const string lotDBufferDrawback = "Adds DBuffers";
            public static readonly Dictionary<GUIContent, string> supportDrawbacks = new Dictionary<GUIContent, string>
            {
                //k_SupportLitShaderModeContent is special case handled separately
                //k_SupportShadowMaskContent is special case handled separately
                { supportSSRContent                  , memoryDrawback },
                { supportSSAOContent                 , memoryDrawback },
                { supportedSSSContent                , memoryDrawback },
                { supportVolumetricFogContent        , memoryDrawback },
                //k_SupportLightLayerContent is special case handled separately
                { MSAASampleCountContent             , memoryDrawback },
                { supportDecalContent                , string.Format("{0}, {1}", memoryDrawback, lotDBufferDrawback) },
                { supportDecalLayersContent          , string.Format("{0}, {1}", memoryDrawback, lotShaderVariantDrawback, lotDBufferDrawback) },
                { metalAndAOContent                  , string.Format("{0}, {1}", memoryDrawback, dBufferDrawback) },
                { supportMotionVectorContent         , memoryDrawback },
                { supportDitheringCrossFadeContent   , shaderVariantDrawback },
                { supportTerrainHoleContent          , shaderVariantDrawback },
                { supportDistortion                  , "" },
                { supportTransparentBackface         , shaderVariantDrawback },
                { supportTransparentDepthPrepass     , shaderVariantDrawback },
                { supportTransparentDepthPostpass    , shaderVariantDrawback },
                { supportRaytracing                  , string.Format("{0}, {1}", memoryDrawback, lotShaderVariantDrawback) },
                { lightProbeSystemContent            , string.Format("{0}, {1}", memoryDrawback, shaderVariantDrawback) }
            };

            public static Dictionary<SupportedLitShaderMode, string> supportLitShaderModeDrawbacks = new Dictionary<SupportedLitShaderMode, string>
            {
                { SupportedLitShaderMode.ForwardOnly, lotShaderVariantDrawback },
                { SupportedLitShaderMode.DeferredOnly, string.Format("{0}, {1}", shaderVariantDrawback, lotGBufferDrawback) },
                { SupportedLitShaderMode.Both, string.Format("{0}, {1}", lotShaderVariantDrawback, lotGBufferDrawback) }
            };

            public static Dictionary<SupportedLitShaderMode, string> supportShadowMaskDrawbacks = new Dictionary<SupportedLitShaderMode, string>
            {
                { SupportedLitShaderMode.ForwardOnly, string.Format("{0}, {1}", shaderVariantDrawback, memoryDrawback) },
                { SupportedLitShaderMode.DeferredOnly, string.Format("{0}, {1}, {2}", shaderVariantDrawback, memoryDrawback, gBufferDrawback) },
                { SupportedLitShaderMode.Both, string.Format("{0}, {1}, {2}", shaderVariantDrawback, memoryDrawback, gBufferDrawback) }
            };

            public static Dictionary<SupportedLitShaderMode, string> supportLightLayerDrawbacks = new Dictionary<SupportedLitShaderMode, string>
            {
                { SupportedLitShaderMode.ForwardOnly, memoryDrawback },
                { SupportedLitShaderMode.DeferredOnly, string.Format("{0}, {1}", memoryDrawback, gBufferDrawback) },
                { SupportedLitShaderMode.Both, string.Format("{0}, {1}", memoryDrawback, gBufferDrawback) }
            };
        }
    }
}
