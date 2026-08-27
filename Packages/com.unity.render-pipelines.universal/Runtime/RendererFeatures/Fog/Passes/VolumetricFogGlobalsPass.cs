#if VOLUMETRIC_FOG

using UnityEngine.Rendering.RenderGraphModule;
using SharedIDs = UnityEngine.Rendering.Universal.VolumetricFogRendererFeature.ShaderIDs;

namespace UnityEngine.Rendering.Universal
{
    // Populates the per-frame VolumetricFogFrameData consumed by the other fog passes.
    internal class VolumetricFogGlobalsPass : ScriptableRenderPass
    {
        static class ShaderIDs
        {
            public static readonly int _MainLightFogDensity = Shader.PropertyToID("_MainLightFogDensity");
            public static readonly int _MainLightFogBaseHeight = Shader.PropertyToID("_MainLightFogBaseHeight");
            public static readonly int _MainLightFogExponents = Shader.PropertyToID("_MainLightFogExponents");
            public static readonly int _VBufferLastSliceDist = Shader.PropertyToID("_VBufferLastSliceDist");
            public static readonly int _VolumetricFilteringEnabled = Shader.PropertyToID("_VolumetricFilteringEnabled");
            public static readonly int _AnalyticFogColor = Shader.PropertyToID("_AnalyticFogColor");
            public static readonly int _MaxFogDistance = Shader.PropertyToID("_MaxFogDistance");
            public static readonly int _AnalyticFogEnabled = Shader.PropertyToID("_AnalyticFogEnabled");
            public static readonly int _AnalyticFogExtinction = Shader.PropertyToID("_AnalyticFogExtinction");
            public static readonly int _AnalyticFogHeightMode = Shader.PropertyToID("_AnalyticFogHeightMode");
            public static readonly int _AnalyticFogBaseHeight = Shader.PropertyToID("_AnalyticFogBaseHeight");
            public static readonly int _AnalyticFogExponents = Shader.PropertyToID("_AnalyticFogExponents");
        }

        static readonly ProfilingSampler s_ProfilingSampler = new ProfilingSampler("Volumetric Fog Globals");

        class PassData
        {
            public bool analyticFogKeyword;
            public bool volumetricFogKeyword;

            public float density;
            public float baseHeight;
            public Vector4 exponents;

            public Vector4 encodingParams;
            public Vector4 decodingParams;
            public Vector4 viewportSize;
            public float rcpSliceCount;
            public float lastSliceDist;
            public bool volumetricFilteringEnabled;

            public Vector4 analyticFogColor;
            public float maxFogDistance;
            public bool analyticFogEnabled;
            public float analyticFogExtinction;
            public bool analyticFogHeightMode;
            public float analyticFogBaseHeight;
            public Vector2 analyticFogExponents;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var fog = VolumeManager.instance.stack.GetComponent<FogVolumeComponent>();
            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();

