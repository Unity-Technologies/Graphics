#if VOLUMETRIC_FOG

using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // Binds the height-fog parameters as globals before opaque rendering so that both
    // the volumetric compute shaders (voxelization / lighting) and URP's surface shading
    // (RealtimeLights.hlsl GetMainLight) attenuate the directional light by the same fog
    // optical depth.
    internal class VolumetricFogGlobalsPass : ScriptableRenderPass
    {
        static class ShaderIDs
        {
            public static readonly int _VolumetricFogBaseExtinction = Shader.PropertyToID("_VolumetricFogBaseExtinction");
            public static readonly int _VolumetricFogBaseHeight = Shader.PropertyToID("_VolumetricFogBaseHeight");
            public static readonly int _VolumetricFogExponents = Shader.PropertyToID("_VolumetricFogExponents");
        }

        static readonly ProfilingSampler s_ProfilingSampler = new ProfilingSampler("Volumetric Fog Globals");

        class PassData
        {
            public bool fogActive;
            public float extinction;
            public float baseHeight;
            public Vector4 exponents;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var fog = VolumeManager.instance.stack.GetComponent<FogVolumeComponent>();
            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();

            float extinction;
            float baseHeight;
            Vector4 exponents;
            if (fog != null && fog.IsActive())
            {
                // Extinction is the reciprocal of the mean free path: the average distance light
                // travels through the fog before being scattered or absorbed.
                extinction = 1.0f / fog.meanFreePath.value;
                baseHeight = fog.baseHeight.value;
                // Density falls off exponentially with height as exp(-(h - baseHeight) / H), where
                // H is the scale height. We want the density to have dropped to 0.1% of its base
                // value at the top of the layer, so exp(-layerDepth / H) = 0.001, which gives
                // H = layerDepth / ln(1000). The factor 0.144765 is 1 / ln(1000).
                float layerDepth = Mathf.Max(0.01f, fog.maximumHeight.value - fog.baseHeight.value);
                float H = layerDepth * 0.144765f;
                // Pass both 1/H and H so the shader can evaluate the falloff and its integral
                // without recomputing the reciprocal per sample.
                exponents = new Vector4(1.0f / H, H, 0, 0);

                Color color = fog.color.value;
                fogData.fogParamsValid = true;
                fogData.globalExtinction = extinction;
                fogData.globalScattering = new Vector4(color.r * extinction, color.g * extinction, color.b * extinction, 0.0f);
                fogData.fogColor = new Vector4(color.r, color.g, color.b, 0.0f);
                fogData.fogAnisotropy = fog.anisotropy.value;
                fogData.maxFogDistance = fog.maxFogDistance.value;
                fogData.depthExtent = fog.depthExtent.value;
                fogData.screenFraction = fog.screenResolutionPercentage.value * 0.01f;
                fogData.sliceCount = fog.volumeSliceCount.value;
                fogData.sliceDistributionUniformity = fog.sliceDistributionUniformity.value;
                fogData.enableReprojection = (fog.denoisingMode.value & VolumetricFogDenoisingMode.Reprojection) != 0;
                fogData.enableGaussian = (fog.denoisingMode.value & VolumetricFogDenoisingMode.Gaussian) != 0;
                fogData.extinctionCutoff = fog.volumetricLightingDensityCutoff.value;
                fogData.enableLightCookies = fog.enableLightCookies.value;
                fogData.lightFilter = fog.lightFilter.value;
                fogData.multipleScatteringIntensity = fog.multipleScatteringIntensity.value;
            }
            else
            {
                // Identity values: TransmittanceHeightFog returns exp(0) = 1 (no attenuation).
                extinction = 0.0f;
                baseHeight = 0.0f;
                exponents = new Vector4(1.0f, 1.0f, 0, 0);
                fogData.fogParamsValid = false;
            }

            using (var builder = renderGraph.AddUnsafePass<PassData>(s_ProfilingSampler.name, out var passData, s_ProfilingSampler))
            {
                passData.fogActive = fogData.fogParamsValid;
                passData.extinction = extinction;
                passData.baseHeight = baseHeight;
                passData.exponents = exponents;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext rgContext) =>
                {
                    var cmd = rgContext.cmd;
                    cmd.SetKeyword(ShaderGlobalKeywords.VolumetricFog, data.fogActive);
                    cmd.SetGlobalFloat(ShaderIDs._VolumetricFogBaseExtinction, data.extinction);
                    cmd.SetGlobalFloat(ShaderIDs._VolumetricFogBaseHeight, data.baseHeight);
                    cmd.SetGlobalVector(ShaderIDs._VolumetricFogExponents, data.exponents);
                });
            }
        }
    }
}

#endif
