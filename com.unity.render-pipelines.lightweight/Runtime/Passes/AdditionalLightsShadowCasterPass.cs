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
        List<int> m_AdditionalShadowCastingLightIndices = new List<int>();

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

        public bool Setup(RenderTargetHandle destination, ref RenderingData renderingData, int maxVisibleAdditinalLights)
        {
            Clear();
            this.destination = destination;

            if (m_AdditionalLightShadowMatrices.Length != maxVisibleAdditinalLights)
            {
                m_AdditionalLightShadowMatrices = new Matrix4x4[maxVisibleAdditinalLights];
                m_AdditionalLightSlices = new ShadowSliceData[maxVisibleAdditinalLights];
                m_AdditionalLightsShadowStrength = new float[maxVisibleAdditinalLights];
            }
            m_AdditionalShadowCastingLightIndices.Clear();

            Bounds bounds;
            List<VisibleLight> visibleLights = renderingData.lightData.visibleLights;
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            for (int i = 0; i < visibleLights.Count && m_AdditionalShadowCastingLightIndices.Count < additionalLightsCount; ++i)
            {
                if (i == renderingData.lightData.mainLightIndex)
                    continue;
                
                VisibleLight shadowLight = visibleLights[i];
                Light light = shadowLight.light;

                if (shadowLight.lightType == LightType.Spot && light != null && light.shadows != LightShadows.None)
                {
                    if (renderingData.cullResults.GetShadowCasterBounds(i, out bounds))
                        m_AdditionalShadowCastingLightIndices.Add(i);
                }
            }

            int shadowCastingLightsCount = m_AdditionalShadowCastingLightIndices.Count;
            if (shadowCastingLightsCount == 0)
                return false;

            // TODO: Add support to point light shadows. We make a simplification here that only works
            // for spot lights and with max spot shadows per pass.
            int atlasWidth = renderingData.shadowData.additionalLightsShadowmapWidth;
            int atlasHeight = renderingData.shadowData.additionalLightsShadowmapHeight;
            int sliceResolution = ShadowUtils.GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, shadowCastingLightsCount);

            bool anyShadows = false;
            int shadowSlicesPerRow = (atlasWidth / sliceResolution);
            for (int i = 0; i < shadowCastingLightsCount; ++i)
            {
                int shadowLightIndex = m_AdditionalShadowCastingLightIndices[i];
                VisibleLight shadowLight = visibleLights[shadowLightIndex];

                // Currently Only Spot Lights are supported in additional lights
                Debug.Assert(shadowLight.lightType == LightType.Spot);
                Matrix4x4 shadowTransform;
                bool success = ShadowUtils.ExtractSpotLightMatrix(ref renderingData.cullResults, ref renderingData.shadowData,
                    shadowLightIndex, out shadowTransform, out m_AdditionalLightSlices[i].viewMatrix, out m_AdditionalLightSlices[i].projectionMatrix);

                if (success)
                {
                    // TODO: We need to pass bias and scale list to shader to be able to support multiple
                    // shadow casting additional lights.
                    m_AdditionalLightSlices[i].offsetX = (i % shadowSlicesPerRow) * sliceResolution;
                    m_AdditionalLightSlices[i].offsetY = (i / shadowSlicesPerRow) * sliceResolution;
                    m_AdditionalLightSlices[i].resolution = sliceResolution;
                    m_AdditionalLightSlices[i].shadowTransform = shadowTransform;

                    m_AdditionalLightsShadowStrength[i] = shadowLight.light.shadowStrength;
                    anyShadows = true;
                }
                else
                {
                    m_AdditionalShadowCastingLightIndices.RemoveAt(i--);
                }
            }

            return anyShadows;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
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
            List<VisibleLight> visibleLights = lightData.visibleLights;

            bool additionalLightHasSoftShadows = false;
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderAdditionalLightShadows);
            using (new ProfilingSample(cmd, k_RenderAdditionalLightShadows))
            {
                int shadowmapWidth = shadowData.additionalLightsShadowmapWidth;
                int shadowmapHeight = shadowData.additionalLightsShadowmapHeight;

                m_AdditionalLightsShadowmapTexture = RenderTexture.GetTemporary(shadowmapWidth, shadowmapHeight,
                    k_ShadowmapBufferBits, m_AdditionalShadowmapFormat);
                m_AdditionalLightsShadowmapTexture.filterMode = FilterMode.Bilinear;
                m_AdditionalLightsShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

                SetRenderTarget(cmd, m_AdditionalLightsShadowmapTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    ClearFlag.Depth, Color.black, TextureDimension.Tex2D);

                for (int i = 0; i < m_AdditionalShadowCastingLightIndices.Count; ++i)
                {
                    int shadowLightIndex = m_AdditionalShadowCastingLightIndices[i];
                    VisibleLight shadowLight = visibleLights[shadowLightIndex];

                    if (m_AdditionalShadowCastingLightIndices.Count > 1)
                        ShadowUtils.ApplySliceTransform(ref m_AdditionalLightSlices[i], shadowmapWidth, shadowmapHeight);

                    var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex,
                            ref shadowData, m_AdditionalLightSlices[i].projectionMatrix, m_AdditionalLightSlices[i].resolution);
                        ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                    ShadowUtils.RenderShadowSlice(cmd, ref context, ref m_AdditionalLightSlices[i], ref settings, m_AdditionalLightSlices[i].projectionMatrix, m_AdditionalLightSlices[i].viewMatrix);
                    additionalLightHasSoftShadows |= shadowLight.light.shadows == LightShadows.Soft;
                }

                SetupAdditionalLightsShadowReceiverConstants(cmd, ref shadowData);
            }

            // We share soft shadow settings for main light and additional lights to save keywords.
            // So we check here if pipeline supports soft shadows and either main light or any additional light has soft shadows
            // to enable the keyword.
            // TODO: In PC and Consoles we can upload shadow data per light and branch on shader. That will be more likely way faster.
            bool mainLightHasSoftShadows = shadowData.supportsMainLightShadows &&
                                          lightData.mainLightIndex != -1 &&
                                          visibleLights[lightData.mainLightIndex].light.shadows == LightShadows.Soft;

            bool softShadows = shadowData.supportsSoftShadows && (mainLightHasSoftShadows || additionalLightHasSoftShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, true);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, softShadows);
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
