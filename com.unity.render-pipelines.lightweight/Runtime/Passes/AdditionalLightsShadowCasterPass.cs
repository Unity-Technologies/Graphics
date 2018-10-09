using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class AdditionalLightsShadowCasterPass : ScriptableRenderPass
    {
        private static class AdditionalShadowsConstantBuffer
        {
            public static int _AdditionalLightsWorldToShadow;
            public static int _AdditionalShadowStrength;
            public static int _AdditionalShadowOffset0;
            public static int _AdditionalShadowOffset1;
            public static int _AdditionalShadowOffset2;
            public static int _AdditionalShadowOffset3;
            public static int _AdditionalShadowmapSize;
        }

        const int k_ShadowmapBufferBits = 16;
        RenderTexture m_AdditionalLightsShadowmapTexture;
        RenderTextureFormat m_AdditionalShadowmapFormat;

        Matrix4x4[] m_AdditionalLightShadowMatrices;
        ShadowSliceData[] m_AdditionalLightSlices;
        float[] m_AdditionalLightsShadowStrength;

        const string k_RenderAdditionalLightShadows = "Render Additional Shadows";


        private RenderTargetHandle destination { get; set; }

        public AdditionalLightsShadowCasterPass()
        {
            RegisterShaderPassName("ShadowCaster");

            m_AdditionalLightShadowMatrices = new Matrix4x4[0];
            m_AdditionalLightSlices = new ShadowSliceData[0];
            m_AdditionalLightsShadowStrength = new float[0];

            AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow = Shader.PropertyToID("_AdditionalLightsWorldToShadow");
            AdditionalShadowsConstantBuffer._AdditionalShadowStrength = Shader.PropertyToID("_AdditionalShadowStrength");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset0 = Shader.PropertyToID("_AdditionalShadowOffset0");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset1 = Shader.PropertyToID("_AdditionalShadowOffset1");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset2 = Shader.PropertyToID("_AdditionalShadowOffset2");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset3 = Shader.PropertyToID("_AdditionalShadowOffset3");
            AdditionalShadowsConstantBuffer._AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalShadowmapSize");

            m_AdditionalShadowmapFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;
        }

        public void Setup(RenderTargetHandle destination, int maxVisibleAdditinalLights)
        {
            this.destination = destination;

            if (m_AdditionalLightShadowMatrices.Length != maxVisibleAdditinalLights)
            {
                m_AdditionalLightShadowMatrices = new Matrix4x4[maxVisibleAdditinalLights];
                m_AdditionalLightSlices = new ShadowSliceData[maxVisibleAdditinalLights];
                m_AdditionalLightsShadowStrength = new float[maxVisibleAdditinalLights];
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            if (renderingData.shadowData.supportsAdditionalLightShadows)
            {
                Clear();
                RenderAdditionalShadowmapAtlas(ref context, ref renderingData.cullResults, ref renderingData.lightData, ref renderingData.shadowData);
            }
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
            m_AdditionalLightsShadowmapTexture = null;

            for (int i = 0; i < m_AdditionalLightShadowMatrices.Length; ++i)
                m_AdditionalLightShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_AdditionalLightSlices.Length; ++i)
                m_AdditionalLightSlices[i].Clear();

            for (int i = 0; i < m_AdditionalLightsShadowStrength.Length; ++i)
                m_AdditionalLightsShadowStrength[i] = 0.0f;
        }

        void RenderAdditionalShadowmapAtlas(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            List<int> additionalLightIndices = lightData.additionalLightIndices;
            List<VisibleLight> visibleLights = lightData.visibleLights;

            int shadowCastingLightsCount = 0;
            int additionalLightsCount = additionalLightIndices.Count;
            for (int i = 0; i < additionalLightsCount; ++i)
            {
                VisibleLight shadowLight = visibleLights[additionalLightIndices[i]];

                if (shadowLight.lightType == LightType.Spot && shadowLight.light.shadows != LightShadows.None)
                    shadowCastingLightsCount++;
            }

            if (shadowCastingLightsCount == 0)
                return;

            Matrix4x4 view, proj;
            Bounds bounds;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderAdditionalLightShadows);
            using (new ProfilingSample(cmd, k_RenderAdditionalLightShadows))
            {
                // TODO: Add support to point light shadows. We make a simplification here that only works
                // for spot lights and with max spot shadows per pass.
                int atlasWidth = shadowData.additionalLightsShadowmapWidth;
                int atlasHeight = shadowData.additionalLightsShadowmapHeight;
                int sliceResolution = ShadowUtils.GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, shadowCastingLightsCount);

                m_AdditionalLightsShadowmapTexture = RenderTexture.GetTemporary(shadowData.additionalLightsShadowmapWidth,
                    shadowData.additionalLightsShadowmapHeight, k_ShadowmapBufferBits, m_AdditionalShadowmapFormat);
                m_AdditionalLightsShadowmapTexture.filterMode = FilterMode.Bilinear;
                m_AdditionalLightsShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

                SetRenderTarget(cmd, m_AdditionalLightsShadowmapTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    ClearFlag.Depth, Color.black, TextureDimension.Tex2D);

                for (int i = 0; i < additionalLightsCount; ++i)
                {
                    int shadowLightIndex = additionalLightIndices[i];
                    VisibleLight shadowLight = visibleLights[shadowLightIndex];
                    Light light = shadowLight.light;

                    // TODO: Add support to point light shadows
                    if (shadowLight.lightType != LightType.Spot || shadowLight.light.shadows == LightShadows.None)
                        continue;

                    if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                        continue;

                    Matrix4x4 shadowTransform;
                    bool success = ShadowUtils.ExtractSpotLightMatrix(ref cullResults, ref shadowData,
                        shadowLightIndex, out shadowTransform, out view, out proj);

                    if (success)
                    {
                        // This way of computing the shadow slice only work for spots and with most 4 shadow casting lights per pass
                        // Change this when point lights are supported.
                        Debug.Assert(shadowCastingLightsCount <= 4 && shadowLight.lightType == LightType.Spot);

                        // TODO: We need to pass bias and scale list to shader to be able to support multiple
                        // shadow casting additional lights.
                        m_AdditionalLightSlices[i].offsetX = (i % 2) * sliceResolution;
                        m_AdditionalLightSlices[i].offsetY = (i / 2) * sliceResolution;
                        m_AdditionalLightSlices[i].resolution = sliceResolution;
                        m_AdditionalLightSlices[i].shadowTransform = shadowTransform;

                        m_AdditionalLightsShadowStrength[i] = light.shadowStrength;

                        if (shadowCastingLightsCount > 1)
                            ShadowUtils.ApplySliceTransform(ref m_AdditionalLightSlices[i], atlasWidth, atlasHeight);

                        var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);
                        Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex,
                            ref shadowData, proj, sliceResolution);
                        ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                        ShadowUtils.RenderShadowSlice(cmd, ref context, ref m_AdditionalLightSlices[i], ref settings, proj, view);
                    }
                }

                SetupAdditionalLightsShadowReceiverConstants(cmd, ref shadowData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupAdditionalLightsShadowReceiverConstants(CommandBuffer cmd, ref ShadowData shadowData)
        {
            for (int i = 0; i < m_AdditionalLightSlices.Length; ++i)
                m_AdditionalLightShadowMatrices[i] = m_AdditionalLightSlices[i].shadowTransform;

            float invShadowAtlasWidth = 1.0f / shadowData.additionalLightsShadowmapWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.additionalLightsShadowmapHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalTexture(destination.id, m_AdditionalLightsShadowmapTexture);
            cmd.SetGlobalMatrixArray(AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow, m_AdditionalLightShadowMatrices);
            cmd.SetGlobalFloatArray(AdditionalShadowsConstantBuffer._AdditionalShadowStrength, m_AdditionalLightsShadowStrength);
            cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset2, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset3, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                shadowData.additionalLightsShadowmapWidth, shadowData.additionalLightsShadowmapHeight));
        }
    }
}