            // Identity values
            float mainLightFogDensity = 0.0f;
            float mainLightFogBaseHeight = 0.0f;
            Vector4 mainLightFogExponents = new Vector4(1.0f, 1.0f, 0, 0);
            if (fog != null && fog.IsActive())
            {
                // The volumetric fog's height band. Density falls off exponentially with height as
                // exp(-(h - baseHeight) / H), where H is the scale height. We want the density to
                // have dropped to 0.1% of its base value at the top of the layer, so
                // exp(-layerDepth / H) = 0.001, which gives H = layerDepth / ln(1000). 0.144765 = 1 / ln(1000).
                float volumetricLayerDepth = Mathf.Max(0.01f, fog.volumetricFogMaximumHeight.value - fog.volumetricFogBaseHeight.value);
                float volumetricH = volumetricLayerDepth * 0.144765f;
                // Pass both 1/H and H so the shader can evaluate the falloff and its integral
                // without recomputing the reciprocal per sample.
                Vector2 volumetricExponents = new Vector2(1.0f / volumetricH, volumetricH);

                // The analytic fog carries its own height band.
                float analyticLayerDepth = Mathf.Max(0.01f, fog.analyticFogMaximumHeight.value - fog.analyticFogBaseHeight.value);
                float analyticH = analyticLayerDepth * 0.144765f;

                // The main-light attenuation tracks whichever rendered fog half defines a height
                // falloff, preferring the volumetric one when both do. Constant-density fog has
                // unbounded optical depth toward the sun, so it contributes no dimming.
                if (fog.volumetricFogEnabled.value && fog.volumetricFogDensityMode.value != VolumetricFogDensityMode.Constant)
                {
                    mainLightFogDensity = fog.volumetricFogDensity.value;
                    mainLightFogBaseHeight = fog.volumetricFogBaseHeight.value;
                    mainLightFogExponents = new Vector4(volumetricExponents.x, volumetricExponents.y, 0, 0);
                }
                else if (fog.analyticFogEnabled.value && fog.analyticFogDensityMode.value == AnalyticFogDensityMode.Height)
                {
                    mainLightFogDensity = fog.analyticFogDensity.value;
                    mainLightFogBaseHeight = fog.analyticFogBaseHeight.value;
                    mainLightFogExponents = new Vector4(1.0f / analyticH, analyticH, 0, 0);
                }

                Color volumetricFogAlbedo = fog.volumetricFogAlbedo.value;
                Color analyticFogColor = fog.analyticFogColor.value;
                fogData.fogParamsValid = true;
                fogData.volumetricFogDensity = fog.volumetricFogDensity.value;
                fogData.globalScattering = new Vector4(volumetricFogAlbedo.r * fog.volumetricFogDensity.value, volumetricFogAlbedo.g * fog.volumetricFogDensity.value, volumetricFogAlbedo.b * fog.volumetricFogDensity.value, 0.0f);
                fogData.analyticFogColor = new Vector4(analyticFogColor.r, analyticFogColor.g, analyticFogColor.b, 0.0f);
                fogData.fogAnisotropy = fog.anisotropy.value;
                fogData.maxFogDistance = fog.maxFogDistance.value;
                fogData.cutoffDistance = fog.cutoffDistance.value;
                fogData.screenFraction = fog.screenResolutionPercentage.value * 0.01f;
                fogData.sliceCount = fog.volumeSliceCount.value;
                fogData.sliceDistributionUniformity = fog.sliceDistributionUniformity.value;
                fogData.enableReprojection = (fog.denoisingMode.value & VolumetricFogDenoisingMode.Reprojection) != 0;
                fogData.enableGaussian = (fog.denoisingMode.value & VolumetricFogDenoisingMode.Gaussian) != 0;
                fogData.extinctionCutoff = fog.volumetricLightingDensityCutoff.value;
                fogData.enableLightCookies = fog.enableLightCookies.value;
                fogData.lightFilter = fog.lightFilter.value;
                fogData.multipleScatteringIntensity = fog.multipleScatteringIntensity.value;
                fogData.analyticFogEnabled = fog.analyticFogEnabled.value;
                fogData.analyticFogDensity = fog.analyticFogDensity.value;
                fogData.analyticFogDensityMode = fog.analyticFogDensityMode.value;
                fogData.analyticFogBaseHeight = fog.analyticFogBaseHeight.value;
                fogData.analyticFogExponents = new Vector2(1.0f / analyticH, analyticH);
                fogData.volumetricFogEnabled = fog.volumetricFogEnabled.value;
                fogData.volumetricFogDensityMode = fog.volumetricFogDensityMode.value;
                fogData.volumetricFogBaseHeight = fog.volumetricFogBaseHeight.value;
                fogData.volumetricFogExponents = volumetricExponents;
                fogData.volumetricFogDensityTexture = fog.volumetricFogDensityTexture.value;
                fogData.volumetricFogDensityTextureTiling = fog.volumetricFogDensityTextureTiling.value;

                if (fogData.volumetricFogEnabled)
                {
                    var cameraData = frameData.Get<UniversalCameraData>();
                    fogData.vBufferParams = new VBufferParams(cameraData.camera, cameraData.cameraTargetDescriptor, fogData.screenFraction, fogData.sliceCount, fogData.cutoffDistance, fogData.sliceDistributionUniformity);
                    fogData.encodingParams = fogData.vBufferParams.encodingParams;
                    fogData.decodingParams = fogData.vBufferParams.decodingParams;
                    fogData.viewportSize = fogData.vBufferParams.viewportSize;
                }
            }
            else
            {
                fogData.fogParamsValid = false;
            }

