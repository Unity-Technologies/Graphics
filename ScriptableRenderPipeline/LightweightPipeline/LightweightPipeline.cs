using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public int pixelAdditionalLightsCount;
        public int totalAdditionalLightsCount;
        public int mainLightIndex;
        public bool shadowsRendered;
    }

    public enum MixedLightingSetup
    {
        None = 0,
        ShadowMask,
        Subtractive,
    };

    public static class CameraRenderTargetID
    {
        // Camera color target. Not used when camera is rendering to backbuffer or camera
        // is rendering to a texture (offscreen camera)
        public static int color;

        // Camera copy color texture. In case there is a single BeforeTransparent postFX
        // we need use copyColor RT as a work RT.
        public static int copyColor;

        // Camera depth target. Only used when post processing or soft particles are enabled.
        public static int depth;

        // If soft particles are enabled and no depth prepass is performed we need to copy depth.
        public static int depthCopy;
    }

    public class LightweightPipeline : RenderPipeline
    {
        private readonly LightweightPipelineAsset m_Asset;

        // Maximum amount of visible lights the shader can process. This controls the constant global light buffer size.
        // It must match the MAX_VISIBLE_LIGHTS in LightweightInput.cginc
        private static readonly int kMaxVisibleLights = 16;

        // Lights are culled per-object. This holds the maximum amount of lights that can be shaded per-object.
        // The engine fills in the lights indices per-object in unity4_LightIndices0 and unity_4LightIndices1
        private static readonly int kMaxPerObjectLights = 8;

        private static readonly int kMaxVertexLights = 4;

        private bool m_IsOffscreenCamera;

        private Vector4[] m_LightPositions = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightColors = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightDistanceAttenuations = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightSpotDirections = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightSpotAttenuations = new Vector4[kMaxVisibleLights];

        private Camera m_CurrCamera;

        private static readonly int kMaxCascades = 4;
        private int m_ShadowCasterCascadesCount = kMaxCascades;
        private int m_ShadowMapRTID;
        private RenderTargetIdentifier m_CurrCameraColorRT;
        private RenderTargetIdentifier m_ShadowMapRT;
        private RenderTargetIdentifier m_ColorRT;
        private RenderTargetIdentifier m_CopyColorRT;
        private RenderTargetIdentifier m_DepthRT;
        private RenderTargetIdentifier m_CopyDepth;
        private RenderTargetIdentifier m_Color;

        private bool m_IntermediateTextureArray;
        private bool m_RequiredDepth;
        private MixedLightingSetup m_MixedLightingSetup;

        private const int kShadowDepthBufferBits = 16;
        private const int kCameraDepthBufferBits = 32;
        private Vector4[] m_DirectionalShadowSplitDistances = new Vector4[kMaxCascades];

        private ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        private ShadowSliceData[] m_ShadowSlices = new ShadowSliceData[kMaxCascades];

        // Pipeline pass names
        private static readonly ShaderPassName m_DepthPrePass = new ShaderPassName("DepthOnly");
        private static readonly ShaderPassName m_LitPassName = new ShaderPassName("LightweightForward");
        private static readonly ShaderPassName m_UnlitPassName = new ShaderPassName("SRPDefaultUnlit"); // Renders all shaders without a lightmode tag

        // Legacy pass names
        public static readonly ShaderPassName s_AlwaysName = new ShaderPassName("Always");
        public static readonly ShaderPassName s_ForwardBaseName = new ShaderPassName("ForwardBase");
        public static readonly ShaderPassName s_PrepassBaseName = new ShaderPassName("PrepassBase");
        public static readonly ShaderPassName s_VertexName = new ShaderPassName("Vertex");
        public static readonly ShaderPassName s_VertexLMRGBMName = new ShaderPassName("VertexLMRGBM");
        public static readonly ShaderPassName s_VertexLMName = new ShaderPassName("VertexLM");
        public static readonly ShaderPassName[] s_LegacyPassNames =
        {
            s_AlwaysName, s_ForwardBaseName, s_PrepassBaseName, s_VertexName, s_VertexLMRGBMName, s_VertexLMName
        };

        private RenderTextureFormat m_ColorFormat;
        private PostProcessRenderContext m_PostProcessRenderContext;
        private PostProcessLayer m_CameraPostProcessLayer;

        private CameraComparer m_CameraComparer = new CameraComparer();
        private LightComparer m_LightCompararer = new LightComparer();

        // Maps from sorted light indices to original unsorted. We need this for shadow rendering
        // and per-object light lists.
        private List<int> m_SortedLightIndexMap = new List<int>();

        private Mesh m_BlitQuad;
        private Material m_BlitMaterial;
        private Material m_CopyDepthMaterial;
        private Material m_ErrorMaterial;
        private int m_BlitTexID = Shader.PropertyToID("_BlitTex");

        private CopyTextureSupport m_CopyTextureSupport;

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            m_Asset = asset;

            BuildShadowSettings();

            PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            // Lights are culled per-camera. Therefore we need to reset light buffers on each camera render
            PerCameraBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            PerCameraBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            PerCameraBuffer._MainLightDistanceAttenuation = Shader.PropertyToID("_MainLightDistanceAttenuation");
            PerCameraBuffer._MainLightSpotDir = Shader.PropertyToID("_MainLightSpotDir");
            PerCameraBuffer._MainLightSpotAttenuation = Shader.PropertyToID("_MainLightSpotAttenuation");
            PerCameraBuffer._MainLightCookie = Shader.PropertyToID("_MainLightCookie");
            PerCameraBuffer._WorldToLight = Shader.PropertyToID("_WorldToLight");
            PerCameraBuffer._AdditionalLightCount = Shader.PropertyToID("_AdditionalLightCount");
            PerCameraBuffer._AdditionalLightPosition = Shader.PropertyToID("_AdditionalLightPosition");
            PerCameraBuffer._AdditionalLightColor = Shader.PropertyToID("_AdditionalLightColor");
            PerCameraBuffer._AdditionalLightDistanceAttenuation = Shader.PropertyToID("_AdditionalLightDistanceAttenuation");
            PerCameraBuffer._AdditionalLightSpotDir = Shader.PropertyToID("_AdditionalLightSpotDir");
            PerCameraBuffer._AdditionalLightSpotAttenuation = Shader.PropertyToID("_AdditionalLightSpotAttenuation");

            m_ShadowMapRTID = Shader.PropertyToID("_ShadowMap");

            CameraRenderTargetID.color = Shader.PropertyToID("_CameraColorRT");
            CameraRenderTargetID.copyColor = Shader.PropertyToID("_CameraCopyColorRT");
            CameraRenderTargetID.depth = Shader.PropertyToID("_CameraDepthTexture");
            CameraRenderTargetID.depthCopy = Shader.PropertyToID("_CameraCopyDepthTexture");

            m_ShadowMapRT = new RenderTargetIdentifier(m_ShadowMapRTID);

            m_ColorRT = new RenderTargetIdentifier(CameraRenderTargetID.color);
            m_CopyColorRT = new RenderTargetIdentifier(CameraRenderTargetID.copyColor);
            m_DepthRT = new RenderTargetIdentifier(CameraRenderTargetID.depth);
            m_CopyDepth = new RenderTargetIdentifier(CameraRenderTargetID.depthCopy);
            m_PostProcessRenderContext = new PostProcessRenderContext();

            m_CopyTextureSupport = SystemInfo.copyTextureSupport;

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != m_Asset.MSAASampleCount)
                QualitySettings.antiAliasing = m_Asset.MSAASampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";

            m_BlitQuad = LightweightUtils.CreateQuadMesh(false);
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(m_Asset.BlitShader);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(m_Asset.CopyDepthShader);
            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");
        }

        public override void Dispose()
        {
            base.Dispose();
            Shader.globalRenderPipeline = "";

            CoreUtils.Destroy(m_ErrorMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_BlitMaterial);
        }

        CullResults m_CullResults;
        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);

            // TODO: This is at the moment required for all pipes. We should not implicitly change user project settings
            // instead this should be forced when using SRP, since all SRP use linear lighting.
            GraphicsSettings.lightsUseLinearIntensity = true;

            SetupPerFrameShaderConstants();

            // Sort cameras array by camera depth
            Array.Sort(cameras, m_CameraComparer);
            foreach (Camera camera in cameras)
            {
                bool sceneViewCamera = camera.cameraType == CameraType.SceneView;
                bool stereoEnabled = XRSettings.isDeviceActive && !sceneViewCamera;
                m_CurrCamera = camera;
                m_IsOffscreenCamera = m_CurrCamera.targetTexture != null && m_CurrCamera.cameraType != CameraType.SceneView;

                ScriptableCullingParameters cullingParameters;
                if (!CullResults.GetCullingParameters(m_CurrCamera, stereoEnabled, out cullingParameters))
                    continue;

                cullingParameters.shadowDistance = Mathf.Min(m_ShadowSettings.maxShadowDistance,
                    m_CurrCamera.farClipPlane);

#if UNITY_EDITOR
                // Emit scene view UI
                if (sceneViewCamera)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif

                CullResults.Cull(ref cullingParameters, context, ref m_CullResults);
                VisibleLight[] visibleLights = m_CullResults.visibleLights.ToArray();

                LightData lightData;
                InitializeLightData(visibleLights, out lightData);

                ShadowPass(visibleLights, ref context, ref lightData);

                FrameRenderingConfiguration frameRenderingConfiguration;
                SetupFrameRendering(out frameRenderingConfiguration, stereoEnabled);
                SetupIntermediateResources(frameRenderingConfiguration, ref context);

                // SetupCameraProperties does the following:
                // Setup Camera RenderTarget and Viewport
                // VR Camera Setup and SINGLE_PASS_STEREO props
                // Setup camera view, proj and their inv matrices.
                // Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
                // Setup camera world clip planes props
                // setup HDR keyword
                // Setup global time properties (_Time, _SinTime, _CosTime)
                context.SetupCameraProperties(m_CurrCamera, stereoEnabled);

                if (LightweightUtils.HasFlag(frameRenderingConfiguration, FrameRenderingConfiguration.DepthPass))
                    DepthPass(ref context);
                ForwardPass(visibleLights, frameRenderingConfiguration, ref context, ref lightData, stereoEnabled);

                // Release temporary RT
                var cmd = CommandBufferPool.Get("After Camera Render");
                cmd.ReleaseTemporaryRT(m_ShadowMapRTID);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.depthCopy);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.depth);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.color);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.copyColor);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                context.Submit();
            }
        }

        private void ShadowPass(VisibleLight[] visibleLights, ref ScriptableRenderContext context, ref LightData lightData)
        {
            if (m_Asset.AreShadowsEnabled() && lightData.mainLightIndex != -1)
            {
                VisibleLight mainLight = visibleLights[lightData.mainLightIndex];

                if (mainLight.light.shadows != LightShadows.None)
                {
                    if (!LightweightUtils.IsSupportedShadowType(mainLight.lightType))
                    {
                        Debug.LogWarning("Only directional and spot shadows are supported by LightweightPipeline.");
                        return;
                    }

                    // There's no way to map shadow light indices. We need to pass in the original unsorted index.
                    // If no additional lights then no light sorting is performed and the indices match.
                    int shadowOriginalIndex = (lightData.totalAdditionalLightsCount > 0) ? GetLightUnsortedIndex(lightData.mainLightIndex) : lightData.mainLightIndex;
                    lightData.shadowsRendered = RenderShadows(ref m_CullResults, ref mainLight,
                        shadowOriginalIndex, ref context);
                }
            }
        }

        private void DepthPass(ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Depth Prepass");
            SetRenderTarget(cmd, m_DepthRT);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            var opaqueDrawSettings = new DrawRendererSettings(m_CurrCamera, m_DepthPrePass);
            opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;

            var opaqueFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };

            context.DrawRenderers(m_CullResults.visibleRenderers, ref opaqueDrawSettings, opaqueFilterSettings);
        }

        private void ForwardPass(VisibleLight[] visibleLights, FrameRenderingConfiguration frameRenderingConfiguration, ref ScriptableRenderContext context, ref LightData lightData, bool stereoEnabled)
        {
            SetupShaderConstants(visibleLights, ref context, ref lightData);

            RendererConfiguration rendererSettings = GetRendererSettings(ref lightData);

            BeginForwardRendering(ref context, frameRenderingConfiguration);
            RenderOpaques(ref context, rendererSettings);
            AfterOpaque(ref context, frameRenderingConfiguration);
            RenderTransparents(ref context, rendererSettings);
            AfterTransparent(ref context, frameRenderingConfiguration);
            EndForwardRendering(ref context, frameRenderingConfiguration);
        }

        private void RenderOpaques(ref ScriptableRenderContext context, RendererConfiguration settings)
        {
            var opaqueDrawSettings = new DrawRendererSettings(m_CurrCamera, m_LitPassName);
            opaqueDrawSettings.SetShaderPassName(1, m_UnlitPassName);
            opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;
            opaqueDrawSettings.rendererConfiguration = settings;

            var opaqueFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };

            context.DrawRenderers(m_CullResults.visibleRenderers, ref opaqueDrawSettings, opaqueFilterSettings);

            // Render objects that did not match any shader pass with error shader
            RenderObjectsWithError(ref context, opaqueFilterSettings, SortFlags.None);

            if (m_CurrCamera.clearFlags == CameraClearFlags.Skybox)
                context.DrawSkybox(m_CurrCamera);
        }

        private void AfterOpaque(ref ScriptableRenderContext context, FrameRenderingConfiguration config)
        {
            if (!m_RequiredDepth)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("After Opaque");
            cmd.SetGlobalTexture(CameraRenderTargetID.depth, m_DepthRT);

            // When only one opaque effect is active we need to blit to a work RT. We blit to copy color.
            // TODO: We can check if there are more than one opaque postfx and avoid an extra blit.
            // TODO: There's currently an issue in the PostFX stack that has a one frame delay when an effect is enabled/disabled
            // when an effect is disabled, HasOpaqueOnlyEffects returns true in the first frame, however inside render the effect
            // state is update, causing RenderPostProcess here to not blit to FinalColorRT. Until the next frame the RT will have garbage.
            if (LightweightUtils.HasFlag(config, FrameRenderingConfiguration.BeforeTransparentPostProcess))
            {
                RenderPostProcess(cmd, m_ColorRT, m_CopyColorRT, true);
                m_CurrCameraColorRT = (m_IsOffscreenCamera) ? BuiltinRenderTextureType.CameraTarget : m_ColorRT;
            }

            if (LightweightUtils.HasFlag(config, FrameRenderingConfiguration.DepthCopy))
            {
                RenderTargetIdentifier colorRT = (m_IsOffscreenCamera) ? BuiltinRenderTextureType.CameraTarget : m_ColorRT;
                CopyTexture(cmd, m_DepthRT, m_CopyDepth, m_CopyDepthMaterial);
                SetRenderTarget(cmd, colorRT, m_CopyDepth);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderTransparents(ref ScriptableRenderContext context, RendererConfiguration config)
        {
            var transparentSettings = new DrawRendererSettings(m_CurrCamera, m_LitPassName);
            transparentSettings.SetShaderPassName(1, m_UnlitPassName);
            transparentSettings.sorting.flags = SortFlags.CommonTransparent;
            transparentSettings.rendererConfiguration = config;

            var transparentFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.transparent
            };

            context.DrawRenderers(m_CullResults.visibleRenderers, ref transparentSettings, transparentFilterSettings);

            // Render objects that did not match any shader pass with error shader
            RenderObjectsWithError(ref context, transparentFilterSettings, SortFlags.None);
        }

        private void AfterTransparent(ref ScriptableRenderContext context, FrameRenderingConfiguration config)
        {
            if (!LightweightUtils.HasFlag(config, FrameRenderingConfiguration.PostProcess))
                return;

            CommandBuffer cmd = CommandBufferPool.Get("After Transparent");
            RenderPostProcess(cmd, BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CameraTarget, false);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void RenderObjectsWithError(ref ScriptableRenderContext context, FilterRenderersSettings filterSettings, SortFlags sortFlags)
        {
            if (m_ErrorMaterial != null)
            {
                DrawRendererSettings errorSettings = new DrawRendererSettings(m_CurrCamera, s_LegacyPassNames[0]);
                for (int i = 1; i < s_LegacyPassNames.Length; ++i)
                    errorSettings.SetShaderPassName(i, s_LegacyPassNames[i]);

                errorSettings.sorting.flags = sortFlags;
                errorSettings.rendererConfiguration = RendererConfiguration.None;
                errorSettings.SetOverrideMaterial(m_ErrorMaterial, 0);
                context.DrawRenderers(m_CullResults.visibleRenderers, ref errorSettings, filterSettings);
            }
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

        private void SetupFrameRendering(out FrameRenderingConfiguration configuration, bool stereoEnabled)
        {
            configuration = (stereoEnabled) ? FrameRenderingConfiguration.Stereo : FrameRenderingConfiguration.None;
            if (stereoEnabled && XRSettings.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
                m_IntermediateTextureArray = true;
            else
                m_IntermediateTextureArray = false;

            bool intermediateTexture = m_CurrCamera.targetTexture != null || m_CurrCamera.cameraType == CameraType.SceneView ||
                                            m_Asset.RenderScale < 1.0f || m_CurrCamera.allowHDR;

            m_ColorFormat = m_CurrCamera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            m_RequiredDepth = false;
            m_CameraPostProcessLayer = m_CurrCamera.GetComponent<PostProcessLayer>();

            bool msaaEnabled = m_CurrCamera.allowMSAA && m_Asset.MSAASampleCount > 1 && (m_CurrCamera.targetTexture == null || m_CurrCamera.targetTexture.antiAliasing > 1);

            // TODO: PostProcessing and SoftParticles are currently not support for VR
            bool postProcessEnabled = m_CameraPostProcessLayer != null && m_CameraPostProcessLayer.enabled && !stereoEnabled;
            bool softParticlesEnabled = m_Asset.RequireCameraDepthTexture && !stereoEnabled;
            if (postProcessEnabled)
            {
                m_RequiredDepth = true;
                intermediateTexture = true;

                configuration |= FrameRenderingConfiguration.PostProcess;

                if (m_CameraPostProcessLayer.HasOpaqueOnlyEffects(m_PostProcessRenderContext))
                    configuration |= FrameRenderingConfiguration.BeforeTransparentPostProcess;

                // Resolving depth msaa requires texture2DMS. Currently if msaa is enabled we do a depth pre-pass.
                if (msaaEnabled)
                    configuration |= FrameRenderingConfiguration.DepthPass;
            }

            // In case of soft particles we need depth copy. If depth copy not supported fallback to depth prepass
            if (softParticlesEnabled)
            {
                m_RequiredDepth = true;
                intermediateTexture = true;

                bool supportsDepthCopy = m_CopyTextureSupport != CopyTextureSupport.None && m_Asset.CopyDepthShader.isSupported;

                // currently fallback to depth prepass if msaa is enabled since. We need texture2DMS to support depth resolve.
                configuration |= (msaaEnabled || !supportsDepthCopy) ? FrameRenderingConfiguration.DepthPass : FrameRenderingConfiguration.DepthCopy;
            }

            if (msaaEnabled)
            {
                configuration |= FrameRenderingConfiguration.Msaa;
                intermediateTexture = intermediateTexture || !LightweightUtils.PlatformSupportsMSAABackBuffer();
            }

            Rect cameraRect = m_CurrCamera.rect;
            if (cameraRect.x > 0.0f || cameraRect.y > 0.0f || cameraRect.width < 1.0f || cameraRect.height < 1.0f)
                intermediateTexture = true;
            else
                configuration |= FrameRenderingConfiguration.DefaultViewport;

            if (intermediateTexture)
                configuration |= FrameRenderingConfiguration.IntermediateTexture;
        }

        private void SetupIntermediateResources(FrameRenderingConfiguration renderingConfig, ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Setup Intermediate Resources");

            int msaaSamples = (m_IsOffscreenCamera) ? Math.Min(m_CurrCamera.targetTexture.antiAliasing, m_Asset.MSAASampleCount) : m_Asset.MSAASampleCount;
            msaaSamples = (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Msaa)) ? msaaSamples : 1;
            m_CurrCameraColorRT = BuiltinRenderTextureType.CameraTarget;

            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture))
            {
                if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
                    SetupIntermediateResourcesStereo(cmd, msaaSamples);
                else
                    SetupIntermediateResourcesSingle(cmd, renderingConfig, msaaSamples);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetupIntermediateResourcesSingle(CommandBuffer cmd, FrameRenderingConfiguration renderingConfig, int msaaSamples)
        {
            float renderScale = (m_CurrCamera.cameraType == CameraType.Game) ? m_Asset.RenderScale : 1.0f;
            int rtWidth = (int)((float)m_CurrCamera.pixelWidth * renderScale);
            int rtHeight = (int)((float)m_CurrCamera.pixelHeight * renderScale);

            if (m_RequiredDepth)
            {
                RenderTextureDescriptor depthRTDesc = new RenderTextureDescriptor(rtWidth, rtHeight, RenderTextureFormat.Depth, kCameraDepthBufferBits);
                cmd.GetTemporaryRT(CameraRenderTargetID.depth, depthRTDesc, FilterMode.Bilinear);

                if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DepthCopy))
                    cmd.GetTemporaryRT(CameraRenderTargetID.depthCopy, depthRTDesc, FilterMode.Bilinear);
            }

            RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(rtWidth, rtHeight, m_ColorFormat, kCameraDepthBufferBits);
            colorRTDesc.msaaSamples = msaaSamples;
            colorRTDesc.enableRandomWrite = false;

            // When offscreen camera current rendertarget is CameraTarget
            if (!m_IsOffscreenCamera)
            {
                cmd.GetTemporaryRT(CameraRenderTargetID.color, colorRTDesc, FilterMode.Bilinear);
                m_CurrCameraColorRT = m_ColorRT;
            }

            // When postprocessing is enabled we might have a before transparent effect. In that case we need to
            // use the camera render target as input. We blit to an opaque RT and then after before postprocessing is done
            // we blit to the final camera RT. If no postprocessing we blit to final camera RT from beginning.
            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.BeforeTransparentPostProcess))
                cmd.GetTemporaryRT(CameraRenderTargetID.copyColor, colorRTDesc, FilterMode.Point);
        }

        private void SetupIntermediateResourcesStereo(CommandBuffer cmd, int msaaSamples)
        {
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
            rtDesc = XRSettings.eyeTextureDesc;
            rtDesc.colorFormat = m_ColorFormat;
            rtDesc.msaaSamples = msaaSamples;

            cmd.GetTemporaryRT(CameraRenderTargetID.color, rtDesc, FilterMode.Bilinear);
        }

        private void SetupShaderConstants(VisibleLight[] visibleLights, ref ScriptableRenderContext context, ref LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SetupShaderConstants");
            SetupShaderLightConstants(cmd, visibleLights, ref lightData);
            SetShaderKeywords(cmd, ref lightData, visibleLights);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void InitializeLightData(VisibleLight[] visibleLights, out LightData lightData)
        {
            int visibleLightsCount = Math.Min(visibleLights.Length, m_Asset.MaxPixelLights);
            m_SortedLightIndexMap.Clear();

            lightData.shadowsRendered = false;

            if (visibleLightsCount <= 1)
                lightData.mainLightIndex = GetMainLight(visibleLights);
            else
                lightData.mainLightIndex = SortLights(visibleLights);

            // If we have a main light we don't shade it in the per-object light loop. We also remove it from the per-object cull list
            int mainLightPresent = (lightData.mainLightIndex >= 0) ? 1 : 0;
            int additionalPixelLightsCount = visibleLightsCount - mainLightPresent;
            int vertexLightCount = (m_Asset.SupportsVertexLight) ? Math.Min(visibleLights.Length, kMaxPerObjectLights) - additionalPixelLightsCount : 0;
            vertexLightCount = Math.Min(vertexLightCount, kMaxVertexLights);

            lightData.pixelAdditionalLightsCount = additionalPixelLightsCount;
            lightData.totalAdditionalLightsCount = additionalPixelLightsCount + vertexLightCount;

            m_MixedLightingSetup = MixedLightingSetup.None;
        }

        private int SortLights(VisibleLight[] visibleLights)
        {
            int totalVisibleLights = visibleLights.Length;

            Dictionary<int, int> visibleLightsIDMap = new Dictionary<int, int>();
            for (int i = 0; i < totalVisibleLights; ++i)
                visibleLightsIDMap.Add(visibleLights[i].GetHashCode(), i);

            // Sorts light so we have all directionals first, then local lights.
            // Directionals are sorted further by shadow, cookie and intensity
            // Locals are sorted further by shadow, cookie and distance to camera
            m_LightCompararer.CurrCamera = m_CurrCamera;
            Array.Sort(visibleLights, m_LightCompararer);

            for (int i = 0; i < totalVisibleLights; ++i)
                m_SortedLightIndexMap.Add(visibleLightsIDMap[visibleLights[i].GetHashCode()]);

            return GetMainLight(visibleLights);
        }

        // How main light is decided:
        // If shadows enabled, main light is always a shadow casting light. Directional has priority over local lights.
        // Otherwise directional lights have priority based on cookie support and intensity
        private int GetMainLight(VisibleLight[] visibleLights)
        {
            int totalVisibleLights = visibleLights.Length;
            bool shadowsEnabled = m_Asset.AreShadowsEnabled();

            if (totalVisibleLights == 0 || m_Asset.MaxPixelLights == 0)
                return -1;

            int brighestDirectionalIndex = -1;
            for (int i = 0; i < totalVisibleLights; ++i)
            {
                VisibleLight currLight = visibleLights[i];

                // Particle system lights have the light property as null. We sort lights so all particles lights
                // come last. Therefore, if first light is particle light then all lights are particle lights.
                // In this case we either have no main light or already found it.
                if (currLight.light == null)
                    break;

                if (shadowsEnabled && currLight.light.shadows != LightShadows.None && LightweightUtils.IsSupportedShadowType(currLight.lightType))
                    return i;

                if (currLight.lightType == LightType.Directional && brighestDirectionalIndex == -1)
                    brighestDirectionalIndex = i;
            }

            return brighestDirectionalIndex;
        }

        private void InitializeLightConstants(VisibleLight[] lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightDistanceAttenuation, out Vector4 lightSpotDir,
            out Vector4 lightSpotAttenuation)
        {
            float directContributionNotBaked = 1.0f;
            lightPos = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
            lightColor = Color.black;
            lightDistanceAttenuation = new Vector4(0.0f, 1.0f, 0.0f, directContributionNotBaked);
            lightSpotDir = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
            lightSpotAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorld.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightData.localToWorld.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            lightColor = lightData.finalColor;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightData.lightType != LightType.Directional)
            {
                // Light attenuation in lightweight matches the unity vanilla one.
                // attenuation = 1.0 / 1.0 + distanceToLightSqr * quadraticAttenuation
                // then a smooth factor is applied to linearly fade attenuation to light range
                // the attenuation smooth factor starts having effect at 80% of light range
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float quadAtten = 25.0f / lightRangeSqr;
                lightDistanceAttenuation = new Vector4(quadAtten, oneOverFadeRangeSqr, lightRangeSqrOverFadeRangeSqr, directContributionNotBaked);
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 dir = lightData.localToWorld.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                // Spot Attenuation with a linear falloff can be defined as
                // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                // This can be rewritten as
                // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                // If we precompute the terms in a MAD instruction
                float spotAngle = Mathf.Deg2Rad * lightData.spotAngle;
                float cosOuterAngle = Mathf.Cos(spotAngle * 0.5f);
                float cosInneAngle = Mathf.Cos(spotAngle * 0.25f);
                float smoothAngleRange = cosInneAngle - cosOuterAngle;
                if (Mathf.Approximately(smoothAngleRange, 0.0f))
                    smoothAngleRange = 1.0f;

                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightSpotAttenuation = new Vector4(invAngleRange, add, 0.0f);
            }

            Light light = lightData.light;
            if (light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
            {
                // TODO: Add support to shadow mask
                if (m_MixedLightingSetup == MixedLightingSetup.None && lightData.light.shadows != LightShadows.None)
                {
                    m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                    lightDistanceAttenuation.w = 0.0f;
                }
            }
        }

        private void SetupPerFrameShaderConstants()
        {
            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Vector4 glossyEnvColor = new Vector4(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Shader.SetGlobalVector(PerFrameBuffer._GlossyEnvironmentColor, glossyEnvColor);

            // Used when subtractive mode is selected
            Shader.SetGlobalColor(PerFrameBuffer._SubtractiveShadowColor, RenderSettings.subtractiveShadowColor.linear);
        }

        private void SetupShaderLightConstants(CommandBuffer cmd, VisibleLight[] lights, ref LightData lightData)
        {
            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, lights, lightData.mainLightIndex);
            if (lightData.shadowsRendered)
                SetupShadowShaderConstants(cmd, ref lights[lightData.mainLightIndex], m_ShadowCasterCascadesCount);

            if (lightData.totalAdditionalLightsCount > 0)
                SetupAdditionalListConstants(cmd, lights, ref lightData);
        }

        private void SetupMainLightConstants(CommandBuffer cmd, VisibleLight[] lights, int lightIndex)
        {
            Vector4 lightPos, lightColor, lightDistanceAttenuation, lightSpotDir, lightSpotAttenuation;
            InitializeLightConstants(lights, lightIndex, out lightPos, out lightColor, out lightDistanceAttenuation, out lightSpotDir, out lightSpotAttenuation);

            if (lightIndex >= 0)
            {
                LightType mainLightType = lights[lightIndex].lightType;
                Light mainLight = lights[lightIndex].light;

                if (LightweightUtils.IsSupportedCookieType(mainLightType) && mainLight.cookie != null)
                {
                    Matrix4x4 lightCookieMatrix;
                    LightweightUtils.GetLightCookieMatrix(lights[lightIndex], out lightCookieMatrix);
                    cmd.SetGlobalTexture(PerCameraBuffer._MainLightCookie, mainLight.cookie);
                    cmd.SetGlobalMatrix(PerCameraBuffer._WorldToLight, lightCookieMatrix);
                }
            }

            cmd.SetGlobalVector(PerCameraBuffer._MainLightPosition, lightPos);
            cmd.SetGlobalColor(PerCameraBuffer._MainLightColor, lightColor);
            cmd.SetGlobalVector(PerCameraBuffer._MainLightDistanceAttenuation, lightDistanceAttenuation);
            cmd.SetGlobalVector(PerCameraBuffer._MainLightSpotDir, lightSpotDir);
            cmd.SetGlobalVector(PerCameraBuffer._MainLightSpotAttenuation, lightSpotAttenuation);
        }

        private void SetupAdditionalListConstants(CommandBuffer cmd, VisibleLight[] lights, ref LightData lightData)
        {
            int additionalLightIndex = 0;

            // We need to update per-object light list with the proper map to our global additional light buffer
            // First we initialize all lights in the map to -1 to tell the system to discard main light index and
            // remaining lights in the scene that don't fit the max additional light buffer (kMaxVisibileAdditionalLights)
            int[] perObjectLightIndexMap = m_CullResults.GetLightIndexMap();
            for (int i = 0; i < lights.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            for (int i = 0; i < lights.Length && additionalLightIndex < kMaxVisibleLights; ++i)
            {
                if (i != lightData.mainLightIndex)
                {
                    // The engine performs per-object light culling and initialize 8 light indices into two vec4 constants unity_4LightIndices0 and unity_4LightIndices1.
                    // In the shader we iterate over each visible light using the indices provided in these constants to index our global light buffer
                    // ex: first light position would be m_LightPosisitions[unity_4LightIndices[0]];

                    // However since we sorted the lights we need to tell the engine how to map the original/unsorted indices to our global buffer
                    // We do it by settings the perObjectLightIndexMap to the appropriate additionalLightIndex.
                    perObjectLightIndexMap[GetLightUnsortedIndex(i)] = additionalLightIndex;
                    InitializeLightConstants(lights, i, out m_LightPositions[additionalLightIndex],
                            out m_LightColors[additionalLightIndex],
                            out m_LightDistanceAttenuations[additionalLightIndex],
                            out m_LightSpotDirections[additionalLightIndex],
                            out m_LightSpotAttenuations[additionalLightIndex]);
                    additionalLightIndex++;
                }
            }
            m_CullResults.SetLightIndexMap(perObjectLightIndexMap);

            cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, new Vector4(lightData.pixelAdditionalLightsCount,
                 lightData.totalAdditionalLightsCount, 0.0f, 0.0f));
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightPosition, m_LightPositions);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightColor, m_LightColors);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightDistanceAttenuation, m_LightDistanceAttenuations);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotDir, m_LightSpotDirections);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotAttenuation, m_LightSpotAttenuations);
        }

        private void SetupShadowShaderConstants(CommandBuffer cmd, ref VisibleLight shadowLight, int cascadeCount)
        {
            Light light = shadowLight.light;
            float strength = 1.0f - light.shadowStrength;
            float bias = light.shadowBias * 0.1f;
            float normalBias = light.shadowNormalBias;
            float nearPlane = light.shadowNearPlane;
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

            cmd.SetGlobalMatrixArray("_WorldToShadow", shadowMatrices);
            cmd.SetGlobalVectorArray("_DirShadowSplitSpheres", m_DirectionalShadowSplitDistances);
            cmd.SetGlobalVector("_ShadowData", new Vector4(strength, bias, normalBias, nearPlane));
            cmd.SetGlobalFloatArray("_PCFKernel", pcfKernel);
        }

        private void SetShaderKeywords(CommandBuffer cmd, ref LightData lightData, VisibleLight[] visibleLights)
        {
            int vertexLightsCount = lightData.totalAdditionalLightsCount - lightData.pixelAdditionalLightsCount;
            CoreUtils.SetKeyword(cmd, "_VERTEX_LIGHTS", vertexLightsCount > 0);

            int mainLightIndex = lightData.mainLightIndex;

            CoreUtils.SetKeyword(cmd, "_MAIN_LIGHT_COOKIE", mainLightIndex != -1 && LightweightUtils.IsSupportedCookieType(visibleLights[mainLightIndex].lightType) && visibleLights[mainLightIndex].light.cookie != null);
            CoreUtils.SetKeyword(cmd, "_MAIN_DIRECTIONAL_LIGHT", mainLightIndex == -1 || visibleLights[mainLightIndex].lightType == LightType.Directional);
            CoreUtils.SetKeyword(cmd, "_MAIN_SPOT_LIGHT", mainLightIndex != -1 && visibleLights[mainLightIndex].lightType == LightType.Spot);
            CoreUtils.SetKeyword(cmd, "_ADDITIONAL_LIGHTS", lightData.totalAdditionalLightsCount > 0);
            CoreUtils.SetKeyword(cmd, "_MIXED_LIGHTING_SHADOWMASK", m_MixedLightingSetup == MixedLightingSetup.ShadowMask);
            CoreUtils.SetKeyword(cmd, "_MIXED_LIGHTING_SUBTRACTIVE", m_MixedLightingSetup == MixedLightingSetup.Subtractive);

            string[] shadowKeywords = new string[] { "_HARD_SHADOWS", "_SOFT_SHADOWS", "_HARD_SHADOWS_CASCADES", "_SOFT_SHADOWS_CASCADES" };
            for (int i = 0; i < shadowKeywords.Length; ++i)
                cmd.DisableShaderKeyword(shadowKeywords[i]);

            if (m_Asset.AreShadowsEnabled() && lightData.shadowsRendered)
            {
                int keywordIndex = (int)m_Asset.ShadowSetting - 1;
                if (m_Asset.CascadeCount > 1)
                    keywordIndex += 2;
                cmd.EnableShaderKeyword(shadowKeywords[keywordIndex]);
            }

            CoreUtils.SetKeyword(cmd, "SOFTPARTICLES_ON", m_Asset.RequireCameraDepthTexture);
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

            var cmd = CommandBufferPool.Get();
            cmd.name = "Render packed shadows";
            cmd.GetTemporaryRT(m_ShadowMapRTID, m_ShadowSettings.shadowAtlasWidth,
                m_ShadowSettings.shadowAtlasHeight, kShadowDepthBufferBits, FilterMode.Bilinear, RenderTextureFormat.Depth);
            SetRenderTarget(cmd, m_ShadowMapRT, ClearFlag.All);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

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

        private void BeginForwardRendering(ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfig)
        {
            RenderTargetIdentifier colorRT = BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier depthRT = BuiltinRenderTextureType.None;

            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
                context.StartMultiEye(m_CurrCamera);

            CommandBuffer cmd = CommandBufferPool.Get("SetCameraRenderTarget");
            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture))
            {
                if (!m_IsOffscreenCamera)
                    colorRT = m_CurrCameraColorRT;

                if (m_RequiredDepth && !LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DepthPass))
                    depthRT = m_DepthRT;
            }


            if (ForceClear())
            {
                SetRenderTarget(cmd, colorRT, depthRT, ClearFlag.All);
            }
            else
            {
                ClearFlag clearFlag = ClearFlag.None;
                CameraClearFlags cameraClearFlags = m_CurrCamera.clearFlags;
                if (cameraClearFlags != CameraClearFlags.Nothing)
                {
                    clearFlag |= ClearFlag.Depth;
                    if (cameraClearFlags == CameraClearFlags.Color || cameraClearFlags == CameraClearFlags.Skybox)
                        clearFlag |= ClearFlag.Color;
                }

                SetRenderTarget(cmd, colorRT, depthRT, clearFlag);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void EndForwardRendering(ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfig)
        {
            // No additional rendering needs to be done if this is an off screen rendering camera
            if (m_IsOffscreenCamera)
                return;

            var cmd = CommandBufferPool.Get("Blit");
            if (m_IntermediateTextureArray)
            {
                SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
                cmd.Blit(m_CurrCameraColorRT, BuiltinRenderTextureType.CurrentActive);
            }
            else if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture))
            {
                // If PostProcessing is enabled, it is already blit to CameraTarget.
                if (!LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.PostProcess))
                    Blit(cmd, renderingConfig, BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CameraTarget);
            }

            SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
            {
                context.StopMultiEye(m_CurrCamera);
                context.StereoEndRender(m_CurrCamera);
            }
        }

        RendererConfiguration GetRendererSettings(ref LightData lightData)
        {
            RendererConfiguration settings = RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
            if (lightData.totalAdditionalLightsCount > 0)
                settings |= RendererConfiguration.PerObjectLightIndices8;
            return settings;
        }

        private void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorRT, ClearFlag clearFlag = ClearFlag.None)
        {
            int depthSlice = (m_IntermediateTextureArray) ? -1 : 0;
            CoreUtils.SetRenderTarget(cmd, colorRT, clearFlag, m_CurrCamera.backgroundColor.linear, 0, CubemapFace.Unknown, depthSlice);
        }

        private void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorRT, RenderTargetIdentifier depthRT, ClearFlag clearFlag = ClearFlag.None)
        {
            if (depthRT == BuiltinRenderTextureType.None)
            {
                SetRenderTarget(cmd, colorRT, clearFlag);
                return;
            }

            int depthSlice = (m_IntermediateTextureArray) ? -1 : 0;
            CoreUtils.SetRenderTarget(cmd, colorRT, depthRT, clearFlag, m_CurrCamera.backgroundColor.linear, 0, CubemapFace.Unknown, depthSlice);
        }

        private void RenderPostProcess(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly)
        {
            m_PostProcessRenderContext.Reset();
            m_PostProcessRenderContext.camera = m_CurrCamera;
            m_PostProcessRenderContext.source = source;
            m_PostProcessRenderContext.sourceFormat = m_ColorFormat;
            m_PostProcessRenderContext.destination = dest;
            m_PostProcessRenderContext.command = cmd;
            m_PostProcessRenderContext.flip = true;

            if (opaqueOnly)
            {
                m_CameraPostProcessLayer.RenderOpaqueOnly(m_PostProcessRenderContext);
                cmd.Blit(m_CopyColorRT, m_ColorRT);
            }
            else
                m_CameraPostProcessLayer.Render(m_PostProcessRenderContext);
        }

        private int GetLightUnsortedIndex(int index)
        {
            return (index < m_SortedLightIndexMap.Count) ? m_SortedLightIndexMap[index] : index;
        }

        private bool ForceClear()
        {
            // Clear RenderTarget to avoid tile initialization on mobile GPUs
            // https://community.arm.com/graphics/b/blog/posts/mali-performance-2-how-to-correctly-handle-framebuffers
            return (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
        }

        private void Blit(CommandBuffer cmd, FrameRenderingConfiguration renderingConfig, RenderTargetIdentifier sourceRT, RenderTargetIdentifier destRT, Material material = null)
        {
            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DefaultViewport))
            {
                cmd.Blit(sourceRT, destRT, material);
            }
            else
            {
                if (m_BlitQuad == null)
                    m_BlitQuad = LightweightUtils.CreateQuadMesh(false);

                cmd.SetGlobalTexture(m_BlitTexID, sourceRT);
                SetRenderTarget(cmd, destRT);
                cmd.SetViewport(m_CurrCamera.pixelRect);
                cmd.DrawMesh(m_BlitQuad, Matrix4x4.identity, m_BlitMaterial);
            }
        }

        private void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier sourceRT, RenderTargetIdentifier destRT, Material copyMaterial)
        {
            if (m_CopyTextureSupport != CopyTextureSupport.None)
                cmd.CopyTexture(sourceRT, destRT);
            else
                cmd.Blit(sourceRT, destRT, copyMaterial);
        }
    }
}
