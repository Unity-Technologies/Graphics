using System;

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
        /// <summary>Display Probe Volumes.</summary>
        ProbeVolume
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

    /// <summary>
    /// Debug Light Layers Filtering.
    /// </summary>
    [GenerateHLSL]
    [Flags]
    public enum DebugLightLayersMask
    {
        /// <summary>No light layer debug.</summary>
        None = 0,
        /// <summary>Debug light layer 1.</summary>
        LightLayer1 = 1 << 0,
        /// <summary>Debug light layer 2.</summary>
        LightLayer2 = 1 << 1,
        /// <summary>Debug light layer 3.</summary>
        LightLayer3 = 1 << 2,
        /// <summary>Debug light layer 4.</summary>
        LightLayer4 = 1 << 3,
        /// <summary>Debug light layer 5.</summary>
        LightLayer5 = 1 << 4,
        /// <summary>Debug light layer 6.</summary>
        LightLayer6 = 1 << 5,
        /// <summary>Debug light layer 7.</summary>
        LightLayer7 = 1 << 6,
        /// <summary>Debug light layer 8.</summary>
        LightLayer8 = 1 << 7,
    }



    static class DebugLightHierarchyExtensions
    {
        public static bool IsEnabledFor(
            this DebugLightFilterMode mode,
            GPULightType gpuLightType,
            SpotLightShape spotLightShape
        )
        {
            switch (gpuLightType)
            {
                case GPULightType.ProjectorBox:
                case GPULightType.ProjectorPyramid:
                case GPULightType.Spot:
                {
                    switch (spotLightShape)
                    {
                        case SpotLightShape.Box: return (mode & DebugLightFilterMode.DirectSpotBox) != 0;
                        case SpotLightShape.Cone: return (mode & DebugLightFilterMode.DirectSpotCone) != 0;
                        case SpotLightShape.Pyramid: return (mode & DebugLightFilterMode.DirectSpotPyramid) != 0;
                        default: throw new ArgumentOutOfRangeException(nameof(spotLightShape));
                    }
                }
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
                || debugLightLayers
                || overrideSmoothness
                || overrideAlbedo
                || overrideNormal
                || overrideAmbientOcclusion
                || overrideSpecularColor
                || overrideEmissiveColor
                || shadowDebugMode == ShadowMapDebugMode.SingleShadow
                || probeVolumeDebugMode != ProbeVolumeDebugMode.None;
        }

        /// <summary>Current Light Filtering.</summary>
        public DebugLightFilterMode debugLightFilterMode = DebugLightFilterMode.None;
        /// <summary>Current Full Screen Lighting debug mode.</summary>
        public DebugLightingMode    debugLightingMode = DebugLightingMode.None;
        /// <summary>True if light layers visualization is enabled.</summary>
        public bool                 debugLightLayers = false;
        /// <summary>Current Light Layers Filtering mode.</summary>
        public DebugLightLayersMask  debugLightLayersFilterMask = (DebugLightLayersMask)(-1); // Select Everything by default
        /// <summary>True if light layers visualization uses light layers of the selected light.</summary>
        public bool                 debugSelectionLightLayers = false;
        /// <summary>True if light layers visualization uses shadow layers of the selected light.</summary>
        public bool                 debugSelectionShadowLayers = false;
        /// <summary>Rendering Layers Debug Colors.</summary>
        public Vector4[]            debugRenderingLayersColors = GetDefaultRenderingLayersColorPalette();
        /// <summary>Current Shadow Maps debug mode.</summary>
        public ShadowMapDebugMode   shadowDebugMode = ShadowMapDebugMode.None;
        /// <summary>Current Probe Volume Debug Mode.</summary>
        [SerializeField] internal ProbeVolumeDebugMode probeVolumeDebugMode = ProbeVolumeDebugMode.None;
		/// <summary>Current Probe Volume Atlas Slicing Mode.</summary>
        [SerializeField] internal ProbeVolumeAtlasSliceMode probeVolumeAtlasSliceMode = ProbeVolumeAtlasSliceMode.IrradianceSH00;
		/// <summary>The minimum display threshold for atlas slices.</summary>
        [SerializeField] internal float probeVolumeMinValue = 0.0f;
		/// <summary>The maximum display threshold for atlas slices.</summary>
        [SerializeField] internal float probeVolumeMaxValue = 1.0f;
		/// <summary>True if Shadow Map debug mode should be displayed for the currently selected light.</summary>
        public bool                 shadowDebugUseSelection = false;
        /// <summary>Index in the list of currently visible lights of the shadow map to display.</summary>
        public uint                 shadowMapIndex = 0;
        /// <summary>Shadow Map debug display visual remapping minimum value.</summary>
        public float                shadowMinValue = 0.0f;
        /// <summary>Shadow Map debug display visual remapping maximum value.</summary>
        public float                shadowMaxValue = 1.0f;
        /// <summary>Use this value to force a rescale of all shadow atlases.</summary>
        public float                shadowResolutionScaleFactor = 1.0f;
        /// <summary>Clear shadow atlases each frame.</summary>
        public bool                 clearShadowAtlas = false;

        /// <summary>Override smoothness of the whole scene for lighting debug.</summary>
        public bool                 overrideSmoothness = false;
        /// <summary>Value used when overriding smoothness.</summary>
        public float                overrideSmoothnessValue = 0.5f;
        /// <summary>Override albedo of the whole scene for lighting debug.</summary>
        public bool                 overrideAlbedo = false;
        /// <summary>Color used when overriding albedo.</summary>
        public Color                overrideAlbedoValue = new Color(0.5f, 0.5f, 0.5f);
        /// <summary>Override normal of the whole scene with object normals for lighting debug.</summary>
        public bool                 overrideNormal = false;
        /// <summary>Override ambient occlusion of the whole scene for lighting debug.</summary>
        public bool                 overrideAmbientOcclusion = false;
        /// <summary>Value used when overriding ambient occlusion.</summary>
        public float                overrideAmbientOcclusionValue = 1.0f;
        /// <summary>Override specular color of the whole scene for lighting debug.</summary>
        public bool                 overrideSpecularColor = false;
        /// <summary>Color used when overriding specular color.</summary>
        public Color                overrideSpecularColorValue = new Color(1.0f, 1.0f, 1.0f);
        /// <summary>Override emissive color of the whole scene for lighting debug.</summary>
        public bool                 overrideEmissiveColor = false;
        /// <summary>Color used when overriding emissive color.</summary>
        public Color                overrideEmissiveColorValue = new Color(1.0f, 1.0f, 1.0f);

        /// <summary>Display sky reflection cubemap as an overlay.</summary>
        public bool                 displaySkyReflection = false;
        /// <summary>Mip map of the displayed sky reflection.</summary>
        public float                skyReflectionMipmap = 0.0f;

        /// <summary>Display lights bounding volumes as a transparent overlay in the scene.</summary>
        public bool                 displayLightVolumes = false;
        /// <summary>Type of light bounding volumes to display.</summary>
        public LightVolumeDebug     lightVolumeDebugByCategory = LightVolumeDebug.Gradient;
        /// <summary>Maximum number of lights against which the light overdraw gradient is displayed.</summary>
        public uint                 maxDebugLightCount = 24;

        /// <summary>Exposure debug mode.</summary>
        public ExposureDebugMode    exposureDebugMode = ExposureDebugMode.None;
        /// <summary>Exposure compensation to apply on current scene exposure.</summary>
        public float                debugExposure = 0.0f;
        /// <summary>Obsolete, please use  the lens attenuation mode in HDRP Default Settings.</summary>
        [Obsolete("Please use the lens attenuation mode in HDRP Default Settings", true)]
        public float                debugLensAttenuation = 0.65f;
        /// <summary>Whether to show tonemap curve in the histogram debug view or not.</summary>
        public bool                 showTonemapCurveAlongHistogramView = true;
        /// <summary>Whether to center the histogram debug view around the middle-grey point or not.</summary>
        public bool                 centerHistogramAroundMiddleGrey = false;
        /// <summary>Whether to show tonemap curve in the histogram debug view or not.</summary>
        public bool                 displayFinalImageHistogramAsRGB = false;
        /// <summary>Whether to show the only the mask in the picture in picture. If unchecked, the mask view is weighted by the scene color.</summary>
        public bool                 displayMaskOnly = false;

        /// <summary>Display the light cookies atlas.</summary>
        public bool                 displayCookieAtlas = false;
        /// <summary>Display the light cookies cubemap array.</summary>
        public bool                 displayCookieCubeArray = false;
        /// <summary>Index of the light cubemap to display.</summary>
        public uint                 cubeArraySliceIndex = 0;
        /// <summary>Mip level of the cookie cubemap display.</summary>
        public uint                 cookieAtlasMipLevel = 0;
        /// <summary>Clear cookie atlas each frame.</summary>
        public bool                 clearCookieAtlas = false;

        /// <summary>Display the planar reflection atlas.</summary>
        public bool                 displayPlanarReflectionProbeAtlas = false;
        /// <summary>Mip level of the planar reflection atlas display.</summary>
        public uint                 planarReflectionProbeMipLevel = 0;
        /// <summary>Clear planar reflection atlas each frame.</summary>
        public bool                 clearPlanarReflectionProbeAtlas = false;

        /// <summary>True if punctual lights should be displayed in the scene.</summary>
        public bool                 showPunctualLight = true;
        /// <summary>True if directional lights should be displayed in the scene.</summary>
        public bool                 showDirectionalLight = true;
        /// <summary>True if area lights should be displayed in the scene.</summary>
        public bool                 showAreaLight = true;
        /// <summary>True if reflection probes lights should be displayed in the scene.</summary>
        public bool                 showReflectionProbe = true;

        /// <summary>Tile and Cluster debug mode.</summary>
        public TileClusterDebug tileClusterDebug = TileClusterDebug.None;
        /// <summary>Category for tile and cluster debug mode.</summary>
        public TileClusterCategoryDebug tileClusterDebugByCategory = TileClusterCategoryDebug.Punctual;
        /// <summary>Cluster Debug mode.</summary>
        public ClusterDebugMode clusterDebugMode = ClusterDebugMode.VisualizeOpaque;
        /// <summary>Distance at which clusters will be visualized.</summary>
        public float clusterDebugDistance = 1.0f;

        // Internal APIs
        internal bool IsDebugDisplayRemovePostprocess()
        {
            return  debugLightingMode == DebugLightingMode.LuxMeter || debugLightingMode == DebugLightingMode.LuminanceMeter ||
                    debugLightingMode == DebugLightingMode.VisualizeCascade || debugLightingMode == DebugLightingMode.VisualizeShadowMasks ||
                    debugLightingMode == DebugLightingMode.IndirectDiffuseOcclusion || debugLightingMode == DebugLightingMode.IndirectSpecularOcclusion ||
                    debugLightingMode == DebugLightingMode.ProbeVolume;
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
                new Vector4(170, 68, 170) / 255
            };

            int i = 0;
            for (; i < lightLayers.Length; i++)
                colors[i] = lightLayers[i];

            for (; i < colors.Length; i++)
                colors[i] = new Vector4(0, 0, 0);

            return colors;
        }
    }
}
