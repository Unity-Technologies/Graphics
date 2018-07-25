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
        VisualizeCascade,
        VisualizeShadowMasks,
        IndirectDiffuseOcclusion,
        IndirectSpecularOcclusion,
        ScreenSpaceTracingRefraction,
        ScreenSpaceTracingReflection
    }

    [GenerateHLSL]
    public enum DebugScreenSpaceTracing
    {
        None,
        Color,
        RayDirWS,
        HitDepth,
        HitSuccess,
        TracingModel,
        HiZPositionNDC,
        HiZRayDirNDC,
        HiZIterationCount,
        HiZMaxUsedMipLevel,
        HiZIntersectionKind,
        HiZHitWeight,
        HiZSampledColor,
        HiZDiff,
        LinearPositionNDC,
        LinearRayDirNDC,
        LinearIterationCount,
        LinearHitWeight,
        LinearSampledColor
    }

    public enum ShadowMapDebugMode
    {
        None,
        VisualizeAtlas,
        VisualizeShadowMap
    }

    [Flags]
    public enum DebugShowLight
    {
        None         = 0,
        Directional  = 1 << GPULightType.Directional,
        Point        = 1 << GPULightType.Point,
        Spot         = (1 << GPULightType.ProjectorBox) | (1 << GPULightType.ProjectorPyramid) | (1 << GPULightType.Spot),
        Line         = 1 << GPULightType.Line,
        Rectangle    = 1 << GPULightType.Rectangle,
    }

    [Serializable]
    public class LightingDebugSettings
    {
        public bool IsDebugDisplayEnabled()
        {
            return debugLightingMode != DebugLightingMode.None || overrideSmoothness || overrideAlbedo || overrideNormal || overrideSpecularColor;
        }

        public bool IsDebugDisplayRemovePostprocess()
        {
            return debugLightingMode != DebugLightingMode.None;
        }

        public DebugLightingMode    debugLightingMode = DebugLightingMode.None;
        public DebugScreenSpaceTracing debugScreenSpaceTracingMode = DebugScreenSpaceTracing.None;
        public ShadowMapDebugMode   shadowDebugMode = ShadowMapDebugMode.None;
        public bool                 shadowDebugUseSelection = false;
        public uint                 shadowMapIndex = 0;
        public uint                 shadowAtlasIndex = 0;
        public uint                 shadowSliceIndex = 0;
        public float                shadowMinValue = 0.0f;
        public float                shadowMaxValue = 1.0f;

        public bool                 overrideSmoothness = false;
        public float                overrideSmoothnessValue = 0.5f;
        public bool                 overrideAlbedo = false;
        public Color                overrideAlbedoValue = new Color(0.5f, 0.5f, 0.5f);
        public bool                 overrideNormal = false;
        public bool                 overrideSpecularColor = false;
        public Color                overrideSpecularColorValue = new Color(1.0f, 1.0f, 1.0f);


        public bool                 displaySkyReflection = false;
        public float                skyReflectionMipmap = 0.0f;

        public float                environmentProxyDepthScale = 20;

        public float                debugExposure = 0.0f;

        public DebugShowLight            showLight = (DebugShowLight)~0;

        public LightLoop.TileClusterDebug tileClusterDebug = LightLoop.TileClusterDebug.None;
        public LightLoop.TileClusterCategoryDebug tileClusterDebugByCategory = LightLoop.TileClusterCategoryDebug.Punctual;
    }
}
