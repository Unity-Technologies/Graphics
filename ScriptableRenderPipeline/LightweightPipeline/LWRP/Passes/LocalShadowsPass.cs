using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LocalShadowsPass : ScriptableRenderPass
    {
        const int k_ShadowmapBufferBits = 16;
        RenderTexture m_LocalShadowmapTexture;
        RenderTextureDescriptor m_LocalShadowmapDescriptor;

        Matrix4x4[] m_LocalShadowMatrices;
        ShadowSliceData[] m_LocalLightSlices;
        float[] m_LocalShadowStrength;

        public LocalShadowsPass(LightweightForwardRenderer renderer, int atlasResolution) : base(renderer)
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

            RenderTextureFormat shadowmapFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;

            m_LocalShadowmapDescriptor = new RenderTextureDescriptor(atlasResolution, atlasResolution, shadowmapFormat, k_ShadowmapBufferBits);

            Clear();
        }

        public override void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int samples)
        {
            //m_LocalShadowmapTexture = RenderTexture.GetTemporary(m_LocalShadowmapDescriptor);
            //m_LocalShadowmapTexture.filterMode = FilterMode.Bilinear;
            //m_LocalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

            m_Disposed = false;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData)
        {
            Clear();
            ShadowData shadowData = lightData.shadowData;
            if (shadowData.supportsLocalShadows)
                lightData.shadowData.renderedLocalShadowQuality = RenderLocalShadowmapAtlas(ref context, ref cullResults, ref lightData, ref shadowData);
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (m_LocalShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_LocalShadowmapTexture);
                m_LocalShadowmapTexture = null;
            }

            m_Disposed = true;
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

        LightShadows RenderLocalShadowmapAtlas(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            LightShadows shadowQuality = LightShadows.None;
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
                return shadowQuality;

            CommandBuffer cmd = CommandBufferPool.Get("Prepare Local Lights Shadowmap");
            Matrix4x4 view, proj;
            Bounds bounds;

            // TODO: Add support to point light shadows. We make a simplification here that only works
            // for spot lights and with max spot shadows per pass.
            int atlasWidth = shadowData.localShadowAtlasWidth;
            int atlasHeight = shadowData.localShadowAtlasHeight;
            int sliceResolution = GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, shadowCastingLightsCount);
            int shadowSampling = 0;

            m_LocalShadowmapTexture = RenderTexture.GetTemporary(m_LocalShadowmapDescriptor);
            m_LocalShadowmapTexture.filterMode = FilterMode.Bilinear;
            m_LocalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;
            m_Disposed = false;

            CoreUtils.SetRenderTarget(cmd, m_LocalShadowmapTexture, ClearFlag.Depth);

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

                var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);

                if (cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowLightIndex, out view, out proj, out settings.splitData))
                {
                    // This way of computing the shadow slice only work for spots and with most 4 shadow casting lights per pass
                    // Change this when point lights are supported.
                    Debug.Assert(localLightsCount <= 4 && shadowLight.lightType == LightType.Spot);

                    // TODO: We need to pass bias and scale list to shader to be able to support multiple
                    // shadow casting local lights.
                    m_LocalLightSlices[i].offsetX = (i % 2) * sliceResolution;
                    m_LocalLightSlices[i].offsetY = (i / 2) * sliceResolution;
                    m_LocalLightSlices[i].resolution = sliceResolution;
                    m_LocalLightSlices[i].shadowTransform = GetShadowTransform(proj, view);

                    if (shadowCastingLightsCount > 1)
                        ApplySliceTransform(ref m_LocalLightSlices[i], atlasWidth, atlasHeight);

                    SetupShadowCasterConstants(cmd, ref shadowLight, proj, sliceResolution);
                    RenderShadowSlice(cmd, ref context, ref m_LocalLightSlices[i], proj, view, settings);
                    m_LocalShadowStrength[i] = light.shadowStrength;
                    shadowSampling = Math.Max(shadowSampling, (int)light.shadows);
                }
            }

            SetupLocalLightsShadowReceiverConstants(ref context, cmd, ref shadowData);
            shadowQuality = (shadowData.supportsSoftShadows) ? (LightShadows)shadowSampling : LightShadows.Hard;
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            return shadowQuality;
        }

        Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
            // apply z reversal to projection matrix. We need to do it manually here.
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;

            // Apply texture scale and offset to save a MAD in shader.
            return textureScaleAndBias * worldToShadow;
        }

        void ApplySliceTransform(ref ShadowSliceData shadowSliceData, int atlasWidth, int atlasHeight)
        {
            Matrix4x4 sliceTransform = Matrix4x4.identity;
            float oneOverAtlasWidth = 1.0f / atlasWidth;
            float oneOverAtlasHeight = 1.0f / atlasHeight;
            sliceTransform.m00 = shadowSliceData.resolution * oneOverAtlasWidth;
            sliceTransform.m11 = shadowSliceData.resolution * oneOverAtlasHeight;
            sliceTransform.m03 = shadowSliceData.offsetX * oneOverAtlasWidth;
            sliceTransform.m13 = shadowSliceData.offsetY * oneOverAtlasHeight;

            // Apply shadow slice scale and offset
            shadowSliceData.shadowTransform = sliceTransform * shadowSliceData.shadowTransform;
        }

        void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context, ref ShadowSliceData shadowSliceData,
            Matrix4x4 proj, Matrix4x4 view, DrawShadowsSettings settings)
        {
            cmd.SetViewport(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
            cmd.EnableScissorRect(new Rect(shadowSliceData.offsetX + 4, shadowSliceData.offsetY + 4, shadowSliceData.resolution - 8, shadowSliceData.resolution - 8));

            cmd.SetViewProjectionMatrices(view, proj);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawShadows(ref settings);
            cmd.DisableScissorRect();
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        int GetMaxTileResolutionInAtlas(int atlasWidth, int atlasHeight, int tileCount)
        {
            int resolution = Mathf.Min(atlasWidth, atlasHeight);
            if (tileCount > Mathf.Log(resolution))
            {
                Debug.LogError(
                    String.Format(
                        "Cannot fit {0} tiles into current shadowmap atlas of size ({1}, {2}). ShadowMap Resolution set to zero.",
                        tileCount, atlasWidth, atlasHeight));
                return 0;
            }

            int currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            while (currentTileCount < tileCount)
            {
                resolution = resolution >> 1;
                currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            }
            return resolution;
        }

        void SetupShadowCasterConstants(CommandBuffer cmd, ref VisibleLight visibleLight, Matrix4x4 proj, float cascadeResolution)
        {
            Light light = visibleLight.light;
            float bias = 0.0f;
            float normalBias = 0.0f;

            // Use same kernel radius as built-in pipeline so we can achieve same bias results
            // with the default light bias parameters.
            const float kernelRadius = 3.65f;

            if (visibleLight.lightType == LightType.Directional)
            {
                // Scale bias by cascade's world space depth range.
                // Directional shadow lights have orthogonal projection.
                // proj.m22 = -2 / (far - near) since the projection's depth range is [-1.0, 1.0]
                // In order to be correct we should multiply bias by 0.5 but this introducing aliasing along cascades more visible.
                float sign = (SystemInfo.usesReversedZBuffer) ? 1.0f : -1.0f;
                bias = light.shadowBias * proj.m22 * sign;

                // Currently only square POT cascades resolutions are used.
                // We scale normalBias
                double frustumWidth = 2.0 / (double)proj.m00;
                double frustumHeight = 2.0 / (double)proj.m11;
                float texelSizeX = (float)(frustumWidth / (double)cascadeResolution);
                float texelSizeY = (float)(frustumHeight / (double)cascadeResolution);
                float texelSize = Mathf.Max(texelSizeX, texelSizeY);

                // Since we are applying normal bias on caster side we want an inset normal offset
                // thus we use a negative normal bias.
                normalBias = -light.shadowNormalBias * texelSize * kernelRadius;
            }
            else if (visibleLight.lightType == LightType.Spot)
            {
                float sign = (SystemInfo.usesReversedZBuffer) ? -1.0f : 1.0f;
                bias = light.shadowBias * sign;
                normalBias = 0.0f;
            }
            else
            {
                Debug.LogWarning("Only spot and directional shadow casters are supported in lightweight pipeline");
            }

            Vector3 lightDirection = -visibleLight.localToWorld.GetColumn(2);
            cmd.SetGlobalVector("_ShadowBias", new Vector4(bias, normalBias, 0.0f, 0.0f));
            cmd.SetGlobalVector("_LightDirection", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0.0f));
        }

        void SetupLocalLightsShadowReceiverConstants(ref ScriptableRenderContext context, CommandBuffer cmd, ref ShadowData shadowData)
        {
            for (int i = 0; i < m_LocalLightSlices.Length; ++i)
                m_LocalShadowMatrices[i] = m_LocalLightSlices[i].shadowTransform;

            float invShadowAtlasWidth = 1.0f / shadowData.localShadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.localShadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalTexture(RenderTargetHandles.LocalShadowmap, m_LocalShadowmapTexture);
            cmd.SetGlobalMatrixArray(LocalShadowConstantBuffer._LocalWorldToShadowAtlas, m_LocalShadowMatrices);
            cmd.SetGlobalFloatArray(LocalShadowConstantBuffer._LocalShadowStrength, m_LocalShadowStrength);
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset2, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset3, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    shadowData.localShadowAtlasWidth, shadowData.localShadowAtlasHeight));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}
