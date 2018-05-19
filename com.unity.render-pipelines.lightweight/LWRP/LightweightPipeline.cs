using System;
using System.Collections.Generic;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor.Experimental.Rendering.LightweightPipeline;
#endif
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public struct LightData
    {
        public int pixelAdditionalLightsCount;
        public int totalAdditionalLightsCount;
        public int mainLightIndex;
        public List<VisibleLight> visibleLights;
        public List<int> localLightIndices;
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

        // Camera depth target. Only used when post processing, soft particles, or screen space shadows are enabled.
        public static int depth;

        // If soft particles are enabled and no depth prepass is performed we need to copy depth.
        public static int depthCopy;

        // Camera opaque target. Used for accessing screen contents during transparent pass.
        public static int opaque;
    }

    public partial class LightweightPipeline : RenderPipeline
    {
        private readonly LightweightPipelineAsset m_Asset;

        private LightweightShadowPass m_ShadowPass;

        // Maximum amount of visible lights the shader can process. This controls the constant global light buffer size.
        // It must match the MAX_VISIBLE_LIGHTS in LightweightInput.cginc
        private static readonly int kMaxVisibleLocalLights = 16;

        // Lights are culled per-object. In platforms that don't use StructuredBuffer
        // the engine will set 4 light indices in the following constant unity_4LightIndices0
        // Additionally the engine set unity_4LightIndices1 but LWRP doesn't use that.
        private static readonly int kMaxNonIndexedLocalLights = 4;
        private static readonly int kMaxVertexLights = 4;

        private const int kDepthStencilBufferBits = 32;

        private static readonly float kRenderScaleThreshold = 0.05f;

        private static readonly string kMSAADepthKeyword = "_MSAA_DEPTH";

        private bool m_IsOffscreenCamera;

        private Vector4 kDefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        private Vector4 kDefaultLightColor = Color.black;
        private Vector4 kDefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        private Vector4 kDefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        private Vector4 kDefaultLightSpotAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

        private Vector4[] m_LightPositions = new Vector4[kMaxVisibleLocalLights];
        private Vector4[] m_LightColors = new Vector4[kMaxVisibleLocalLights];
        private Vector4[] m_LightDistanceAttenuations = new Vector4[kMaxVisibleLocalLights];
        private Vector4[] m_LightSpotDirections = new Vector4[kMaxVisibleLocalLights];
        private Vector4[] m_LightSpotAttenuations = new Vector4[kMaxVisibleLocalLights];

        private Camera m_CurrCamera;

        private RenderTargetIdentifier m_CurrCameraColorRT;
        private RenderTargetIdentifier m_ColorRT;
        private RenderTargetIdentifier m_CopyColorRT;
        private RenderTargetIdentifier m_DepthRT;
        private RenderTargetIdentifier m_CopyDepth;
        private float[] m_OpaqueScalerValues = {1.0f, 0.5f, 0.25f, 0.25f};

        private float m_RenderScale;
        private int m_MSAASamples;

        private bool m_IntermediateTextureArray;
        private bool m_RequireDepthTexture;
        private bool m_RequireCopyColor;
        private bool m_DepthRenderBuffer;
        private MixedLightingSetup m_MixedLightingSetup;

        private ComputeBuffer m_LightIndicesBuffer;
        private List<int> m_LocalLightIndices;
        private int m_MaxLocalLightsShadedPerPass;
        private bool m_UseComputeBuffer;

        // Pipeline pass names
        private static readonly ShaderPassName m_DepthPrepass = new ShaderPassName("DepthOnly");
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

        private Material m_BlitMaterial;
        private Material m_CopyDepthMaterial;
        private Material m_ErrorMaterial;
        private Material m_SamplingMaterial;
        private int m_SampleOffset;
        private int m_SampleCount;
        private int m_BlitTexID = Shader.PropertyToID("_BlitTex");

        private CopyTextureSupport m_CopyTextureSupport;

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            m_Asset = asset;

            SetRenderingFeatures();
            SetPipelineCapabilities(asset);

            PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            // Lights are culled per-camera. Therefore we need to reset light buffers on each camera render
            PerCameraBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            PerCameraBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            PerCameraBuffer._MainLightCookie = Shader.PropertyToID("_MainLightCookie");
            PerCameraBuffer._WorldToLight = Shader.PropertyToID("_WorldToLight");
            PerCameraBuffer._AdditionalLightCount = Shader.PropertyToID("_AdditionalLightCount");
            PerCameraBuffer._AdditionalLightPosition = Shader.PropertyToID("_AdditionalLightPosition");
            PerCameraBuffer._AdditionalLightColor = Shader.PropertyToID("_AdditionalLightColor");
            PerCameraBuffer._AdditionalLightDistanceAttenuation = Shader.PropertyToID("_AdditionalLightDistanceAttenuation");
            PerCameraBuffer._AdditionalLightSpotDir = Shader.PropertyToID("_AdditionalLightSpotDir");
            PerCameraBuffer._AdditionalLightSpotAttenuation = Shader.PropertyToID("_AdditionalLightSpotAttenuation");
            PerCameraBuffer._LightIndexBuffer = Shader.PropertyToID("_LightIndexBuffer");
            PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");

            CameraRenderTargetID.color = Shader.PropertyToID("_CameraColorRT");
            CameraRenderTargetID.copyColor = Shader.PropertyToID("_CameraCopyColorRT");
            CameraRenderTargetID.depth = Shader.PropertyToID("_CameraDepthTexture");
            CameraRenderTargetID.depthCopy = Shader.PropertyToID("_CameraCopyDepthTexture");
            CameraRenderTargetID.opaque = Shader.PropertyToID("_CameraOpaqueTexture");

            m_SampleOffset = Shader.PropertyToID("_SampleOffset");
            m_SampleCount = Shader.PropertyToID("_SampleCount");

            m_ColorRT = new RenderTargetIdentifier(CameraRenderTargetID.color);
            m_CopyColorRT = new RenderTargetIdentifier(CameraRenderTargetID.copyColor);
            m_DepthRT = new RenderTargetIdentifier(CameraRenderTargetID.depth);
            m_CopyDepth = new RenderTargetIdentifier(CameraRenderTargetID.depthCopy);
            m_PostProcessRenderContext = new PostProcessRenderContext();

            m_CopyTextureSupport = SystemInfo.copyTextureSupport;

            // TODO: Profile performance of using ComputeBuffer on mobiles that support it vs
            // fixed buffer size
            if (Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer)
                m_UseComputeBuffer = false;
            else
                m_UseComputeBuffer = true;

            m_LightIndicesBuffer = null;
            m_MaxLocalLightsShadedPerPass = m_UseComputeBuffer ? kMaxVisibleLocalLights : kMaxNonIndexedLocalLights;
            m_LocalLightIndices = new List<int>(m_MaxLocalLightsShadedPerPass);

            m_ShadowPass = new LightweightShadowPass(m_Asset, kMaxVisibleLocalLights);

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != m_Asset.MSAASampleCount)
                QualitySettings.antiAliasing = m_Asset.MSAASampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";

            m_BlitMaterial = CoreUtils.CreateEngineMaterial(m_Asset.BlitShader);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(m_Asset.CopyDepthShader);
            m_SamplingMaterial = CoreUtils.CreateEngineMaterial(m_Asset.SamplingShader);
            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");
        }

        public override void Dispose()
        {
            base.Dispose();
            Shader.globalRenderPipeline = "";

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

            CoreUtils.Destroy(m_ErrorMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif

            if (m_LightIndicesBuffer != null)
            {
                m_LightIndicesBuffer.Release();
                m_LightIndicesBuffer = null;
            }
        }

        private void SetRenderingFeatures()
        {
#if UNITY_EDITOR
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeSupportFlags = SupportedRenderingFeatures.ReflectionProbeSupportFlags.None,
                defaultMixedLightingMode = SupportedRenderingFeatures.LightmapMixedBakeMode.Subtractive,
                supportedMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeMode.Subtractive,
                supportedLightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed,
                supportedLightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
                rendererSupportsLightProbeProxyVolumes = false,
                rendererSupportsMotionVectors = false,
                rendererSupportsReceiveShadows = true,
                rendererSupportsReflectionProbes = true
            };
            SceneViewDrawMode.SetupDrawMode();
#endif
        }

        CullResults m_CullResults;
        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);
            RenderPipeline.BeginFrameRendering(cameras);

            GraphicsSettings.lightsUseLinearIntensity = true;
            SetupPerFrameShaderConstants();

            // Sort cameras array by camera depth
            Array.Sort(cameras, m_CameraComparer);
            foreach (Camera camera in cameras)
            {
                m_CurrCamera = camera;
                bool sceneViewCamera = m_CurrCamera.cameraType == CameraType.SceneView;
                bool stereoEnabled = IsStereoEnabled(m_CurrCamera);

                // Disregard variations around kRenderScaleThreshold.
                m_RenderScale = (Mathf.Abs(1.0f - m_Asset.RenderScale) < kRenderScaleThreshold) ? 1.0f : m_Asset.RenderScale;

                // XR has it's own scaling mechanism.
                m_RenderScale = (m_CurrCamera.cameraType == CameraType.Game && !stereoEnabled) ? m_RenderScale : 1.0f;
                m_IsOffscreenCamera = m_CurrCamera.targetTexture != null && m_CurrCamera.cameraType != CameraType.SceneView;

                SetupPerCameraShaderConstants();
                RenderPipeline.BeginCameraRendering(m_CurrCamera);

                ScriptableCullingParameters cullingParameters;
                if (!CullResults.GetCullingParameters(m_CurrCamera, stereoEnabled, out cullingParameters))
                    continue;

                cullingParameters.shadowDistance = Mathf.Min(m_ShadowPass.RenderingDistance, m_CurrCamera.farClipPlane);

#if UNITY_EDITOR
                // Emit scene view UI
                if (sceneViewCamera)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif

                CullResults.Cull(ref cullingParameters, context, ref m_CullResults);
                List<VisibleLight> visibleLights = m_CullResults.visibleLights;

                LightData lightData;
                InitializeLightData(visibleLights, out lightData);

                bool screenspaceShadows = m_ShadowPass.Execute(ref m_CullResults, ref lightData, ref context);

                FrameRenderingConfiguration frameRenderingConfiguration;
                SetupFrameRenderingConfiguration(out frameRenderingConfiguration, screenspaceShadows, stereoEnabled, sceneViewCamera);
                SetupIntermediateResources(frameRenderingConfiguration, screenspaceShadows, ref context);

                // SetupCameraProperties does the following:
                // Setup Camera RenderTarget and Viewport
                // VR Camera Setup and SINGLE_PASS_STEREO props
                // Setup camera view, proj and their inv matrices.
                // Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
                // Setup camera world clip planes props
                // setup HDR keyword
                // Setup global time properties (_Time, _SinTime, _CosTime)
                context.SetupCameraProperties(m_CurrCamera, stereoEnabled);

                if (CoreUtils.HasFlag(frameRenderingConfiguration, FrameRenderingConfiguration.DepthPrePass))
                    DepthPass(ref context, frameRenderingConfiguration);

                if (screenspaceShadows)
                    m_ShadowPass.CollectShadows(m_CurrCamera, frameRenderingConfiguration, ref context);

                ForwardPass(frameRenderingConfiguration, ref lightData, ref context);

                CommandBuffer cmd;
#if UNITY_EDITOR
                if (sceneViewCamera)
                {
                    cmd = CommandBufferPool.Get("Copy Depth to Camera");
                    cmd.DisableShaderKeyword(kMSAADepthKeyword);
                    CopyTexture(cmd, CameraRenderTargetID.depth, BuiltinRenderTextureType.CameraTarget, m_CopyDepthMaterial, true);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
#endif

                cmd = CommandBufferPool.Get("Dispose of Temporaries");
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.depthCopy);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.depth);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.color);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.copyColor);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.opaque);

                m_ShadowPass.Dispose(cmd);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                context.Submit();
            }
        }

        private void DepthPass(ref ScriptableRenderContext context, FrameRenderingConfiguration frameRenderingConfiguration)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Depth Prepass");
            SetRenderTarget(cmd, m_DepthRT, ClearFlag.Depth);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            var opaqueDrawSettings = new DrawRendererSettings(m_CurrCamera, m_DepthPrepass);
            opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;

            var opaqueFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };

            StartStereoRendering(m_CurrCamera, ref context, frameRenderingConfiguration);

            context.DrawRenderers(m_CullResults.visibleRenderers, ref opaqueDrawSettings, opaqueFilterSettings);

            StopStereoRendering(m_CurrCamera, ref context, frameRenderingConfiguration);
        }

        private void OpaqueTexturePass(ref ScriptableRenderContext context, FrameRenderingConfiguration frameRenderingConfiguration)
        {
            var opaqueScaler = m_OpaqueScalerValues[(int)m_Asset.OpaqueDownsampling];
            RenderTextureDescriptor opaqueDesc = CreateRTDesc(frameRenderingConfiguration, opaqueScaler);

            CommandBuffer cmd = CommandBufferPool.Get("Opaque Copy");
            cmd.GetTemporaryRT(CameraRenderTargetID.opaque, opaqueDesc, m_Asset.OpaqueDownsampling == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
            switch (m_Asset.OpaqueDownsampling)
            {
                case Downsampling.None:
                    cmd.Blit(m_CurrCameraColorRT, CameraRenderTargetID.opaque);
                    break;
                case Downsampling._2xBilinear:
                    cmd.Blit(m_CurrCameraColorRT, CameraRenderTargetID.opaque);
                    break;
                case Downsampling._4xBox:
                    m_SamplingMaterial.SetFloat(m_SampleOffset, 2);
                    cmd.Blit(m_CurrCameraColorRT, CameraRenderTargetID.opaque, m_SamplingMaterial, 0);
                    break;
                case Downsampling._4xBilinear:
                    cmd.Blit(m_CurrCameraColorRT, CameraRenderTargetID.opaque);
                    break;
            }
            SetRenderTarget(cmd, m_CurrCameraColorRT, m_DepthRT);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ForwardPass(FrameRenderingConfiguration frameRenderingConfiguration, ref LightData lightData, ref ScriptableRenderContext context)
        {
            SetupShaderConstants(ref lightData, ref context);

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
            if (m_RequireDepthTexture)
            {
                CommandBuffer cmd = CommandBufferPool.Get("After Opaque");
                cmd.SetGlobalTexture(CameraRenderTargetID.depth, m_DepthRT);

                bool setRenderTarget = false;
                RenderTargetIdentifier depthRT = m_DepthRT;

                // TODO: There's currently an issue in the PostFX stack that has a one frame delay when an effect is enabled/disabled
                // when an effect is disabled, HasOpaqueOnlyEffects returns true in the first frame, however inside render the effect
                // state is update, causing RenderPostProcess here to not blit to FinalColorRT. Until the next frame the RT will have garbage.
                if (CoreUtils.HasFlag(config, FrameRenderingConfiguration.BeforeTransparentPostProcess))
                {
                    // When only have one effect in the stack we blit to a work RT then blit it back to active color RT.
                    // This seems like an extra blit but it saves us a depth copy/blit which has some corner cases like msaa depth resolve.
                    if (m_RequireCopyColor)
                    {
                        RenderPostProcess(cmd, m_CurrCameraColorRT, m_CopyColorRT, true);
                        cmd.Blit(m_CopyColorRT, m_CurrCameraColorRT);
                    }
                    else
                        RenderPostProcess(cmd, m_CurrCameraColorRT, m_CurrCameraColorRT, true);

                    setRenderTarget = true;
                    SetRenderTarget(cmd, m_CurrCameraColorRT, m_DepthRT);
                }

                if (CoreUtils.HasFlag(config, FrameRenderingConfiguration.DepthCopy))
                {
                    bool forceBlit = false;
                    if (m_MSAASamples > 1)
                    {
                        cmd.SetGlobalFloat(m_SampleCount, (float)m_MSAASamples);
                        cmd.EnableShaderKeyword(kMSAADepthKeyword);
                        forceBlit = true;
                    }
                    else
                        cmd.DisableShaderKeyword(kMSAADepthKeyword);

                    CopyTexture(cmd, m_DepthRT, m_CopyDepth, m_CopyDepthMaterial, forceBlit);
                    depthRT = m_CopyDepth;
                    setRenderTarget = true;
                    cmd.SetGlobalTexture(CameraRenderTargetID.depth, m_CopyDepth);
                }

                if (setRenderTarget)
                    SetRenderTarget(cmd, m_CurrCameraColorRT, depthRT);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            if (m_Asset.RequireOpaqueTexture)
            {
                OpaqueTexturePass(ref context, config);
            }
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
            if (!CoreUtils.HasFlag(config, FrameRenderingConfiguration.PostProcess))
                return;

            CommandBuffer cmd = CommandBufferPool.Get("After Transparent");
            RenderPostProcess(cmd, m_CurrCameraColorRT, BuiltinRenderTextureType.CameraTarget, false);
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

        private void SetupFrameRenderingConfiguration(out FrameRenderingConfiguration configuration, bool screenspaceShadows, bool stereoEnabled, bool sceneViewCamera)
        {
            configuration = (stereoEnabled) ? FrameRenderingConfiguration.Stereo : FrameRenderingConfiguration.None;
#if !UNITY_SWITCH
            if (stereoEnabled && XRSettings.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
                m_IntermediateTextureArray = true;
            else
#endif
                m_IntermediateTextureArray = false;

            bool hdrEnabled = m_Asset.SupportsHDR && m_CurrCamera.allowHDR;

            bool defaultRenderScale = Mathf.Approximately(GetRenderScale(), 1.0f);
            bool intermediateTexture = m_CurrCamera.targetTexture != null || m_CurrCamera.cameraType == CameraType.SceneView ||
                !defaultRenderScale || hdrEnabled;

            m_ColorFormat = hdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            m_RequireCopyColor = false;
            m_DepthRenderBuffer = false;
            m_CameraPostProcessLayer = m_CurrCamera.GetComponent<PostProcessLayer>();

            bool msaaEnabled = m_CurrCamera.allowMSAA && m_Asset.MSAASampleCount > 1 && (m_CurrCamera.targetTexture == null || m_CurrCamera.targetTexture.antiAliasing > 1);

            // TODO: PostProcessing and SoftParticles are currently not support for VR
            bool postProcessEnabled = m_CameraPostProcessLayer != null && m_CameraPostProcessLayer.enabled && !stereoEnabled;
            m_RequireDepthTexture = m_Asset.RequireDepthTexture && !stereoEnabled;
            bool canSkipDepthCopy = !m_RequireDepthTexture;
            if (postProcessEnabled)
            {
                m_RequireDepthTexture = true;
                intermediateTexture = true;

                configuration |= FrameRenderingConfiguration.PostProcess;
                if (m_CameraPostProcessLayer.HasOpaqueOnlyEffects(m_PostProcessRenderContext))
                {
                    configuration |= FrameRenderingConfiguration.BeforeTransparentPostProcess;
                    if (m_CameraPostProcessLayer.sortedBundles[PostProcessEvent.BeforeTransparent].Count == 1)
                        m_RequireCopyColor = true;
                }
            }

            if (sceneViewCamera)
            {
                m_RequireDepthTexture = true;
                canSkipDepthCopy = false;
            }

            m_RequireDepthTexture = m_RequireDepthTexture || screenspaceShadows;
            canSkipDepthCopy = canSkipDepthCopy && !screenspaceShadows;

            if (msaaEnabled)
            {
                configuration |= FrameRenderingConfiguration.Msaa;
                intermediateTexture = intermediateTexture || !PlatformSupportsMSAABackBuffer();
                canSkipDepthCopy = false;
            }

            if (m_RequireDepthTexture)
            {
                bool hasTex2DMS = SystemInfo.supportsMultisampledTextures != 0;
                bool copyShaderSupported = m_Asset.CopyDepthShader.isSupported && (msaaEnabled == hasTex2DMS);
                bool supportsDepthCopy = m_CopyTextureSupport != CopyTextureSupport.None || copyShaderSupported;
                bool requiresDepthPrepassToResolveMSAA = msaaEnabled && !hasTex2DMS;
                bool requiresDepthPrepass = !supportsDepthCopy || screenspaceShadows || requiresDepthPrepassToResolveMSAA;

                m_DepthRenderBuffer = !requiresDepthPrepass;
                intermediateTexture = intermediateTexture || m_DepthRenderBuffer;

                if (requiresDepthPrepass)
                    configuration |= FrameRenderingConfiguration.DepthPrePass;
                else if (supportsDepthCopy && !canSkipDepthCopy)
                    configuration |= FrameRenderingConfiguration.DepthCopy;
            }

            Rect cameraRect = m_CurrCamera.rect;
            if (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f || Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f))
                configuration |= FrameRenderingConfiguration.DefaultViewport;
            else
                intermediateTexture = true;

            if (intermediateTexture)
                configuration |= FrameRenderingConfiguration.IntermediateTexture;
        }

        private RenderTextureDescriptor CreateRTDesc(FrameRenderingConfiguration renderingConfig, float scaler = 1.0f)
        {
            RenderTextureDescriptor desc;
#if !UNITY_SWITCH
            if (CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
                desc = XRSettings.eyeTextureDesc;
            else
#endif
                desc = new RenderTextureDescriptor(m_CurrCamera.pixelWidth, m_CurrCamera.pixelHeight);

            float renderScale = GetRenderScale();
            desc.width = (int)((float)desc.width * renderScale * scaler);
            desc.height = (int)((float)desc.height * renderScale * scaler);
            return desc;
        }

        private void SetupIntermediateResources(FrameRenderingConfiguration renderingConfig, bool shadows, ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Setup Intermediate Resources");

            int msaaSamples = (m_IsOffscreenCamera) ? Math.Min(m_CurrCamera.targetTexture.antiAliasing, m_Asset.MSAASampleCount) : m_Asset.MSAASampleCount;
            msaaSamples = (CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Msaa)) ? msaaSamples : 1;
            m_MSAASamples = msaaSamples;
            m_CurrCameraColorRT = BuiltinRenderTextureType.CameraTarget;

            if (CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture) || m_RequireDepthTexture)
                SetupIntermediateRenderTextures(cmd, renderingConfig, shadows, msaaSamples);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetupIntermediateRenderTextures(CommandBuffer cmd, FrameRenderingConfiguration renderingConfig, bool shadows, int msaaSamples)
        {
            RenderTextureDescriptor baseDesc = CreateRTDesc(renderingConfig);

            if (m_RequireDepthTexture)
            {
                var depthRTDesc = baseDesc;
                depthRTDesc.colorFormat = RenderTextureFormat.Depth;
                depthRTDesc.depthBufferBits = kDepthStencilBufferBits;

                // This also means we don't do a depth copy
                bool doesDepthPrepass = CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DepthPrePass);
                if (!doesDepthPrepass)
                {
                    depthRTDesc.bindMS = msaaSamples > 1;
                    depthRTDesc.msaaSamples = msaaSamples;
                }

                cmd.GetTemporaryRT(CameraRenderTargetID.depth, depthRTDesc, FilterMode.Point);

                if (!doesDepthPrepass)
                {
                    depthRTDesc.bindMS = false;
                    depthRTDesc.msaaSamples = 1;
                    cmd.GetTemporaryRT(CameraRenderTargetID.depthCopy, depthRTDesc, FilterMode.Point);
                }

                m_ShadowPass.InitializeResources(cmd, baseDesc);
            }

            var colorRTDesc = baseDesc;
            colorRTDesc.colorFormat = m_ColorFormat;
            colorRTDesc.depthBufferBits = kDepthStencilBufferBits; // TODO: does the color RT always need depth?
            colorRTDesc.sRGB = true;
            colorRTDesc.msaaSamples = msaaSamples;
            colorRTDesc.enableRandomWrite = false;

            // When offscreen camera current rendertarget is CameraTarget
            if (!m_IsOffscreenCamera)
            {
                cmd.GetTemporaryRT(CameraRenderTargetID.color, colorRTDesc, FilterMode.Bilinear);
                m_CurrCameraColorRT = m_ColorRT;
            }

            // When BeforeTransparent PostFX is enabled and only one effect is in the stack we need to create a temp
            // color RT to blit the effect.
            if (m_RequireCopyColor)
                cmd.GetTemporaryRT(CameraRenderTargetID.copyColor, colorRTDesc, FilterMode.Point);
        }

        private void SetupShaderConstants(ref LightData lightData, ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SetupShaderConstants");
            SetupShaderLightConstants(cmd, ref lightData);
            SetShaderKeywords(cmd, ref lightData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void InitializeLightData(List<VisibleLight> visibleLights, out LightData lightData)
        {
            m_LocalLightIndices.Clear();
            for (int i = 0; i < visibleLights.Count; ++i)
            {
                if (visibleLights[i].lightType != LightType.Directional)
                    m_LocalLightIndices.Add(i);
            }

            // Clear to default all light constant data
            for (int i = 0; i < kMaxVisibleLocalLights; ++i)
                InitializeLightConstants(visibleLights, -1, out m_LightPositions[i],
                    out m_LightColors[i],
                    out m_LightDistanceAttenuations[i],
                    out m_LightSpotDirections[i],
                    out m_LightSpotAttenuations[i]);

            int visibleLightsCount = Math.Min(visibleLights.Count, m_Asset.MaxPixelLights);
            lightData.mainLightIndex = GetMainLight(visibleLights);

            // If we have a main light we don't shade it in the per-object light loop. We also remove it from the per-object cull list
            int mainLightPresent = (lightData.mainLightIndex >= 0) ? 1 : 0;
            int additionalPixelLightsCount = Math.Min(visibleLightsCount - mainLightPresent, m_MaxLocalLightsShadedPerPass);
            int vertexLightCount = (m_Asset.SupportsVertexLight) ? Math.Min(visibleLights.Count, m_MaxLocalLightsShadedPerPass) - additionalPixelLightsCount : 0;
            vertexLightCount = Math.Min(vertexLightCount, kMaxVertexLights);

            lightData.pixelAdditionalLightsCount = additionalPixelLightsCount;
            lightData.totalAdditionalLightsCount = additionalPixelLightsCount + vertexLightCount;
            lightData.visibleLights = visibleLights;
            lightData.localLightIndices = m_LocalLightIndices;

            m_MixedLightingSetup = MixedLightingSetup.None;

            if (lightData.totalAdditionalLightsCount > 0)
            {
                int[] perObjectLightIndexMap = m_CullResults.GetLightIndexMap();
                int directionalLightCount = 0;

                // Disable all directional lights from the perobject light indices
                for (int i = 0; i < visibleLights.Count; ++i)
                {
                    VisibleLight light = visibleLights[i];
                    if (light.lightType == LightType.Directional)
                    {
                        perObjectLightIndexMap[i] = -1;
                        ++directionalLightCount;
                    }
                    else
                        perObjectLightIndexMap[i] -= directionalLightCount;
                }
                m_CullResults.SetLightIndexMap(perObjectLightIndexMap);

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (m_UseComputeBuffer)
                {
                    int lightIndicesCount = m_CullResults.GetLightIndicesCount();
                    if (lightIndicesCount > 0)
                    {
                        if (m_LightIndicesBuffer == null)
                        {
                            m_LightIndicesBuffer = new ComputeBuffer(lightIndicesCount, sizeof(int));
                        }
                        else if (m_LightIndicesBuffer.count < lightIndicesCount)
                        {
                            m_LightIndicesBuffer.Release();
                            m_LightIndicesBuffer = new ComputeBuffer(lightIndicesCount, sizeof(int));
                        }

                        m_CullResults.FillLightIndices(m_LightIndicesBuffer);
                    }
                }
            }
        }

        // Main Light is always a directional light
        private int GetMainLight(List<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Count;

            if (totalVisibleLights == 0 || m_Asset.MaxPixelLights == 0)
                return -1;

            for (int i = 0; i < totalVisibleLights; ++i)
            {
                VisibleLight currLight = visibleLights[i];

                // Particle system lights have the light property as null. We sort lights so all particles lights
                // come last. Therefore, if first light is particle light then all lights are particle lights.
                // In this case we either have no main light or already found it.
                if (currLight.light == null)
                    break;

                // In case no shadow light is present we will return the brightest directional light
                if (currLight.lightType == LightType.Directional)
                    return i;
            }

            return -1;
        }

        private void InitializeLightConstants(List<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightDistanceAttenuation, out Vector4 lightSpotDir,
            out Vector4 lightSpotAttenuation)
        {
            lightPos = kDefaultLightPosition;
            lightColor = kDefaultLightColor;
            lightDistanceAttenuation = kDefaultLightSpotAttenuation;
            lightSpotDir = kDefaultLightSpotDirection;
            lightSpotAttenuation = kDefaultLightAttenuation;

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

            // VisibleLight.finalColor already returns color in active color space
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
                lightDistanceAttenuation = new Vector4(quadAtten, oneOverFadeRangeSqr, lightRangeSqrOverFadeRangeSqr, 1.0f);
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
                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * lightData.spotAngle * 0.5f);
                // We neeed to do a null check for particle lights
                // This should be changed in the future
                // Particle lights will use an inline function
                float cosInnerAngle;
                if (lightData.light != null)
                    cosInnerAngle = Mathf.Cos(LightmapperUtils.ExtractInnerCone(lightData.light) * 0.5f);
                else
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(lightData.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightSpotAttenuation = new Vector4(invAngleRange, add, 0.0f);
            }

            Light light = lightData.light;

            // TODO: Add support to shadow mask
            if (light != null && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
            {
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
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            Shader.SetGlobalVector(PerFrameBuffer._GlossyEnvironmentColor, glossyEnvColor);

            // Used when subtractive mode is selected
            Shader.SetGlobalVector(PerFrameBuffer._SubtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
        }

        private void SetupPerCameraShaderConstants()
        {
            float cameraWidth = GetScaledCameraWidth(m_CurrCamera);
            float cameraHeight = GetScaledCameraHeight(m_CurrCamera);
            Shader.SetGlobalVector(PerCameraBuffer._ScaledScreenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
        }

        private void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref lightData);
            SetupAdditionalLightConstants(cmd, ref lightData);
        }

        private void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightDistanceAttenuation, lightSpotDir, lightSpotAttenuation;
            List<VisibleLight> lights = lightData.visibleLights;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightDistanceAttenuation, out lightSpotDir, out lightSpotAttenuation);

            if (lightData.mainLightIndex >= 0)
            {
                VisibleLight mainLight = lights[lightData.mainLightIndex];
                Light mainLightRef = mainLight.light;

                if (IsSupportedCookieType(mainLight.lightType) && mainLightRef.cookie != null)
                {
                    Matrix4x4 lightCookieMatrix;
                    GetLightCookieMatrix(mainLight, out lightCookieMatrix);
                    cmd.SetGlobalTexture(PerCameraBuffer._MainLightCookie, mainLightRef.cookie);
                    cmd.SetGlobalMatrix(PerCameraBuffer._WorldToLight, lightCookieMatrix);
                }
            }

            cmd.SetGlobalVector(PerCameraBuffer._MainLightPosition, new Vector4(lightPos.x, lightPos.y, lightPos.z, lightDistanceAttenuation.w));
            cmd.SetGlobalVector(PerCameraBuffer._MainLightColor, lightColor);
        }

        private void SetupAdditionalLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            List<VisibleLight> lights = lightData.visibleLights;
            if (lightData.totalAdditionalLightsCount > 0)
            {
                int localLightsCount = 0;
                for (int i = 0; i < lights.Count && localLightsCount < kMaxVisibleLocalLights; ++i)
                {
                    VisibleLight light = lights[i];
                    if (light.lightType != LightType.Directional)
                    {
                        InitializeLightConstants(lights, i, out m_LightPositions[localLightsCount],
                            out m_LightColors[localLightsCount],
                            out m_LightDistanceAttenuations[localLightsCount],
                            out m_LightSpotDirections[localLightsCount],
                            out m_LightSpotAttenuations[localLightsCount]);
                        localLightsCount++;
                    }
                }

                cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, new Vector4(lightData.pixelAdditionalLightsCount,
                        lightData.totalAdditionalLightsCount, 0.0f, 0.0f));

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (m_UseComputeBuffer)
                    cmd.SetGlobalBuffer("_LightIndexBuffer", m_LightIndicesBuffer);
            }
            else
            {
                cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, Vector4.zero);
            }

            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightPosition, m_LightPositions);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightColor, m_LightColors);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightDistanceAttenuation, m_LightDistanceAttenuations);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotDir, m_LightSpotDirections);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotAttenuation, m_LightSpotAttenuations);
        }

        private void SetShaderKeywords(CommandBuffer cmd, ref LightData lightData)
        {
            int vertexLightsCount = lightData.totalAdditionalLightsCount - lightData.pixelAdditionalLightsCount;

            CoreUtils.SetKeyword(cmd, LightweightKeywords.AdditionalLightsText, lightData.totalAdditionalLightsCount > 0);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.MixedLightingSubtractiveText, m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.VertexLightsText, vertexLightsCount > 0);

            // TODO: We have to discuss cookie approach on LWRP.
            // CoreUtils.SetKeyword(cmd, LightweightKeywords.MainLightCookieText, mainLightIndex != -1 && LightweightUtils.IsSupportedCookieType(visibleLights[mainLightIndex].lightType) && visibleLights[mainLightIndex].light.cookie != null);

            bool anyShadowsEnabled = m_ShadowPass.IsDirectionalShadowsEnabled || m_ShadowPass.IsLocalShadowsEnabled;
            CoreUtils.SetKeyword(cmd, LightweightKeywords.DirectionalShadowsText, m_ShadowPass.DirectionalShadowsRendered);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.LocalShadowsText, m_ShadowPass.LocalShadowsRendered);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.SoftShadowsText, m_ShadowPass.IsSoftShadowsEnabled && anyShadowsEnabled);

            // TODO: Remove this. legacy particles support will be removed from Unity in 2018.3. This should be a shader_feature instead with prop exposed in the Standard particles shader.
            CoreUtils.SetKeyword(cmd, "SOFTPARTICLES_ON", m_RequireDepthTexture && m_Asset.RequireSoftParticles);
        }

        private void BeginForwardRendering(ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfig)
        {
            RenderTargetIdentifier colorRT = BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier depthRT = BuiltinRenderTextureType.None;

            StartStereoRendering(m_CurrCamera, ref context, renderingConfig);

            CommandBuffer cmd = CommandBufferPool.Get("SetCameraRenderTarget");
            bool intermediateTexture = CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture);
            if (intermediateTexture)
            {
                if (!m_IsOffscreenCamera)
                    colorRT = m_CurrCameraColorRT;

                if (m_DepthRenderBuffer)
                    depthRT = m_DepthRT;
            }

            ClearFlag clearFlag = ClearFlag.None;
            CameraClearFlags cameraClearFlags = m_CurrCamera.clearFlags;
            if (cameraClearFlags != CameraClearFlags.Nothing)
            {
                clearFlag |= ClearFlag.Depth;
                if (cameraClearFlags == CameraClearFlags.Color || cameraClearFlags == CameraClearFlags.Skybox)
                    clearFlag |= ClearFlag.Color;
            }

            SetRenderTarget(cmd, colorRT, depthRT, clearFlag);
            m_CurrCameraColorRT = colorRT;

            // If rendering to an intermediate RT we resolve viewport on blit due to offset not being supported
            // while rendering to a RT.
            if (!intermediateTexture && !CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DefaultViewport))
                cmd.SetViewport(m_CurrCamera.pixelRect);

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
                cmd.Blit(m_CurrCameraColorRT, BuiltinRenderTextureType.CameraTarget);
            }
            else if (CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture))
            {
                Material blitMaterial = m_BlitMaterial;
                if (CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
                    blitMaterial = null;

                // If PostProcessing is enabled, it is already blit to CameraTarget.
                if (!CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.PostProcess))
                    Blit(cmd, renderingConfig, m_CurrCameraColorRT, BuiltinRenderTextureType.CameraTarget, blitMaterial);
            }

            SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
            {
                context.StopMultiEye(m_CurrCamera);
                context.StereoEndRender(m_CurrCamera);
            }
        }

        private bool IsStereoEnabled(Camera camera)
        {
#if !UNITY_SWITCH
            bool isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            return XRSettings.isDeviceActive && !isSceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
#else
            return false;
#endif
        }

        private float GetRenderScale()
        {
            return m_RenderScale;
        }

        private float GetScaledCameraWidth(Camera camera)
        {
            return (float)camera.pixelWidth * GetRenderScale();
        }

        private float GetScaledCameraHeight(Camera camera)
        {
            return (float)camera.pixelHeight * GetRenderScale();
        }

        private RendererConfiguration GetRendererSettings(ref LightData lightData)
        {
            RendererConfiguration settings = RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
            if (lightData.totalAdditionalLightsCount > 0)
            {
                if (m_UseComputeBuffer)
                    settings |= RendererConfiguration.ProvideLightIndices;
                else
                    settings |= RendererConfiguration.PerObjectLightIndices8;
            }
            return settings;
        }

        private void RenderPostProcess(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly)
        {
            m_PostProcessRenderContext.Reset();
            m_PostProcessRenderContext.camera = m_CurrCamera;
            m_PostProcessRenderContext.source = source;
            m_PostProcessRenderContext.sourceFormat = m_ColorFormat;
            m_PostProcessRenderContext.destination = dest;
            m_PostProcessRenderContext.command = cmd;
            m_PostProcessRenderContext.flip = m_CurrCamera.targetTexture == null;

            if (opaqueOnly)
            {
                m_CameraPostProcessLayer.RenderOpaqueOnly(m_PostProcessRenderContext);
            }
            else
                m_CameraPostProcessLayer.Render(m_PostProcessRenderContext);
        }

        private void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorRT, ClearFlag clearFlag = ClearFlag.None)
        {
            int depthSlice = (m_IntermediateTextureArray) ? -1 : 0;
            CoreUtils.SetRenderTarget(cmd, colorRT, clearFlag, CoreUtils.ConvertSRGBToActiveColorSpace(m_CurrCamera.backgroundColor), 0, CubemapFace.Unknown, depthSlice);
        }

        private void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorRT, RenderTargetIdentifier depthRT, ClearFlag clearFlag = ClearFlag.None)
        {
            if (depthRT == BuiltinRenderTextureType.None || !m_DepthRenderBuffer)
            {
                SetRenderTarget(cmd, colorRT, clearFlag);
                return;
            }

            int depthSlice = (m_IntermediateTextureArray) ? -1 : 0;
            CoreUtils.SetRenderTarget(cmd, colorRT, depthRT, clearFlag, CoreUtils.ConvertSRGBToActiveColorSpace(m_CurrCamera.backgroundColor), 0, CubemapFace.Unknown, depthSlice);
        }

        private void Blit(CommandBuffer cmd, FrameRenderingConfiguration renderingConfig, RenderTargetIdentifier sourceRT, RenderTargetIdentifier destRT, Material material = null)
        {
            cmd.SetGlobalTexture(m_BlitTexID, sourceRT);
            if (CoreUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DefaultViewport))
            {
                cmd.Blit(sourceRT, destRT, material);
            }
            else
            {
                // Viewport doesn't work when rendering to an RT
                // We have to manually blit by rendering a fullscreen quad
                SetRenderTarget(cmd, destRT);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(m_CurrCamera.pixelRect);
                DrawFullScreen(cmd, m_BlitMaterial);
            }
        }

        private void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier sourceRT, RenderTargetIdentifier destRT, Material copyMaterial, bool forceBlit = false)
        {
            if (m_CopyTextureSupport != CopyTextureSupport.None && !forceBlit)
                cmd.CopyTexture(sourceRT, destRT);
            else
                cmd.Blit(sourceRT, destRT, copyMaterial);
        }
    }
}
