using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Serializable]
    public class ShadowSettings
    {
        public bool supportsDirectionalShadows;
        public bool screenSpace;
        public int directionalShadowAtlasWidth;
        public int directionalShadowAtlasHeight;
        public float maxShadowDistance;
        public int directionalLightCascadeCount;
        public Vector3 directionalLightCascades;
        public bool supportsLocalShadows;
        public int localShadowAtlasWidth;
        public int localShadowAtlasHeight;
        public bool supportsSoftShadows;
        public int bufferBitCount;

        public RenderTextureFormat shadowmapTextureFormat;
        public RenderTextureFormat screenspaceShadowmapTextureFormat;

        static ShadowSettings defaultShadowSettings = null;

        public static ShadowSettings Default
        {
            get
            {
                if (defaultShadowSettings == null)
                {
                    defaultShadowSettings = new ShadowSettings();
                    defaultShadowSettings.supportsDirectionalShadows = true;
                    defaultShadowSettings.screenSpace = true;
                    defaultShadowSettings.directionalShadowAtlasHeight = defaultShadowSettings.directionalShadowAtlasWidth = 2048;
                    defaultShadowSettings.directionalLightCascadeCount = 1;
                    defaultShadowSettings.directionalLightCascades = new Vector3(0.067f, 0.2f, 0.467f);
                    defaultShadowSettings.supportsLocalShadows = true;
                    defaultShadowSettings.localShadowAtlasWidth = 512;
                    defaultShadowSettings.localShadowAtlasHeight = 512;
                    defaultShadowSettings.bufferBitCount = 16;
                    defaultShadowSettings.shadowmapTextureFormat = RenderTextureFormat.Shadowmap;
                    defaultShadowSettings.screenspaceShadowmapTextureFormat = RenderTextureFormat.R8;
                    defaultShadowSettings.supportsSoftShadows = false;
                }
                return defaultShadowSettings;
            }
        }
    }

    public struct ShadowSliceData
    {
        public Matrix4x4 shadowTransform;
        public int offsetX;
        public int offsetY;
        public int resolution;

        public void Clear()
        {
            shadowTransform = Matrix4x4.identity;
            offsetX = offsetY = 0;
            resolution = 1024;
        }
    }

    public class LightweightShadowPass
    {
        public bool IsDirectionalShadowsEnabled { get { return m_ShadowSettings.supportsDirectionalShadows; } }
        public bool IsLocalShadowsEnabled { get { return m_ShadowSettings.supportsLocalShadows; } }
        public bool RequireScreenSpaceShadowmap { get { return IsDirectionalShadowsEnabled && m_ShadowSettings.screenSpace; } }
        public bool DirectionalShadowsRendered { get { return m_DirectionalShadowmapQuality != LightShadows.None; } }
        public bool LocalShadowsRendered { get { return m_LocalShadowmapQuality != LightShadows.None; } }
        public bool IsSoftShadowsEnabled { get { return m_ShadowSettings.supportsSoftShadows; } }

        public float RenderingDistance { get { return m_ShadowSettings.maxShadowDistance; } }

        private const int kMaxCascades = 4;

        private int m_ShadowCasterCascadesCount;
        private int m_DirectionalShadowmapID;
        private int m_LocalShadowmapID;
        private int m_ScreenSpaceShadowmapID;

        private ShadowSettings m_ShadowSettings = ShadowSettings.Default;

        private Material m_ScreenSpaceShadowsMaterial;

        private RenderTexture m_DirectionalShadowmapTexture;
        private RenderTexture m_LocalShadowmapTexture;
        private RenderTargetIdentifier m_ScreenSpaceShadowmapTexture;

        private RenderTextureDescriptor m_DirectionalShadowmapDescriptor;
        private RenderTextureDescriptor m_LocalShadowmapDescriptor;

        private LightShadows m_DirectionalShadowmapQuality;
        private LightShadows m_LocalShadowmapQuality;

        private Matrix4x4[] m_DirectionalShadowMatrices;
        private ShadowSliceData[] m_CascadeSlices;
        private Vector4[] m_CascadeSplitDistances;
        private Vector4 m_CascadeSplitRadii;

        private Matrix4x4[] m_LocalShadowMatrices;
        private ShadowSliceData[] m_LocalLightSlices;
        private float[] m_LocalShadowStrength;

        public LightweightShadowPass(LightweightPipelineAsset pipelineAsset, int maxLocalLightsCount)
        {
            BuildShadowSettings(pipelineAsset);

            m_DirectionalShadowMatrices = new Matrix4x4[kMaxCascades + 1];
            m_CascadeSlices = new ShadowSliceData[kMaxCascades];
            m_CascadeSplitDistances = new Vector4[kMaxCascades];

            m_LocalShadowMatrices = new Matrix4x4[maxLocalLightsCount];
            m_LocalLightSlices = new ShadowSliceData[maxLocalLightsCount];
            m_LocalShadowStrength = new float[maxLocalLightsCount];

            DirectionalShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_WorldToShadow");
            DirectionalShadowConstantBuffer._ShadowData = Shader.PropertyToID("_ShadowData");
            DirectionalShadowConstantBuffer._DirShadowSplitSpheres = Shader.PropertyToID("_DirShadowSplitSpheres");
            DirectionalShadowConstantBuffer._DirShadowSplitSphereRadii = Shader.PropertyToID("_DirShadowSplitSphereRadii");
            DirectionalShadowConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_ShadowOffset0");
            DirectionalShadowConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_ShadowOffset1");
            DirectionalShadowConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_ShadowOffset2");
            DirectionalShadowConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_ShadowOffset3");
            DirectionalShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_ShadowmapSize");

            LocalShadowConstantBuffer._LocalWorldToShadowAtlas = Shader.PropertyToID("_LocalWorldToShadowAtlas");
            LocalShadowConstantBuffer._LocalShadowStrength = Shader.PropertyToID("_LocalShadowStrength");
            LocalShadowConstantBuffer._LocalShadowOffset0 = Shader.PropertyToID("_LocalShadowOffset0");
            LocalShadowConstantBuffer._LocalShadowOffset1 = Shader.PropertyToID("_LocalShadowOffset1");
            LocalShadowConstantBuffer._LocalShadowOffset2 = Shader.PropertyToID("_LocalShadowOffset2");
            LocalShadowConstantBuffer._LocalShadowOffset3 = Shader.PropertyToID("_LocalShadowOffset3");
            LocalShadowConstantBuffer._LocalShadowmapSize = Shader.PropertyToID("_LocalShadowmapSize");

            m_DirectionalShadowmapID = Shader.PropertyToID("_ShadowMap");
            m_LocalShadowmapID = Shader.PropertyToID("_LocalShadowMapAtlas");
            m_ScreenSpaceShadowmapID = Shader.PropertyToID("_ScreenSpaceShadowMap");
            m_ScreenSpaceShadowmapTexture = new RenderTargetIdentifier(m_ScreenSpaceShadowmapID);

            m_DirectionalShadowmapDescriptor = new RenderTextureDescriptor(m_ShadowSettings.directionalShadowAtlasWidth,
                    m_ShadowSettings.directionalShadowAtlasHeight, m_ShadowSettings.shadowmapTextureFormat, m_ShadowSettings.bufferBitCount);

            m_LocalShadowmapDescriptor = new RenderTextureDescriptor(m_ShadowSettings.localShadowAtlasWidth,
                    m_ShadowSettings.localShadowAtlasHeight, m_ShadowSettings.shadowmapTextureFormat, m_ShadowSettings.bufferBitCount);

            m_ScreenSpaceShadowsMaterial = CoreUtils.CreateEngineMaterial(pipelineAsset.ScreenSpaceShadowShader);

            Clear();
        }

        public void InitializeResources(CommandBuffer cmd, RenderTextureDescriptor renderTextureDesc)
        {
            if (RequireScreenSpaceShadowmap)
            {
                renderTextureDesc.depthBufferBits = 0;
                renderTextureDesc.colorFormat = m_ShadowSettings.screenspaceShadowmapTextureFormat;
                cmd.GetTemporaryRT(m_ScreenSpaceShadowmapID, renderTextureDesc, FilterMode.Bilinear);
            }
        }

        public void Dispose(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_ScreenSpaceShadowmapID);

            if (m_DirectionalShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_DirectionalShadowmapTexture);
                m_DirectionalShadowmapTexture = null;
            }

            if (m_LocalShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_LocalShadowmapTexture);
                m_LocalShadowmapTexture = null;
            }
        }

        public bool Execute(ref CullResults cullResults, ref LightData lightData, ref ScriptableRenderContext context)
        {
            Clear();

            bool directionalShadowmapRendered = false;
            if (IsDirectionalShadowsEnabled)
                directionalShadowmapRendered = RenderDirectionalCascadeShadowmap(ref cullResults, ref lightData, ref context);

            if (IsLocalShadowsEnabled)
                RenderLocalShadowmapAtlas(ref cullResults, ref lightData, ref context);

            return directionalShadowmapRendered && m_ShadowSettings.screenSpace;
        }

        public void CollectShadows(Camera camera, FrameRenderingConfiguration frameRenderingConfiguration, ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Collect Shadows");

            SetShadowCollectPassKeywords(cmd);

            // Note: The source isn't actually 'used', but there's an engine peculiarity (bug) that
            // doesn't like null sources when trying to determine a stereo-ized blit.  So for proper
            // stereo functionality, we use the screen-space shadow map as the source (until we have
            // a better solution).
            // An alternative would be DrawProcedural, but that would require further changes in the shader.
            cmd.SetRenderTarget(m_ScreenSpaceShadowmapTexture);
            cmd.ClearRenderTarget(true, true, Color.white);
            cmd.Blit(m_ScreenSpaceShadowmapTexture, m_ScreenSpaceShadowmapTexture, m_ScreenSpaceShadowsMaterial);

            LightweightPipeline.StartStereoRendering(camera, ref context, frameRenderingConfiguration);

            context.ExecuteCommandBuffer(cmd);

            LightweightPipeline.StopStereoRendering(camera, ref context, frameRenderingConfiguration);

            CommandBufferPool.Release(cmd);
        }

        private void BuildShadowSettings(LightweightPipelineAsset pipelineAsset)
        {
            // Until we can have keyword stripping forcing single cascade hard shadows on gles2
            bool supportsScreenSpaceShadows = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            m_ShadowSettings = ShadowSettings.Default;
            m_ShadowSettings.supportsDirectionalShadows = pipelineAsset.SupportsDirectionalShadows;
            m_ShadowSettings.screenSpace = m_ShadowSettings.supportsDirectionalShadows && supportsScreenSpaceShadows;
            m_ShadowSettings.directionalLightCascadeCount = (m_ShadowSettings.screenSpace) ? pipelineAsset.CascadeCount : 1;

            m_ShadowSettings.directionalShadowAtlasWidth = pipelineAsset.DirectionalShadowAtlasResolution;
            m_ShadowSettings.directionalShadowAtlasHeight = pipelineAsset.DirectionalShadowAtlasResolution;
            m_ShadowSettings.maxShadowDistance = pipelineAsset.ShadowDistance;

            switch (m_ShadowSettings.directionalLightCascadeCount)
            {
                case 1:
                    m_ShadowSettings.directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
                    break;

                case 2:
                    m_ShadowSettings.directionalLightCascades = new Vector3(pipelineAsset.Cascade2Split, 1.0f, 0.0f);
                    break;

                default:
                    m_ShadowSettings.directionalLightCascades = pipelineAsset.Cascade4Split;
                    break;
            }

            m_ShadowSettings.supportsLocalShadows = pipelineAsset.SupportsLocalShadows;
            m_ShadowSettings.localShadowAtlasWidth = m_ShadowSettings.localShadowAtlasHeight = pipelineAsset.LocalShadowAtlasResolution;
            m_ShadowSettings.supportsSoftShadows = pipelineAsset.SupportsSoftShadows;

            m_ShadowSettings.bufferBitCount = 16;

            m_ShadowSettings.shadowmapTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;

            m_ShadowSettings.screenspaceShadowmapTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;
        }

        private void Clear()
        {
            m_DirectionalShadowmapTexture = null;
            m_LocalShadowmapTexture = null;
            m_DirectionalShadowmapQuality = LightShadows.None;
            m_LocalShadowmapQuality = LightShadows.None;

            for (int i = 0; i < m_DirectionalShadowMatrices.Length; ++i)
                m_DirectionalShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_LocalShadowMatrices.Length; ++i)
                m_LocalShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            m_CascadeSplitRadii = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();

            for (int i = 0; i < m_LocalLightSlices.Length; ++i)
                m_LocalLightSlices[i].Clear();

            for (int i = 0; i < m_LocalShadowStrength.Length; ++i)
                m_LocalShadowStrength[i] = 0.0f;
        }

        private void SetShadowCollectPassKeywords(CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, LightweightKeywords.SoftShadowsText, m_DirectionalShadowmapQuality == LightShadows.Soft);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.CascadeShadowsText, m_ShadowSettings.directionalLightCascadeCount > 1);
        }

        private bool RenderDirectionalCascadeShadowmap(ref CullResults cullResults, ref LightData lightData, ref ScriptableRenderContext context)
        {
            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return false;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            Debug.Assert(shadowLight.lightType == LightType.Directional);

            if (light.shadows == LightShadows.None)
                return false;

            CommandBuffer cmd = CommandBufferPool.Get("Prepare Directional Shadowmap");
            m_ShadowCasterCascadesCount = m_ShadowSettings.directionalLightCascadeCount;

            int shadowResolution = GetMaxTileResolutionInAtlas(m_ShadowSettings.directionalShadowAtlasWidth, m_ShadowSettings.directionalShadowAtlasHeight, m_ShadowCasterCascadesCount);
            float shadowNearPlane = light.shadowNearPlane;

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return false;

            Matrix4x4 view, proj;
            var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);

            m_DirectionalShadowmapTexture = RenderTexture.GetTemporary(m_DirectionalShadowmapDescriptor);
            m_DirectionalShadowmapTexture.filterMode = FilterMode.Bilinear;
            m_DirectionalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

            CoreUtils.SetRenderTarget(cmd, m_DirectionalShadowmapTexture, ClearFlag.Depth);

            bool success = false;
            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex,
                        cascadeIndex, m_ShadowCasterCascadesCount, m_ShadowSettings.directionalLightCascades, shadowResolution, shadowNearPlane, out view, out proj,
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
                    ApplySliceTransform(ref m_CascadeSlices[cascadeIndex], m_ShadowSettings.directionalShadowAtlasWidth, m_ShadowSettings.directionalShadowAtlasHeight);

                SetupShadowCasterConstants(cmd, ref shadowLight, proj, shadowResolution);
                RenderShadowSlice(cmd, ref context, ref m_CascadeSlices[cascadeIndex], proj, view, settings);
            }

            if (success)
            {
                m_DirectionalShadowmapQuality = (IsSoftShadowsEnabled) ? light.shadows : LightShadows.Hard;

                // In order to avoid shader variants explosion we only do hard shadows when sampling shadowmap in the lit pass.
                // GLES2 platform is forced to hard single cascade shadows.
                if (!m_ShadowSettings.screenSpace)
                    m_DirectionalShadowmapQuality = LightShadows.Hard;

                SetupDirectionalShadowReceiverConstants(cmd, shadowLight, ref context);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            return success;
        }

        private void RenderLocalShadowmapAtlas(ref CullResults cullResults, ref LightData lightData, ref ScriptableRenderContext context)
        {
            List<int> localLightIndices = lightData.localLightIndices;
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

            CommandBuffer cmd = CommandBufferPool.Get("Prepare Local Lights Shadowmap");
            Matrix4x4 view, proj;
            Bounds bounds;

            // TODO: Add support to point light shadows. We make a simplification here that only works
            // for spot lights and with max spot shadows per pass.
            int atlasWidth = m_ShadowSettings.localShadowAtlasWidth;
            int atlasHeight = m_ShadowSettings.localShadowAtlasHeight;
            int sliceResolution = GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, shadowCastingLightsCount);
            int shadowSampling = 0;

            m_LocalShadowmapTexture = RenderTexture.GetTemporary(m_LocalShadowmapDescriptor);
            m_LocalShadowmapTexture.filterMode = FilterMode.Bilinear;
            m_LocalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

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

            SetupLocalLightsShadowReceiverConstants(cmd, ref context);

            m_LocalShadowmapQuality = (IsSoftShadowsEnabled) ? (LightShadows)shadowSampling : LightShadows.Hard;
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
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

        private void ApplySliceTransform(ref ShadowSliceData shadowSliceData, int atlasWidth, int atlasHeight)
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

        private void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context, ref ShadowSliceData shadowSliceData,
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

        private int GetMaxTileResolutionInAtlas(int atlasWidth, int atlasHeight, int tileCount)
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

        private void SetupShadowCasterConstants(CommandBuffer cmd, ref VisibleLight visibleLight, Matrix4x4 proj, float cascadeResolution)
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

        private void SetupDirectionalShadowReceiverConstants(CommandBuffer cmd, VisibleLight shadowLight, ref ScriptableRenderContext context)
        {
            Light light = shadowLight.light;

            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < kMaxCascades; ++i)
                m_DirectionalShadowMatrices[i] = (cascadeCount >= i) ? m_CascadeSlices[i].shadowTransform : Matrix4x4.identity;

            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m33 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            m_DirectionalShadowMatrices[kMaxCascades] = noOpShadowMatrix;

            float invShadowAtlasWidth = 1.0f / m_ShadowSettings.directionalShadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / m_ShadowSettings.directionalShadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            cmd.SetGlobalTexture(m_DirectionalShadowmapID, m_DirectionalShadowmapTexture);
            cmd.SetGlobalMatrixArray(DirectionalShadowConstantBuffer._WorldToShadow, m_DirectionalShadowMatrices);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowData, new Vector4(light.shadowStrength, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalVectorArray(DirectionalShadowConstantBuffer._DirShadowSplitSpheres, m_CascadeSplitDistances);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._DirShadowSplitSphereRadii, m_CascadeSplitRadii);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset2, new Vector4(-invHalfShadowAtlasWidth,  invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset3, new Vector4(invHalfShadowAtlasWidth,  invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    m_ShadowSettings.directionalShadowAtlasWidth, m_ShadowSettings.directionalShadowAtlasHeight));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void SetupLocalLightsShadowReceiverConstants(CommandBuffer cmd, ref ScriptableRenderContext context)
        {
            for (int i = 0; i < m_LocalLightSlices.Length; ++i)
                m_LocalShadowMatrices[i] = m_LocalLightSlices[i].shadowTransform;

            float invShadowAtlasWidth = 1.0f / m_ShadowSettings.localShadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / m_ShadowSettings.localShadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalTexture(m_LocalShadowmapID, m_LocalShadowmapTexture);
            cmd.SetGlobalMatrixArray(LocalShadowConstantBuffer._LocalWorldToShadowAtlas, m_LocalShadowMatrices);
            cmd.SetGlobalFloatArray(LocalShadowConstantBuffer._LocalShadowStrength, m_LocalShadowStrength);
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset2, new Vector4(-invHalfShadowAtlasWidth,  invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset3, new Vector4(invHalfShadowAtlasWidth,  invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    m_ShadowSettings.localShadowAtlasWidth, m_ShadowSettings.localShadowAtlasHeight));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    };
}
