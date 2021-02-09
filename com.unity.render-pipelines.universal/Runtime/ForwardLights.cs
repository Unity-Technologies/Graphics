using Unity.Collections;
using System;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Computes and submits lighting and reflection probe data to the GPU.
    /// </summary>
    public class ForwardLights
    {
        static class LightConstantBuffer
        {
            public static int _MainLightPosition;   // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightColor;      // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightOcclusionProbesChannel;    // Deferred?

            public static int _AdditionalLightsCount;
            public static int _AdditionalLightsPosition;
            public static int _AdditionalLightsColor;
            public static int _AdditionalLightsAttenuation;
            public static int _AdditionalLightsSpotDir;
            public static int _AdditionalLightOcclusionProbeChannel;

            public static int _ReflectionProbesParams; // #note move out of LightConstantBuffer?
        }

        const int k_MaxReflectionProbesPerObject = 2;
        const int k_ReflectionProbesCubeSize = 128;
        const bool k_useHDR = true;
        const int k_PanoWidth = 4 * k_ReflectionProbesCubeSize;
        const int k_PanoHeight = 2 * k_ReflectionProbesCubeSize;
        Material cubeToPano;
        static int _cubeToPanoMipLvl; // #note needs to fix the miplvl.
        static int _cubeToPanoTexture;

        int m_AdditionalLightsBufferId;
        int m_AdditionalLightsIndicesId;

        int m_ReflectionProbesBufferId;
        int m_ReflectionProbesIndicesId;
        int m_ReflectionProbeTexturesId;

        const string k_SetupLightConstants = "Setup Light Constants";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_SetupLightConstants);
        MixedLightingSetup m_MixedLightingSetup;

        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;
        Vector4[] m_AdditionalLightOcclusionProbeChannels;

        bool m_UseStructuredBuffer;

        public ForwardLights()
        {
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;

            cubeToPano = new Material(Shader.Find("Hidden/CubeToPano"));
            _cubeToPanoMipLvl = Shader.PropertyToID("_cubeMipLvl");
            _cubeToPanoTexture = Shader.PropertyToID("_srcCubeTexture");

            LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._MainLightOcclusionProbesChannel = Shader.PropertyToID("_MainLightOcclusionProbes");
            LightConstantBuffer._AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
            LightConstantBuffer._ReflectionProbesParams = Shader.PropertyToID("_ReflectionProbesParams");

            if (m_UseStructuredBuffer)
            {
                m_AdditionalLightsBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
                m_AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");
                m_ReflectionProbesIndicesId = Shader.PropertyToID("_ReflectionProbeIndices");
                m_ReflectionProbesBufferId = Shader.PropertyToID("_ReflectionProbesBuffer");
                m_ReflectionProbeTexturesId = Shader.PropertyToID("_ReflectionProbeTextures");
            }
            else
            {
                LightConstantBuffer._AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
                LightConstantBuffer._AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
                LightConstantBuffer._AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
                LightConstantBuffer._AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
                LightConstantBuffer._AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");

                int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                m_AdditionalLightPositions = new Vector4[maxLights];
                m_AdditionalLightColors = new Vector4[maxLights];
                m_AdditionalLightAttenuations = new Vector4[maxLights];
                m_AdditionalLightSpotDirections = new Vector4[maxLights];
                m_AdditionalLightOcclusionProbeChannels = new Vector4[maxLights];
            }
        }

        public void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            bool additionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                SetupShaderLightConstants(cmd, ref renderingData);
                // #note can only call this after light constants?
                SetupReflectionProbeConstants(cmd, ref renderingData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex,
                    additionalLightsCount > 0 && additionalLightsPerVertex);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel,
                    additionalLightsCount > 0 && !additionalLightsPerVertex);

                bool isShadowMask = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.ShadowMask;
                bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
                bool isSubtractive = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ShadowsShadowMask, isShadowMask);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, isSubtractive); // Backward compatibility
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
        {
            UniversalRenderPipeline.InitializeLightConstants_Common(lights, lightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionProbeChannel);

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            Light light = lightData.light;

            if (light == null)
                return;

            if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                lightData.light.shadows != LightShadows.None &&
                m_MixedLightingSetup == MixedLightingSetup.None)
            {
                switch (light.bakingOutput.mixedLightingMode)
                {
                    case MixedLightingMode.Subtractive:
                        m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                        break;
                    case MixedLightingMode.Shadowmask:
                        m_MixedLightingSetup = MixedLightingSetup.ShadowMask;
                        break;
                }
            }
        }

        void InitializeProbeConstants(NativeArray<VisibleReflectionProbe> probes, int probeIndex, out Vector4 probePos, out Vector4 probeAABBMin, out Vector4 probeAABBMax, out Vector4 hdrData)
        {
            var probeData = probes[probeIndex];
            probePos = probeData.localToWorldMatrix.MultiplyPoint(Vector3.zero);
            probePos.w = (probeData.isBoxProjection) ? 1f : 0f;
            probeAABBMin = probeData.bounds.min;
            probeAABBMin.w = probeData.blendDistance;
            probeAABBMax = probeData.bounds.max;
            hdrData = probeData.hdrData;
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Universal pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref renderingData.lightData);
            SetupAdditionalLightConstants(cmd, ref renderingData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

            cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightOcclusionProbesChannel, lightOcclusionChannel);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref LightData lightData = ref renderingData.lightData;
            var cullResults = renderingData.cullResults;
            var lights = lightData.visibleLights;
            int maxAdditionalLightsCount = UniversalRenderPipeline.maxVisibleAdditionalLights;
            int additionalLightsCount = SetupPerObjectLightIndices(cullResults, ref lightData);
            if (additionalLightsCount > 0)
            {
                if (m_UseStructuredBuffer)
                {
                    NativeArray<ShaderInput.LightData> additionalLightsData = new NativeArray<ShaderInput.LightData>(additionalLightsCount, Allocator.Temp);
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            ShaderInput.LightData data;
                            InitializeLightConstants(lights, i,
                                out data.position, out data.color, out data.attenuation,
                                out data.spotDirection, out data.occlusionProbeChannels);
                            additionalLightsData[lightIter] = data;
                            lightIter++;
                        }
                    }

                    var lightDataBuffer = ShaderData.instance.GetLightDataBuffer(additionalLightsCount);
                    lightDataBuffer.SetData(additionalLightsData);

                    int lightIndices = cullResults.lightAndReflectionProbeIndexCount;
                    var lightIndicesBuffer = ShaderData.instance.GetLightIndicesBuffer(lightIndices);

                    cmd.SetGlobalBuffer(m_AdditionalLightsBufferId, lightDataBuffer);
                    cmd.SetGlobalBuffer(m_AdditionalLightsIndicesId, lightIndicesBuffer);

                    additionalLightsData.Dispose();
                }
                else
                {
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            InitializeLightConstants(lights, i, out m_AdditionalLightPositions[lightIter],
                                out m_AdditionalLightColors[lightIter],
                                out m_AdditionalLightAttenuations[lightIter],
                                out m_AdditionalLightSpotDirections[lightIter],
                                out m_AdditionalLightOcclusionProbeChannels[lightIter]);
                            lightIter++;
                        }
                    }

                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPosition, m_AdditionalLightPositions);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColor, m_AdditionalLightColors);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightOcclusionProbeChannel, m_AdditionalLightOcclusionProbeChannels);
                }

                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, new Vector4(lightData.maxPerObjectAdditionalLightsCount,
                    0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, Vector4.zero);
            }
        }

        void SetupReflectionProbeConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cullResults = renderingData.cullResults;
            var probes = cullResults.visibleReflectionProbes;
            int maxReflectionProbesCount = UniversalRenderPipeline.maxVisibleReflectionProbes;
            int reflectionProbesCount = SetupPerObjectReflectionProbeIndices(cullResults);

            if (reflectionProbesCount > 0)
            {
                UpdateReflectionProbeCubeMapCache(cmd, cullResults);

                if (m_UseStructuredBuffer)
                {
                    var reflectionProbeData = new NativeArray<ShaderInput.ReflectionProbeData>(reflectionProbesCount, Allocator.Temp);

                    for (int i = 0; i < probes.Length && i < maxReflectionProbesCount; ++i)
                    {
                        VisibleReflectionProbe probe = probes[i];
                        ShaderInput.ReflectionProbeData data;
                        InitializeProbeConstants(probes, i, out data.position, out data.boxMin, out data.boxMax, out data.hdr);
                        reflectionProbeData[i] = data;
                    }

                    var probeDataBuffer = ShaderData.instance.GetReflectionProbeDataBuffer(reflectionProbesCount);
                    probeDataBuffer.SetData(reflectionProbeData);

                    int probeIndices = cullResults.lightAndReflectionProbeIndexCount;
                    var probeIndicesBuffer = ShaderData.instance.GetReflectionProbeIndicesBuffer(probeIndices);

                    cmd.SetGlobalBuffer(m_ReflectionProbesBufferId, probeDataBuffer);
                    cmd.SetGlobalBuffer(m_ReflectionProbesIndicesId, probeIndicesBuffer);

                    reflectionProbeData.Dispose();
                }
                else
                {
                    // #note To do implement UBO fall back path...
                    throw new NotImplementedException();
                }

                // #note might need to pack more data into this??
                // #note probably does not contain the right stuff
                cmd.SetGlobalVector(LightConstantBuffer._ReflectionProbesParams, new Vector4(Math.Min(reflectionProbesCount, k_MaxReflectionProbesPerObject),
                    0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._ReflectionProbesParams, Vector4.zero);
            }
        }

        void UpdateReflectionProbeCubeMapCache(CommandBuffer cmd, CullingResults cullResults)
        {
            // #note handle CubeMapTextureArray cache here?? See HDRP (TextureCacheCubeMap.cs & ReflectionProbeCache.cs)
            // Notes @mortenm:
            //  This is an important one too: TransferToSlice() in those files
            //  to convert on the fly from cube map to panorama the array requires an uncompressed format. If on the other hand you know the platform supports cube map arrays then you could use a compressed format such as BC6
            //  but initially to get it running you could just always set it to RGBm or 4xfp16 or 11_11_10F
            //  also an unrelated subtlety I wanted to mention since it is easy to miss is NewFrame() must be called on each texture cache once per frame
            //  also have you been able to find the blit shader used in TransferToPanoCache()? It's in ../com.unity.render-pipelines.high-definition/Runtime/Core/CoreResources It is CubeToPano.shader

            var probes = cullResults.visibleReflectionProbes;

            //Array.Sort(reflectionProbes, new ReflectionProbeSorter());

            if (probes.Length == 0 || probes[0].texture == null)
                return;

            var size = Math.Min(probes.Length, UniversalRenderPipeline.maxVisibleReflectionProbes);

            // #note we need to use the same reflection texture size to use texture 2d or use the largest and upscale the others
            var textureArray = new Texture2DArray(k_PanoWidth, k_PanoHeight, size,
                k_useHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 0,
            };
            cmd.SetGlobalTexture(m_ReflectionProbeTexturesId, textureArray);

            // #note should we use reflectionProbe.hdr (bool)?
            for (int i = 0; i < size; ++i)
            {
                if (probes[i].texture == null)
                    continue;

                //#note handle mip lvl like in hdrp
                cmd.SetGlobalInt(_cubeToPanoMipLvl, 0);
                cmd.SetGlobalTexture(_cubeToPanoTexture, probes[i].texture);
                RenderTexture rt = new RenderTexture(k_PanoWidth, k_PanoHeight, 0, GraphicsFormat.R16G16B16A16_SFloat)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0,
                };

                cmd.Blit(null, rt, cubeToPano, 0);
                cmd.CopyTexture(rt, 0, 0, textureArray, i, 0);
            }
        }

        int SetupPerObjectLightIndices(CullingResults cullResults, ref LightData lightData)
        {
            if (lightData.additionalLightsCount == 0)
                return lightData.additionalLightsCount;

            var visibleLights = lightData.visibleLights;
            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional lights atm.
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (additionalLightsCount >= UniversalRenderPipeline.maxVisibleAdditionalLights)
                    break;

                VisibleLight light = visibleLights[i];
                if (i == lightData.mainLightIndex)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++globalDirectionalLightsCount;
                }
                else
                {
                    perObjectLightIndexMap[i] -= globalDirectionalLightsCount;
                    ++additionalLightsCount;
                }
            }

            // Disable all remaining lights we cannot fit into the global light buffer.
            for (int i = globalDirectionalLightsCount + additionalLightsCount; i < perObjectLightIndexMap.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullResults.SetLightIndexMap(perObjectLightIndexMap);

            if (m_UseStructuredBuffer && additionalLightsCount > 0)
            {
                int lightAndReflectionProbeIndices = cullResults.lightAndReflectionProbeIndexCount;
                Assertions.Assert.IsTrue(lightAndReflectionProbeIndices > 0, "Pipelines configures additional lights but per-object light and probe indices count is zero.");
                cullResults.FillLightAndReflectionProbeIndices(ShaderData.instance.GetLightIndicesBuffer(lightAndReflectionProbeIndices));
            }

            perObjectLightIndexMap.Dispose();
            return additionalLightsCount;
        }

        int SetupPerObjectReflectionProbeIndices(CullingResults cullResults)
        {
            var visibleProbes = cullResults.visibleReflectionProbes;

            if (visibleProbes.Length == 0) // #note assumption: visibileProbes.Length is equivalent to what lightData.additionalLightsCount does for lights
                return visibleProbes.Length;

            int maxVisibleReflectionProbes = UniversalRenderPipeline.maxVisibleReflectionProbes;
            var perObjectReflectionProbeIndexMap = cullResults.GetReflectionProbeIndexMap(Allocator.Temp);
            var reflectionProbesCount = Mathf.Min(perObjectReflectionProbeIndexMap.Length, maxVisibleReflectionProbes);

            // Disable reflection probes that do not fit into the buffer.
            for (int i = maxVisibleReflectionProbes; i < perObjectReflectionProbeIndexMap.Length; ++i)
                perObjectReflectionProbeIndexMap[i] = -1;

            // #note do we still need to do any form of sorting?

            cullResults.SetReflectionProbeIndexMap(perObjectReflectionProbeIndexMap);

            // #note duplicate code & work with SetupPerObjectLightIndices.
            if (m_UseStructuredBuffer && reflectionProbesCount > 0)
            {
                int lightAndReflectionProbeIndices = cullResults.lightAndReflectionProbeIndexCount;
                Assertions.Assert.IsTrue(lightAndReflectionProbeIndices > 0, "Pipelines configures reflection probes but per-object light and probe indices count is zero.");
                cullResults.FillLightAndReflectionProbeIndices(ShaderData.instance.GetReflectionProbeIndicesBuffer(lightAndReflectionProbeIndices));
            }

            perObjectReflectionProbeIndexMap.Dispose();
            return reflectionProbesCount;
        }
    }
}
