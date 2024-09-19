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
            public static readonly GUIContent renderingSectionTitle = EditorGUIUtility.TrTextContent("Rendering");
            public static readonly GUIContent lightingSectionTitle = EditorGUIUtility.TrTextContent("Lighting");
            public static readonly GUIContent materialSectionTitle = EditorGUIUtility.TrTextContent("Material");
            public static readonly GUIContent postProcessSectionTitle = EditorGUIUtility.TrTextContent("Post-processing");
            public static readonly GUIContent volumesSectionTitle = EditorGUIUtility.TrTextContent("Volumes");
            public static readonly GUIContent xrTitle = EditorGUIUtility.TrTextContent("XR");
            public static readonly GUIContent virtualTexturingTitle = EditorGUIUtility.TrTextContent("Virtual Texturing", "Virtual Texturing Settings. These are only available when Virtual Texturing is enabled in the Player Settings.");
            public static readonly GUIContent lightLoopSubTitle = EditorGUIUtility.TrTextContent("Lights");
            public static readonly GUIContent tierSubTitle = EditorGUIUtility.TrTextContent("Tier Settings");

            public static readonly GUIContent volumetricSubTitle = EditorGUIUtility.TrTextContent("Volumetrics");
            public static readonly GUIContent volumetricCloudsSubTitle = EditorGUIUtility.TrTextContent("Volumetric Clouds");
            public static readonly GUIContent lightProbeSubTitle = EditorGUIUtility.TrTextContent("Light Probe Lighting");
            public static readonly GUIContent cookiesSubTitle = EditorGUIUtility.TrTextContent("Cookies");
            public static readonly GUIContent reflectionsSubTitle = EditorGUIUtility.TrTextContent("Reflections");
            public static readonly GUIContent skySubTitle = EditorGUIUtility.TrTextContent("Sky");
            public static readonly GUIContent decalsSubTitle = EditorGUIUtility.TrTextContent("Decals");
            public static readonly GUIContent decalsMetalAndAOSubTitle = EditorGUIUtility.TrTextContent("Decals Metal And AO");
            public static readonly GUIContent decalResolutionSubTitle = EditorGUIUtility.TrTextContent("Transparent Texture Resolution");
            public static readonly GUIContent decalResolutionTiers = EditorGUIUtility.TrTextContent("Resolution Tiers");

            public static readonly GUIContent shadowSubTitle = EditorGUIUtility.TrTextContent("Shadows");

            public static readonly GUIContent punctualLightshadowSubTitle = EditorGUIUtility.TrTextContent("Punctual Light Shadows");
            public static readonly GUIContent directionalLightshadowSubTitle = EditorGUIUtility.TrTextContent("Directional Light Shadows");
            public static readonly GUIContent areaLightshadowSubTitle = EditorGUIUtility.TrTextContent("Area Light Shadows");

            public static readonly GUIContent shadowLightAtlasSubTitle = EditorGUIUtility.TrTextContent("Light Atlas");

            public static readonly GUIContent shadowResolutionTiers = EditorGUIUtility.TrTextContent("Resolution Tiers");

            public static readonly GUIContent dynamicResolutionSubTitle = EditorGUIUtility.TrTextContent("Dynamic resolution");
            public static readonly GUIContent lowResTransparencySubTitle = EditorGUIUtility.TrTextContent("Low res Transparency");

            public static readonly GUIContent motionBlurQualitySettings = EditorGUIUtility.TrTextContent("Motion Blur");
            public static readonly GUIContent bloomQualitySettings = EditorGUIUtility.TrTextContent("Bloom");
            public static readonly GUIContent chromaticAberrationQualitySettings = EditorGUIUtility.TrTextContent("Chromatic Aberration");

            public static readonly GUIContent depthOfFieldQualitySettings = EditorGUIUtility.TrTextContent("Depth Of Field");
            public static readonly GUIContent farBlurSubTitle = EditorGUIUtility.TrTextContent("Far Blur");
            public static readonly GUIContent nearBlurSubTitle = EditorGUIUtility.TrTextContent("Near Blur");
            public static readonly GUIContent maxRadiusQuality = EditorGUIUtility.TrTextContent("Max Radius");
            public static readonly GUIContent sampleCountQuality = EditorGUIUtility.TrTextContent("Sample Count");
            public static readonly GUIContent pbrResolutionQualityTitle = EditorGUIUtility.TrTextContent("Enable High Resolution");
            public static readonly GUIContent resolutionQuality = EditorGUIUtility.TrTextContent("Resolution");
            public static readonly GUIContent adaptiveSamplingWeight = EditorGUIUtility.TrTextContent("Adaptive Sampling Weight");
            public static readonly GUIContent highQualityPrefiltering = EditorGUIUtility.TrTextContent("High Quality Prefiltering");
            public static readonly GUIContent highQualityFiltering = EditorGUIUtility.TrTextContent("High Quality Filtering");
            public static readonly GUIContent dofPhysicallyBased = EditorGUIUtility.TrTextContent("Physically Based");
            public static readonly GUIContent limitNearBlur = EditorGUIUtility.TrTextContent("Limit Manual Range Near Blur");
            public static readonly GUIContent maxSamplesQuality = EditorGUIUtility.TrTextContent("Max Samples");

            // Lens Flares
            public static readonly GUIContent LensFlareTitle = EditorGUIUtility.TrTextContent("Lens Flares");

            // SSAO
            public static readonly GUIContent SSAOQualitySettingSubTitle = EditorGUIUtility.TrTextContent("Screen Space Ambient Occlusion");
            public static readonly GUIContent AOStepCount = EditorGUIUtility.TrTextContent("Step Count");
            public static readonly GUIContent AOFullRes = EditorGUIUtility.TrTextContent("Full Resolution");
            public static readonly GUIContent AOMaxRadiusInPixels = EditorGUIUtility.TrTextContent("Maximum Radius in Pixels");
            public static readonly GUIContent AODirectionCount = EditorGUIUtility.TrTextContent("Direction Count");
            public static readonly GUIContent AOBilateralUpsample = EditorGUIUtility.TrTextContent("Bilateral Upsample");

            // RTAO
            public static readonly GUIContent RTAOQualitySettingSubTitle = EditorGUIUtility.TrTextContent("Ray Traced Ambient Occlusion");
            public static readonly GUIContent RTAORayLength = EditorGUIUtility.TrTextContent("Max Ray Length");
            public static readonly GUIContent RTAOSampleCount = EditorGUIUtility.TrTextContent("Sample Count");
            public static readonly GUIContent RTAODenoise = EditorGUIUtility.TrTextContent("Denoise");
            public static readonly GUIContent RTAODenoiserRadius = EditorGUIUtility.TrTextContent("Denoiser Radius");

            public static readonly GUIContent contactShadowsSettingsSubTitle = EditorGUIUtility.TrTextContent("Contact Shadows");
            public static readonly GUIContent contactShadowsSampleCount = EditorGUIUtility.TrTextContent("Sample Count");

            public static readonly GUIContent SSRSettingsSubTitle = EditorGUIUtility.TrTextContent("Screen Space Reflection");
            public static readonly GUIContent SSRMaxRaySteps = EditorGUIUtility.TrTextContent("Max Ray Steps");

            // RTR
            public static readonly GUIContent RTRSettingsSubTitle = EditorGUIUtility.TrTextContent("Ray Traced Reflections (Performance)");
            public static readonly GUIContent RTRMinSmoothness = EditorGUIUtility.TrTextContent("Minimum Smoothness");
            public static readonly GUIContent RTRSmoothnessFadeStart = EditorGUIUtility.TrTextContent("Smoothness Fade Start");
            public static readonly GUIContent RTRRayLength = EditorGUIUtility.TrTextContent("Max Ray Length");
            public static readonly GUIContent RTRFullResolution = EditorGUIUtility.TrTextContent("Full Resolution");
            public static readonly GUIContent RTRRayMaxIterations = EditorGUIUtility.TrTextContent("Ray Max Iterations");
            public static readonly GUIContent RTRDenoise = EditorGUIUtility.TrTextContent("Denoise");
            public static readonly GUIContent RTRDenoiserRadius = EditorGUIUtility.TrTextContent("Denoiser Radius");
            public static readonly GUIContent RTRDenoiserAntiFlicker = EditorGUIUtility.TrTextContent("Anti Flickering Strength");

            // RTGI
            public static readonly GUIContent RTGISettingsSubTitle = EditorGUIUtility.TrTextContent("Ray Traced Global Illumination (Performance)");
            public static readonly GUIContent RTGIRayLength = EditorGUIUtility.TrTextContent("Max Ray Length");
            public static readonly GUIContent RTGIFullResolution = EditorGUIUtility.TrTextContent("Full Resolution");
            public static readonly GUIContent RTGIRaySteps = EditorGUIUtility.TrTextContent("Ray Steps");
            public static readonly GUIContent RTGIDenoise = EditorGUIUtility.TrTextContent("Denoise");
            public static readonly GUIContent RTGIHalfResDenoise = EditorGUIUtility.TrTextContent("Half Resolution Denoiser");
            public static readonly GUIContent RTGIDenoiserRadius = EditorGUIUtility.TrTextContent("Denoiser Radius");
            public static readonly GUIContent RTGISecondDenoise = EditorGUIUtility.TrTextContent("Second Denoiser Pass");

            // SSGI
            public static readonly GUIContent SSGISettingsSubTitle = EditorGUIUtility.TrTextContent("Screen Space Global Illumination");
            public static readonly GUIContent SSGIRaySteps = EditorGUIUtility.TrTextContent("Ray Steps");
            public static readonly GUIContent SSGIRadius = EditorGUIUtility.TrTextContent("Radius");
            public static readonly GUIContent SSGIClampValue = EditorGUIUtility.TrTextContent("Clamp Value");
            public static readonly GUIContent SSGIDenoise = EditorGUIUtility.TrTextContent("Denoise");
            public static readonly GUIContent SSGIHalfResDenoise = EditorGUIUtility.TrTextContent("Half Resolution Denoiser");
            public static readonly GUIContent SSGIDenoiserRadius = EditorGUIUtility.TrTextContent("Denoiser Radius");
            public static readonly GUIContent SSGISecondDenoise = EditorGUIUtility.TrTextContent("Second Denoiser Pass");

            // Water rendering
            public static readonly GUIContent waterSubTitle = EditorGUIUtility.TrTextContent("Water");
            public static readonly GUIContent supportWaterContent = EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP allocates memory for the water surfaces simulation and rendering. This allows you to use water surfaces in your Unity Project.");
            public static readonly GUIContent waterSimulationResolutionContent = EditorGUIUtility.TrTextContent("Simulation Resolution", "Specifies the resolution of the water simulation. A higher resolution increases the visual quality, but increases the cost.");
            public static readonly GUIContent supportWaterDeformationContent = EditorGUIUtility.TrTextContent("Deformation", "When enabled, HDRP allocates additional memory to support water deformation.");
            public static readonly GUIContent waterDecalAtlasSizeContent = EditorGUIUtility.TrTextContent("Decal Atlas Size", "Specifies the size of the atlas used to store texture water decals.");
            public static readonly GUIContent maximumWaterDecalCountContent = EditorGUIUtility.TrTextContent("Maximum Decal Count", "Sets the maximum amount of water decals HDRP can support.");
            public static readonly GUIContent supportWaterFoamContent = EditorGUIUtility.TrTextContent("Foam", "When enabled, HDRP allocates additional memory to support water foam.");
            public static readonly GUIContent foamAtlasSizeContent = EditorGUIUtility.TrTextContent("Foam Atlas Size", "Specifies the size of the atlas used to store texture water foam.");
            public static readonly GUIContent supportWaterExclusionContent = EditorGUIUtility.TrTextContent("Exclusion", "When enabled, HDRP allocates a stencil bit to support water excluders.");

            // High Quality Line Rendering
            public static readonly GUIContent highQualityLineRenderingSubTitle = EditorGUIUtility.TrTextContent("High Quality Line Rendering");
            public static readonly GUIContent supportHighQualityLineRenderingContent = EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP allocates memory for high quality line rendering. This allows you to render lines with high quality anti-aliasing and transparency in your Unity Project.");
            public static readonly GUIContent highQualityLineRenderingMemoryBudget = EditorGUIUtility.TrTextContent("Memory Budget", "Determines the size of graphics memory allocations for high quality line rendering.");
            // Compute Thickness
            public static readonly GUIContent computeThicknessSubTitle = EditorGUIUtility.TrTextContent("Compute Thickness");
            public static readonly GUIContent computeThicknessEnableContent = EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP allocates memory for the Compute Thickness pass. For each Game Object layer selected in LayerMask, the thickness of all objects in that layer is written in a buffer. This buffer can be sampled only in Shader Graph via HDSampleBuffer node with the layer index as input.");
            public static readonly GUIContent computeThicknessResolutionContent = EditorGUIUtility.TrTextContent("Resolution", "Specifies the resolution of the Compute Thickness buffers. A higher resolution increases visual quality, but increases the cost.");
            public static readonly GUIContent computeThicknessLayerContent = EditorGUIUtility.TrTextContent("Layer Mask", "Specifies the list of Game Objects layers included in the Thickness Compute pass. For each layer selected, HDRP allocated a Render Target. In VR, all layers will be computed into the same buffer.");

            // Fog
            public static readonly GUIContent FogSettingsSubTitle = EditorGUIUtility.TrTextContent("Volumetric Fog");
            public static readonly GUIContent FogSettingsBudget = EditorGUIUtility.TrTextContent("Volumetric Fog Budget");
            public static readonly GUIContent FogSettingsRatio = EditorGUIUtility.TrTextContent("Resolution Depth Ratio");

            public static readonly GUIContent materialQualityLevelContent = EditorGUIUtility.TrTextContent("Default Material Quality Level", "");

            public static readonly GUIContent supportShadowMaskContent = EditorGUIUtility.TrTextContent("Shadowmask", "When enabled, HDRP allocates Shader variants and memory for processing shadow masks. This allows you to use shadow masks in your Unity Project.");
            public static readonly GUIContent supportSSRContent = EditorGUIUtility.TrTextContent("Screen Space Reflection", "When enabled, HDRP allocates memory for processing screen space reflection (SSR). This allows you to use SSR in your Unity Project.");
            public static readonly GUIContent cubeResolutionTitle = EditorGUIUtility.TrTextContent("Cube Reflection Resolution Tiers", "Resolution of a cube Reflection Probe rendered from a camera. Unity represents a cube Reflection Probe in its 2d atlas with a 2d octahedral projection texture.");
            public static readonly GUIContent planarResolutionTitle = EditorGUIUtility.TrTextContent("Planar Reflection Resolution Tiers", "Resolution of a planar Reflection Probe rendered from a camera.");
            public static readonly GUIContent supportSSRTransparentContent = EditorGUIUtility.TrTextContent("Transparent", "When enabled, HDRP executes additional steps to achieve screen space reflection (SSR) on transparent objects. This feature requires both screen space reflections and transparent depth prepass to be enabled.");
            public static readonly GUIContent supportSSAOContent = EditorGUIUtility.TrTextContent("Screen Space Ambient Occlusion", "When enabled, HDRP allocates memory for processing screen space ambient occlusion (SSAO). This allows you to use SSAO in your Unity Project.");
            public static readonly GUIContent supportSSGIContent = EditorGUIUtility.TrTextContent("Screen Space Global Illumination", "When enabled, HDRP allocates memory for processing screen space global illumination (SSGI). This allows you to use SSGI in your Unity Project.");
            public static readonly GUIContent renderingLayerMaskBuffer = EditorGUIUtility.TrTextContent("Rendering Layer Mask Buffer", "When enabled, HDRP writes Rendering Layer Mask of Renderers to a buffer target that can be sampled in a shader in order to create fullscreen effects.\nThis comes with a performance and a memory cost.");
            public static readonly GUIContent supportedSSSContent = EditorGUIUtility.TrTextContent("Subsurface Scattering", "When enabled, HDRP allocates memory for processing subsurface scattering (SSS). This allows you to use SSS in your Unity Project.");
            public static readonly GUIContent sssSampleBudget = EditorGUIUtility.TrTextContent("Sample Budget", "Maximum number of samples the Subsurface Scattering algorithm is allowed to take.");
            public static readonly GUIContent sssDownsampleSteps = EditorGUIUtility.TrTextContent("Downsample Level", "The number of downsample steps done to the source irradance textrure before it is used by the Subsurface Scattering algorithm. Higher value will improve performance, but might lower quality.");
            public static readonly GUIContent supportVolumetricFogContent = EditorGUIUtility.TrTextContent("Volumetric Fog", "When enabled, HDRP allocates Shader variants and memory for volumetric effects. This allows you to use volumetric lighting and fog in your Unity Project.");
            public static readonly GUIContent supportVolumetricCloudsContent = EditorGUIUtility.TrTextContent("Volumetric Clouds", "When enabled, HDRP allocates memory for processing volumetric clouds. This allows you to use volumetric clouds in your Unity Project.");
            public static readonly GUIContent volumetricResolutionContent = EditorGUIUtility.TrTextContent("High Quality ", "When enabled, HDRP increases the resolution of volumetric lighting buffers. Warning: There is a high performance cost, do not enable on consoles.");
            public static readonly GUIContent supportLightLayerContent = EditorGUIUtility.TrTextContent("Light Layers", "When enabled, HDRP allocates memory for processing Light Layers. This allows you to use Light Layers in your Unity Project. For deferred rendering, this allocation includes an extra render target in memory and extra cost.");
            public static readonly GUIContent colorBufferFormatContent = EditorGUIUtility.TrTextContent("Color Buffer Format", "Specifies the format used by the scene color render target. R11G11B10 is a faster option and should have sufficient precision.");
            public static readonly GUIContent supportCustomPassContent = EditorGUIUtility.TrTextContent("Custom Pass", "When enabled, HDRP allocates a custom pass buffer. It also enable custom passes inside Custom Pass Volume components.");
            public static readonly GUIContent customBufferFormatContent = EditorGUIUtility.TrTextContent("Custom Buffer Format", "Specifies the format used by the custom pass render target.");
            public static readonly GUIContent supportLitShaderModeContent = EditorGUIUtility.TrTextContent("Lit Shader Mode", "Specifies the rendering modes HDRP supports for Lit Shaders. HDRP removes all allocated memory and Shader variants for modes you do not specify.");
            public static readonly GUIContent MSAASampleCountContent = EditorGUIUtility.TrTextContent("Multisample Anti-aliasing Quality", "Specifies the default quality for MSAA. Set Lit Shader Mode to Forward Only or Both to use this feature.\nMSAA is not supported when water or raytracing is enabled");
            public static readonly GUIContent supportDecalContent = EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP allocates Shader variants and memory to the decals buffer and cluster decal. This allows you to use decals in your Unity Project.");
            public static readonly GUIContent supportDecalLayersContent = EditorGUIUtility.TrTextContent("Layers", "When enabled, HDRP allocates Shader variants and memory to the decals layers buffer. This allows you to use decal layers in your Unity Project.");
            public static readonly GUIContent supportSurfaceGradientContent = EditorGUIUtility.TrTextContent("Additive Normal Blending", "When enabled, HDRP uses surface gradients to preserve the affected objects normal when applying decal normals.");
            public static readonly GUIContent decalNormalFormatContent = EditorGUIUtility.TrTextContent("High Precision Normal Buffer", "When enabled, HDRP uses a high precision format for the buffer storing decal normals.");
            public static readonly GUIContent supportMotionVectorContent = EditorGUIUtility.TrTextContent("Motion Vectors", "When enabled, HDRP allocates memory for processing motion vectors which it uses for Motion Blur, TAA, and temporal re-projection of various effect like SSR.");
            public static readonly GUIContent supportRuntimeAOVAPIContent = EditorGUIUtility.TrTextContent("Runtime AOV API", "When disabled, HDRP removes all AOV API Shader variants when you build for the Unity Player. This decreases build time.");
            public static readonly GUIContent supportDitheringCrossFadeContent = EditorGUIUtility.TrTextContent("Dithering Cross-fade", "When disabled, HDRP removes all dithering cross fade Shader variants when you build for the Unity Player. This decreases build time.");
            public static readonly GUIContent supportTerrainHoleContent = EditorGUIUtility.TrTextContent("Terrain Hole", "When disabled, HDRP removes all Terrain hole Shader variants when you build for the Unity Player. This decreases build time.");
            public static readonly GUIContent supportDistortion = EditorGUIUtility.TrTextContent("Distortion", "When disabled, HDRP removes all distortion Shader variants when you build for the Unity Player. This decreases build time.");
            public static readonly GUIContent supportTransparentBackface = EditorGUIUtility.TrTextContent("Transparent Backface", "When disabled, HDRP removes all transparent backface Shader variants when you build for the Unity Player. This decreases build time.");
            public static readonly GUIContent supportTransparentDepthPrepass = EditorGUIUtility.TrTextContent("Transparent Depth Prepass", "When disabled, HDRP removes all transparent depth prepass Shader variants when you build for the Unity Player. This decreases build time.");
            public static readonly GUIContent supportTransparentDepthPostpass = EditorGUIUtility.TrTextContent("Transparent Depth Postpass", "When disabled, HDRP removes all transparent depth postpass Shader variants when you build for the Unity Player. This decreases build time.");
            public static readonly GUIContent supportRaytracing = EditorGUIUtility.TrTextContent("Realtime Raytracing");
            public static readonly GUIContent supportedRayTracingMode = EditorGUIUtility.TrTextContent("Supported Ray Tracing Mode");
            public static readonly GUIContent supportVFXRayTracing = EditorGUIUtility.TrTextContent("Visual Effects Ray Tracing", "When enabled, Visual Effects Outputs which have Enable Ray Tracing on will be accounted for in Ray-traced effects.");
            public static readonly GUIContent rayTracingUnsupportedWarning = EditorGUIUtility.TrTextContent("Ray tracing is not supported on your device. Please refer to the documentation.");
            public static readonly GUIContent rayTracingRestrictionOnlyWarning = EditorGUIUtility.TrTextContent("Ray tracing is currently only supported on DX12, Playstation 5 and Xbox Series X.", null, CoreEditorStyles.iconWarn);
            public static readonly GUIContent rayTracingMSAAUnsupported = EditorGUIUtility.TrTextContent("When Ray tracing is enabled in asset, MSAA is not supported. Please refer to the documentation.");
            public static readonly GUIContent waterMSAAUnsupported = EditorGUIUtility.TrTextContent("When Water is enabled in asset, MSAA is not supported. Please refer to the documentation.");
            public static readonly GUIContent maximumLODLevel = EditorGUIUtility.TrTextContent("Maximum LOD Level");
            public static readonly GUIContent LODBias = EditorGUIUtility.TrTextContent("LOD Bias");
            public static readonly GUIContent supportRuntimeDebugDisplayContentLabel = EditorGUIUtility.TrTextContent("Runtime Debug Shaders", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.");
            public static readonly GUIContent lightProbeSystemContent = EditorGUIUtility.TrTextContent("Light Probe System", "What system to use for Light Probes.");
            public static readonly GUIContent probeVolumeMemoryBudget = EditorGUIUtility.TrTextContent("Memory Budget", "Determines the width and height of the textures used to store GI data from probes. Note that the textures also has a fixed depth dimension.");
            public static readonly GUIContent probeVolumeBlendingMemoryBudget = EditorGUIUtility.TrTextContent("Scenario Blending Memory Budget", "Determines the width and height of the textures used to blend between lighting scenarios. Note that the textures also has a fixed depth dimension.");
            public static readonly GUIContent supportProbeVolumeScenarios = EditorGUIUtility.TrTextContent("Lighting Scenarios", "Support lighting scenarios for probe volume.");
            public static readonly GUIContent supportProbeVolumeScenarioBlending = EditorGUIUtility.TrTextContent("Scenario Blending", "Support lighting scenarios blending for probe volume.");
            public static readonly GUIContent supportProbeVolumeDiskStreaming = EditorGUIUtility.TrTextContent("Enable Disk Streaming", "Enable cell streamin from the disk for probe volume. Enabling this will force GPU streaming for probe volume.");
            public static readonly GUIContent supportProbeVolumeGPUStreaming = EditorGUIUtility.TrTextContent("Enable GPU Streaming", "Enable cell streaming in and out of GPU memory for the probe volume.");
            public static readonly GUIContent probeVolumeSHBands = EditorGUIUtility.TrTextContent("SH Bands", "Determines up to what SH bands the Probe Volume will use. Choosing L2 will lead to better quality, but also higher memory and runtime cost.");
            public static readonly GUIContent maxLocalVolumetricFogSizeStyle = EditorGUIUtility.TrTextContent("Max Local Fog Size", "Specifies the maximum size for the individual 3D Local Volumetric Fog texture that HDRP uses for Local Volumetric Fog. This settings will affect your memory consumption.");
            public static readonly GUIContent maxLocalVolumetricFogOnScreenStyle = EditorGUIUtility.TrTextContent("Max Local Fog On Screen", "Sets the maximum number of Local Volumetric Fog can handle on screen at once. This settings will affect your memory consumption.");
            public static readonly GUIContent supportScreenSpaceLensFlare = EditorGUIUtility.TrTextContent("Screen Space Lens Flare", "When enabled, HDRP allocates shader variants and memory for Screen Space Lens Flare effect.");
            public static readonly GUIContent supportDataDrivenLensFlare = EditorGUIUtility.TrTextContent("Data Driven Lens Flare", "When enabled, HDRP allocates shader variants and memory for Data Driven Lens Flare effect.");

            public const string cacheErrorFormat = "This configuration will lead to more than 2 GB reserved for this cache at runtime! ({0} requested) Only {1} element will be reserved instead.";
            public const string cacheInfoFormat = "Reserving {0} in memory at runtime.";
            public const string multipleDifferenteValueMessage = "Multiple different values";
            public const string rayTracingUnsupportedMessage = "The current HDRP Asset does not support Ray Tracing.";

            public static readonly GUIContent cookieSizeContent = EditorGUIUtility.TrTextContent("Cookie Size", "Specifies the maximum size for the individual 2D cookies that HDRP uses for Directional and Spot Lights.");
            public static readonly GUIContent cookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Texture Array Size", "Sets the maximum Texture Array size for the 2D cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.");
#if UNITY_2020_1_OR_NEWER
#else
            public static readonly GUIContent pointCoockieSizeContent = EditorGUIUtility.TrTextContent("Point Cookie Size", "Specifies the maximum size for the Cube cookies HDRP uses for Point Lights.");
#endif
            public static readonly GUIContent pointCookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Cubemap Array Size", "Sets the maximum Texture Array size for the Cube cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.");
            public static readonly GUIContent cookieAtlasSizeContent = EditorGUIUtility.TrTextContent("2D Atlas Size", "Specifies the size of the atlas used for 2D cookies (Directional, Spot and Rectangle Lights).");
            public static readonly GUIContent cookieAtlasFormatContent = EditorGUIUtility.TrTextContent("Format", "Specifies the HDR format of the atlas used for 2D cookies. R16G16B16A16 can be use for EXR cookies (it provides more precision than R11G11B10)");
            public static readonly GUIContent cookieAtlasLastValidMipContent = EditorGUIUtility.TrTextContent("2D Atlas Last Valid Mip", "Apply border when the cookie is copied into the atlas. It avoid the cookie border to be clamped when sampling mips but can introduce some blurriness.");

            public static readonly GUIContent reflectionProbeCompressCacheContent = EditorGUIUtility.TrTextContent("Compress Baked Reflection Probes", "When enabled, HDRP compresses baked Reflection Probes to save disk space.");
            public static readonly GUIContent reflectionProbeFormatContent = EditorGUIUtility.TrTextContent("Reflection Probe Format", "Color format used for reflection and planar probes. Keep in mind that probes are not pre-exposed when selecting the format.");
            public static readonly GUIContent reflectionProbeAtlasSizeContent = EditorGUIUtility.TrTextContent("Reflection 2D Atlas Size", "Specifies the resolution of Reflection Probes Atlas.");
            public static readonly GUIContent reflectionProbeAtlasLastValidCubeMipContent = EditorGUIUtility.TrTextContent("Reflection 2D Atlas Last Valid Cube Mip", "Apply border when the cube Reflection Probe is copied into the atlas. This avoids the Reflection Probe border to be clamped when sampling mips but can introduce some blurriness.");
            public static readonly GUIContent reflectionProbeAtlasLastValidPlanarMipContent = EditorGUIUtility.TrTextContent("Reflection 2D Atlas Last Valid Planar Mip", "Apply border when the planar Reflection Probe is copied into the atlas. This avoids the Reflection Probe border to be clamped when sampling mips but can introduce some blurriness.");
            public static readonly GUIContent reflectionProbeDecreaseResToFitContent = EditorGUIUtility.TrTextContent("Decrease Reflection Probe Resolution To Fit", "When enabled, HDRP decreases a Reflection Probe resolution if the Reflection Probe doesn't fit in the cache.");

            public static readonly GUIContent supportFabricBSDFConvolutionContent = EditorGUIUtility.TrTextContent("Fabric BSDF Convolution", "When enabled, HDRP calculates a separate version of each Reflection Probe for the Fabric Shader, creating more accurate lighting effects. See the documentation for more information and limitations of this feature.");

            public static readonly GUIContent skyReflectionSizeContent = EditorGUIUtility.TrTextContent("Reflection Size", "Specifies the maximum resolution of the cube map HDRP uses to represent the sky.");
            public static readonly GUIContent skyLightingOverrideMaskContent = EditorGUIUtility.TrTextContent("Lighting Override Mask", "Specifies the layer mask HDRP uses to override sky lighting.");
            public const string skyLightingHelpBoxContent = "Be careful, Sky Lighting Override Mask is set to Everything. This is most likely a mistake as it serves no purpose.";

            public static readonly GUIContent maxDirectionalContent = EditorGUIUtility.TrTextContent("Maximum Directional on Screen", "Sets the maximum number of Directional Lights HDRP can handle on screen at once.");
            public static readonly GUIContent maxPonctualContent = EditorGUIUtility.TrTextContent("Maximum Punctual on Screen", "Sets the maximum number of Point and Spot Lights HDRP can handle on screen at once.");
            public static readonly GUIContent maxAreaContent = EditorGUIUtility.TrTextContent("Maximum Area on Screen", "Sets the maximum number of area Lights HDRP can handle on screen at once.");
            public static readonly GUIContent maxCubeProbesContent = EditorGUIUtility.TrTextContent("Maximum Cube Reflection Probes on Screen", "Sets the maximum number of Cube Reflection Probes HDRP can handle on screen at once. This value is capped to " + HDRenderPipeline.k_MaxCubeReflectionsOnScreen + " for performance reasons");
            public static readonly GUIContent maxPlanarProbesContent = EditorGUIUtility.TrTextContent("Maximum Planar Reflection Probes on Screen", "Sets the maximum number of Planar Reflection Probes HDRP can handle on screen at once.");
            public static readonly GUIContent maxDecalContent = EditorGUIUtility.TrTextContent("Maximum Clustered Decals on Screen", "Sets the maximum number of decals that can affect transparent GameObjects on screen.");
            public static readonly GUIContent maxLightPerCellContent = EditorGUIUtility.TrTextContent("Maximum Lights per Cell (Ray Tracing)", "Sets the maximum number of lights HDRP can handle in each cell of the ray tracing light cluster.");

            public static readonly GUIContent resolutionContent = EditorGUIUtility.TrTextContent("Resolution", "Specifies the resolution of the shadow Atlas.");
            public static readonly GUIContent cachedShadowAtlasResolution = EditorGUIUtility.TrTextContent("Cached Resolution", "Specifies the resolution of the shadow Atlas that contains the cached shadow maps.");
            public static readonly GUIContent directionalShadowPrecisionContent = EditorGUIUtility.TrTextContent("Precision", "Select the shadow map bit depth, this forces HDRP to use selected bit depth for shadow maps.");
            public static readonly GUIContent precisionContent = EditorGUIUtility.TrTextContent("Precision", "Select the shadow map bit depth, this forces HDRP to use selected bit depth for shadow maps.");
            public static readonly GUIContent dynamicRescaleContent = EditorGUIUtility.TrTextContent("Dynamic Rescale", "When enabled, scales the shadow map size using the screen size of the Light to leave more space for other shadows in the atlas.");
            public static readonly GUIContent maxRequestContent = EditorGUIUtility.TrTextContent("Maximum Shadows on Screen", "Sets the maximum number of shadows HDRP can handle on screen at once. See the documentation for details on how many shadows each light type casts.");
            public static readonly GUIContent maxResolutionContent = EditorGUIUtility.TrTextContent("Max Resolution", "Specifies the maximum resolution that each shadow map can have.");
            public static readonly GUIContent lowQualityContent = EditorGUIUtility.TrTextContent("Low", "Specifies the resolution of the shadows set to low quality.");
            public static readonly GUIContent mediumQualityContent = EditorGUIUtility.TrTextContent("Medium", "Specifies the resolution of the shadows set to medium quality.");
            public static readonly GUIContent highQualityContent = EditorGUIUtility.TrTextContent("High", "Specifies the resolution of the shadows set to high quality.");
            public static readonly GUIContent veryHighQualityContent = EditorGUIUtility.TrTextContent("Very High", "Specifies the resolution of the shadows set to very high quality.");
            public static readonly GUIContent allowMixedCachedCascadeShadows = EditorGUIUtility.TrTextContent("Allow Mixed Cached Shadows", "Allow mixed cached shadows for directional lights, it will incurr in further memory cost.");

            public static readonly GUIContent useContactShadows = EditorGUIUtility.TrTextContent("Use Contact Shadows", "Use contact shadows for lights.");
            public static readonly GUIContent supportScreenSpaceShadows = EditorGUIUtility.TrTextContent("Screen Space Shadows", "Enables the support of screen space shadows in HDRP.");
            public static readonly GUIContent maxScreenSpaceShadowSlots = EditorGUIUtility.TrTextContent("Maximum", "Sets the maximum number of screen space shadows slots HDRP can handle on screen at once. Monochrome (Opaque/Transparent) shadows requires one slot, colored shadows requires three and area lights shadows require two.");
            public static readonly GUIContent screenSpaceShadowFormat = EditorGUIUtility.TrTextContent("Buffer Format", "Defines the format of the buffer used for screen space shadows. The buffer format can be R8G8B8A8 or R16G16B16A16.");
            public static readonly GUIContent maxShadowResolution = EditorGUIUtility.TrTextContent("Max Resolution", "Specifies the maximum resolution for any single shadow map.");

            public static readonly GUIContent drawDistanceContent = EditorGUIUtility.TrTextContent("Draw Distance", "Sets the maximum distance from the Camera at which HDRP draws Decals.");
            public static readonly GUIContent atlasWidthContent = EditorGUIUtility.TrTextContent("Atlas Width", "Sets the width of the Decal Atlas.");
            public static readonly GUIContent atlasHeightContent = EditorGUIUtility.TrTextContent("Atlas Height", "Sets the height of the Decal Atlas.");
            public static readonly GUIContent metalAndAOContent = EditorGUIUtility.TrTextContent("Metal and Ambient Occlusion Properties", "When enabled, Decals affect metal and ambient occlusion properties.");
            public static readonly GUIContent punctualFilteringQuality = EditorGUIUtility.TrTextContent("Punctual Shadow Filtering Quality", "Specifies the quality of punctual shadows. See the documentation for details on the algorithm HDRP uses for each preset.");
            public static readonly GUIContent directionalFilteringQuality = EditorGUIUtility.TrTextContent("Directional Shadow Filtering Quality", "Specifies the quality of directional shadows. See the documentation for details on the algorithm HDRP uses for each preset.");
            public static readonly GUIContent areaFilteringQuality = EditorGUIUtility.TrTextContent("Area Shadow Filtering Quality", "Specifies the quality of area shadows. See the documentation for details on the algorithm HDRP uses for each preset.");


            public static readonly GUIContent DLSSTitle = EditorGUIUtility.TrTextContent("NVIDIA Deep Learning Super Sampling (DLSS)");
            public static readonly GUIContent enabled = EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP dynamically lowers the resolution of render targets to reduce the workload on the GPU.");
            public static readonly GUIContent enableDLSS = EditorGUIUtility.TrTextContent("Enable DLSS", "Enables NVIDIA Deep Learning Super Sampling (DLSS).");
            public static readonly GUIContent DLSSQualitySettingContent = EditorGUIUtility.TrTextContent("DLSS Mode", "Selects a performance quality setting for NVIDIA Deep Learning Super Sampling (DLSS).");
            public static readonly GUIContent DLSSInjectionPoint = EditorGUIUtility.TrTextContent("DLSS Injection Point", "The injection point at which to apply DLSS upscaling.");
            public static readonly GUIContent defaultInjectionPoint = EditorGUIUtility.TrTextContent("Injection Point", "The injection point at which to apply the upscaling.");
            public static readonly GUIContent TAAUInjectionPoint = EditorGUIUtility.TrTextContent("TAA Upscale Injection Point", "The injection point at which to apply the upscaling.");
            public static readonly GUIContent STPInjectionPoint = EditorGUIUtility.TrTextContent("STP Injection Point", "The injection point at which to apply the upscaling.");
            public static readonly GUIContent DLSSUseOptimalSettingsContent = EditorGUIUtility.TrTextContent("DLSS Use Optimal Settings", "Sets the sharpness and scale automatically for NVIDIA Deep Learning Super Sampling, depending on the values of quality settings. When DLSS Optimal Settings is on, the percentage settings for Dynamic Resolution Scaling are ignored.");
            public static readonly GUIContent DLSSSharpnessContent = EditorGUIUtility.TrTextContent("DLSS Sharpness", "NVIDIA Deep Learning Super Sampling pixel sharpness of upsampler. Controls how the DLSS upsampler will render edges on the image. More sharpness usually means more contrast and clearer image but can increase flickering and fireflies. This setting is ignored if optimal settings are used.");

            public static readonly GUIContent FSR2Title = EditorGUIUtility.TrTextContent("AMD FidelityFX Super Resolution 2.0 (FSR2)");
            public static readonly GUIContent enableFSR2 = EditorGUIUtility.TrTextContent("Enable Fidelity FX 2.2", "Enables FidelityFX 2.0 Super Resolution (FSR2).");
            public static readonly GUIContent FSR2InjectionPoint = EditorGUIUtility.TrTextContent("FSR2 Injection Point", "The injection point at which to apply FidelityFX 2.0 Super Resolution (FSR2).");
            public static readonly GUIContent FSR2EnableSharpness = EditorGUIUtility.TrTextContent("FSR2 Enable Sharpness", "Enable an additional sharpening pass on FidelityFX 2.0 Super Resolution (FSR2).");
            public static readonly GUIContent FSR2UseOptimalSettingsContent = EditorGUIUtility.TrTextContent("FSR2 Use Optimal Settings", "Sets the scale automatically for AMD Fidelity FX 2.0 Super Resolution (FSR2), depending on the values of quality settings. When FSR2 Optimal Settings is on, the percentage settings for Dynamic Resolution Scaling are ignored.");
            public static readonly GUIContent FSR2QualitySettingContent = EditorGUIUtility.TrTextContent("FSR2 Mode", "Selects a performance quality setting for AMD FidelityFX 2.0 Super Resolution (FSR2).");
            public static readonly GUIContent FSR2Sharpness = EditorGUIUtility.TrTextContent("FSR2 Sharpness", "The sharpness value between 0 and 1, where 0 is no additional sharpness and 1 is maximum additional sharpness.");

            public static readonly GUIContent FSRTitle = EditorGUIUtility.TrTextContent("AMD FidelityFX Super Resolution 1.0 (FSR)");

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
            public const string FSR2FeatureNotDetectedMsg = "Unity cannot detect Unity detected AMD Fidelity FX 2 Super Resolution and will use the Fallback Upscale Filter instead.";

            public const string STPSwDrsWarningMsg = "STP cannot support dynamic resolution without hardware dynamic resolution mode. You can use the forced screen percentage feature to guarantee a fixed resoution for STP or HDRP will fall back to the next best supported upscaling filter instead.";

            public const string hwDrsUnsupportedWarningMsg = "The current graphics device does not support hardware dynamic resolution mode. HDRP will automatically fall back to software dynamic resolution mode.";

            public static readonly GUIContent fsrOverrideSharpness = EditorGUIUtility.TrTextContent("Override FSR Sharpness", "Overrides the FSR sharpness value for the render pipeline asset.");
            public static readonly GUIContent fsrSharpnessText = EditorGUIUtility.TrTextContent("FSR Sharpness", "Controls the intensity of the sharpening filter used by AMD FidelityFX Super Resolution.");

            public static readonly GUIContent maxPercentage = EditorGUIUtility.TrTextContent("Maximum Screen Percentage", "Sets the maximum screen percentage that dynamic resolution can reach.");
            public static readonly GUIContent minPercentage = EditorGUIUtility.TrTextContent("Minimum Screen Percentage", "Sets the minimum screen percentage that dynamic resolution can reach.");
            public static readonly GUIContent dynResType = EditorGUIUtility.TrTextContent("Dynamic Resolution Type", "Specifies the type of dynamic resolution that HDRP uses.");
            public static readonly GUIContent dynResTypeWarning = EditorGUIUtility.TrTextContent("The current graphics API does not support hardware dynamic resolution mode. HDRP will automatically fall back to software dynamic resolution mode.");
            public static readonly GUIContent useMipBias = EditorGUIUtility.TrTextContent("Use Mip Bias", "Offsets the mip bias to recover more detail. This only works if the camera is utilizing TAA.");
            public static readonly GUIContent upsampleFilter = EditorGUIUtility.TrTextContent("Default Upscale Filter", "Specifies the filter that HDRP uses for upscaling unless overwritten by API by the user.");
            public static readonly GUIContent fallbackUpsampleFilter = EditorGUIUtility.TrTextContent("Default Fallback Upscale Filter", "Specifies the filter that HDRP uses for upscaling as a fallback if DLSS is not detected. Can be overwritten via API.");
            public static readonly GUIContent forceScreenPercentage = EditorGUIUtility.TrTextContent("Force Screen Percentage", "When enabled, HDRP uses the Forced Screen Percentage value as the screen percentage.");
            public static readonly GUIContent forcedScreenPercentage = EditorGUIUtility.TrTextContent("Forced Screen Percentage", "Sets a specific screen percentage value. HDRP forces this screen percentage for dynamic resolution.");
            public static readonly GUIContent lowResTransparencyMinimumThreshold = EditorGUIUtility.TrTextContent("Low Res Transparency Min Threshold", "The minimum percentage threshold allowed to clamp low resolution transparency. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.");
            public static readonly GUIContent lowResSSGIMinimumThreshold = EditorGUIUtility.TrTextContent("Low Res Screen Space GI Min Threshold", "The minimum percentage threshold allowed to clamp low resolution Screen Space Global Illumination. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.");
            public static readonly GUIContent lowResVolumetricCloudsMinimumThreshold = EditorGUIUtility.TrTextContent("Low Res Volumetric Clouds Min Threshold", "The minimum percentage threshold allowed to clamp tracing resolution for Volumetric Clouds. When the resolution percentage falls below this threshold, HDRP will trace the Volumetric Clouds in half res.");
            public const string lowResTransparencyThresholdDisabledMsg = "Low res transparency is currently disabled in the quality settings. \"Low Res Transparency Min Threshold\" will be ignored.";
            public static readonly GUIContent rayTracingHalfResThreshold = EditorGUIUtility.TrTextContent("Ray Tracing Half Res Threshold", "The minimum percentage threshold allowed to render ray tracing effects at half resolution. When the resolution percentage falls below this threshold, HDRP will render ray tracing effects at full resolution.");

            public static readonly GUIContent lowResTransparentEnabled = EditorGUIUtility.TrTextContent("Enable", "When enabled, materials tagged as Low Res Transparent, will be rendered in a quarter res offscreen buffer and then composited to full res.");
            public static readonly GUIContent checkerboardDepthBuffer = EditorGUIUtility.TrTextContent("Checkerboarded Depth Buffer Downsample", "When enabled, the depth buffer used for low res transparency is generated in a min/max checkerboard pattern from original full res buffer.");
            public static readonly GUIContent lowResTranspUpsample = EditorGUIUtility.TrTextContent("Upsample type", "The type of upsampling filter used to composite the low resolution transparency.");

            public static readonly GUIContent XRSinglePass = EditorGUIUtility.TrTextContent("Single Pass", "When enabled, XR views are rendered simultaneously and the render loop is processed only once. This setting will improve CPU and GPU performance but will use more GPU memory.");
            public static readonly GUIContent XROcclusionMesh = EditorGUIUtility.TrTextContent("Occlusion Mesh", "When enabled, the occlusion mesh will be rendered for each view during the depth prepass to reduce shaded fragments.");
            public static readonly GUIContent XRCameraJitter = EditorGUIUtility.TrTextContent("Camera Jitter", "When enabled, jitter will be added to the camera to provide more samples for temporal effects. This is usually not required in VR due to micro variations from the tracking.");
            public static readonly GUIContent XRMotionBlur = EditorGUIUtility.TrTextContent("Allow Motion Blur", "When enabled, motion blur can be used in XR. When this option is disabled, regardless of the settings, motion blur will be turned off when in XR.");

            public static readonly GUIContent lutSize = EditorGUIUtility.TrTextContent("Grading LUT Size", "Sets size of the internal and external color grading lookup textures (LUTs).");
            public static readonly GUIContent lutFormat = EditorGUIUtility.TrTextContent("Grading LUT Format", "Specifies the encoding format for color grading lookup textures. Lower precision formats are faster and use less memory at the expense of color precision.");
            public static readonly GUIContent bufferFormat = EditorGUIUtility.TrTextContent("Buffer Format", "Specifies the encoding format of the color buffers that are used during post processing. Lower precision formats are faster and use less memory at the expense of color precision.");

            public static readonly GUIContent volumeProfileLabel = EditorGUIUtility.TrTextContent("Volume Profile", "Settings that will override the values defined in the Default Volume Profile set in the Render Pipeline Global settings. Local Volumes inside scenes may override these settings further.");
            public static System.Lazy<GUIStyle> volumeProfileContextMenuStyle = new(() => new GUIStyle(CoreEditorStyles.contextMenuStyle) { margin = new RectOffset(0, 1, 3, 0) });

            public static readonly GUIContent[] shadowBitDepthNames = { new GUIContent("32 bit"), new GUIContent("16 bit") };
            public static readonly int[] shadowBitDepthValues = { (int)DepthBits.Depth32, (int)DepthBits.Depth16 };

            public static readonly GUIContent gpuResidentDrawerMode = EditorGUIUtility.TrTextContent("GPU Resident Drawer", "Enables draw submission through the GPU Resident Drawer, which can improve CPU performance");
            public static readonly GUIContent smallMeshScreenPercentage = EditorGUIUtility.TrTextContent("Small-Mesh Screen-Percentage", "Default minimum screen percentage (0-20%) gpu-driven Renderers can cover before getting culled. If a Renderer is part of a LODGroup, this will be ignored.");
            public static readonly GUIContent enableOcclusionCullingInCameras = EditorGUIUtility.TrTextContent("GPU Occlusion Culling", "Enables GPU occlusion culling in Game and SceneView cameras.");
            public static readonly GUIContent useDepthPrepassForOccluders = EditorGUIUtility.TrTextContent("Occluders From Depth Prepass", "Always builds occluders from the depth pre-pass. If this flag is on, or the Full Depth Prepass within Deferred frame setting is enabled, or Lit Shader Mode is Forward Only, occluders are built during the depth pre-pass.  Otherwise occluders are built during the gbuffer pass.");

            public static GUIContent brgShaderStrippingErrorMessage =
                EditorGUIUtility.TrTextContent("\"BatchRendererGroup Variants\" setting must be \"Keep All\". To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".");
            public static GUIContent staticBatchingInfoMessage =
                EditorGUIUtility.TrTextContent("Static Batching is not recommended when using GPU draw submission modes, performance may improve if Static Batching is disabled in Player Settings.");

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
