using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Serializable]
    public class ShadowSettings
    {
        public bool     enabled;
        public int      shadowAtlasWidth;
        public int      shadowAtlasHeight;

        public float    maxShadowDistance;
        public int      directionalLightCascadeCount;
        public Vector3  directionalLightCascades;
        public float    directionalLightNearPlaneOffset;

        static ShadowSettings defaultShadowSettings = null;

        public static ShadowSettings Default
        {
            get
            {
                if (defaultShadowSettings == null)
                {
                    defaultShadowSettings = new ShadowSettings();
                    defaultShadowSettings.enabled = true;
                    defaultShadowSettings.shadowAtlasHeight = defaultShadowSettings.shadowAtlasWidth = 4096;
                    defaultShadowSettings.directionalLightCascadeCount = 1;
                    defaultShadowSettings.directionalLightCascades = new Vector3(0.05F, 0.2F, 0.3F);
                    defaultShadowSettings.directionalLightCascadeCount = 4;
                    defaultShadowSettings.directionalLightNearPlaneOffset = 5;
                    defaultShadowSettings.maxShadowDistance = 1000.0F;
                }
                return defaultShadowSettings;
            }
        }
    }

    public struct ShadowSliceData
    {
        public Matrix4x4    shadowTransform;
        public int          atlasX;
        public int          atlasY;
        public int          shadowResolution;
    }

    public struct LightData
    {
        public int pixelLightsCount;
        public int vertexLightsCount;
        public int shadowLightIndex;
        public bool isSingleDirectionalLight;
        public bool shadowsRendered;
    }

    public class LightweightPipeline : RenderPipeline
    {
        private readonly LightweightPipelineAsset m_Asset;

        // Max amount of visible lights. This controls the lights constant buffers in shader but not the max shaded lights.
        // Lights are set per-object and the max shaded lights for each object are controlled by the max pixel lights in pipeline asset and kMaxVertexLights.
        private static readonly int kMaxVisibleLights = 16;
        private static readonly int kMaxPerObjectLights = 4;

        private Vector4[] m_LightPositions = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightColors = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightAttenuations = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightSpotDirections = new Vector4[kMaxVisibleLights];

        private Camera m_CurrCamera = null;

        private int m_LightIndicesCount = 0;
        private ComputeBuffer m_LightIndexListBuffer;

        private static readonly int kMaxCascades = 4;
        private int m_ShadowCasterCascadesCount = kMaxCascades;
        private int m_ShadowMapProperty;
        private int m_CameraRTProperty;
        private RenderTargetIdentifier m_ShadowMapRTID;
        private RenderTargetIdentifier m_CameraRTID;

        private bool m_RenderToIntermediateTarget = false;
        private bool m_IntermediateTextureArray = false;

        private const int kShadowDepthBufferBits = 16;
        private const int kCameraDepthBufferBits = 32;
        private Vector4[] m_DirectionalShadowSplitDistances = new Vector4[kMaxCascades];

        private ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        private ShadowSliceData[] m_ShadowSlices = new ShadowSliceData[kMaxCascades];

        private static readonly ShaderPassName m_LitPassName = new ShaderPassName("LightweightForward");
        private static readonly ShaderPassName m_UnlitPassName = new ShaderPassName("SRPDefaultUnlit");

        private RenderTextureFormat m_ColorFormat = RenderTextureFormat.ARGB32;
        private PostProcessRenderContext m_PostProcessRenderContext;

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            m_Asset = asset;

            BuildShadowSettings();
            m_ShadowMapProperty = Shader.PropertyToID("_ShadowMap");
            m_CameraRTProperty = Shader.PropertyToID("_CameraRT");
            m_ShadowMapRTID = new RenderTargetIdentifier(m_ShadowMapProperty);
            m_CameraRTID = new RenderTargetIdentifier(m_CameraRTProperty);
            m_PostProcessRenderContext = new PostProcessRenderContext();

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != m_Asset.MSAASampleCount)
                QualitySettings.antiAliasing = m_Asset.MSAASampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";
        }

        public override void Dispose()
        {
            base.Dispose();

            Shader.globalRenderPipeline = "";
            if (m_LightIndexListBuffer != null)
            {
                m_LightIndexListBuffer.Dispose();
                m_LightIndexListBuffer = null;
                m_LightIndicesCount = 0;
            }
        }

        CullResults m_CullResults;
        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);

            bool stereoEnabled = XRSettings.isDeviceActive;

            foreach (Camera camera in cameras)
            {
                m_CurrCamera = camera;

                ScriptableCullingParameters cullingParameters;
                if (!CullResults.GetCullingParameters(m_CurrCamera, stereoEnabled, out cullingParameters))
                    continue;

                cullingParameters.shadowDistance = Mathf.Min(m_ShadowSettings.maxShadowDistance, m_CurrCamera.farClipPlane);
                CullResults.Cull(ref cullingParameters, context,ref m_CullResults);

                VisibleLight[] visibleLights = m_CullResults.visibleLights.ToArray();

                LightData lightData;
                InitializeLightData(visibleLights, out lightData);

                // Render Shadow Map
                if (lightData.shadowLightIndex > -1)
                    lightData.shadowsRendered = RenderShadows(ref m_CullResults, ref visibleLights[lightData.shadowLightIndex], lightData.shadowLightIndex, ref context);

                // Setup camera matrices and RT
                context.SetupCameraProperties(m_CurrCamera, stereoEnabled);

                // Setup light and shadow shader constants
                SetupShaderLightConstants(visibleLights, ref lightData, ref m_CullResults, ref context);
                if (lightData.shadowsRendered)
                    SetupShadowShaderConstants(ref context, ref visibleLights[lightData.shadowLightIndex], lightData.shadowLightIndex, m_ShadowCasterCascadesCount);
                SetShaderKeywords(ref lightData, ref context);

                RendererConfiguration configuration = RendererConfiguration.PerObjectReflectionProbes;
                if (m_Asset.EnableLightmap)
                    configuration |= RendererConfiguration.PerObjectLightmaps;

                if (m_Asset.EnableAmbientProbe)
                    configuration |= RendererConfiguration.PerObjectLightProbe;

                if (!lightData.isSingleDirectionalLight)
                    configuration |= RendererConfiguration.PerObjectLightIndices8;


                PostProcessLayer postProcessLayer = GetCurrCameraPostProcessLayer();
                bool postProcessEnabled = postProcessLayer != null && postProcessLayer.enabled;
                m_RenderToIntermediateTarget = postProcessEnabled || GetRenderToIntermediateTarget();

                BeginForwardRendering(ref context, stereoEnabled);

                // Render Opaques
                var litSettings = new DrawRendererSettings(m_CullResults, m_CurrCamera, m_LitPassName);
                litSettings.sorting.flags = SortFlags.CommonOpaque;
                litSettings.inputFilter.SetQueuesOpaque();
                litSettings.rendererConfiguration = configuration;

                var unlitSettings = new DrawRendererSettings(m_CullResults, m_CurrCamera, m_UnlitPassName);
                unlitSettings.sorting.flags = SortFlags.CommonTransparent;
                unlitSettings.inputFilter.SetQueuesTransparent();

                context.DrawRenderers(ref litSettings);

                // TODO: Check skybox shader
                context.DrawSkybox(m_CurrCamera);

                // Render Alpha blended
                litSettings.sorting.flags = SortFlags.CommonTransparent;
                litSettings.inputFilter.SetQueuesTransparent();

                RenderStateBlock renderStateBlock = new RenderStateBlock();
                context.DrawRenderers(ref litSettings, renderStateBlock);

                context.DrawRenderers(ref litSettings);
                context.DrawRenderers(ref unlitSettings);

                if (postProcessEnabled)
                    RenderPostProcess(ref context, postProcessLayer);

                EndForwardRendering(ref context, stereoEnabled, postProcessEnabled);
                
                // Release temporary RT
                var discardRT = CommandBufferPool.Get();
                discardRT.ReleaseTemporaryRT(m_ShadowMapProperty);
                discardRT.ReleaseTemporaryRT(m_CameraRTProperty);
                context.ExecuteCommandBuffer(discardRT);
                CommandBufferPool.Release(discardRT);
            }

            context.Submit();
        }

        private void BuildShadowSettings()
        {
            m_ShadowSettings = ShadowSettings.Default;
            m_ShadowSettings.directionalLightCascadeCount = m_Asset.CascadeCount;

            m_ShadowSettings.shadowAtlasWidth = m_Asset.ShadowAtlasResolution;
            m_ShadowSettings.shadowAtlasHeight = m_Asset.ShadowAtlasResolution;
            m_ShadowSettings.maxShadowDistance = m_Asset.ShadowDistance;

            switch (m_ShadowSettings.directionalLightCascadeCount)
            {
                case 1:
                    m_ShadowSettings.directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
                    break;

                case 2:
                    m_ShadowSettings.directionalLightCascades = new Vector3(m_Asset.Cascade2Split, 1.0f, 0.0f);
                    break;

                default:
                    m_ShadowSettings.directionalLightCascades = m_Asset.Cascade4Split;
                    break;
            }
        }

        private void InitializeLightData(VisibleLight[] lights, out LightData lightData)
        {
            for (int i = 0; i < kMaxVisibleLights; ++i)
            {
                m_LightPositions[i] = Vector4.zero;
                m_LightColors[i] = Vector4.zero;
                m_LightAttenuations[i] = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
                m_LightSpotDirections[i] = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
            }

            int lightsCount = lights.Length;
            int maxPerPixelLights = Math.Min(m_Asset.MaxSupportedPixelLights, kMaxPerObjectLights);
            lightData.pixelLightsCount = Math.Min(lightsCount, maxPerPixelLights);
            lightData.vertexLightsCount = (m_Asset.SupportsVertexLight) ? Math.Min(lightsCount - lightData.pixelLightsCount, kMaxPerObjectLights) : 0;
            lightData.isSingleDirectionalLight = lightData.pixelLightsCount == 1 && lightData.vertexLightsCount == 0 && lights[0].lightType == LightType.Directional;

            // Directional light path can handle unlit.
            if (lightsCount == 0)
                lightData.isSingleDirectionalLight = true;

            lightData.shadowsRendered = false;

            InitializeMainShadowLightIndex(lights, out lightData.shadowLightIndex);
        }

        private void SetupShaderLightConstants(VisibleLight[] lights, ref LightData lightData, ref CullResults cullResults, ref ScriptableRenderContext context)
        {
            if (lights.Length == 0)
                return;

            if (lightData.isSingleDirectionalLight)
                SetupShaderSingleDirectionalLightConstants(ref lights [0], ref context);
            else
                SetupShaderLightListConstants(lights, ref lightData, ref context);
        }

        private void SetupShaderSingleDirectionalLightConstants(ref VisibleLight light, ref ScriptableRenderContext context)
        {
            Vector4 lightDir = -light.localToWorld.GetColumn(2);

            CommandBuffer cmd = new CommandBuffer() { name = "SetupLightConstants" };
            cmd.SetGlobalVector("_LightPosition0", new Vector4(lightDir.x, lightDir.y, lightDir.z, 0.0f));
            cmd.SetGlobalColor("_LightColor0", light.finalColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        private void SetupShaderLightListConstants(VisibleLight[] lights, ref LightData lightData, ref ScriptableRenderContext context)
        {
            int maxLights = Math.Min(kMaxVisibleLights, lights.Length);

            for (int i = 0; i < maxLights; ++i)
            {
                VisibleLight currLight = lights [i];
                if (currLight.lightType == LightType.Directional)
                {
                    Vector4 dir = -currLight.localToWorld.GetColumn (2);
                    m_LightPositions [i] = new Vector4 (dir.x, dir.y, dir.z, 0.0f);
                }
                else
                {
                    Vector4 pos = currLight.localToWorld.GetColumn (3);
                    m_LightPositions [i] = new Vector4 (pos.x, pos.y, pos.z, 1.0f);
                }

                m_LightColors[i] = currLight.finalColor;

                float rangeSq = currLight.range * currLight.range;
                float quadAtten = (currLight.lightType == LightType.Directional) ? 0.0f : 25.0f / rangeSq;

                if (currLight.lightType == LightType.Spot)
                {
                    Vector4 dir = currLight.localToWorld.GetColumn (2);
                    m_LightSpotDirections [i] = new Vector4 (-dir.x, -dir.y, -dir.z, 0.0f);

                    float spotAngle = Mathf.Deg2Rad * currLight.spotAngle;
                    float cosOuterAngle = Mathf.Cos (spotAngle * 0.5f);
                    float cosInneAngle = Mathf.Cos (spotAngle * 0.25f);
                    float angleRange = cosInneAngle - cosOuterAngle;
                    m_LightAttenuations [i] = new Vector4 (cosOuterAngle,
                        Mathf.Approximately (angleRange, 0.0f) ? 1.0f : angleRange, quadAtten, rangeSq);
                }
                else
                {
                    m_LightSpotDirections [i] = new Vector4 (0.0f, 0.0f, 1.0f, 0.0f);
                    m_LightAttenuations [i] = new Vector4 (-1.0f, 1.0f, quadAtten, rangeSq);
                }
            }

            // Lightweight pipeline only upload kMaxVisibleLights to shader cbuffer.
            // We tell the pipe to disable remaining lights by setting it to -1.
            int[] lightIndexMap = m_CullResults.GetLightIndexMap();
            for (int i = kMaxVisibleLights; i < lightIndexMap.Length; ++i)
                lightIndexMap[i] = -1;
            m_CullResults.SetLightIndexMap(lightIndexMap);

            CommandBuffer cmd = CommandBufferPool.Get("SetupShadowShaderConstants");
            cmd.SetGlobalVector("globalLightCount", new Vector4 (lightData.pixelLightsCount, lightData.vertexLightsCount, 0.0f, 0.0f));
            cmd.SetGlobalVectorArray ("globalLightPos", m_LightPositions);
            cmd.SetGlobalVectorArray ("globalLightColor", m_LightColors);
            cmd.SetGlobalVectorArray ("globalLightAtten", m_LightAttenuations);
            cmd.SetGlobalVectorArray ("globalLightSpotDir", m_LightSpotDirections);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetShaderKeywords(ref LightData lightData, ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = new CommandBuffer() { name = "SetShaderKeywords" };
            SetShaderKeywords(cmd, lightData.shadowsRendered, lightData.isSingleDirectionalLight, lightData.vertexLightsCount > 0);
            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        private bool RenderShadows(ref CullResults cullResults, ref VisibleLight shadowLight, int shadowLightIndex, ref ScriptableRenderContext context)
        {
            m_ShadowCasterCascadesCount = m_ShadowSettings.directionalLightCascadeCount;

            if (shadowLight.lightType == LightType.Spot)
                m_ShadowCasterCascadesCount = 1;

            int shadowResolution = GetMaxTileResolutionInAtlas(m_ShadowSettings.shadowAtlasWidth, m_ShadowSettings.shadowAtlasHeight, m_ShadowCasterCascadesCount);

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return false;

            var setRenderTargetCommandBuffer = CommandBufferPool.Get();
            setRenderTargetCommandBuffer.name = "Render packed shadows";
            setRenderTargetCommandBuffer.GetTemporaryRT(m_ShadowMapProperty, m_ShadowSettings.shadowAtlasWidth,
                m_ShadowSettings.shadowAtlasHeight, kShadowDepthBufferBits, FilterMode.Bilinear, RenderTextureFormat.Depth);
            setRenderTargetCommandBuffer.SetRenderTarget(m_ShadowMapRTID);
            setRenderTargetCommandBuffer.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(setRenderTargetCommandBuffer);
            CommandBufferPool.Release(setRenderTargetCommandBuffer);

            float shadowNearPlane = m_Asset.ShadowNearOffset;
            Vector3 splitRatio = m_ShadowSettings.directionalLightCascades;

            Matrix4x4 view, proj;
            var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);
            bool needRendering = false;

            if (shadowLight.lightType == LightType.Spot)
            {
                needRendering = cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowLightIndex, out view, out proj,
                        out settings.splitData);

                if (!needRendering)
                    return false;

                SetupShadowSliceTransform(0, shadowResolution, proj, view);
                RenderShadowSlice(ref context, 0, proj, view, settings);
            }
            else if (shadowLight.lightType == LightType.Directional)
            {
                for (int cascadeIdx = 0; cascadeIdx < m_ShadowCasterCascadesCount; ++cascadeIdx)
                {
                    needRendering = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex,
                            cascadeIdx, m_ShadowCasterCascadesCount, splitRatio, shadowResolution, shadowNearPlane, out view, out proj,
                            out settings.splitData);

                    m_DirectionalShadowSplitDistances[cascadeIdx] = settings.splitData.cullingSphere;
                    m_DirectionalShadowSplitDistances[cascadeIdx].w *= settings.splitData.cullingSphere.w;

                    if (!needRendering)
                        return false;

                    SetupShadowSliceTransform(cascadeIdx, shadowResolution, proj, view);
                    RenderShadowSlice(ref context, cascadeIdx, proj, view, settings);
                }
            }
            else
            {
                Debug.LogWarning("Only spot and directional shadow casters are supported in lightweight pipeline");
                return false;
            }

            return true;
        }

        private void SetupShadowSliceTransform(int cascadeIndex, int shadowResolution, Matrix4x4 proj, Matrix4x4 view)
        {
            // Assumes MAX_CASCADES = 4
            m_ShadowSlices[cascadeIndex].atlasX = (cascadeIndex % 2) * shadowResolution;
            m_ShadowSlices[cascadeIndex].atlasY = (cascadeIndex / 2) * shadowResolution;
            m_ShadowSlices[cascadeIndex].shadowResolution = shadowResolution;
            m_ShadowSlices[cascadeIndex].shadowTransform = Matrix4x4.identity;

            var matScaleBias = Matrix4x4.identity;
            matScaleBias.m00 = 0.5f;
            matScaleBias.m11 = 0.5f;
            matScaleBias.m22 = 0.5f;
            matScaleBias.m03 = 0.5f;
            matScaleBias.m23 = 0.5f;
            matScaleBias.m13 = 0.5f;

            // Later down the pipeline the proj matrix will be scaled to reverse-z in case of DX.
            // We need account for that scale in the shadowTransform.
            if (SystemInfo.usesReversedZBuffer)
                matScaleBias.m22 = -0.5f;

            var matTile = Matrix4x4.identity;
            matTile.m00 = (float)m_ShadowSlices[cascadeIndex].shadowResolution /
                (float)m_ShadowSettings.shadowAtlasWidth;
            matTile.m11 = (float)m_ShadowSlices[cascadeIndex].shadowResolution /
                (float)m_ShadowSettings.shadowAtlasHeight;
            matTile.m03 = (float)m_ShadowSlices[cascadeIndex].atlasX / (float)m_ShadowSettings.shadowAtlasWidth;
            matTile.m13 = (float)m_ShadowSlices[cascadeIndex].atlasY / (float)m_ShadowSettings.shadowAtlasHeight;

            m_ShadowSlices[cascadeIndex].shadowTransform = matTile * matScaleBias * proj * view;
        }

        private void RenderShadowSlice(ref ScriptableRenderContext context, int cascadeIndex,
            Matrix4x4 proj, Matrix4x4 view, DrawShadowsSettings settings)
        {
            var buffer = CommandBufferPool.Get("Prepare Shadowmap Slice");
            buffer.SetViewport(new Rect(m_ShadowSlices[cascadeIndex].atlasX, m_ShadowSlices[cascadeIndex].atlasY,
                    m_ShadowSlices[cascadeIndex].shadowResolution, m_ShadowSlices[cascadeIndex].shadowResolution));
            buffer.SetViewProjectionMatrices(view, proj);
            context.ExecuteCommandBuffer(buffer);

            context.DrawShadows(ref settings);
            CommandBufferPool.Release(buffer);
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

        private void SetupShadowShaderConstants(ref ScriptableRenderContext context, ref VisibleLight shadowLight, int shadowLightIndex, int cascadeCount)
        {
            Vector3 shadowLightDir = Vector3.Normalize(shadowLight.localToWorld.GetColumn(2));

            // TODO: multiplying by 0.1 to get similar results to Unity vanilla shadow bias
            float bias = shadowLight.light.shadowBias * 0.1f;
            float normalBias = shadowLight.light.shadowNormalBias;
            float shadowResolution = m_ShadowSlices[0].shadowResolution;

            const int maxShadowCascades = 4;
            Matrix4x4[] shadowMatrices = new Matrix4x4[maxShadowCascades];
            for (int i = 0; i < cascadeCount; ++i)
                shadowMatrices[i] = (cascadeCount >= i) ? m_ShadowSlices[i].shadowTransform : Matrix4x4.identity;

            // TODO: shadow resolution per cascade in case cascades endup being supported.
            float invShadowResolution = 1.0f / shadowResolution;
            float[] pcfKernel =
            {
                -0.5f * invShadowResolution, 0.5f * invShadowResolution,
                0.5f * invShadowResolution, 0.5f * invShadowResolution,
                -0.5f * invShadowResolution, -0.5f * invShadowResolution,
                0.5f * invShadowResolution, -0.5f * invShadowResolution
            };

            var setupShadow = CommandBufferPool.Get("SetupShadowShaderConstants");
            setupShadow.SetGlobalMatrixArray("_WorldToShadow", shadowMatrices);
            setupShadow.SetGlobalVectorArray("_DirShadowSplitSpheres", m_DirectionalShadowSplitDistances);
            setupShadow.SetGlobalVector("_ShadowLightDirection", new Vector4(-shadowLightDir.x, -shadowLightDir.y, -shadowLightDir.z, 0.0f));
            setupShadow.SetGlobalVector("_ShadowData", new Vector4(shadowLightIndex, bias, normalBias, 0.0f));
            setupShadow.SetGlobalFloatArray("_PCFKernel", pcfKernel);
            context.ExecuteCommandBuffer(setupShadow);
            CommandBufferPool.Release(setupShadow);
        }

        private void SetShaderKeywords(CommandBuffer cmd, bool renderShadows, bool singleDirecitonal, bool vertexLightSupport)
        {
            if (m_Asset.ForceLinearRendering)
                cmd.EnableShaderKeyword("LIGHTWEIGHT_LINEAR");
            else
                cmd.DisableShaderKeyword("LIGHTWEIGHT_LINEAR");

            if (vertexLightSupport)
                cmd.EnableShaderKeyword("_VERTEX_LIGHTS");
            else
                cmd.DisableShaderKeyword("_VERTEX_LIGHTS");

            if (singleDirecitonal)
                cmd.EnableShaderKeyword("_SINGLE_DIRECTIONAL_LIGHT");
            else
                cmd.DisableShaderKeyword("_SINGLE_DIRECTIONAL_LIGHT");

            string[] shadowKeywords = new string[] { "_HARD_SHADOWS", "_SOFT_SHADOWS", "_HARD_SHADOWS_CASCADES", "_SOFT_SHADOWS_CASCADES" };
            for (int i = 0; i < shadowKeywords.Length; ++i)
                cmd.DisableShaderKeyword(shadowKeywords[i]);

            if (renderShadows && m_Asset.CurrShadowType != ShadowType.NO_SHADOW)
            {
                int keywordIndex = (int)m_Asset.CurrShadowType - 1;
                if (m_Asset.CascadeCount > 1)
                    keywordIndex += 2;
                cmd.EnableShaderKeyword(shadowKeywords[keywordIndex]);
            }

            if (m_Asset.EnableAmbientProbe)
                cmd.EnableShaderKeyword("_LIGHT_PROBES_ON");
            else
                cmd.DisableShaderKeyword("_LIGHT_PROBES_ON");
        }

        private void InitializeMainShadowLightIndex(VisibleLight[] lights, out int shadowIndex)
        {
            shadowIndex = -1;
            if (m_Asset.CurrShadowType == ShadowType.NO_SHADOW)
                return;

            float maxIntensity = -1;
            for (int i = 0; i < lights.Length; ++i)
            {
                Light light = lights[i].light;
                if (light.shadows != LightShadows.None && IsSupportedShadowType(light.type) && light.intensity > maxIntensity)
                {
                    shadowIndex = i;
                    maxIntensity = light.intensity;
                }
            }
        }

        private bool IsSupportedShadowType(LightType type)
        {
            return (type == LightType.Directional || type == LightType.Spot);
        }

        private void BeginForwardRendering(ref ScriptableRenderContext context, bool stereoEnabled)
        {
            if (stereoEnabled)
                context.StartMultiEye(m_CurrCamera);

            var cmd = CommandBufferPool.Get("SetCameraRenderTarget");
            if (m_RenderToIntermediateTarget)
            {
                if (m_CurrCamera.activeTexture == null)
                {
                    m_IntermediateTextureArray = false;
                    if (stereoEnabled)
                    {
                        RenderTextureDescriptor xrDesc = XRSettings.eyeTextureDesc;
                        xrDesc.depthBufferBits = kCameraDepthBufferBits;
                        xrDesc.colorFormat = m_ColorFormat;
                        xrDesc.msaaSamples = m_Asset.MSAASampleCount;

                        m_IntermediateTextureArray = (xrDesc.dimension == TextureDimension.Tex2DArray);

                        cmd.GetTemporaryRT(m_CameraRTProperty, xrDesc, FilterMode.Bilinear);
                    }
                    else
                    {
                        cmd.GetTemporaryRT(m_CameraRTProperty, Screen.width, Screen.height, kCameraDepthBufferBits,
                            FilterMode.Bilinear, m_ColorFormat, RenderTextureReadWrite.Default, m_Asset.MSAASampleCount);
                    }

                    if (m_IntermediateTextureArray)
                        cmd.SetRenderTarget(m_CameraRTID, 0, CubemapFace.Unknown, -1);
                    else
                        cmd.SetRenderTarget(m_CameraRTID);
                }
                else
                {
                    cmd.SetRenderTarget(new RenderTargetIdentifier(m_CurrCamera.activeTexture));
                }
            }
            else
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CurrentActive);
            }

            // Clear RenderTarget to avoid tile initialization on mobile GPUs
            // https://community.arm.com/graphics/b/blog/posts/mali-performance-2-how-to-correctly-handle-framebuffers
            if (m_CurrCamera.clearFlags != CameraClearFlags.Nothing)
            {
                bool clearDepth = (m_CurrCamera.clearFlags != CameraClearFlags.Nothing);
                bool clearColor = (m_CurrCamera.clearFlags == CameraClearFlags.Color);
                cmd.ClearRenderTarget(clearDepth, clearColor, m_CurrCamera.backgroundColor);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void EndForwardRendering(ref ScriptableRenderContext context, bool stereoEnabled, bool postProcessing)
        {

            if (m_RenderToIntermediateTarget || postProcessing)
            {
                var cmd = CommandBufferPool.Get("Blit");
                if (m_IntermediateTextureArray)
                {
                    cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
                    cmd.Blit(m_CameraRTID, BuiltinRenderTextureType.CurrentActive);
                }
                // If PostProcessing is enabled, it is already blitted to CameraTarget.
                else if (!postProcessing)
                    cmd.Blit(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CameraTarget);

                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            if (stereoEnabled)
            {
                context.StopMultiEye(m_CurrCamera);
                context.StereoEndRender(m_CurrCamera);
            }
        }

        private void RenderPostProcess(ref ScriptableRenderContext renderContext, PostProcessLayer postProcessLayer)
        {
            var postProcessCommand = CommandBufferPool.Get("Post Processing");
            m_PostProcessRenderContext.Reset();
            m_PostProcessRenderContext.camera = m_CurrCamera;
            m_PostProcessRenderContext.source = BuiltinRenderTextureType.CurrentActive;
            m_PostProcessRenderContext.sourceFormat = m_ColorFormat;
            m_PostProcessRenderContext.destination = BuiltinRenderTextureType.CameraTarget;
            m_PostProcessRenderContext.command = postProcessCommand;
            m_PostProcessRenderContext.flip = true;

            postProcessLayer.Render(m_PostProcessRenderContext);
            renderContext.ExecuteCommandBuffer(postProcessCommand);
            CommandBufferPool.Release(postProcessCommand);
        }

        private bool GetRenderToIntermediateTarget()
        {
            bool allowMSAA = m_CurrCamera.allowMSAA && m_Asset.MSAASampleCount > 1 && !PlatformSupportsMSAABackBuffer();
            if (m_CurrCamera.cameraType == CameraType.SceneView || allowMSAA || m_CurrCamera.activeTexture != null)
                return true;

            return false;
        }

        private PostProcessLayer GetCurrCameraPostProcessLayer()
        {
            return m_CurrCamera.GetComponent<PostProcessLayer>();
        }

        private bool PlatformSupportsMSAABackBuffer()
        {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SAMSUNGTV
            return true;
#else
            return false;
#endif
        }
    }
}
