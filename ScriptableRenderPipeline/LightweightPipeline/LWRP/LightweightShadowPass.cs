using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Serializable]
    public class ShadowSettings
    {
        public LightShadows shadowType;
        public bool screenSpace;
        public int shadowAtlasWidth;
        public int shadowAtlasHeight;

        public float maxShadowDistance;
        public int directionalLightCascadeCount;
        public Vector3 directionalLightCascades;
        public float directionalLightNearPlaneOffset;

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
                    defaultShadowSettings.shadowType = LightShadows.None;
                    defaultShadowSettings.screenSpace = true;
                    defaultShadowSettings.shadowAtlasHeight = defaultShadowSettings.shadowAtlasWidth = 4096;
                    defaultShadowSettings.directionalLightCascadeCount = 1;
                    defaultShadowSettings.directionalLightCascades = new Vector3(0.05F, 0.2F, 0.3F);
                    defaultShadowSettings.directionalLightNearPlaneOffset = 5;
                    defaultShadowSettings.maxShadowDistance = 1000.0F;
                    defaultShadowSettings.shadowmapTextureFormat = RenderTextureFormat.Shadowmap;
                    defaultShadowSettings.screenspaceShadowmapTextureFormat = RenderTextureFormat.R8;
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
    }

    public class LightweightShadowPass
    {
        public bool IsEnabled { get { return m_ShadowSettings.shadowType != LightShadows.None; } }
        public bool RequireScreenSpaceShadowmap { get { return IsEnabled && m_ShadowSettings.screenSpace; } }
        public float RenderingDistance { get { return m_ShadowSettings.maxShadowDistance; } }
        public bool HasDirectionalShadowmap { get { return m_ShadowSampling != LightShadows.None; } }
        public bool HasLocalLightsShadowmap { get { return m_LocalLightShadowSampling != LightShadows.None; } }

        // TODO: move to a global settings file
        private static readonly int kMaxPerObjectLights = 8;
        private static readonly int kMaxLocalPixelLightPerPass = 4;

        private const int kMaxCascades = 4;
        private int m_ShadowCasterCascadesCount;
        private int m_DirectionalShadowmapRTID;
        private int m_AdditionalShadowmapRTID;
        private int m_ScreenSpaceShadowMapRTID;

        private Matrix4x4[] m_ShadowMatrices = new Matrix4x4[kMaxCascades + 1];
        private Matrix4x4[] m_LocalLightShadowMatrices = new Matrix4x4[kMaxLocalPixelLightPerPass];
        private RenderTexture m_DirectionalShadowmapRT;
        private RenderTexture m_AdditionalShadowmapRT;
        private RenderTargetIdentifier m_ScreenSpaceShadowMapRT;

        private const int kShadowBufferBits = 16;
        private Vector4[] m_DirectionalShadowSplitDistances = new Vector4[kMaxCascades];
        private Vector4 m_DirectionalShadowSplitRadii;

        private ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        private ShadowSliceData[] m_ShadowCascadeSlices = new ShadowSliceData[kMaxCascades];
        private ShadowSliceData[] m_LocalLightShadowSlices = new ShadowSliceData[kMaxPerObjectLights];
        private float[] m_LocalShadowStrength = new float[kMaxLocalPixelLightPerPass];
        private LightShadows m_ShadowSampling;
        private LightShadows m_LocalLightShadowSampling;
        private int m_LocalLightShadowAtlasResolution = 512;

        private Material m_ScreenSpaceShadowsMaterial;

        public LightweightShadowPass(LightweightPipelineAsset pipelineAsset)
        {
            BuildShadowSettings(pipelineAsset);

            DirectionalShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_WorldToShadow");
            DirectionalShadowConstantBuffer._ShadowData = Shader.PropertyToID("_ShadowData");
            DirectionalShadowConstantBuffer._DirShadowSplitSpheres = Shader.PropertyToID("_DirShadowSplitSpheres");
            DirectionalShadowConstantBuffer._DirShadowSplitSphereRadii = Shader.PropertyToID("_DirShadowSplitSphereRadii");
            DirectionalShadowConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_ShadowOffset0");
            DirectionalShadowConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_ShadowOffset1");
            DirectionalShadowConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_ShadowOffset2");
            DirectionalShadowConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_ShadowOffset3");
            DirectionalShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_ShadowmapSize");

            LocalShadowConstantBuffer._LocalWorldToShadow = Shader.PropertyToID("_LocalWorldToShadowAtlas");
            LocalShadowConstantBuffer._LocalShadowData = Shader.PropertyToID("_LocalShadowData");
            LocalShadowConstantBuffer._LocalShadowOffset0 = Shader.PropertyToID("_LocalShadowOffset0");
            LocalShadowConstantBuffer._LocalShadowOffset1 = Shader.PropertyToID("_LocalShadowOffset1");
            LocalShadowConstantBuffer._LocalShadowOffset2 = Shader.PropertyToID("_LocalShadowOffset2");
            LocalShadowConstantBuffer._LocalShadowOffset3 = Shader.PropertyToID("_LocalShadowOffset3");
            LocalShadowConstantBuffer._LocalShadowmapSize = Shader.PropertyToID("_LocalShadowmapSize");

            m_DirectionalShadowmapRTID = Shader.PropertyToID("_ShadowMap");
            m_AdditionalShadowmapRTID = Shader.PropertyToID("_LocalShadowMapAtlas");
            m_ScreenSpaceShadowMapRTID = Shader.PropertyToID("_ScreenSpaceShadowMap");
            m_ScreenSpaceShadowMapRT = new RenderTargetIdentifier(m_ScreenSpaceShadowMapRTID);

            for (int i = 0; i < kMaxCascades; ++i)
                m_DirectionalShadowSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            m_DirectionalShadowSplitRadii = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            m_ScreenSpaceShadowsMaterial = CoreUtils.CreateEngineMaterial(pipelineAsset.ScreenSpaceShadowShader);
        }

        public void InitializeResources(CommandBuffer cmd, RenderTextureDescriptor renderTextureDesc)
        {
            if (RequireScreenSpaceShadowmap)
            {
                renderTextureDesc.depthBufferBits = 0;
                renderTextureDesc.colorFormat = m_ShadowSettings.screenspaceShadowmapTextureFormat;
                cmd.GetTemporaryRT(m_ScreenSpaceShadowMapRTID, renderTextureDesc, FilterMode.Bilinear);
            }
        }

        public void Dispose(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_ScreenSpaceShadowMapRTID);

            if (m_DirectionalShadowmapRT)
            {
                RenderTexture.ReleaseTemporary(m_DirectionalShadowmapRT);
                m_DirectionalShadowmapRT = null;
            }

            if (m_AdditionalShadowmapRT)
            {
                RenderTexture.ReleaseTemporary(m_AdditionalShadowmapRT);
                m_AdditionalShadowmapRT = null;
            }
        }

        public bool Execute(ref CullResults cullResults, ref LightData lightData, ref ScriptableRenderContext context)
        {
            Clear();

            if (!IsEnabled)
                return false;

            bool directionalShadowmapRendered = RenderDirectionalCascadeShadowmap(ref cullResults, ref lightData, ref context);
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
            cmd.Blit(m_ScreenSpaceShadowMapRT, m_ScreenSpaceShadowMapRT, m_ScreenSpaceShadowsMaterial);

            LightweightUtils.StartStereoRendering(camera, ref context, frameRenderingConfiguration);

            context.ExecuteCommandBuffer(cmd);

            LightweightUtils.StopStereoRendering(camera, ref context, frameRenderingConfiguration);

            CommandBufferPool.Release(cmd);
        }

        private void BuildShadowSettings(LightweightPipelineAsset pipelineAsset)
        {
            m_ShadowSettings = ShadowSettings.Default;
            m_ShadowSettings.shadowType = (LightShadows)pipelineAsset.ShadowSetting;
            m_ShadowSettings.screenSpace = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;
            m_ShadowSettings.directionalLightCascadeCount = pipelineAsset.CascadeCount;

            m_ShadowSettings.shadowAtlasWidth = pipelineAsset.ShadowAtlasResolution;
            m_ShadowSettings.shadowAtlasHeight = pipelineAsset.ShadowAtlasResolution;
            m_ShadowSettings.maxShadowDistance = pipelineAsset.ShadowDistance;
            m_ShadowSettings.shadowmapTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;

            m_ShadowSettings.screenspaceShadowmapTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                    ? RenderTextureFormat.R8
                    : RenderTextureFormat.ARGB32;

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
        }

        private void Clear()
        {
            m_DirectionalShadowmapRT = null;
            m_AdditionalShadowmapRT = null;
            m_ShadowSampling = LightShadows.None;
            m_LocalLightShadowSampling = LightShadows.None;
            for (int i = 0; i < kMaxLocalPixelLightPerPass; ++i)
            {
                m_LocalLightShadowMatrices[i] = Matrix4x4.identity;
                m_LocalShadowStrength[i] = 0.0f;
            }
        }

        private void SetShadowCollectPassKeywords(CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, "_SHADOWS_SOFT", m_ShadowSampling == LightShadows.Soft);
            CoreUtils.SetKeyword(cmd, "_SHADOWS_CASCADE", m_ShadowSettings.directionalLightCascadeCount > 1);
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

            int shadowResolution = GetMaxTileResolutionInAtlas(m_ShadowSettings.shadowAtlasWidth, m_ShadowSettings.shadowAtlasHeight, m_ShadowCasterCascadesCount);
            float shadowNearPlane = light.shadowNearPlane;

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return false;

            Matrix4x4 view, proj;
            var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);

            RenderTextureDescriptor shadowmapDescriptor = new RenderTextureDescriptor(m_ShadowSettings.shadowAtlasWidth,
                m_ShadowSettings.shadowAtlasHeight, m_ShadowSettings.shadowmapTextureFormat, kShadowBufferBits);
            shadowmapDescriptor.shadowSamplingMode = ShadowSamplingMode.CompareDepths;
            m_DirectionalShadowmapRT = RenderTexture.GetTemporary(shadowmapDescriptor);
            m_DirectionalShadowmapRT.filterMode = FilterMode.Bilinear;
            m_DirectionalShadowmapRT.wrapMode = TextureWrapMode.Clamp;

            CoreUtils.SetRenderTarget(cmd, m_DirectionalShadowmapRT, ClearFlag.Depth);

            bool success = false;
            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex,
                        cascadeIndex, m_ShadowCasterCascadesCount, m_ShadowSettings.directionalLightCascades, shadowResolution, shadowNearPlane, out view, out proj,
                        out settings.splitData);

                float cullingSphereRadius = settings.splitData.cullingSphere.w;
                m_DirectionalShadowSplitDistances[cascadeIndex] = settings.splitData.cullingSphere;
                m_DirectionalShadowSplitRadii[cascadeIndex] = cullingSphereRadius * cullingSphereRadius;

                if (!success)
                    break;

                m_ShadowCascadeSlices[cascadeIndex].offsetX = (cascadeIndex % 2) * shadowResolution;
                m_ShadowCascadeSlices[cascadeIndex].offsetY = (cascadeIndex / 2) * shadowResolution;
                m_ShadowCascadeSlices[cascadeIndex].resolution = shadowResolution;

                SetupShadowCasterConstants(cmd, ref shadowLight, proj, shadowResolution);
                SetupShadowSliceTransform(ref m_ShadowCascadeSlices[cascadeIndex], m_ShadowSettings.shadowAtlasWidth, m_ShadowSettings.shadowAtlasHeight, proj, view);
                RenderShadowSlice(cmd, ref context, ref m_ShadowCascadeSlices[cascadeIndex], proj, view, settings);
            }

            if (success)
            {
                m_ShadowSampling = (m_ShadowSettings.shadowType != LightShadows.Soft) ? LightShadows.Hard : light.shadows;

                // In order to avoid shader variants explosion we only do hard shadows when sampling shadowmap in the lit pass.
                // GLES2 platform is forced to hard single cascade shadows.
                if (!m_ShadowSettings.screenSpace)
                    m_ShadowSampling = LightShadows.Hard;

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
            for (int i = 0; i < localLightIndices.Count; ++i)
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
            int atlasWidth = m_LocalLightShadowAtlasResolution;
            int atlasHeight = m_LocalLightShadowAtlasResolution;
            int sliceResolution = GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, shadowCastingLightsCount);
            int shadowSampling = 0;

            RenderTextureDescriptor shadowmapDescriptor = new RenderTextureDescriptor(m_LocalLightShadowAtlasResolution, m_LocalLightShadowAtlasResolution, m_ShadowSettings.shadowmapTextureFormat, kShadowBufferBits);
            shadowmapDescriptor.shadowSamplingMode = ShadowSamplingMode.CompareDepths;
            m_AdditionalShadowmapRT = RenderTexture.GetTemporary(shadowmapDescriptor);
            m_AdditionalShadowmapRT.filterMode = FilterMode.Bilinear;
            m_AdditionalShadowmapRT.wrapMode = TextureWrapMode.Clamp;

            CoreUtils.SetRenderTarget(cmd, m_AdditionalShadowmapRT, ClearFlag.Depth);

            for (int i = 0; i < localLightIndices.Count; ++i)
            {
                int shadowLightIndex = localLightIndices[i];
                VisibleLight shadowLight = visibleLights[shadowLightIndex];
                Light light = shadowLight.light;

                if (shadowLight.lightType != LightType.Spot || shadowLight.light.shadows == LightShadows.None)
                    continue;

                if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                    continue;

                var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);

                if (cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowLightIndex, out view, out proj, out settings.splitData))
                {
                    // This way of computing the shadow slice only work for spots and with most 4 shadow casting lights per pass
                    // Change this when point lights are supported.
                    Debug.Assert(kMaxLocalPixelLightPerPass == 4 && shadowLight.lightType == LightType.Spot);
                    m_LocalLightShadowSlices[i].offsetX = (i % 2) * sliceResolution;
                    m_LocalLightShadowSlices[i].offsetY = (i / 2) * sliceResolution;
                    m_LocalLightShadowSlices[i].resolution = sliceResolution;

                    SetupShadowCasterConstants(cmd, ref shadowLight, proj, sliceResolution);
                    SetupShadowSliceTransform(ref m_LocalLightShadowSlices[i], atlasWidth, atlasHeight, proj, view);
                    RenderShadowSlice(cmd, ref context, ref m_LocalLightShadowSlices[i], proj, view, settings);
                    m_LocalShadowStrength[i] = light.shadowStrength;
                    shadowSampling = Math.Max(shadowSampling, (int)light.shadows);
                    lightData.localShadowMask |= (1 << shadowLightIndex);
                }
            }

            SetupLocalLightsShadowReceiverConstants(cmd, ref context);

            m_LocalLightShadowSampling = (LightShadows)Math.Min(shadowSampling, (int)m_ShadowSettings.shadowType);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void SetupShadowSliceTransform(ref ShadowSliceData shadowSliceData, int atlasWidth, int atlasHeight, Matrix4x4 proj, Matrix4x4 view)
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
            worldToShadow = textureScaleAndBias * worldToShadow;

            var cascadeAtlas = Matrix4x4.identity;
            float oneOverAtlasWidth = 1.0f / atlasWidth;
            float oneOverAtlasHeight = 1.0f / atlasHeight;
            cascadeAtlas.m00 = shadowSliceData.resolution * oneOverAtlasWidth;
            cascadeAtlas.m11 = shadowSliceData.resolution * oneOverAtlasHeight;
            cascadeAtlas.m03 = shadowSliceData.offsetX * oneOverAtlasWidth;
            cascadeAtlas.m13 = shadowSliceData.offsetY * oneOverAtlasHeight;

            // Apply shadow slice scale and offset
            worldToShadow = cascadeAtlas * worldToShadow;

            shadowSliceData.shadowTransform = worldToShadow;
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
                m_ShadowMatrices[i] = (cascadeCount >= i) ? m_ShadowCascadeSlices[i].shadowTransform : Matrix4x4.identity;

            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m33 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            m_ShadowMatrices[kMaxCascades] = noOpShadowMatrix;

            float invShadowAtlasWidth = 1.0f / m_ShadowSettings.shadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / m_ShadowSettings.shadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            cmd.SetGlobalTexture(m_DirectionalShadowmapRTID, m_DirectionalShadowmapRT);
            cmd.SetGlobalMatrixArray(DirectionalShadowConstantBuffer._WorldToShadow, m_ShadowMatrices);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowData, new Vector4(light.shadowStrength, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalVectorArray(DirectionalShadowConstantBuffer._DirShadowSplitSpheres, m_DirectionalShadowSplitDistances);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._DirShadowSplitSphereRadii, m_DirectionalShadowSplitRadii);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset1, new Vector4( invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset2, new Vector4(-invHalfShadowAtlasWidth,  invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset3, new Vector4( invHalfShadowAtlasWidth,  invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                m_ShadowSettings.shadowAtlasWidth, m_ShadowSettings.shadowAtlasHeight));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void SetupLocalLightsShadowReceiverConstants(CommandBuffer cmd, ref ScriptableRenderContext context)
        {
            for (int i = 0; i < kMaxLocalPixelLightPerPass; ++i)
                m_LocalLightShadowMatrices[i] = m_LocalLightShadowSlices[i].shadowTransform;

            float invShadowResolution = 1.0f / m_LocalLightShadowAtlasResolution;
            float invHalfShadowResolution = 0.5f * invShadowResolution;

            cmd.SetGlobalTexture(m_AdditionalShadowmapRTID, m_AdditionalShadowmapRT);
            cmd.SetGlobalMatrixArray(LocalShadowConstantBuffer._LocalWorldToShadow, m_LocalLightShadowMatrices);
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowData, new Vector4(m_LocalShadowStrength[0], m_LocalShadowStrength[1], m_LocalShadowStrength[2], m_LocalShadowStrength[3]));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset0, new Vector4(-invHalfShadowResolution, -invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset1, new Vector4( invHalfShadowResolution, -invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset2, new Vector4(-invHalfShadowResolution,  invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowOffset3, new Vector4( invHalfShadowResolution,  invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(LocalShadowConstantBuffer._LocalShadowmapSize, new Vector4(invShadowResolution, invShadowResolution, m_LocalLightShadowAtlasResolution, m_LocalLightShadowAtlasResolution));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    };
};