            using (var builder = renderGraph.AddUnsafePass<PassData>(s_ProfilingSampler.name, out var passData, s_ProfilingSampler))
            {
                passData.analyticFogKeyword = fogData.fogParamsValid && fogData.analyticFogEnabled && !fogData.volumetricFogEnabled;
                passData.volumetricFogKeyword = fogData.fogParamsValid && fogData.volumetricFogEnabled;

                passData.density = mainLightFogDensity;
                passData.baseHeight = mainLightFogBaseHeight;
                passData.exponents = mainLightFogExponents;

                if (passData.volumetricFogKeyword)
                {
                    passData.encodingParams = fogData.encodingParams;
                    passData.decodingParams = fogData.decodingParams;
                    passData.viewportSize = fogData.viewportSize;
                    passData.rcpSliceCount = 1.0f / fogData.vBufferParams.sliceCount;
                    float lastSliceDepth = 1.0f - 0.5f / fogData.vBufferParams.sliceCount;
                    passData.lastSliceDist =
                        fogData.decodingParams.x * Mathf.Pow(2.0f, lastSliceDepth * fogData.decodingParams.y)
                        + fogData.decodingParams.z;
                    passData.volumetricFilteringEnabled = fogData.enableGaussian;
                }

                passData.analyticFogColor = fogData.analyticFogColor;
                passData.maxFogDistance = fogData.maxFogDistance;
                passData.analyticFogEnabled = fogData.analyticFogEnabled;
                passData.analyticFogExtinction = fogData.analyticFogDensity;
                passData.analyticFogHeightMode = fogData.analyticFogDensityMode == AnalyticFogDensityMode.Height;
                passData.analyticFogBaseHeight = fogData.analyticFogBaseHeight;
                passData.analyticFogExponents = fogData.analyticFogExponents;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext rgContext) =>
                {
                    var cmd = rgContext.cmd;
                    cmd.SetKeyword(ShaderGlobalKeywords.VolumetricFog, data.analyticFogKeyword || data.volumetricFogKeyword);
                    // The mode pair has no off variant, so analytic stays enabled as the
                    // do-nothing default whenever the volumetric mode isn't active.
                    cmd.SetKeyword(ShaderGlobalKeywords.FogAnalytic, !data.volumetricFogKeyword);
                    cmd.SetKeyword(ShaderGlobalKeywords.FogVolumetric, data.volumetricFogKeyword);
                    cmd.SetGlobalFloat(ShaderIDs._MainLightFogDensity, data.density);
                    cmd.SetGlobalFloat(ShaderIDs._MainLightFogBaseHeight, data.baseHeight);
                    cmd.SetGlobalVector(ShaderIDs._MainLightFogExponents, data.exponents);

                    if (data.volumetricFogKeyword)
                    {
                        cmd.SetGlobalVector(SharedIDs._DepthEncodingParams, data.encodingParams);
                        cmd.SetGlobalVector(SharedIDs._DepthDecodingParams, data.decodingParams);
                        cmd.SetGlobalVector(SharedIDs._VBufferSize, data.viewportSize);
                        cmd.SetGlobalFloat(SharedIDs._VBufferRcpSliceCount, data.rcpSliceCount);
                        cmd.SetGlobalFloat(ShaderIDs._VBufferLastSliceDist, data.lastSliceDist);
                        cmd.SetGlobalFloat(ShaderIDs._VolumetricFilteringEnabled, data.volumetricFilteringEnabled ? 1.0f : 0.0f);
                    }

                    cmd.SetGlobalVector(ShaderIDs._AnalyticFogColor, data.analyticFogColor);
                    cmd.SetGlobalFloat(ShaderIDs._MaxFogDistance, data.maxFogDistance);
                    cmd.SetGlobalFloat(ShaderIDs._AnalyticFogEnabled, data.analyticFogEnabled ? 1.0f : 0.0f);
                    cmd.SetGlobalFloat(ShaderIDs._AnalyticFogExtinction, data.analyticFogExtinction);
                    cmd.SetGlobalFloat(ShaderIDs._AnalyticFogHeightMode, data.analyticFogHeightMode ? 1.0f : 0.0f);
                    cmd.SetGlobalFloat(ShaderIDs._AnalyticFogBaseHeight, data.analyticFogBaseHeight);
                    cmd.SetGlobalVector(ShaderIDs._AnalyticFogExponents, data.analyticFogExponents);
                });
            }
        }
    }
}

#endif
