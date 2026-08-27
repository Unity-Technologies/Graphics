#if VOLUMETRIC_FOG

using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // Per-frame state shared across the volumetric fog passes
    internal sealed class VolumetricFogFrameData : ContextItem
    {
        public bool fogParamsValid;
        public float volumetricFogDensity;
        public Vector4 globalScattering;
        public Vector4 analyticFogColor;
        public float fogAnisotropy;
        public float maxFogDistance;
        public float cutoffDistance;
        public float screenFraction;
        public int sliceCount;
        public float sliceDistributionUniformity;
        public bool enableReprojection;
        public bool enableGaussian;
        public float extinctionCutoff;
        public bool enableLightCookies;
        public VolumetricFogLightFilter lightFilter;
        public float multipleScatteringIntensity;
        public bool analyticFogEnabled;
        public float analyticFogDensity;
        public AnalyticFogDensityMode analyticFogDensityMode;
        public float analyticFogBaseHeight;
        public Vector2 analyticFogExponents;
        public bool volumetricFogEnabled;
        public VolumetricFogDensityMode volumetricFogDensityMode;
        public float volumetricFogBaseHeight;
        public Vector2 volumetricFogExponents;
        public Texture volumetricFogDensityTexture;
        public float volumetricFogDensityTextureTiling;

        public VBufferParams vBufferParams;

        public TextureHandle maxZMaskTexture;
        public TextureHandle vbufferDensity;
        public TextureHandle vbuffer;
        public TextureHandle opticalFogOpacity;

        public Vector4 encodingParams;
        public Vector4 decodingParams;
        public Vector4 viewportSize;

        public override void Reset()
        {
            fogParamsValid = false;
            volumetricFogDensity = 0.0f;
            globalScattering = default;
            analyticFogColor = default;
            fogAnisotropy = 0.0f;
            maxFogDistance = 0.0f;
            cutoffDistance = 0.0f;
            screenFraction = 0.0f;
            sliceCount = 0;
            sliceDistributionUniformity = 0.0f;
            enableReprojection = false;
            enableGaussian = false;
            extinctionCutoff = 0.0f;
            enableLightCookies = false;
            lightFilter = VolumetricFogLightFilter.AllLights;
            multipleScatteringIntensity = 0.0f;
            analyticFogEnabled = false;
            analyticFogDensity = 0.0f;
            analyticFogDensityMode = AnalyticFogDensityMode.Constant;
            analyticFogBaseHeight = 0.0f;
            analyticFogExponents = default;
            volumetricFogEnabled = false;
            volumetricFogDensityMode = VolumetricFogDensityMode.Constant;
            volumetricFogBaseHeight = 0.0f;
            volumetricFogExponents = default;
            volumetricFogDensityTexture = null;
            volumetricFogDensityTextureTiling = 0.0f;
            vBufferParams = default;
            maxZMaskTexture = TextureHandle.nullHandle;
            vbufferDensity = TextureHandle.nullHandle;
            vbuffer = TextureHandle.nullHandle;
            opticalFogOpacity = TextureHandle.nullHandle;
            encodingParams = default;
            decodingParams = default;
            viewportSize = default;
        }
    }
}

#endif
