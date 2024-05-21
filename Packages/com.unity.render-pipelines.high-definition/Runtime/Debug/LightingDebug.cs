using System;
using Unity.Burst.CompilerServices;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Full Screen Lighting Debug Mode.
    /// </summary>
    [GenerateHLSL]
    public enum DebugLightingMode
    {
        /// <summary>No lighting debug mode.</summary>
        None,
        // Caution: Shader code assume that all lighting decomposition mode are contiguous
        // i.e start with DiffuseLighting and end with EmissiveLighting. Keep those boundary.
        /// <summary>Display only diffuse lighting.</summary>
        DiffuseLighting,
        /// <summary>Display only specular lighting.</summary>
        SpecularLighting,
        /// <summary>Display only direct diffuse lighting.</summary>
        DirectDiffuseLighting,
        /// <summary>Display only direct specular lighting.</summary>
        DirectSpecularLighting,
        /// <summary>Display only indirect diffuse lighting.</summary>
        IndirectDiffuseLighting,
        /// <summary>Display only reflection.</summary>
        ReflectionLighting,
        /// <summary>Display only refraction.</summary>
        RefractionLighting,
        /// <summary>Display only Emissive lighting.</summary>
        EmissiveLighting,
        /// <summary>Display lux values.</summary>
        LuxMeter,
        /// <summary>Display luminance values.</summary>
        LuminanceMeter,
        /// <summary>Disable scene lightings and replaces it with a pre-computed lighting environment.</summary>
        MatcapView,
        /// <summary>Display Directional Shadow Cascades splits.</summary>
        VisualizeCascade,
        /// <summary>Display Shadow Masks.</summary>
        VisualizeShadowMasks,
        /// <summary>Display indirect diffuse occlusion.</summary>
        IndirectDiffuseOcclusion,
        /// <summary>Display indirect specular occlusion.</summary>
        IndirectSpecularOcclusion,
        /// <summary>Display Probe Volumes Sampled Subdivision.</summary>
        ProbeVolumeSampledSubdivision
    }

    /// <summary>
    /// Debug Light Filtering.
    /// </summary>
    [GenerateHLSL]
    [Flags]
    public enum DebugLightFilterMode
    {
        /// <summary>No light filtering.</summary>
        None = 0,
        /// <summary>Display directional lights.</summary>
        DirectDirectional = 1 << 0,
        /// <summary>Display punctual lights.</summary>
        DirectPunctual = 1 << 1,
        /// <summary>Display rectangle lights.</summary>
        DirectRectangle = 1 << 2,
        /// <summary>Display tube lights.</summary>
        DirectTube = 1 << 3,
        /// <summary>Display Spot lights.</summary>
        DirectSpotCone = 1 << 4,
        /// <summary>Display Pyramid lights.</summary>
        DirectSpotPyramid = 1 << 5,
        /// <summary>Display Box lights.</summary>
        DirectSpotBox = 1 << 6,
        /// <summary>Display Reflection Probes.</summary>
        IndirectReflectionProbe = 1 << 7,
        /// <summary>Display Planar Probes.</summary>
        IndirectPlanarProbe = 1 << 8,
    }

    static class DebugLightHierarchyExtensions
    {

        [IgnoreWarning(1370)] //Ignore throwing exception warning on burst..
        public static bool IsEnabledFor(
             this DebugLightFilterMode mode,
             GPULightType gpuLightType
         )
        {
            switch (gpuLightType)
            {
                case GPULightType.ProjectorBox: return (mode & DebugLightFilterMode.DirectSpotBox) != 0;
                case GPULightType.ProjectorPyramid: return (mode & DebugLightFilterMode.DirectSpotPyramid) != 0;
                case GPULightType.Spot: return (mode & DebugLightFilterMode.DirectSpotCone) != 0;
                case GPULightType.Tube: return (mode & DebugLightFilterMode.DirectTube) != 0;
                case GPULightType.Point: return (mode & DebugLightFilterMode.DirectPunctual) != 0;
                case GPULightType.Rectangle: return (mode & DebugLightFilterMode.DirectRectangle) != 0;
                case GPULightType.Directional: return (mode & DebugLightFilterMode.DirectDirectional) != 0;
                default: throw new ArgumentOutOfRangeException(nameof(gpuLightType));
            }
        }

        public static bool IsEnabledFor(
            this DebugLightFilterMode mode,
            ProbeSettings.ProbeType probeType
        )
        {
            switch (probeType)
            {
                case ProbeSettings.ProbeType.PlanarProbe: return (mode & DebugLightFilterMode.IndirectPlanarProbe) != 0;
                case ProbeSettings.ProbeType.ReflectionProbe: return (mode & DebugLightFilterMode.IndirectReflectionProbe) != 0;
                default: throw new ArgumentOutOfRangeException(nameof(probeType));
            }
        }
    }

    /// <summary>
    /// Shadow Maps Debug Mode.
    /// </summary>
    [GenerateHLSL]
    public enum ShadowMapDebugMode
    {
        /// <summary>No Shadow Maps debug.</summary>
        None,
        /// <summary>Display punctual lights shadow atlas as an overlay.</summary>
        VisualizePunctualLightAtlas,
        /// <summary>Display directional light shadow atlas as an overlay.</summary>
        VisualizeDirectionalLightAtlas,
        /// <summary>Display area lights shadow atlas as an overlay.</summary>
        VisualizeAreaLightAtlas,
        /// <summary>Display punctual lights cached shadow atlas as an overlay.</summary>
        VisualizeCachedPunctualLightAtlas,
        /// <summary>Display area lights cached shadow atlas as an overlay.</summary>
        VisualizeCachedAreaLightAtlas,
        /// <summary>Display a single light shadow map as an overlay.</summary>
        VisualizeShadowMap,
        /// <summary>Replace rendering with a black and white view of the shadow of a single light in the scene.</summary>
        SingleShadow,
    }

    /// <summary>
    /// Exposure debug mode.
    /// </summary>
    [GenerateHLSL]
    public enum ExposureDebugMode
    {
        /// <summary>No exposure debug.</summary>
        None,
        /// <summary>Display the EV100 values of the scene, color-coded.</summary>
        SceneEV100Values,
        /// <summary>Display the Histogram used for exposure.</summary>
        HistogramView,
        /// <summary>Display an RGB histogram of the final image (after post-processing).</summary>
        FinalImageHistogramView,
        /// <summary>Visualize the scene color weighted as the metering mode selected.</summary>
        MeteringWeighted,
    }

    /// <summary>
    /// HDR debug mode.
    /// </summary>
    [GenerateHLSL]
    public enum HDRDebugMode
    {
        /// <summary>No hdr debug.</summary>
        None,
        /// <summary>Gamut view - show the gamuts and what part of the gamut are represented in the image.</summary>
        GamutView,
        /// <summary>Gamut clip - show what part of the scene are covered by the Rec709 gamut and what parts are in the Rec2020 gamut.</summary>
        GamutClip,
        /// <summary>Show in colors between yellow and red any value that is above the paper white value. Luminance otherwise.</summary>
        ValuesAbovePaperWhite,
    }

    /// <summary>
    /// Probe Volume Debug Modes.
    /// </summary>
    [GenerateHLSL]
    internal enum ProbeVolumeDebugMode
    {
        None,
        VisualizeAtlas,
        VisualizeDebugColors,
        VisualizeValidity
    }

    /// <summary>
    /// Probe Volume Atlas Slicing Modes.
    /// </summary>
    [GenerateHLSL]
    internal enum ProbeVolumeAtlasSliceMode
    {
        IrradianceSH00,
        IrradianceSH1_1,
        IrradianceSH10,
        IrradianceSH11,
        IrradianceSH2_2,
        IrradianceSH2_1,
        IrradianceSH20,
        IrradianceSH21,
        IrradianceSH22,
        Validity,
        OctahedralDepth
    }

    /// <summary>
    /// Lighting Debug Settings.
    /// </summary>
    [Serializable]
    public class LightingDebugSettings
    {
        /// <summary>
        /// Returns true if any lighting debug mode is enabled.
        /// </summary>
        /// <returns>True if any lighting debug mode is enabled</returns>
        public bool IsDebugDisplayEnabled()
        {
            return debugLightingMode != DebugLightingMode.None
                || debugLightFilterMode != DebugLightFilterMode.None
                || overrideSmoothness
                || overrideAlbedo
                || overrideNormal
                || overrideAmbientOcclusion
                || overrideSpecularColor
                || overrideEmissiveColor
                || shadowDebugMode == ShadowMapDebugMode.SingleShadow;
        }

        /// <summary>Current Light Filtering.</summary>
        public DebugLightFilterMode debugLightFilterMode = DebugLightFilterMode.None;
        /// <summary>Current Full Screen Lighting debug mode.</summary>
        public DebugLightingMode debugLightingMode = DebugLightingMode.None;
        /// <summary>Current filtered Rendering Layer Mask.</summary>
        public RenderingLayerMask debugLightLayersFilterMask = RenderingLayerMask.Everything;
        /// <summary>True if filter should match Light Layer mask of the selected light.</summary>
        public bool debugSelectionLightLayers = false;
        /// <summary>True if filter should match Custom Shadow Layer mask of the selected light.</summary>
        public bool debugSelectionShadowLayers = false;
        /// <summary>Rendering Layers Debug Colors.</summary>
        public Vector4[] debugRenderingLayersColors = GetDefaultRenderingLayersColorPalette();
        /// <summary>Current Shadow Maps debug mode.</summary>
        public ShadowMapDebugMode shadowDebugMode = ShadowMapDebugMode.None;
        /// <summary>True if Shadow Map debug mode should be displayed for the currently selected light.</summary>
        public bool shadowDebugUseSelection = false;
        /// <summary>Index in the list of currently visible lights of the shadow map to display.</summary>
        public uint shadowMapIndex = 0;
        /// <summary>Shadow Map debug display visual remapping minimum value.</summary>
        public float shadowMinValue = 0.0f;
        /// <summary>Shadow Map debug display visual remapping maximum value.</summary>
        public float shadowMaxValue = 1.0f;
        /// <summary>Use this value to force a rescale of all shadow atlases.</summary>
        public float shadowResolutionScaleFactor = 1.0f;
        /// <summary>Clear shadow atlases each frame.</summary>
        public bool clearShadowAtlas = false;

        /// <summary>Override smoothness of the whole scene for lighting debug.</summary>
        public bool overrideSmoothness = false;
        /// <summary>Value used when overriding smoothness.</summary>
        public float overrideSmoothnessValue = 0.5f;
        /// <summary>Override albedo of the whole scene for lighting debug.</summary>
        public bool overrideAlbedo = false;
        /// <summary>Color used when overriding albedo.</summary>
        public Color overrideAlbedoValue = new Color(0.5f, 0.5f, 0.5f);
        /// <summary>Override normal of the whole scene with object normals for lighting debug.</summary>
        public bool overrideNormal = false;
        /// <summary>Override ambient occlusion of the whole scene for lighting debug.</summary>
        public bool overrideAmbientOcclusion = false;
        /// <summary>Value used when overriding ambient occlusion.</summary>
        public float overrideAmbientOcclusionValue = 1.0f;
        /// <summary>Override specular color of the whole scene for lighting debug.</summary>
        public bool overrideSpecularColor = false;
        /// <summary>Color used when overriding specular color.</summary>
        public Color overrideSpecularColorValue = new Color(1.0f, 1.0f, 1.0f);
        /// <summary>Override emissive color of the whole scene for lighting debug.</summary>
        public bool overrideEmissiveColor = false;
        /// <summary>Color used when overriding emissive color.</summary>
        public Color overrideEmissiveColorValue = new Color(1.0f, 1.0f, 1.0f);

        /// <summary>Display sky reflection cubemap as an overlay.</summary>
        public bool displaySkyReflection = false;
        /// <summary>Mip map of the displayed sky reflection.</summary>
        public float skyReflectionMipmap = 0.0f;

        /// <summary>Display lights bounding volumes as a transparent overlay in the scene.</summary>
        public bool displayLightVolumes = false;
        /// <summary>Type of light bounding volumes to display.</summary>
        public LightVolumeDebug lightVolumeDebugByCategory = LightVolumeDebug.Gradient;
        /// <summary>Maximum number of lights against which the light overdraw gradient is displayed.</summary>
        public uint maxDebugLightCount = 24;

        /// <summary>Exposure debug mode.</summary>
        public ExposureDebugMode exposureDebugMode = ExposureDebugMode.None;
        /// <summary>Exposure compensation to apply on current scene exposure.</summary>
        public float debugExposure = 0.0f;
        /// <summary>Obsolete, please use  the lens attenuation mode in HDRP Global Settings.</summary>
        [Obsolete("Please use the lens attenuation mode in HDRP Global Settings", true)]
        public float debugLensAttenuation = 0.65f;
        /// <summary>Whether to show tonemap curve in the histogram debug view or not.</summary>
        public bool showTonemapCurveAlongHistogramView = true;
        /// <summary>Whether to center the histogram debug view around the middle-grey point or not.</summary>
        public bool centerHistogramAroundMiddleGrey = false;
        /// <summary>Whether to show tonemap curve in the histogram debug view or not.</summary>
        public bool displayFinalImageHistogramAsRGB = false;
        /// <summary>Whether to show the only the mask in the picture in picture. If unchecked, the mask view is weighted by the scene color.</summary>
        public bool displayMaskOnly = false;
        /// <summary>Whether to show the on scene overlay displaying pixels excluded by the exposure computation via histogram.</summary>
        public bool displayOnSceneOverlay = true;

        /// <summary>HDR debug mode.</summary>
        public HDRDebugMode hdrDebugMode = HDRDebugMode.None;


        /// <summary>Display the light cookies atlas.</summary>
        public bool displayCookieAtlas = false;
        /// <summary>Display the light cookies cubemap array.</summary>
        public bool displayCookieCubeArray = false;
        /// <summary>Index of the light cubemap to display.</summary>
        public uint cubeArraySliceIndex = 0;
        /// <summary>Mip level of the cookie cubemap display.</summary>
        public uint cookieAtlasMipLevel = 0;
        /// <summary>Clear cookie atlas each frame.</summary>
        public bool clearCookieAtlas = false;

        /// <summary>Display the reflection probe atlas.</summary>
        public bool displayReflectionProbeAtlas = false;
        /// <summary>Mip level of the reflection probe atlas display.</summary>
        public uint reflectionProbeMipLevel = 0;
        /// <summary>Slice of the reflection probe atlas display.</summary>
        public uint reflectionProbeSlice = 0;
        /// <summary>Apply exposure to displayed atlas.</summary>
        public bool reflectionProbeApplyExposure = false;
        /// <summary>Clear reflection probe atlas each frame.</summary>
        public bool clearReflectionProbeAtlas = false;

        /// <summary>True if punctual lights should be displayed in the scene.</summary>
        public bool showPunctualLight = true;
        /// <summary>True if directional lights should be displayed in the scene.</summary>
        public bool showDirectionalLight = true;
        /// <summary>True if area lights should be displayed in the scene.</summary>
        public bool showAreaLight = true;
        /// <summary>True if reflection probes lights should be displayed in the scene.</summary>
        public bool showReflectionProbe = true;

        /// <summary>Display the Local Volumetric Fog atlas.</summary>
        [Obsolete("The local volumetric fog atlas was removed. This field is unused.")]
        public bool displayLocalVolumetricFogAtlas = false;
        /// <summary>Local Volumetric Fog atlas slice.</summary>
        public uint localVolumetricFogAtlasSlice = 0;
        /// <summary>True if Local Volumetric Fog Atlas debug mode should be displayed for the currently selected Local Volumetric Fog.</summary>
        public bool localVolumetricFogUseSelection = false;

        /// <summary>Tile and Cluster debug mode.</summary>
        public TileClusterDebug tileClusterDebug = TileClusterDebug.None;
        /// <summary>Category for tile and cluster debug mode.</summary>
        public TileClusterCategoryDebug tileClusterDebugByCategory = TileClusterCategoryDebug.Punctual;
        /// <summary>Cluster Debug mode.</summary>
        public ClusterDebugMode clusterDebugMode = ClusterDebugMode.VisualizeOpaque;
        /// <summary>Distance at which clusters will be visualized.</summary>
        public float clusterDebugDistance = 1.0f;

        /// <summary>Light category for cluster debug view.</summary>
        public ClusterLightCategoryDebug clusterLightCategory = ClusterLightCategoryDebug.All;


        /// <summary>Enable to make HDRP mix the albedo of the Material with its material capture.</summary>
        public bool matCapMixAlbedo = false ;

        /// <summary>Set the intensity of the material capture. This increases the brightness of the Scene. This is useful if the albedo darkens the Scene considerably.</summary>
        public float matCapMixScale = 1.0f;

#if UNITY_EDITOR
        public LightingDebugSettings()
        {
            var matCapMode = HDRenderPipelinePreferences.matCapMode;
            matCapMixAlbedo = matCapMode.mixAlbedo.value;
            matCapMixScale = matCapMode.viewScale.value;
        }
#endif

        // Internal APIs
        internal bool IsDebugDisplayRemovePostprocess()
        {
            return debugLightingMode == DebugLightingMode.LuxMeter || debugLightingMode == DebugLightingMode.LuminanceMeter ||
                debugLightingMode == DebugLightingMode.VisualizeShadowMasks ||
                debugLightingMode == DebugLightingMode.IndirectDiffuseOcclusion || debugLightingMode == DebugLightingMode.IndirectSpecularOcclusion ||
                debugLightingMode == DebugLightingMode.ProbeVolumeSampledSubdivision;
        }

        internal static Vector4[] GetDefaultRenderingLayersColorPalette()
        {
            var colors = new Vector4[32];

            var lightLayers = new Vector4[]
            {
                new Vector4(230, 159, 0) / 255,
                new Vector4(86, 180, 233) / 255,
                new Vector4(255, 182, 291) / 255,
                new Vector4(0, 158, 115) / 255,
                new Vector4(240, 228, 66) / 255,
                new Vector4(0, 114, 178) / 255,
                new Vector4(213, 94, 0) / 255,
                new Vector4(170, 68, 170) / 255,
                new Vector4(1.0f, 0.5f, 0.5f),
                new Vector4(0.5f, 1.0f, 0.5f),
                new Vector4(0.5f, 0.5f, 1.0f),
                new Vector4(0.5f, 1.0f, 1.0f),
                new Vector4(0.75f, 0.25f, 1.0f),
                new Vector4(0.25f, 1.0f, 0.75f),
                new Vector4(0.25f, 0.25f, 0.75f),
                new Vector4(0.75f, 0.25f, 0.25f),
            };

            int i = 0;
            for (; i < lightLayers.Length; i++)
                colors[i] = lightLayers[i];

            for (; i < colors.Length; i++)
                colors[i] = new Vector4(0, 0, 0);

            return colors;
        }

        internal int ComputeOverrideHash()
        {
            int hash = (overrideSmoothness ? 1 : 0);
            hash |= (overrideAlbedo ? 1 : 0) << 1;
            hash |= (overrideNormal ? 1 : 0) << 2;
            hash |= (overrideAmbientOcclusion ? 1 : 0) << 3;
            hash |= (overrideSpecularColor ? 1 : 0) << 4;
            hash |= (overrideEmissiveColor ? 1 : 0) << 5;
            unchecked
            {
                hash = hash * 23 + overrideSmoothnessValue.GetHashCode();
                hash = hash * 23 + overrideAlbedoValue.GetHashCode();
                hash = hash * 23 + overrideAmbientOcclusionValue.GetHashCode();
                hash = hash * 23 + overrideSpecularColorValue.GetHashCode();
                hash = hash * 23 + overrideEmissiveColorValue.GetHashCode();
            }
            return hash;
        }
    }
}
