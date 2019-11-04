using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a shadow map atlas for additional shadow-casting Lights.
    /// </summary>
    public class AdditionalLightsShadowCasterPass : ScriptableRenderPass
    {
        private static class AdditionalShadowsConstantBuffer
        {
            public static int _AdditionalLightsWorldToShadow;
            public static int _AdditionalShadowParams;
            public static int _AdditionalShadowOffset0;
            public static int _AdditionalShadowOffset1;
            public static int _AdditionalShadowOffset2;
            public static int _AdditionalShadowOffset3;
            public static int _AdditionalShadowmapSize;
        }

        public static int m_AdditionalShadowsBufferId;
        public static int m_AdditionalShadowsIndicesId;
        bool m_UseStructuredBuffer;

        const int k_ShadowmapBufferBits = 16;
        private RenderTargetHandle m_AdditionalLightsShadowmap;
        RenderTexture m_AdditionalLightsShadowmapTexture;

        int m_ShadowmapWidth;
        int m_ShadowmapHeight;

        ShadowSliceData[] m_AdditionalLightSlices = null;

        // Shader data for UBO path
        Matrix4x4[] m_AdditionalLightsWorldToShadow = null;
        Vector4[] m_AdditionalLightsShadowParams = null;

        // Shader data for SSBO
        ShaderInput.ShadowData[] m_AdditionalLightsShadowData = null;

        List<int> m_AdditionalShadowCastingLightIndices = new List<int>();
        List<int> m_AdditionalShadowCastingLightIndicesMap = new List<int>();
        bool m_SupportsBoxFilterForShadows;
        const string m_ProfilerTag = "Render Additional Shadows";

        public AdditionalLightsShadowCasterPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;

            AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow = Shader.PropertyToID("_AdditionalLightsWorldToShadow");
            AdditionalShadowsConstantBuffer._AdditionalShadowParams = Shader.PropertyToID("_AdditionalShadowParams");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset0 = Shader.PropertyToID("_AdditionalShadowOffset0");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset1 = Shader.PropertyToID("_AdditionalShadowOffset1");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset2 = Shader.PropertyToID("_AdditionalShadowOffset2");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset3 = Shader.PropertyToID("_AdditionalShadowOffset3");
            AdditionalShadowsConstantBuffer._AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalShadowmapSize");
            m_AdditionalLightsShadowmap.Init("_AdditionalLightsShadowmapTexture");

            m_AdditionalShadowsBufferId = Shader.PropertyToID("_AdditionalShadowsBuffer");
            m_AdditionalShadowsIndicesId = Shader.PropertyToID("_AdditionalShadowsIndices");
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;
            m_SupportsBoxFilterForShadows = Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch;

            if (!m_UseStructuredBuffer)
            {
                // Preallocated a fixed size. CommandBuffer.SetGlobal* does allow this data to grow.
                int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                m_AdditionalLightsWorldToShadow = new Matrix4x4[maxLights];
                m_AdditionalLightsShadowParams = new Vector4[maxLights];
            }
        }

        public bool Setup(ref RenderingData renderingData)
        {
            if (!renderingData.shadowData.supportsAdditionalLightShadows)
                return false;

            Clear();

            m_ShadowmapWidth = renderingData.shadowData.additionalLightsShadowmapWidth;
            m_ShadowmapHeight = renderingData.shadowData.additionalLightsShadowmapHeight;

            var visibleLights = renderingData.lightData.visibleLights;
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;

            if (m_AdditionalLightSlices == null || m_AdditionalLightSlices.Length < additionalLightsCount)
                m_AdditionalLightSlices = new ShadowSliceData[additionalLightsCount];

            if (m_AdditionalLightsShadowData == null || m_AdditionalLightsShadowData.Length < additionalLightsCount)
                m_AdditionalLightsShadowData = new ShaderInput.ShadowData[additionalLightsCount];

            int validShadowCastingLights = 0;
            bool supportsSoftShadows = renderingData.shadowData.supportsSoftShadows;
            for (int i = 0; i < visibleLights.Length && m_AdditionalShadowCastingLightIndices.Count < additionalLightsCount; ++i)
            {
                VisibleLight shadowLight = visibleLights[i];

                // Skip main directional light as it is not packed into the shadow atlas
                if (i == renderingData.lightData.mainLightIndex)
                    continue;

                int shadowCastingLightIndex = m_AdditionalShadowCastingLightIndices.Count;
                bool isValidShadowSlice = false;
                if (IsValidShadowCastingLight(ref renderingData.lightData, i))
                {
                    if (renderingData.cullResults.GetShadowCasterBounds(i, out var bounds))
                    {
                        bool success = ShadowUtils.ExtractSpotLightMatrix(ref renderingData.cullResults,
                            ref renderingData.shadowData,
                            i,
                            out var shadowTransform,
                            out m_AdditionalLightSlices[shadowCastingLightIndex].viewMatrix,
                            out m_AdditionalLightSlices[shadowCastingLightIndex].projectionMatrix);

                        if (success)
                        {
                            m_AdditionalShadowCastingLightIndices.Add(i);
                            var light = shadowLight.light;
                            float shadowStrength = light.shadowStrength;
                            float softShadows = (supportsSoftShadows && light.shadows == LightShadows.Soft) ? 1.0f : 0.0f;
                            Vector4 shadowParams = new Vector4(shadowStrength, softShadows, 0.0f, 0.0f);
                            if (m_UseStructuredBuffer)
                            {
                                m_AdditionalLightsShadowData[shadowCastingLightIndex].worldToShadowMatrix = shadowTransform;
                                m_AdditionalLightsShadowData[shadowCastingLightIndex].shadowParams = shadowParams;
                            }
                            else
                            {
                                m_AdditionalLightsWorldToShadow[shadowCastingLightIndex] = shadowTransform;
                                m_AdditionalLightsShadowParams[shadowCastingLightIndex] = shadowParams;
                            }
                            isValidShadowSlice = true;
                            validShadowCastingLights++;
                        }
                    }
                }

                if (m_UseStructuredBuffer)
                {
                    // When using StructuredBuffers all the valid shadow casting slices data
                    // are stored in a the ShadowData buffer and then we setup a index map to
                    // map from light indices to shadow buffer index. A index map of -1 means
                    // the light is not a valid shadow casting light and there's no data for it
                    // in the shadow buffer.
                    int indexMap = (isValidShadowSlice) ? shadowCastingLightIndex : -1;
                    m_AdditionalShadowCastingLightIndicesMap.Add(indexMap);
                }
                else if (!isValidShadowSlice)
                {
                    // When NOT using structured buffers we have no performant way to sample the
                    // index map as int[]. Unity shader compiler converts int[] to float4[] to force memory alignment.
                    // This makes indexing int[] arrays very slow. So, in order to avoid indexing shadow lights we
                    // setup slice data and reserve shadow map space even for invalid shadow slices.
                    // The data is setup with zero shadow strength. This has the same visual effect of no shadow
                    // attenuation contribution from this light.
                    // This makes sampling shadow faster but introduces waste in shadow map atlas.
                    // The waste increases with the amount of additional lights to shade.
                    // Therefore Universal RP try to keep the limit at sane levels when using uniform buffers.
                    Matrix4x4 identity = Matrix4x4.identity;
                    m_AdditionalShadowCastingLightIndices.Add(i);
                    m_AdditionalLightsWorldToShadow[shadowCastingLightIndex] = identity;
                    m_AdditionalLightsShadowParams[shadowCastingLightIndex] = Vector4.zero;
                    m_AdditionalLightSlices[shadowCastingLightIndex].viewMatrix = identity;
                    m_AdditionalLightSlices[shadowCastingLightIndex].projectionMatrix = identity;
                }
            }

            // Lights that need to be rendered in the shadow map atlas
            if (validShadowCastingLights == 0)
                return false;

            int atlasWidth = renderingData.shadowData.additionalLightsShadowmapWidth;
            int atlasHeight = renderingData.shadowData.additionalLightsShadowmapHeight;
            int sliceResolution = ShadowUtils.GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, validShadowCastingLights);

            // In the UI we only allow for square shadow map atlas. Here we check if we can fit
            // all shadow slices into half resolution of the atlas and adjust height to have tighter packing.
            int maximumSlices = (m_ShadowmapWidth / sliceResolution) * (m_ShadowmapHeight / sliceResolution);
            if (validShadowCastingLights <= (maximumSlices / 2))
                m_ShadowmapHeight /= 2;

            int shadowSlicesPerRow = (atlasWidth / sliceResolution);
            float oneOverAtlasWidth = 1.0f / m_ShadowmapWidth;
            float oneOverAtlasHeight = 1.0f / m_ShadowmapHeight;

            int sliceIndex = 0;
            int shadowCastingLightsBufferCount = m_AdditionalShadowCastingLightIndices.Count;
            Matrix4x4 sliceTransform = Matrix4x4.identity;
            sliceTransform.m00 = sliceResolution * oneOverAtlasWidth;
            sliceTransform.m11 = sliceResolution * oneOverAtlasHeight;

            for (int i = 0; i < shadowCastingLightsBufferCount; ++i)
            {
                // we can skip the slice if strength is zero. Some slices with zero
                // strength exists when using uniform array path.
                if (!m_UseStructuredBuffer && Mathf.Approximately(m_AdditionalLightsShadowParams[i].x, 0.0f))
                    continue;

                m_AdditionalLightSlices[i].offsetX = (sliceIndex % shadowSlicesPerRow) * sliceResolution;
                m_AdditionalLightSlices[i].offsetY = (sliceIndex / shadowSlicesPerRow) * sliceResolution;
                m_AdditionalLightSlices[i].resolution = sliceResolution;

                sliceTransform.m03 = m_AdditionalLightSlices[i].offsetX * oneOverAtlasWidth;
                sliceTransform.m13 = m_AdditionalLightSlices[i].offsetY * oneOverAtlasHeight;

                // We bake scale and bias to each shadow map in the atlas in the matrix.
                // saves some instructions in shader.
                if (m_UseStructuredBuffer)
                    m_AdditionalLightsShadowData[i].worldToShadowMatrix = sliceTransform * m_AdditionalLightsShadowData[i].worldToShadowMatrix;
                else
                    m_AdditionalLightsWorldToShadow[i] = sliceTransform * m_AdditionalLightsWorldToShadow[i];
                sliceIndex++;
            }

            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_AdditionalLightsShadowmapTexture = ShadowUtils.GetTemporaryShadowTexture(m_ShadowmapWidth, m_ShadowmapHeight, k_ShadowmapBufferBits);
            ConfigureTarget(new RenderTargetIdentifier(m_AdditionalLightsShadowmapTexture));
            ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.shadowData.supportsAdditionalLightShadows)
                RenderAdditionalShadowmapAtlas(ref context, ref renderingData.cullResults, ref renderingData.lightData, ref renderingData.shadowData);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (m_AdditionalLightsShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_AdditionalLightsShadowmapTexture);
                m_AdditionalLightsShadowmapTexture = null;
            }
        }

        void Clear()
        {
            m_AdditionalShadowCastingLightIndices.Clear();
            m_AdditionalShadowCastingLightIndicesMap.Clear();
            m_AdditionalLightsShadowmapTexture = null;
        }

        void RenderAdditionalShadowmapAtlas(ref ScriptableRenderContext context, ref CullingResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            NativeArray<VisibleLight> visibleLights = lightData.visibleLights;

            bool additionalLightHasSoftShadows = false;
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingSample(cmd, m_ProfilerTag))
            {
                bool anyShadowSliceRenderer = false;
                int shadowSlicesCount = m_AdditionalShadowCastingLightIndices.Count;
                for (int i = 0; i < shadowSlicesCount; ++i)
                {
                    // we do the shadow strength check here again here because when using
                    // the uniform array path we might have zero strength shadow lights.
                    // In that case we need the shadow data buffer but we can skip
                    // rendering them to shadowmap.
                    if (!m_UseStructuredBuffer && Mathf.Approximately(m_AdditionalLightsShadowParams[i].x, 0.0f))
                        continue;

                    // Index of the VisibleLight
                    int shadowLightIndex = m_AdditionalShadowCastingLightIndices[i];
                    VisibleLight shadowLight = visibleLights[shadowLightIndex];

                    ShadowSliceData shadowSliceData = m_AdditionalLightSlices[i];

                    var settings = new ShadowDrawingSettings(cullResults, shadowLightIndex);
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex,
                        ref shadowData, shadowSliceData.projectionMatrix, shadowSliceData.resolution);
                    ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                    ShadowUtils.RenderShadowSlice(cmd, ref context, ref shadowSliceData, ref settings);
                    additionalLightHasSoftShadows |= shadowLight.light.shadows == LightShadows.Soft;
                    anyShadowSliceRenderer = true;
                }

                // We share soft shadow settings for main light and additional lights to save keywords.
                // So we check here if pipeline supports soft shadows and either main light or any additional light has soft shadows
                // to enable the keyword.
                // TODO: In PC and Consoles we can upload shadow data per light and branch on shader. That will be more likely way faster.
                bool mainLightHasSoftShadows = shadowData.supportsMainLightShadows &&
                                               lightData.mainLightIndex != -1 &&
                                               visibleLights[lightData.mainLightIndex].light.shadows ==
                                               LightShadows.Soft;

                bool softShadows = shadowData.supportsSoftShadows &&
                                   (mainLightHasSoftShadows || additionalLightHasSoftShadows);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, anyShadowSliceRenderer);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, softShadows);

                if (anyShadowSliceRenderer)
                    SetupAdditionalLightsShadowReceiverConstants(cmd, ref shadowData, softShadows);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupAdditionalLightsShadowReceiverConstants(CommandBuffer cmd, ref ShadowData shadowData, bool softShadows)
        {
            int shadowLightsCount = m_AdditionalShadowCastingLightIndices.Count;
            
            float invShadowAtlasWidth = 1.0f / shadowData.additionalLightsShadowmapWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.additionalLightsShadowmapHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalTexture(m_AdditionalLightsShadowmap.id, m_AdditionalLightsShadowmapTexture);

            if (m_UseStructuredBuffer)
            {
                NativeArray<ShaderInput.ShadowData> shadowBufferData = new NativeArray<ShaderInput.ShadowData>(shadowLightsCount, Allocator.Temp);
                for (int i = 0; i < shadowLightsCount; ++i)
                {
                    ShaderInput.ShadowData data;
                    data.worldToShadowMatrix = m_AdditionalLightsShadowData[i].worldToShadowMatrix;
                    data.shadowParams = m_AdditionalLightsShadowData[i].shadowParams;
                    shadowBufferData[i] = data;
                }

                var shadowBuffer = ShaderData.instance.GetShadowDataBuffer(shadowLightsCount);
                shadowBuffer.SetData(shadowBufferData);

                var shadowIndicesMapBuffer = ShaderData.instance.GetShadowIndicesBuffer(m_AdditionalShadowCastingLightIndicesMap.Count);
                shadowIndicesMapBuffer.SetData(m_AdditionalShadowCastingLightIndicesMap, 0, 0,
                    m_AdditionalShadowCastingLightIndicesMap.Count);

                cmd.SetGlobalBuffer(m_AdditionalShadowsBufferId, shadowBuffer);
                cmd.SetGlobalBuffer(m_AdditionalShadowsIndicesId, shadowIndicesMapBuffer);
                shadowBufferData.Dispose();
            }
            else
            {
                cmd.SetGlobalMatrixArray(AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow, m_AdditionalLightsWorldToShadow);
                cmd.SetGlobalVectorArray(AdditionalShadowsConstantBuffer._AdditionalShadowParams, m_AdditionalLightsShadowParams);
            }

            if (softShadows)
            {
                if (m_SupportsBoxFilterForShadows)
                {
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset0,
                        new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset1,
                        new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset2,
                        new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset3,
                        new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                }

                // Currently only used when !SHADER_API_MOBILE but risky to not set them as it's generic
                // enough so custom shaders might use it.
                cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    shadowData.additionalLightsShadowmapWidth, shadowData.additionalLightsShadowmapHeight));
            }
        }

        bool IsValidShadowCastingLight(ref LightData lightData, int i)
        {
            if (i == lightData.mainLightIndex)
                return false;

            VisibleLight shadowLight = lightData.visibleLights[i];

            // Directional and Point light shadows are not supported in the shadow map atlas
            if (shadowLight.lightType == LightType.Point || shadowLight.lightType == LightType.Directional)
                return false;

            Light light = shadowLight.light;
            return light != null && light.shadows != LightShadows.None && !Mathf.Approximately(light.shadowStrength, 0.0f);
        }
    }
}
