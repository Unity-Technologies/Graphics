using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DirectionalShadowsPass : ScriptableRenderPass
    {
        const int k_MaxCascades = 4;
        const int k_ShadowmapBufferBits = 16;
        int m_ShadowCasterCascadesCount;

        RenderTexture m_DirectionalShadowmapTexture;
        RenderTextureDescriptor m_DirectionalShadowmapDescriptor;

        Matrix4x4[] m_DirectionalShadowMatrices;
        ShadowSliceData[] m_CascadeSlices;
        Vector4[] m_CascadeSplitDistances;
        Vector4 m_CascadeSplitRadii;

        public DirectionalShadowsPass(LightweightForwardRenderer renderer, int atlasResolution) : base(renderer)
        {
            RegisterShaderPassName("ShadowCaster");

            m_DirectionalShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
            m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
            m_CascadeSplitDistances = new Vector4[k_MaxCascades];

            DirectionalShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_WorldToShadow");
            DirectionalShadowConstantBuffer._ShadowData = Shader.PropertyToID("_ShadowData");
            DirectionalShadowConstantBuffer._DirShadowSplitSpheres = Shader.PropertyToID("_DirShadowSplitSpheres");
            DirectionalShadowConstantBuffer._DirShadowSplitSphereRadii = Shader.PropertyToID("_DirShadowSplitSphereRadii");
            DirectionalShadowConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_ShadowOffset0");
            DirectionalShadowConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_ShadowOffset1");
            DirectionalShadowConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_ShadowOffset2");
            DirectionalShadowConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_ShadowOffset3");
            DirectionalShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_ShadowmapSize");

            RenderTextureFormat shadowmapFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;

            m_DirectionalShadowmapDescriptor = new RenderTextureDescriptor(atlasResolution,
                    atlasResolution, shadowmapFormat, k_ShadowmapBufferBits);

            Clear();
        }

        public override void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int samples)
        {
            //m_DirectionalShadowmapTexture = RenderTexture.GetTemporary(m_DirectionalShadowmapDescriptor);
            //m_DirectionalShadowmapTexture.filterMode = FilterMode.Bilinear;
            //m_DirectionalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData)
        {
            Clear();

            ShadowData shadowData = lightData.shadowData;
            if (shadowData.supportsDirectionalShadows)
                lightData.shadowData.renderedDirectionalShadowQuality = RenderDirectionalCascadeShadowmap(ref context, ref cullResults, ref lightData, ref shadowData);
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (m_DirectionalShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_DirectionalShadowmapTexture);
                m_DirectionalShadowmapTexture = null;
            }
            m_Disposed = true;
        }

        void Clear()
        {
            m_DirectionalShadowmapTexture = null;

            for (int i = 0; i < m_DirectionalShadowMatrices.Length; ++i)
                m_DirectionalShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            m_CascadeSplitRadii = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();
        }

        LightShadows RenderDirectionalCascadeShadowmap(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            LightShadows shadowQuality = LightShadows.None;
            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return shadowQuality;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            Debug.Assert(shadowLight.lightType == LightType.Directional);

            if (light.shadows == LightShadows.None)
                return shadowQuality;

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return shadowQuality;

            CommandBuffer cmd = CommandBufferPool.Get("Prepare Directional Shadowmap");
            m_ShadowCasterCascadesCount = shadowData.directionalLightCascadeCount;

            int shadowResolution = GetMaxTileResolutionInAtlas(shadowData.directionalShadowAtlasWidth, shadowData.directionalShadowAtlasHeight, m_ShadowCasterCascadesCount);
            float shadowNearPlane = light.shadowNearPlane;

            Matrix4x4 view, proj;
            var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);

            m_DirectionalShadowmapTexture = RenderTexture.GetTemporary(m_DirectionalShadowmapDescriptor);
            m_DirectionalShadowmapTexture.filterMode = FilterMode.Bilinear;
            m_DirectionalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;
            CoreUtils.SetRenderTarget(cmd, m_DirectionalShadowmapTexture, ClearFlag.Depth);
            m_Disposed = false;

            bool success = false;
            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex,
                        cascadeIndex, m_ShadowCasterCascadesCount, shadowData.directionalLightCascades, shadowResolution, shadowNearPlane, out view, out proj,
                        out settings.splitData);

                float cullingSphereRadius = settings.splitData.cullingSphere.w;
                m_CascadeSplitDistances[cascadeIndex] = settings.splitData.cullingSphere;
                m_CascadeSplitRadii[cascadeIndex] = cullingSphereRadius * cullingSphereRadius;

                if (!success)
                    break;

                m_CascadeSlices[cascadeIndex].offsetX = (cascadeIndex % 2) * shadowResolution;
                m_CascadeSlices[cascadeIndex].offsetY = (cascadeIndex / 2) * shadowResolution;
                m_CascadeSlices[cascadeIndex].resolution = shadowResolution;
                m_CascadeSlices[cascadeIndex].shadowTransform = GetShadowTransform(proj, view);

                // If we have shadow cascades baked into the atlas we bake cascade transform
                // in each shadow matrix to save shader ALU and L/S
                if (m_ShadowCasterCascadesCount > 1)
                    ApplySliceTransform(ref m_CascadeSlices[cascadeIndex], shadowData.directionalShadowAtlasWidth, shadowData.directionalShadowAtlasHeight);

                SetupShadowCasterConstants(cmd, ref shadowLight, proj, shadowResolution);
                RenderShadowSlice(cmd, ref context, ref m_CascadeSlices[cascadeIndex], proj, view, settings);
            }

            if (success)
            {
                shadowQuality = (shadowData.supportsSoftShadows) ? light.shadows : LightShadows.Hard;

                // In order to avoid shader variants explosion we only do hard shadows when sampling shadowmap in the lit pass.
                // GLES2 platform is forced to hard single cascade shadows.
                if (!shadowData.requiresScreenSpaceOcclusion)
                    shadowQuality = LightShadows.Hard;

                SetupDirectionalShadowReceiverConstants(ref context, cmd, ref shadowData, shadowLight);
            }

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

        void SetupDirectionalShadowReceiverConstants(ref ScriptableRenderContext context, CommandBuffer cmd, ref ShadowData shadowData, VisibleLight shadowLight)
        {
            Light light = shadowLight.light;

            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < k_MaxCascades; ++i)
                m_DirectionalShadowMatrices[i] = (cascadeCount >= i) ? m_CascadeSlices[i].shadowTransform : Matrix4x4.identity;

            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m33 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            m_DirectionalShadowMatrices[k_MaxCascades] = noOpShadowMatrix;

            float invShadowAtlasWidth = 1.0f / shadowData.directionalShadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.directionalShadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            cmd.SetGlobalTexture(RenderTargetHandles.DirectionalShadowmap, m_DirectionalShadowmapTexture);
            cmd.SetGlobalMatrixArray(DirectionalShadowConstantBuffer._WorldToShadow, m_DirectionalShadowMatrices);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowData, new Vector4(light.shadowStrength, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalVectorArray(DirectionalShadowConstantBuffer._DirShadowSplitSpheres, m_CascadeSplitDistances);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._DirShadowSplitSphereRadii, m_CascadeSplitRadii);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset2, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset3, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    shadowData.directionalShadowAtlasWidth, shadowData.directionalShadowAtlasHeight));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    };
}
