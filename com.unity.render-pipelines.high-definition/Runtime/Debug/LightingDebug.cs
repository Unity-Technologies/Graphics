using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum DebugLightingMode
    {
        None,
        DiffuseLighting,
        SpecularLighting,
        LuxMeter,
        LuminanceMeter,
        MatcapView,
        VisualizeCascade,
        VisualizeShadowMasks,
        IndirectDiffuseOcclusion,
        IndirectSpecularOcclusion
    }

    [GenerateHLSL]
    [Flags]
    public enum DebugLightFilterMode
    {
        None = 0,
        DirectDirectional = 1 << 0,
        DirectPunctual = 1 << 1,
        DirectRectangle = 1 << 2,
        DirectTube = 1 << 3,
        DirectSpotCone = 1 << 4,
        DirectSpotPyramid = 1 << 5,
        DirectSpotBox = 1 << 6,
        IndirectReflectionProbe = 1 << 7,
        IndirectPlanarProbe = 1 << 8,
    }

    public static class DebugLightHierarchyExtensions
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

    [GenerateHLSL]
    public enum ShadowMapDebugMode
    {
        None,
        VisualizePunctualLightAtlas,
        VisualizeDirectionalLightAtlas,
        VisualizeAreaLightAtlas,
        VisualizeShadowMap,
        SingleShadow,
    }

    [Serializable]
    public class LightingDebugSettings
    {
        public bool IsDebugDisplayEnabled()
        {
            return debugLightingMode != DebugLightingMode.None
                || debugLightFilterMode != DebugLightFilterMode.None
                || overrideSmoothness
                || overrideAlbedo
                || overrideNormal
                || overrideSpecularColor
                || overrideEmissiveColor
                || shadowDebugMode == ShadowMapDebugMode.SingleShadow;
        }

        public bool IsDebugDisplayRemovePostprocess()
        {
            return debugLightingMode != DebugLightingMode.None && debugLightingMode != DebugLightingMode.MatcapView;
        }

        public DebugLightFilterMode debugLightFilterMode = DebugLightFilterMode.None;
        public DebugLightingMode    debugLightingMode = DebugLightingMode.None;
        public ShadowMapDebugMode   shadowDebugMode = ShadowMapDebugMode.None;
        public bool                 shadowDebugUseSelection = false;
        public uint                 shadowMapIndex = 0;
        public uint                 shadowAtlasIndex = 0;
        public uint                 shadowSliceIndex = 0;
        public float                shadowMinValue = 0.0f;
        public float                shadowMaxValue = 1.0f;
        public float                shadowResolutionScaleFactor = 1.0f;
        public bool                 clearShadowAtlas = false;

        public bool                 overrideSmoothness = false;
        public float                overrideSmoothnessValue = 0.5f;
        public bool                 overrideAlbedo = false;
        public Color                overrideAlbedoValue = new Color(0.5f, 0.5f, 0.5f);
        public bool                 overrideNormal = false;
        public bool                 overrideSpecularColor = false;
        public Color                overrideSpecularColorValue = new Color(1.0f, 1.0f, 1.0f);
        public bool                 overrideEmissiveColor = false;
        public Color                overrideEmissiveColorValue = new Color(1.0f, 1.0f, 1.0f);

        public bool                 displaySkyReflection = false;
        public float                skyReflectionMipmap = 0.0f;

        public bool                 displayLightVolumes = false;
        public LightVolumeDebug     lightVolumeDebugByCategory = LightVolumeDebug.Gradient;
        public uint                 maxDebugLightCount = 24;

        public float                environmentProxyDepthScale = 20;

        public float                debugExposure = 0.0f;

        public bool                 showPunctualLight = true;
        public bool                 showDirectionalLight = true;
        public bool                 showAreaLight = true;
        public bool                 showReflectionProbe = true;

        public TileClusterDebug tileClusterDebug = TileClusterDebug.None;
        public TileClusterCategoryDebug tileClusterDebugByCategory = TileClusterCategoryDebug.Punctual;
    }
}
