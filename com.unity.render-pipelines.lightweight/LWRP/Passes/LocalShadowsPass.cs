using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LocalShadowsPass : ScriptableRenderPass
    {
        private static class LocalShadowConstantBuffer
        {
            public static int _LocalWorldToShadowAtlas;
            public static int _LocalShadowStrength;
            public static int _LocalShadowOffset0;
            public static int _LocalShadowOffset1;
            public static int _LocalShadowOffset2;
            public static int _LocalShadowOffset3;
            public static int _LocalShadowmapSize;
        }
        
        const int k_ShadowmapBufferBits = 16;
        RenderTexture m_LocalShadowmapTexture;
        RenderTextureFormat m_LocalShadowmapFormat;

        Matrix4x4[] m_LocalShadowMatrices;
        ShadowSliceData[] m_LocalLightSlices;
        float[] m_LocalShadowStrength;

        const string k_RenderLocalShadows = "Render Local Shadows";
        
        
        private RenderTargetHandle destination { get; set; }

        public LocalShadowsPass(LightweightForwardRenderer renderer) : base(renderer)
        {
            RegisterShaderPassName("ShadowCaster");

            int maxVisibleLocalLights = renderer.maxVisibleLocalLights;
            m_LocalShadowMatrices = new Matrix4x4[maxVisibleLocalLights];
            m_LocalLightSlices = new ShadowSliceData[maxVisibleLocalLights];
            m_LocalShadowStrength = new float[maxVisibleLocalLights];

            LocalShadowConstantBuffer._LocalWorldToShadowAtlas = Shader.PropertyToID("_LocalWorldToShadowAtlas");
            LocalShadowConstantBuffer._LocalShadowStrength = Shader.PropertyToID("_LocalShadowStrength");
            LocalShadowConstantBuffer._LocalShadowOffset0 = Shader.PropertyToID("_LocalShadowOffset0");
            LocalShadowConstantBuffer._LocalShadowOffset1 = Shader.PropertyToID("_LocalShadowOffset1");
            LocalShadowConstantBuffer._LocalShadowOffset2 = Shader.PropertyToID("_LocalShadowOffset2");
            LocalShadowConstantBuffer._LocalShadowOffset3 = Shader.PropertyToID("_LocalShadowOffset3");
            LocalShadowConstantBuffer._LocalShadowmapSize = Shader.PropertyToID("_LocalShadowmapSize");

            m_LocalShadowmapFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;
        }
        
        public void Setup(RenderTargetHandle destination)
        {
            this.destination = destination;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            if (renderingData.shadowData.renderLocalShadows)
            {
                Clear();
                RenderLocalShadowmapAtlas(ref context, ref cullResults, ref renderingData.lightData, ref renderingData.shadowData);
            }
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (m_LocalShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_LocalShadowmapTexture);
                m_LocalShadowmapTexture = null;
            }
        }

        void Clear()
        {
            m_LocalShadowmapTexture = null;

            for (int i = 0; i < m_LocalShadowMatrices.Length; ++i)
                m_LocalShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_LocalLightSlices.Length; ++i)
                m_LocalLightSlices[i].Clear();

            for (int i = 0; i < m_LocalShadowStrength.Length; ++i)
                m_LocalShadowStrength[i] = 0.0f;
        }

        void RenderLocalShadowmapAtlas(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            List<int> localLightIndices = lightData.visibleLocalLightIndices;
            List<VisibleLight> visibleLights = lightData.visibleLights;

            int shadowCastingLightsCount = 0;
            int localLightsCount = localLightIndices.Count;
            for (int i = 0; i < localLightsCount; ++i)
            {
                VisibleLight shadowLight = visibleLights[localLightIndices[i]];

                if (shadowLight.lightType == LightType.Spot && shadowLight.light.shadows != LightShadows.None)
                    shadowCastingLightsCount++;
            }

            if (shadowCastingLightsCount == 0)
                return;

            Matrix4x4 view, proj;
            Bounds bounds;
            int shadowSampling = 0;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderLocalShadows);
            using (new ProfilingSample(cmd, k_RenderLocalShadows))
            {
                // TODO: Add support to point light shadows. We make a simplification here that only works
                // for spot lights and with max spot shadows per pass.
                int atlasWidth = shadowData.localShadowAtlasWidth;
                int atlasHeight = shadowData.localShadowAtlasHeight;
                int sliceResolution = LightweightShadowUtils.GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, shadowCastingLightsCount);

                m_LocalShadowmapTexture = RenderTexture.GetTemporary(shadowData.localShadowAtlasWidth,
                        shadowData.localShadowAtlasHeight, k_ShadowmapBufferBits, m_LocalShadowmapFormat);
                m_LocalShadowmapTexture.filterMode = FilterMode.Bilinear;
                m_LocalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

                SetRenderTarget(cmd, m_LocalShadowmapTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    ClearFlag.Depth, Color.black, TextureDimension.Tex2D);

                for (int i = 0; i < localLightsCount; ++i)
                {
                    int shadowLightIndex = localLightIndices[i];
                    VisibleLight shadowLight = visibleLights[shadowLightIndex];
                    Light light = shadowLight.light;

                    // TODO: Add support to point light shadows
                    if (shadowLight.lightType != LightType.Spot || shadowLight.light.shadows == LightShadows.None)
                        continue;

                    if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                        continue;

                    Matrix4x4 shadowTransform;
                    bool success = LightweightShadowUtils.ExtractSpotLightMatrix(ref cullResults, ref shadowData,
                            shadowLightIndex, out shadowTransform, out view, out proj);

                    if (success)
                    {
                        // This way of computing the shadow slice only work for spots and with most 4 shadow casting lights per pass
                        // Change this when point lights are supported.
                        Debug.Assert(localLightsCount <= 4 && shadowLight.lightType == LightType.Spot);

                        // TODO: We need to pass bias and scale list to shader to be able to support multiple
                        // shadow casting local lights.
                        m_LocalLightSlices[i].offsetX = (i % 2) * sliceResolution;
                        m_LocalLightSlices[i].offsetY = (i / 2) * sliceResolution;
                        m_LocalLightSlices[i].resolution = sliceResolution;
                        m_LocalLightSlices[i].shadowTransform = shadowTransform;

                        m_LocalShadowStrength[i] = light.shadowStrength;
                        shadowSampling = Math.Max(shadowSampling, (int)light.shadows);

                        if (shadowCastingLightsCount > 1)
                            LightweightShadowUtils.ApplySliceTransform(ref m_LocalLightSlices[i], atlasWidth, atlasHeight);

                        var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);
                        LightweightShadowUtils.SetupShadowCasterConstants(cmd, ref shadowLight, proj, sliceResolution);
                        LightweightShadowUtils.RenderShadowSlice(cmd, ref context, ref m_LocalLightSlices[i], proj, view, settings);
                    }
                }

                SetupLocalLightsShadowReceiverConstants(ref context, cmd, ref shadowData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // TODO: We should have RenderingData as a readonly but currently we need this to pass shadow rendering to litpass
            shadowData.renderedLocalShadowQuality = (shadowData.supportsSoftShadows) ? (LightShadows)shadowSampling : LightShadows.Hard;
        }

        void SetupLocalLightsShadowReceiverConstants(ref ScriptableRenderContext context, CommandBuffer cmd, ref ShadowData shadowData)
        {
            for (int i = 0; i < m_LocalLightSlices.Length; ++i)
                m_LocalShadowMatrices[i] = m_LocalLightSlices[i].shadowTransform;

            float invShadowAtlasWidth = 1.0f / shadowData.localShadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.localShadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalTexture(destination.id, m_LocalShadowmapTexture);
            cmd.SetGlobalMatrixArray(LocalShadowConstantBuffer._LocalWorldToShadowAtlas, m_LocalShadowMatrices);
            cmd.SetGlobalFloatArray(LocalShadowConstantBuffer._LocalShadowStrength, m_LocalShadowStrength);
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset2, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset3, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    shadowData.localShadowAtlasWidth, shadowData.localShadowAtlasHeight));
        }
    }
}
