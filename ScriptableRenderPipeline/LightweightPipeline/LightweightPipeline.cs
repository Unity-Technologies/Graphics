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
        public bool isSingleLight;
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
        private LightType m_SingleLightType = LightType.Directional;

        private int m_LightIndicesCount = 0;
        private ComputeBuffer m_LightIndexListBuffer;

        private static readonly int kMaxCascades = 4;
        private int m_ShadowCasterCascadesCount = kMaxCascades;
        private int m_ShadowMapTexture;
        private int m_CameraColorTexture;
        private int m_CameraDepthTexture;
        private RenderTargetIdentifier m_ShadowMapRT;
        private RenderTargetIdentifier m_CameraColorRT;
        private RenderTargetIdentifier m_CameraDepthRT;

        private bool m_RenderToIntermediateTarget = false;
        private bool m_IntermediateTextureArray = false;

        private const int kShadowDepthBufferBits = 16;
        private const int kCameraDepthBufferBits = 32;
        private Vector4[] m_DirectionalShadowSplitDistances = new Vector4[kMaxCascades];

        private ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        private ShadowSliceData[] m_ShadowSlices = new ShadowSliceData[kMaxCascades];

        private static readonly ShaderPassName m_LitPassName = new ShaderPassName("LightweightForward");
        private static readonly ShaderPassName m_UnlitPassName = new ShaderPassName("SRPDefaultUnlit");

        private RenderTextureFormat m_ColorFormat;
        private PostProcessRenderContext m_PostProcessRenderContext;

        private CameraComparer m_CameraComparer = new CameraComparer();

        private Mesh m_BlitQuad = null;
        private Material m_BlitMaterial = null;
        private int m_BlitTexID = Shader.PropertyToID("_BlitTex");

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            m_Asset = asset;

            BuildShadowSettings();
            m_ShadowMapTexture = Shader.PropertyToID("_ShadowMap");
            m_CameraColorTexture = Shader.PropertyToID("_CameraRT");
            m_CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");

            m_ShadowMapRT = new RenderTargetIdentifier(m_ShadowMapTexture);
            m_CameraColorRT = new RenderTargetIdentifier(m_CameraColorTexture);
            m_CameraDepthRT = new RenderTargetIdentifier(m_CameraDepthTexture);
            m_PostProcessRenderContext = new PostProcessRenderContext();

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != m_Asset.MSAASampleCount)
                QualitySettings.antiAliasing = m_Asset.MSAASampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";

            m_BlitQuad = LightweightUtils.CreateQuadMesh(false);
            m_BlitMaterial = new Material(m_Asset.BlitShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
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

            // TODO: This is at the moment required for all pipes. We should not implicitly change user project settings
            // instead this should be forced when using SRP, since all SRP use linear lighting.
            GraphicsSettings.lightsUseLinearIntensity = true;

            Array.Sort(cameras, m_CameraComparer);
            foreach (Camera camera in cameras)
            {
                m_CurrCamera = camera;

                PostProcessLayer postProcessLayer;
                RenderingConfiguration renderingConfig = SetupRendering(out postProcessLayer);
                bool postProcessEnabled = LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.PostProcess);
                ScriptableCullingParameters cullingParameters;
                if (!CullResults.GetCullingParameters(m_CurrCamera, stereoEnabled, out cullingParameters))
                    continue;

                cullingParameters.shadowDistance = Mathf.Min(m_ShadowSettings.maxShadowDistance,
                    m_CurrCamera.farClipPlane);

                // emit scene view UI
                if (camera.cameraType == CameraType.SceneView)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);

                CullResults.Cull(ref cullingParameters, context, ref m_CullResults);

                VisibleLight[] visibleLights = m_CullResults.visibleLights.ToArray();

                LightData lightData;
                InitializeLightData(visibleLights, out lightData);

                // Render Shadow Map
                if (lightData.shadowLightIndex > -1)
                    lightData.shadowsRendered = RenderShadows(ref m_CullResults,
                        ref visibleLights[lightData.shadowLightIndex], lightData.shadowLightIndex, ref context);

                // Setup camera matrices
                context.SetupCameraProperties(m_CurrCamera, stereoEnabled);

                // Setup light and shadow shader constants
                SetupShaderLightConstants(visibleLights, ref lightData, ref m_CullResults, ref context);
                if (lightData.shadowsRendered)
                    SetupShadowShaderConstants(ref context, ref visibleLights[lightData.shadowLightIndex],
                        lightData.shadowLightIndex, m_ShadowCasterCascadesCount);
                SetShaderKeywords(ref lightData, ref context);

                RendererConfiguration rendererSettings = GetRendererSettings(ref lightData);

                BeginForwardRendering(ref context, renderingConfig);
                RenderOpaques(ref context, rendererSettings);
                context.DrawSkybox(m_CurrCamera);

                if (postProcessEnabled)
                    RenderPostProcess(ref context, postProcessLayer, true);

                RenderTransparents(ref context, rendererSettings);

                if (postProcessEnabled)
                    RenderPostProcess(ref context, postProcessLayer, false);

                EndForwardRendering(ref context, renderingConfig);

                // Release temporary RT
                var discardRT = CommandBufferPool.Get();
                discardRT.ReleaseTemporaryRT(m_ShadowMapTexture);
                discardRT.ReleaseTemporaryRT(m_CameraColorTexture);
                discardRT.ReleaseTemporaryRT(m_CameraDepthTexture);
                context.ExecuteCommandBuffer(discardRT);
                CommandBufferPool.Release(discardRT);

                context.Submit();
            }
        }

        private void RenderOpaques(ref ScriptableRenderContext context, RendererConfiguration settings)
        {
            var opaqueDrawSettings = new DrawRendererSettings(m_CurrCamera, m_LitPassName);
            opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;
            opaqueDrawSettings.rendererConfiguration = settings;

            var opaqueFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };

            context.DrawRenderers(m_CullResults.visibleRenderers, ref opaqueDrawSettings, opaqueFilterSettings);
        }

        private void RenderTransparents(ref ScriptableRenderContext context, RendererConfiguration settings)
        {
            var transparentSettings = new DrawRendererSettings(m_CurrCamera, m_LitPassName);
            transparentSettings.SetShaderPassName(1, m_UnlitPassName);
            transparentSettings.sorting.flags = SortFlags.CommonTransparent;
            transparentSettings.rendererConfiguration = settings;

            var transparentFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.transparent
            };

            context.DrawRenderers(m_CullResults.visibleRenderers, ref transparentSettings, transparentFilterSettings);

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

        private RenderingConfiguration SetupRendering(out PostProcessLayer postProcessLayer)
        {
            RenderingConfiguration renderingConfig = (XRSettings.enabled) ? RenderingConfiguration.Stereo : RenderingConfiguration.None;
            if (XRSettings.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
                renderingConfig |= RenderingConfiguration.IntermediateTextureArray;

            m_ColorFormat = m_CurrCamera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

            postProcessLayer = m_CurrCamera.GetComponent<PostProcessLayer>();
            bool intermediateTexture = false;
            if (postProcessLayer != null && postProcessLayer.enabled)
            {
                renderingConfig |= RenderingConfiguration.PostProcess;
                intermediateTexture = true;
            }
            // When post process is enabled we disable msaa due to lack of depth resolve
            // One can still use PostFX AA
            else if (m_CurrCamera.allowMSAA && m_Asset.MSAASampleCount > 1)
            {
                renderingConfig |= RenderingConfiguration.Msaa;
                intermediateTexture = !LightweightUtils.PlatformSupportsMSAABackBuffer();
            }

            Rect cameraRect = m_CurrCamera.rect;
            if (cameraRect.x > 0.0f || cameraRect.y > 0.0f || cameraRect.width < 1.0f || cameraRect.height < 1.0f)
                intermediateTexture = true;
            else
                renderingConfig |= RenderingConfiguration.DefaultViewport;

            intermediateTexture |= (m_CurrCamera.targetTexture != null || m_Asset.RenderScale < 1.0f || m_CurrCamera.allowHDR);
            if (intermediateTexture && !LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.IntermediateTextureArray))
                renderingConfig |= RenderingConfiguration.IntermediateTexture;

            return renderingConfig;
        }

        private void InitializeLightData(VisibleLight[] lights, out LightData lightData)
        {
            int lightsCount = lights.Length;
            int maxPerPixelLights = Math.Min(m_Asset.MaxSupportedPixelLights, kMaxPerObjectLights);
            lightData.pixelLightsCount = Math.Min(lightsCount, maxPerPixelLights);
            lightData.vertexLightsCount = (m_Asset.SupportsVertexLight) ? Math.Min(lightsCount - lightData.pixelLightsCount, kMaxPerObjectLights) : 0;

            // TODO: Handle Vertex lights in this case
            lightData.isSingleLight = lightData.pixelLightsCount <= 1;
            if (lightData.isSingleLight)
                m_SingleLightType = (lightData.pixelLightsCount == 1) ? lights[0].lightType : LightType.Directional;

            lightData.shadowsRendered = false;

            InitializeMainShadowLightIndex(lights, out lightData.shadowLightIndex);
        }

        private void InitializeLightConstants(VisibleLight[] lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightSpotDir,
            out Vector4 lightAttenuationParams)
        {
            lightPos = Vector4.zero;
            lightColor = Color.black;
            lightAttenuationParams = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
            lightSpotDir = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

            // When no lights are available in the pipeline or maxPixelLights is set to 0
            // In this case we want to initialize the lightData to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight light = lights[lightIndex];
            if (light.lightType == LightType.Directional)
            {
                Vector4 dir = -light.localToWorld.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = light.localToWorld.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            lightColor = light.finalColor;

            float rangeSq = light.range * light.range;
            float quadAtten = 0.0f;
            if (light.lightType != LightType.Directional)
                quadAtten = (m_Asset.AttenuationTexture != null) ? 1.0f : 25.0f / rangeSq;

            if (light.lightType == LightType.Spot)
            {
                Vector4 dir = light.localToWorld.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                float spotAngle = Mathf.Deg2Rad * light.spotAngle;
                float cosOuterAngle = Mathf.Cos(spotAngle * 0.5f);
                float cosInneAngle = Mathf.Cos(spotAngle * 0.25f);
                float angleRange = cosInneAngle - cosOuterAngle;
                lightAttenuationParams = new Vector4(cosOuterAngle,
                    Mathf.Approximately(angleRange, 0.0f) ? 1.0f : angleRange, quadAtten, rangeSq);
            }
            else
            {
                lightSpotDir = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
                lightAttenuationParams = new Vector4(-1.0f, 1.0f, quadAtten, rangeSq);
            }
        }

        private void SetupShaderLightConstants(VisibleLight[] lights, ref LightData lightData, ref CullResults cullResults, ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SetupSingleLightConstants");

            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Vector4 glossyEnvColor = new Vector4(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;

            cmd.SetGlobalVector("_GlossyEnvironmentColor", glossyEnvColor);
            if (m_Asset.AttenuationTexture != null) cmd.SetGlobalTexture("_AttenuationTexture", m_Asset.AttenuationTexture);

            if (lightData.isSingleLight)
                SetupShaderSingleLightConstants(cmd, lights, (lightData.pixelLightsCount > 0) ? 0 : -1, ref context);
            else
                SetupShaderLightListConstants(cmd, lights, ref lightData, ref context);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetupShaderSingleLightConstants(CommandBuffer cmd, VisibleLight[] lights, int lightIndex, ref ScriptableRenderContext context)
        {
            Vector4 lightPos, lightColor, lightSpotDir, lightAttenuationParams;
            InitializeLightConstants(lights, lightIndex, out lightPos, out lightColor, out lightSpotDir, out lightAttenuationParams);

            cmd.SetGlobalVector("_LightPosition", lightPos);
            cmd.SetGlobalColor("_LightColor", lightColor);
            cmd.SetGlobalVector("_LightSpotDir", lightSpotDir);
            cmd.SetGlobalVector("_LightAttenuationParams", lightAttenuationParams);
        }

        private void SetupShaderLightListConstants(CommandBuffer cmd, VisibleLight[] lights, ref LightData lightData, ref ScriptableRenderContext context)
        {
            int maxLights = Math.Min(kMaxVisibleLights, lights.Length);

            for (int i = 0; i < maxLights; ++i)
                InitializeLightConstants(lights, i, out m_LightPositions[i], out m_LightColors[i], out m_LightSpotDirections[i], out m_LightAttenuations[i]);

            // Lightweight pipeline only upload kMaxVisibleLights to shader cbuffer.
            // We tell the pipe to disable remaining lights by setting it to -1.
            int[] lightIndexMap = m_CullResults.GetLightIndexMap();
            for (int i = kMaxVisibleLights; i < lightIndexMap.Length; ++i)
                lightIndexMap[i] = -1;
            m_CullResults.SetLightIndexMap(lightIndexMap);

            cmd.SetGlobalVector("globalLightCount", new Vector4 (lightData.pixelLightsCount, lightData.vertexLightsCount, 0.0f, 0.0f));
            cmd.SetGlobalVectorArray ("globalLightPos", m_LightPositions);
            cmd.SetGlobalVectorArray ("globalLightColor", m_LightColors);
            cmd.SetGlobalVectorArray ("globalLightAtten", m_LightAttenuations);
            cmd.SetGlobalVectorArray ("globalLightSpotDir", m_LightSpotDirections);
        }

        private void SetShaderKeywords(ref LightData lightData, ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = new CommandBuffer() { name = "SetShaderKeywords" };
            SetShaderKeywords(cmd, lightData.shadowsRendered, lightData.isSingleLight, lightData.vertexLightsCount > 0);
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
            setRenderTargetCommandBuffer.GetTemporaryRT(m_ShadowMapTexture, m_ShadowSettings.shadowAtlasWidth,
                m_ShadowSettings.shadowAtlasHeight, kShadowDepthBufferBits, FilterMode.Bilinear, RenderTextureFormat.Depth);
            setRenderTargetCommandBuffer.SetRenderTarget(m_ShadowMapRT);
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

        private void SetShaderKeywords(CommandBuffer cmd, bool renderShadows, bool singleLight, bool vertexLightSupport)
        {
            LightweightUtils.SetKeyword(cmd, "_LIGHTWEIGHT_FORCE_LINEAR", m_Asset.ForceLinearRendering);
            LightweightUtils.SetKeyword(cmd, "_VERTEX_LIGHTS", vertexLightSupport);
            LightweightUtils.SetKeyword(cmd, "_ATTENUATION_TEXTURE", m_Asset.AttenuationTexture != null);

            if (!singleLight)
            {
                LightweightUtils.SetKeyword(cmd, "_SINGLE_DIRECTIONAL_LIGHT", false);
                LightweightUtils.SetKeyword(cmd, "_SINGLE_SPOT_LIGHT", false);
                LightweightUtils.SetKeyword(cmd, "_SINGLE_POINT_LIGHT", false);
            }
            else
            {
                switch (m_SingleLightType)
                {
                        case LightType.Directional:
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_DIRECTIONAL_LIGHT", true);
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_SPOT_LIGHT", false);
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_POINT_LIGHT", false);
                        break;

                        case LightType.Spot:
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_DIRECTIONAL_LIGHT", false);
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_SPOT_LIGHT", true);
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_POINT_LIGHT", false);
                        break;

                        case LightType.Point:
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_DIRECTIONAL_LIGHT", false);
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_SPOT_LIGHT", false);
                        LightweightUtils.SetKeyword(cmd, "_SINGLE_POINT_LIGHT", true);
                        break;
                }
            }

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

        private void BeginForwardRendering(ref ScriptableRenderContext context, RenderingConfiguration renderingConfig)
        {
            RenderTargetIdentifier colorRT = BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier depthRT = BuiltinRenderTextureType.None;

            // When postprocess is enabled, msaa is forced to be disabled due to lack of depth resolve.
            int msaaSamples = (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.Msaa)) ? m_Asset.MSAASampleCount : 1;

            var cmd = CommandBufferPool.Get("SetCameraRenderTarget");
            if (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.IntermediateTexture))
            {
                int rtWidth = (int)((float)m_CurrCamera.pixelWidth * m_Asset.RenderScale);
                int rtHeight = (int)((float)m_CurrCamera.pixelHeight * m_Asset.RenderScale);

                if (m_CurrCamera.targetTexture == null)
                {
                    RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
                    if (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.Stereo))
                    {
                        context.StartMultiEye(m_CurrCamera);
                        rtDesc = XRSettings.eyeTextureDesc;
                        rtDesc.colorFormat = m_ColorFormat;
                        rtDesc.msaaSamples = msaaSamples;

                        cmd.GetTemporaryRT(m_CameraColorTexture, rtDesc, FilterMode.Bilinear);
                    }
                    else
                    {
                        cmd.GetTemporaryRT(m_CameraColorTexture, rtWidth, rtHeight, kCameraDepthBufferBits,
                            FilterMode.Bilinear, m_ColorFormat, RenderTextureReadWrite.Default, msaaSamples);
                    }

                    colorRT = m_CameraColorRT;
                }

                if (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.PostProcess))
                {
                    cmd.GetTemporaryRT(m_CameraDepthTexture, rtWidth, rtHeight, kCameraDepthBufferBits, FilterMode.Bilinear, RenderTextureFormat.Depth);
                    depthRT = m_CameraDepthRT;
                }
            }

            SetupRenderTargets(cmd, colorRT, depthRT);

            // Clear RenderTarget to avoid tile initialization on mobile GPUs
            // https://community.arm.com/graphics/b/blog/posts/mali-performance-2-how-to-correctly-handle-framebuffers
            if (m_CurrCamera.clearFlags != CameraClearFlags.Nothing)
            {
                bool clearDepth = (m_CurrCamera.clearFlags != CameraClearFlags.Nothing);
                bool clearColor = (m_CurrCamera.clearFlags == CameraClearFlags.Color || m_CurrCamera.clearFlags == CameraClearFlags.Skybox);
                cmd.ClearRenderTarget(clearDepth, clearColor, m_CurrCamera.backgroundColor.linear);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void EndForwardRendering(ref ScriptableRenderContext context, RenderingConfiguration renderingConfig)
        {
            // No additional rendering needs to be done if this is an offscren rendering camera
            if (m_CurrCamera.targetTexture != null)
                return;

            var cmd = CommandBufferPool.Get("Blit");
            if (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.IntermediateTextureArray))
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
                cmd.Blit(m_CameraColorRT, BuiltinRenderTextureType.CurrentActive);
            }
            else if (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.IntermediateTexture))
            {
                // If PostProcessing is enabled, it is already blitted to CameraTarget.
                if (!LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.PostProcess))
                    Blit(cmd, renderingConfig, m_CameraColorRT, BuiltinRenderTextureType.CameraTarget);
            }

            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.Stereo))
            {
                context.StopMultiEye(m_CurrCamera);
                context.StereoEndRender(m_CurrCamera);
            }
        }

        RendererConfiguration GetRendererSettings(ref LightData lightData)
        {
            RendererConfiguration settings = RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
            if (!lightData.isSingleLight)
                settings |= RendererConfiguration.PerObjectLightIndices8;
            return settings;
        }

        private void SetupRenderTargets(CommandBuffer cmd, RenderTargetIdentifier colorRT, RenderTargetIdentifier depthRT)
        {
            if (m_IntermediateTextureArray)
            {
                if (depthRT != BuiltinRenderTextureType.None)
                    cmd.SetRenderTarget(colorRT, 0, CubemapFace.Unknown, -1);
                else
                    cmd.SetRenderTarget(colorRT, depthRT, 0, CubemapFace.Unknown, -1);
            }
            else
            {
                if (depthRT != BuiltinRenderTextureType.None)
                    cmd.SetRenderTarget(colorRT, depthRT);
                else
                    cmd.SetRenderTarget(colorRT);
            }
        }

        private void RenderPostProcess(ref ScriptableRenderContext renderContext, PostProcessLayer postProcessLayer, bool opaqueOnly)
        {
            var postProcessCommand = CommandBufferPool.Get("Post Processing");
            postProcessCommand.SetGlobalTexture("m_CameraDepthTexture", m_CameraDepthRT);

            m_PostProcessRenderContext.Reset();
            m_PostProcessRenderContext.camera = m_CurrCamera;
            m_PostProcessRenderContext.source = BuiltinRenderTextureType.CurrentActive;
            m_PostProcessRenderContext.sourceFormat = m_ColorFormat;
            m_PostProcessRenderContext.destination = BuiltinRenderTextureType.CameraTarget;
            m_PostProcessRenderContext.command = postProcessCommand;
            m_PostProcessRenderContext.flip = true;

            if (opaqueOnly)
                postProcessLayer.RenderOpaqueOnly(m_PostProcessRenderContext);
            else
                postProcessLayer.Render(m_PostProcessRenderContext);
            renderContext.ExecuteCommandBuffer(postProcessCommand);
            CommandBufferPool.Release(postProcessCommand);
        }

        private void Blit(CommandBuffer cmd, RenderingConfiguration renderingConfig, RenderTargetIdentifier sourceRT, RenderTargetIdentifier destRT, Material material = null)
        {
            if (LightweightUtils.HasFlag(renderingConfig, RenderingConfiguration.DefaultViewport))
            {
                cmd.Blit(sourceRT, destRT, material);
            }
            else
            {
                if (m_BlitQuad == null)
                    m_BlitQuad = LightweightUtils.CreateQuadMesh(false);

                cmd.SetGlobalTexture(m_BlitTexID, sourceRT);
                cmd.SetRenderTarget(destRT);
                cmd.SetViewport(m_CurrCamera.pixelRect);
                cmd.DrawMesh(m_BlitQuad, Matrix4x4.identity, m_BlitMaterial);
            }
        }
    }
}
