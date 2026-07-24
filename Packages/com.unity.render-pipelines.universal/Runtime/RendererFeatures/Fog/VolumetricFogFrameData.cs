#if VOLUMETRIC_FOG

using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // Per-frame state shared across the volumetric fog passes when the VolumetricFogRendererFeature owns them. The
    // globals pass populates the fog params read by every other pass, the max-Z mask pass writes maxZMaskTexture,
    // voxelization writes vbufferDensity and the shared V-buffer params, lighting reads the max-Z mask and writes
    // vbuffer / encoding-decoding params, apply reads everything.
    internal sealed class VolumetricFogFrameData : ContextItem
    {
        public bool fogParamsValid;
        public float globalExtinction;
        public Vector4 globalScattering;
        public Vector4 fogColor;
        public float fogAnisotropy;
        public float maxFogDistance;
        public float depthExtent;
        public float screenFraction;
        public int sliceCount;
        public float sliceDistributionUniformity;
        public bool enableReprojection;
        public bool enableGaussian;
        public float extinctionCutoff;
        public bool enableLightCookies;
        public VolumetricFogLightFilter lightFilter;
        public float multipleScatteringIntensity;

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
            globalExtinction = 0.0f;
            globalScattering = default;
            fogColor = default;
            fogAnisotropy = 0.0f;
            maxFogDistance = 0.0f;
            depthExtent = 0.0f;
            screenFraction = 0.0f;
            sliceCount = 0;
            sliceDistributionUniformity = 0.0f;
            enableReprojection = false;
            enableGaussian = false;
            extinctionCutoff = 0.0f;
            enableLightCookies = false;
            lightFilter = VolumetricFogLightFilter.AllLights;
            multipleScatteringIntensity = 0.0f;
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
