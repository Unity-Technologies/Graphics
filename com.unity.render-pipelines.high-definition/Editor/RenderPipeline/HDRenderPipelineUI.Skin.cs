using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HighDefinition.RenderPipelineSettings;
using UnityEngine.Rendering;

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
            public static readonly GUIContent xrTitle = EditorGUIUtility.TrTextContent("XR");
            public static readonly GUIContent virtualTexturingTitle = EditorGUIUtility.TrTextContent("Virtual Texturing", "Virtual Texturing Settings. These are only available when Virtual Texturing is enabled in the Player Settings.");
            public static readonly GUIContent lightLoopSubTitle = EditorGUIUtility.TrTextContent("Lights");
            public static readonly GUIContent postProcessQualitySubTitle = EditorGUIUtility.TrTextContent("Post-processing Quality Settings");
            public static readonly GUIContent lightingQualitySettings = EditorGUIUtility.TrTextContent("Lighting Quality Settings");

            public static readonly GUIContent volumetricSubTitle = EditorGUIUtility.TrTextContent("Volumetrics");
            public static readonly GUIContent volumetricCloudsSubTitle = EditorGUIUtility.TrTextContent("Volumetric Clouds");
            public static readonly GUIContent probeVolumeSubTitle = EditorGUIUtility.TrTextContent("Probe Volumes (Experimental)");
            public static readonly GUIContent cookiesSubTitle = EditorGUIUtility.TrTextContent("Cookies");
            public static readonly GUIContent reflectionsSubTitle = EditorGUIUtility.TrTextContent("Reflections");
            public static readonly GUIContent skySubTitle = EditorGUIUtility.TrTextContent("Sky");
            public static readonly GUIContent decalsSubTitle = EditorGUIUtility.TrTextContent("Decals");
            public static readonly GUIContent decalsMetalAndAOSubTitle = EditorGUIUtility.TrTextContent("Decals Metal And AO");

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
            public static readonly GUIContent resolutionQuality = EditorGUIUtility.TrTextContent("Resolution");
            public static readonly GUIContent highQualityPrefiltering = EditorGUIUtility.TrTextContent("High Quality Prefiltering");
            public static readonly GUIContent highQualityFiltering = EditorGUIUtility.TrTextContent("High Quality Filtering");
            public static readonly GUIContent dofPhysicallyBased = EditorGUIUtility.TrTextContent("Physically Based");
            public static readonly GUIContent maxSamplesQuality = EditorGUIUtility.TrTextContent("Max Samples");

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
            public static readonly GUIContent RTRClampValue = EditorGUIUtility.TrTextContent("Clamp Value");
            public static readonly GUIContent RTRFullResolution = EditorGUIUtility.TrTextContent("Full Resolution");
            public static readonly GUIContent RTRRayMaxIterations = EditorGUIUtility.TrTextContent("Ray Max Iterations");
            public static readonly GUIContent RTRDenoise = EditorGUIUtility.TrTextContent("Denoise");
            public static readonly GUIContent RTRDenoiserRadius = EditorGUIUtility.TrTextContent("Denoiser Radius");
            public static readonly GUIContent RTRSmoothDenoising = EditorGUIUtility.TrTextContent("Affect Smooth Surfaces");

            // RTGI
            public static readonly GUIContent RTGISettingsSubTitle = EditorGUIUtility.TrTextContent("Ray Traced Global Illumination (Performance)");
            public static readonly GUIContent RTGIRayLength = EditorGUIUtility.TrTextContent("Max Ray Length");
            public static readonly GUIContent RTGIClampValue = EditorGUIUtility.TrTextContent("Clamp Value");
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

            // Fog
            public static readonly GUIContent FogSettingsSubTitle = EditorGUIUtility.TrTextContent("Volumetric Fog");
            public static readonly GUIContent FogSettingsBudget = EditorGUIUtility.TrTextContent("Volumetric Fog Budget");
            public static readonly GUIContent FogSettingsRatio = EditorGUIUtility.TrTextContent("Resolution Depth Ratio");

            public static readonly GUIContent materialQualityLevelContent = EditorGUIUtility.TrTextContent("Default Material Quality Level", "");

            public static readonly GUIContent supportShadowMaskContent = EditorGUIUtility.TrTextContent("Shadowmask", "When enabled, HDRP allocates Shader variants and memory for processing shadow masks. This allows you to use shadow masks in your Unity Project.");
            public static readonly GUIContent supportSSRContent = EditorGUIUtility.TrTextContent("Screen Space Reflection", "When enabled, HDRP allocates memory for processing screen space reflection (SSR). This allows you to use SSR in your Unity Project.");
            public static readonly GUIContent planarResolutionTitle = EditorGUIUtility.TrTextContent("Planar Resolution Tiers");
            public static readonly GUIContent supportSSRTransparentContent = EditorGUIUtility.TrTextContent("Transparent", "When enabled, HDRP executes additional steps to achieve screen space reflection (SSR) on transparent objects. This feature requires both screen space reflections and transparent depth prepass to be enabled.");
            public static readonly GUIContent supportSSAOContent = EditorGUIUtility.TrTextContent("Screen Space Ambient Occlusion", "When enabled, HDRP allocates memory for processing screen space ambient occlusion (SSAO). This allows you to use SSAO in your Unity Project.");
            public static readonly GUIContent supportSSGIContent = EditorGUIUtility.TrTextContent("Screen Space Global Illumination", "When enabled, HDRP allocates memory for processing screen space global illumination (SSGI). This allows you to use SSGI in your Unity Project.");
            public static readonly GUIContent supportedSSSContent = EditorGUIUtility.TrTextContent("Subsurface Scattering", "When enabled, HDRP allocates memory for processing subsurface scattering (SSS). This allows you to use SSS in your Unity Project.");
            public static readonly GUIContent sssSampleBudget = EditorGUIUtility.TrTextContent("Sample Budget", "Maximum number of samples the Subsurface Scattering algorithm is allowed to take.");
            public static readonly GUIContent supportVolumetricFogContent = EditorGUIUtility.TrTextContent("Volumetric Fog", "When enabled, HDRP allocates Shader variants and memory for volumetric effects. This allows you to use volumetric lighting and fog in your Unity Project.");
            public static readonly GUIContent supportVolumetricCloudsContent = EditorGUIUtility.TrTextContent("Volumetric Clouds", "When enabled, HDRP allocates memory for processing volumetric clouds. This allows you to use volumetric clouds in your Unity Project.");
            public static readonly GUIContent volumetricResolutionContent = EditorGUIUtility.TrTextContent("High Quality ", "When enabled, HDRP increases the resolution of volumetric lighting buffers. Warning: There is a high performance cost, do not enable on consoles.");
            public static readonly GUIContent supportLightLayerContent = EditorGUIUtility.TrTextContent("Light Layers", "When enabled, HDRP allocates memory for processing Light Layers. This allows you to use Light Layers in your Unity Project. For deferred rendering, this allocation includes an extra render target in memory and extra cost.");
            public static readonly GUIContent colorBufferFormatContent = EditorGUIUtility.TrTextContent("Color Buffer Format", "Specifies the format used by the scene color render target. R11G11B10 is a faster option and should have sufficient precision.");
            public static readonly GUIContent supportCustomPassContent = EditorGUIUtility.TrTextContent("Custom Pass", "When enabled, HDRP allocates a custom pass buffer. It also enable custom passes inside Custom Pass Volume components.");
            public static readonly GUIContent customBufferFormatContent = EditorGUIUtility.TrTextContent("Custom Buffer Format", "Specifies the format used by the custom pass render target.");
            public static readonly GUIContent supportLitShaderModeContent = EditorGUIUtility.TrTextContent("Lit Shader Mode", "Specifies the rendering modes HDRP supports for Lit Shaders. HDRP removes all allocated memory and Shader variants for modes you do not specify.");
            public static readonly GUIContent MSAASampleCountContent = EditorGUIUtility.TrTextContent("Multisample Anti-aliasing Quality", "Specifies the default quality for MSAA. Set Lit Shader Mode to Forward Only or Both to use this feature.");
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
            public static readonly GUIContent supportRaytracing = EditorGUIUtility.TrTextContent("Realtime Raytracing (Preview)");
            public static readonly GUIContent supportedRayTracingMode = EditorGUIUtility.TrTextContent("Supported Ray Tracing Mode (Preview)");
            public static readonly GUIContent rayTracingUnsupportedWarning = EditorGUIUtility.TrTextContent("Ray tracing is not supported on your device. Please refer to the documentation.");
            public static readonly GUIContent rayTracingRestrictionOnlyWarning = EditorGUIUtility.TrTextContent("Ray tracing is currently only supported on DX12 and Playstation 5.");
            public static readonly GUIContent rayTracingMSAAUnsupported = EditorGUIUtility.TrTextContent("When Ray tracing is enabled in asset, MSAA is not supported. Please refer to the documentation.");
            public static readonly GUIContent maximumLODLevel = EditorGUIUtility.TrTextContent("Maximum LOD Level");
            public static readonly GUIContent LODBias = EditorGUIUtility.TrTextContent("LOD Bias");
            public static readonly GUIContent supportRuntimeDebugDisplayContentLabel = EditorGUIUtility.TrTextContent("Runtime Debug Shaders", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.");
            public static readonly GUIContent supportProbeVolumeContent = EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP allocates Shader variants and memory for probe volume based GI. This allows you to use probe volumes in your Unity Project.");
            public static readonly GUIContent probeVolumeMemoryBudget = EditorGUIUtility.TrTextContent("Memory Budget", "Determines the width and height of the textures used to store GI data from probes. Note that the textures also have a fixed depth dimension.");
            public static readonly GUIContent probeVolumeSHBands = EditorGUIUtility.TrTextContent("SH Bands", "Determines up to what SH bands the Probe Volume will use. Choosing L2 will lead to better quality, but also higher memory and runtime cost.");
            public static readonly GUIContent maxLocalVolumetricFogSizeStyle = EditorGUIUtility.TrTextContent("Max Local Fog Size", "Specifies the maximum size for the individual 3D Local Volumetric Fog texture that HDRP uses for Local Volumetric Fog. This settings will affect your memory consumption.");
            public static readonly GUIContent maxLocalVolumetricFogOnScreenStyle = EditorGUIUtility.TrTextContent("Max Local Fog On Screen", "Sets the maximum number of Local Volumetric Fog can handle on screen at once. This settings will affect your memory consumption.");

            public const string cacheErrorFormat = "This configuration will lead to more than 2 GB reserved for this cache at runtime! ({0} requested) Only {1} element will be reserved instead.";
            public const string cacheInfoFormat = "Reserving {0} in memory at runtime.";
            public const string multipleDifferenteValueMessage = "Multiple different values";

            public static readonly GUIContent cookieSizeContent = EditorGUIUtility.TrTextContent("Cookie Size", "Specifies the maximum size for the individual 2D cookies that HDRP uses for Directional and Spot Lights.");
            public static readonly GUIContent cookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Texture Array Size", "Sets the maximum Texture Array size for the 2D cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.");
#if UNITY_2020_1_OR_NEWER
#else
            public static readonly GUIContent pointCoockieSizeContent = EditorGUIUtility.TrTextContent("Point Cookie Size", "Specifies the maximum size for the Cube cookies HDRP uses for Point Lights.");
#endif
            public static readonly GUIContent pointCookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Cubemap Array Size", "Sets the maximum Texture Array size for the Cube cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.");
            public static readonly GUIContent maxPlanarReflectionOnScreen = EditorGUIUtility.TrTextContent("Max Planar Reflection On Screen", "Sets the maximum number of the Planar Reflection HDRP can handle on screen at once. For performance reasons this number cannot be higher than 32.");

            public static readonly GUIContent cookieAtlasSizeContent = EditorGUIUtility.TrTextContent("2D Atlas Size", "Specifies the size of the atlas used for 2D cookies (Directional, Spot and Rectangle Lights).");
            public static readonly GUIContent cookieAtlasFormatContent = EditorGUIUtility.TrTextContent("Format", "Specifies the HDR format of the atlas used for 2D cookies. R16G16B16A16 can be use for EXR cookies (it provides more precision than R11G11B10)");
            public static readonly GUIContent cookieAtlasLastValidMipContent = EditorGUIUtility.TrTextContent("2D Atlas Last Valid Mip", "Apply border when the cookie is copied into the atlas. It avoid the cookie border to be clamped when sampling mips but can intoduce some blurriness.");

            public static readonly GUIContent compressProbeCacheContent = EditorGUIUtility.TrTextContent("Compress Reflection Probe Cache", "When enabled, HDRP compresses the Reflection Probe cache to save disk space.");
            public static readonly GUIContent cubemapSizeContent = EditorGUIUtility.TrTextContent("Reflection Cubemap Size", "Specifies the maximum resolution of the individual Reflection Probe cube maps.");
            public static readonly GUIContent probeCacheSizeContent = EditorGUIUtility.TrTextContent("Probe Cache Size", "Sets the maximum size of the Probe Cache.");
            public static readonly GUIContent reflectionProbeFormatContent = EditorGUIUtility.TrTextContent("Reflection and Planar Probes Format", "Color format used for reflection and planar probes. Keep in mind that probes are not pre-exposed when selecting the format.");

            public static readonly GUIContent compressPlanarProbeCacheContent = EditorGUIUtility.TrTextContent("Compress Planar Reflection Probe Cache", "When enabled, HDRP compresses the Planar Reflection Probe cache to save disk space.");
            public static readonly GUIContent planarTextureSizeContent = EditorGUIUtility.TrTextContent("Planar Reflection Texture Size", "Specifies the maximum resolution of Planar Reflection Textures.");
            public static readonly GUIContent planarProbeCacheSizeContent = EditorGUIUtility.TrTextContent("Planar Probe Cache Size", "Sets the maximum size of the Planar Probe Cache.");
            public static readonly GUIContent planarAtlasSizeContent = EditorGUIUtility.TrTextContent("Planar Reflection Atlas Size", "Specifies the resolution of Planar Reflection Atlas.");

            public static readonly GUIContent supportFabricBSDFConvolutionContent = EditorGUIUtility.TrTextContent("Fabric BSDF Convolution", "When enabled, HDRP calculates a separate version of each Reflection Probe for the Fabric Shader, creating more accurate lighting effects. See the documentation for more information and limitations of this feature.");

            public static readonly GUIContent skyReflectionSizeContent = EditorGUIUtility.TrTextContent("Reflection Size", "Specifies the maximum resolution of the cube map HDRP uses to represent the sky.");
            public static readonly GUIContent skyLightingOverrideMaskContent = EditorGUIUtility.TrTextContent("Lighting Override Mask", "Specifies the layer mask HDRP uses to override sky lighting.");
            public const string skyLightingHelpBoxContent = "Be careful, Sky Lighting Override Mask is set to Everything. This is most likely a mistake as it serves no purpose.";

            public static readonly GUIContent maxDirectionalContent = EditorGUIUtility.TrTextContent("Maximum Directional on Screen", "Sets the maximum number of Directional Lights HDRP can handle on screen at once.");
            public static readonly GUIContent maxPonctualContent = EditorGUIUtility.TrTextContent("Maximum Punctual on Screen", "Sets the maximum number of Point and Spot Lights HDRP can handle on screen at once.");
            public static readonly GUIContent maxAreaContent = EditorGUIUtility.TrTextContent("Maximum Area on Screen", "Sets the maximum number of area Lights HDRP can handle on screen at once.");
            public static readonly GUIContent maxEnvContent = EditorGUIUtility.TrTextContent("Maximum Reflection Probes on Screen", "Sets the maximum number of Planar and Reflection Probes HDRP can handle on screen at once.");
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

            public static readonly GUIContent useContactShadows = EditorGUIUtility.TrTextContent("Use Contact Shadows", "Use contact shadows for lights.");
            public static readonly GUIContent supportScreenSpaceShadows = EditorGUIUtility.TrTextContent("Screen Space Shadows", "Enables the support of screen space shadows in HDRP.");
            public static readonly GUIContent maxScreenSpaceShadowSlots = EditorGUIUtility.TrTextContent("Maximum", "Sets the maximum number of screen space shadows slots HDRP can handle on screen at once. Monochrome (Opaque/Transparent) shadows requires one slot, colored shadows requires three and area lights shadows require two.");
            public static readonly GUIContent screenSpaceShadowFormat = EditorGUIUtility.TrTextContent("Buffer Format", "Defines the format of the buffer used for screen space shadows. The buffer format can be R8G8B8A8 or R16G16B16A16.");
            public static readonly GUIContent maxShadowResolution = EditorGUIUtility.TrTextContent("Max Resolution", "Specifies the maximum resolution for any single shadow map.");

            public static readonly GUIContent drawDistanceContent = EditorGUIUtility.TrTextContent("Draw Distance", "Sets the maximum distance from the Camera at which HDRP draws Decals.");
            public static readonly GUIContent atlasWidthContent = EditorGUIUtility.TrTextContent("Atlas Width", "Sets the width of the Decal Atlas.");
            public static readonly GUIContent atlasHeightContent = EditorGUIUtility.TrTextContent("Atlas Height", "Sets the height of the Decal Atlas.");
            public static readonly GUIContent metalAndAOContent = EditorGUIUtility.TrTextContent("Metal and Ambient Occlusion Properties", "When enabled, Decals affect metal and ambient occlusion properties.");
            public static readonly GUIContent filteringQuality = EditorGUIUtility.TrTextContent("Filtering Quality", "Specifies the quality of shadows. See the documentation for details on the algorithm HDRP uses for each preset. (Unsupported in Deferred Only)");

            public static readonly GUIContent enabled = EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP dynamically lowers the resolution of render targets to reduce the workload on the GPU.");
            public static readonly GUIContent enableDLSS = EditorGUIUtility.TrTextContent("Enable DLSS", "Enables NVIDIA Deep Learning Super Sampling (DLSS).");
            public static readonly GUIContent DLSSQualitySettingContent = EditorGUIUtility.TrTextContent("Mode", "Selects a performance quality setting for NVIDIA Deep Learning Super Sampling (DLSS).");
            public static readonly GUIContent DLSSUseOptimalSettingsContent = EditorGUIUtility.TrTextContent("Use Optimal Settings", "Sets the sharpness and scale automatically for NVIDIA Deep Learning Super Sampling, depending on the values of quality settings. When DLSS Optimal Settings is on, the percentage settings for Dynamic Resolution Scaling are ignored.");
            public static readonly GUIContent DLSSSharpnessContent = EditorGUIUtility.TrTextContent("Sharpness", "NVIDIA Deep Learning Super Sampling pixel sharpness of upsampler. Controls how the DLSS upsampler will render edges on the image. More sharpness usually means more contrast and clearer image but can increase flickering and fireflies. This setting is ignored if use optimal settings is used");

            public const string DLSSPackageLabel = "NVIDIA Deep Learning Super Sampling (DLSS) is not active in this project. To activate it, install the NVIDIA package.";

            public const string DLSSFeatureDetectedMsg = "Unity detected NVIDIA Deep Learning Super Sampling and will ignore the Fallback Upscale Filter.";
            public const string DLSSFeatureNotDetectedMsg = "Unity cannot detect NVIDIA Deep Learning Super Sampling (DLSS) and will use the Fallback Upscale Filter instead.";
            public const string DLSSIgnorePercentages = "Unity detected that NVIDIA Deep Learning Super Sampling (DLSS) is using Optimal Settings. When DLSS Optimal Settings is on, the percentage settings for Dynamic Resolution Scaling are ignored.";
            public const string DLSSWinTargetWarning = "HDRP does not support DLSS for the current build target. To enable DLSS, set your build target to Windows x86_64.";
            public const string DLSSSwitchTarget64Button = "Fix";
            public static readonly GUIContent maxPercentage = EditorGUIUtility.TrTextContent("Maximum Screen Percentage", "Sets the maximum screen percentage that dynamic resolution can reach.");
            public static readonly GUIContent minPercentage = EditorGUIUtility.TrTextContent("Minimum Screen Percentage", "Sets the minimum screen percentage that dynamic resolution can reach.");
            public static readonly GUIContent dynResType = EditorGUIUtility.TrTextContent("Dynamic Resolution Type", "Specifies the type of dynamic resolution that HDRP uses.");
            public static readonly GUIContent useMipBias = EditorGUIUtility.TrTextContent("Use Mip Bias", "Offsets the mip bias to recover more detail. This only works if the camera is utilizing TAA.");
            public static readonly GUIContent upsampleFilter = EditorGUIUtility.TrTextContent("Default Upscale Filter", "Specifies the filter that HDRP uses for upscaling unless overwritten by API by the user.");
            public static readonly GUIContent fallbackUpsampleFilter = EditorGUIUtility.TrTextContent("Default Fallback Upscale Filter", "Specifies the filter that HDRP uses for upscaling as a fallback if DLSS is not detected. Can be overwritten via API.");
            public static readonly GUIContent forceScreenPercentage = EditorGUIUtility.TrTextContent("Force Screen Percentage", "When enabled, HDRP uses the Forced Screen Percentage value as the screen percentage.");
            public static readonly GUIContent forcedScreenPercentage = EditorGUIUtility.TrTextContent("Forced Screen Percentage", "Sets a specific screen percentage value. HDRP forces this screen percentage for dynamic resolution.");
            public static readonly GUIContent lowResTransparencyMinimumThreshold = EditorGUIUtility.TrTextContent("Low Res Transparency Min Threshold", "The minimum percentage threshold allowed to clamp low resolution transparency. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.");
            public const string lowResTransparencyThresholdDisabledMsg = "Low res transparency is currently disabled in the quality settings. \"Low Res Transparency Min Threshold\" will be ignored.";
            public static readonly GUIContent rayTracingHalfResThreshold = EditorGUIUtility.TrTextContent("Ray Tracing Half Res Threshold", "The minimum percentage threshold allowed to render ray tracing effects at half resolution. When the resolution percentage falls below this threshold, HDRP will render ray tracing effects at full resolution.");

            public static readonly GUIContent lowResTransparentEnabled = EditorGUIUtility.TrTextContent("Enable", "When enabled, materials tagged as Low Res Transparent, will be rendered in a quarter res offscreen buffer and then composited to full res.");
            public static readonly GUIContent checkerboardDepthBuffer = EditorGUIUtility.TrTextContent("Checkerboarded depth buffer downsample", "When enabled, the depth buffer used for low res transparency is generated in a min/max checkerboard pattern from original full res buffer.");
            public static readonly GUIContent lowResTranspUpsample = EditorGUIUtility.TrTextContent("Upsample type", "The type of upsampling filter used to composite the low resolution transparency.");

            public static readonly GUIContent XRSinglePass = EditorGUIUtility.TrTextContent("Single Pass", "When enabled, XR views are rendered simultaneously and the render loop is processed only once. This setting will improve CPU and GPU performance but will use more GPU memory.");
            public static readonly GUIContent XROcclusionMesh = EditorGUIUtility.TrTextContent("Occlusion Mesh", "When enabled, the occlusion mesh will be rendered for each view during the depth prepass to reduce shaded fragments.");
            public static readonly GUIContent XRCameraJitter = EditorGUIUtility.TrTextContent("Camera Jitter", "When enabled, jitter will be added to the camera to provide more samples for temporal effects. This is usually not required in VR due to micro variations from the tracking.");
            public static readonly GUIContent XRMotionBlur = EditorGUIUtility.TrTextContent("Allow Motion Blur", "When enabled, motion blur can be used in XR. When this option is disabled, regardless of the settings, motion blur will be turned off when in XR.");

            public static readonly GUIContent lutSize = EditorGUIUtility.TrTextContent("Grading LUT Size", "Sets size of the internal and external color grading lookup textures (LUTs).");
            public static readonly GUIContent lutFormat = EditorGUIUtility.TrTextContent("Grading LUT Format", "Specifies the encoding format for color grading lookup textures. Lower precision formats are faster and use less memory at the expense of color precision.");
            public static readonly GUIContent bufferFormat = EditorGUIUtility.TrTextContent("Buffer Format", "Specifies the encoding format of the color buffers that are used during post processing. Lower precision formats are faster and use less memory at the expense of color precision.");

            public static readonly GUIContent[] shadowBitDepthNames = { new GUIContent("32 bit"), new GUIContent("16 bit") };
            public static readonly int[] shadowBitDepthValues = { (int)DepthBits.Depth32, (int)DepthBits.Depth16 };

            // TEMP: HDShadowFilteringQuality.VeryHigh - This filtering mode is not ready so disabling in UI
            // To re-enable remove the two following light and re-enable the third one
            public static readonly GUIContent[] shadowFilteringNames = { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") };
            public static readonly int[] shadowFilteringValue = { 0, 1, 2 };

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
                { supportProbeVolumeContent          , string.Format("{0}, {1}", memoryDrawback, shaderVariantDrawback) }
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
