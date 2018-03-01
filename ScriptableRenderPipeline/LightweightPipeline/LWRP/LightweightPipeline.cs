using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine.Experimental.GlobalIllumination;
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

        public RenderTextureFormat renderTextureFormat;

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
                    defaultShadowSettings.directionalLightNearPlaneOffset = 5;
                    defaultShadowSettings.maxShadowDistance = 1000.0F;
                    defaultShadowSettings.renderTextureFormat = RenderTextureFormat.Shadowmap;
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
        public LightShadows shadowMapSampleType;
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

        private Vector4 kDefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        private Vector4 kDefaultLightColor = Color.black;
        private Vector4 kDefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        private Vector4 kDefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        private Vector4 kDefaultLightSpotAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

        private Vector4[] m_LightPositions = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightColors = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightDistanceAttenuations = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightSpotDirections = new Vector4[kMaxVisibleLights];
        private Vector4[] m_LightSpotAttenuations = new Vector4[kMaxVisibleLights];

        private Camera m_CurrCamera;

        private const int kMaxCascades = 4;
        private int m_ShadowCasterCascadesCount;
        private int m_ShadowMapRTID;
        private int m_ScreenSpaceShadowMapRTID;
        private Matrix4x4[] m_ShadowMatrices = new Matrix4x4[kMaxCascades + 1];
        private RenderTargetIdentifier m_CurrCameraColorRT;
        private RenderTargetIdentifier m_ShadowMapRT;
        private RenderTargetIdentifier m_ScreenSpaceShadowMapRT;
        private RenderTargetIdentifier m_ColorRT;
        private RenderTargetIdentifier m_CopyColorRT;
        private RenderTargetIdentifier m_DepthRT;
        private RenderTargetIdentifier m_CopyDepth;
        private RenderTargetIdentifier m_Color;

        private bool m_IntermediateTextureArray;
        private bool m_RequireDepthTexture;
        private bool m_RequireCopyColor;
        private bool m_DepthRenderBuffer;
        private MixedLightingSetup m_MixedLightingSetup;

        private const int kDepthStencilBufferBits = 32;
        private Vector4[] m_DirectionalShadowSplitDistances = new Vector4[kMaxCascades];
        private Vector4 m_DirectionalShadowSplitRadii;

        private ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        private ShadowSliceData[] m_ShadowSlices = new ShadowSliceData[kMaxCascades];

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
        private LightComparer m_LightComparer = new LightComparer();

        // Maps from sorted light indices to original unsorted. We need this for shadow rendering
        // and per-object light lists.
        private List<int> m_SortedLightIndexMap = new List<int>();

        private Dictionary<VisibleLight, int> m_VisibleLightsIDMap = new Dictionary<VisibleLight, int>(new LightEqualityComparer());

        private Mesh m_BlitQuad;
        private Material m_BlitMaterial;
        private Material m_CopyDepthMaterial;
        private Material m_ErrorMaterial;
        private Material m_ScreenSpaceShadowsMaterial;
        private int m_BlitTexID = Shader.PropertyToID("_BlitTex");

        private CopyTextureSupport m_CopyTextureSupport;

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            m_Asset = asset;

            BuildShadowSettings();
            SetRenderingFeatures();

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

            ShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_WorldToShadow");
            ShadowConstantBuffer._ShadowData = Shader.PropertyToID("_ShadowData");
            ShadowConstantBuffer._DirShadowSplitSpheres = Shader.PropertyToID("_DirShadowSplitSpheres");
            ShadowConstantBuffer._DirShadowSplitSphereRadii = Shader.PropertyToID("_DirShadowSplitSphereRadii");
            ShadowConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_ShadowOffset0");
            ShadowConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_ShadowOffset1");
            ShadowConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_ShadowOffset2");
            ShadowConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_ShadowOffset3");
            ShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_ShadowmapSize");

            m_ShadowMapRTID = Shader.PropertyToID("_ShadowMap");
            m_ScreenSpaceShadowMapRTID = Shader.PropertyToID("_ScreenSpaceShadowMap");

            CameraRenderTargetID.color = Shader.PropertyToID("_CameraColorRT");
            CameraRenderTargetID.copyColor = Shader.PropertyToID("_CameraCopyColorRT");
            CameraRenderTargetID.depth = Shader.PropertyToID("_CameraDepthTexture");
            CameraRenderTargetID.depthCopy = Shader.PropertyToID("_CameraCopyDepthTexture");

            m_ShadowMapRT = new RenderTargetIdentifier(m_ShadowMapRTID);
            m_ScreenSpaceShadowMapRT = new RenderTargetIdentifier(m_ScreenSpaceShadowMapRTID);

            m_ColorRT = new RenderTargetIdentifier(CameraRenderTargetID.color);
            m_CopyColorRT = new RenderTargetIdentifier(CameraRenderTargetID.copyColor);
            m_DepthRT = new RenderTargetIdentifier(CameraRenderTargetID.depth);
            m_CopyDepth = new RenderTargetIdentifier(CameraRenderTargetID.depthCopy);
            m_PostProcessRenderContext = new PostProcessRenderContext();

            m_CopyTextureSupport = SystemInfo.copyTextureSupport;

            for (int i = 0; i < kMaxCascades; ++i)
                m_DirectionalShadowSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            m_DirectionalShadowSplitRadii = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != m_Asset.MSAASampleCount)
                QualitySettings.antiAliasing = m_Asset.MSAASampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";

            m_BlitQuad = LightweightUtils.CreateQuadMesh(false);
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(m_Asset.BlitShader);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(m_Asset.CopyDepthShader);
            m_ScreenSpaceShadowsMaterial = CoreUtils.CreateEngineMaterial(m_Asset.ScreenSpaceShadowShader);
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
                RenderPipeline.BeginCameraRendering(camera);

                bool sceneViewCamera = camera.cameraType == CameraType.SceneView;
                bool stereoEnabled = XRSettings.isDeviceActive && !sceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
                m_CurrCamera = camera;
                m_IsOffscreenCamera = m_CurrCamera.targetTexture != null && m_CurrCamera.cameraType != CameraType.SceneView;

                var cmd = CommandBufferPool.Get("");
                cmd.BeginSample("LightweightPipeline.Render");
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

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
                List<VisibleLight> visibleLights = m_CullResults.visibleLights;

                LightData lightData;
                InitializeLightData(visibleLights, out lightData);

                bool shadows = ShadowPass(visibleLights, ref context, ref lightData);

                FrameRenderingConfiguration frameRenderingConfiguration;
                SetupFrameRenderingConfiguration(out frameRenderingConfiguration, shadows, stereoEnabled, sceneViewCamera);
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

                if (LightweightUtils.HasFlag(frameRenderingConfiguration, FrameRenderingConfiguration.DepthPrePass))
                {
                    DepthPass(ref context, frameRenderingConfiguration);

                    // Only screen space shadowmap mode is supported.
                    if (shadows)
                        ShadowCollectPass(visibleLights, ref context, ref lightData, frameRenderingConfiguration);
                }

                if (!shadows)
                {
                    var setRT = CommandBufferPool.Get("Generate Small Shadow Buffer");
                    setRT.GetTemporaryRT(m_ScreenSpaceShadowMapRTID, 4, 4, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
                    setRT.Blit(Texture2D.whiteTexture, m_ScreenSpaceShadowMapRT);
                    context.ExecuteCommandBuffer(setRT);
                }

                ForwardPass(visibleLights, frameRenderingConfiguration, ref context, ref lightData, stereoEnabled);


                cmd.name = "After Camera Render";
#if UNITY_EDITOR
                if (sceneViewCamera)
                    CopyTexture(cmd, CameraRenderTargetID.depth, BuiltinRenderTextureType.CameraTarget, m_CopyDepthMaterial, true);
#endif
                cmd.ReleaseTemporaryRT(m_ShadowMapRTID);
                cmd.ReleaseTemporaryRT(m_ScreenSpaceShadowMapRTID);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.depthCopy);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.depth);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.color);
                cmd.ReleaseTemporaryRT(CameraRenderTargetID.copyColor);

                cmd.EndSample("LightweightPipeline.Render");

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                context.Submit();
            }
        }

        private bool ShadowPass(List<VisibleLight> visibleLights, ref ScriptableRenderContext context, ref LightData lightData)
        {
            if (m_Asset.AreShadowsEnabled() && lightData.mainLightIndex != -1)
            {
                VisibleLight mainLight = visibleLights[lightData.mainLightIndex];

                if (mainLight.light.shadows != LightShadows.None)
                {
                    if (!LightweightUtils.IsSupportedShadowType(mainLight.lightType))
                    {
                        Debug.LogWarning("Only directional and spot shadows are supported by LightweightPipeline.");
                        return false;
                    }

                    // There's no way to map shadow light indices. We need to pass in the original unsorted index.
                    // If no additional lights then no light sorting is performed and the indices match.
                    int shadowOriginalIndex = (lightData.totalAdditionalLightsCount > 0) ? GetLightUnsortedIndex(lightData.mainLightIndex) : lightData.mainLightIndex;
                    bool shadowsRendered = RenderShadows(ref m_CullResults, ref mainLight, shadowOriginalIndex, ref context);
                    if (shadowsRendered)
                    {
                        lightData.shadowMapSampleType = (m_Asset.ShadowSetting != ShadowType.SOFT_SHADOWS)
                            ? LightShadows.Hard
                            : mainLight.light.shadows;
                    }
                    else
                    {
                        lightData.shadowMapSampleType = LightShadows.None;
                    }

                    return shadowsRendered;
                }
            }

            return false;
        }

        private void ShadowCollectPass(List<VisibleLight> visibleLights, ref ScriptableRenderContext context, ref LightData lightData, FrameRenderingConfiguration frameRenderingConfiguration)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Collect Shadows");

            SetupShadowReceiverConstants(cmd, visibleLights[lightData.mainLightIndex]);
            SetShadowCollectPassKeywords(cmd, visibleLights[lightData.mainLightIndex], ref lightData);


            // TODO: Support RenderScale for the SSSM target.  Should probably move allocation elsewhere, or at
            // least propogate RenderTextureDescriptor generation
            if (LightweightUtils.HasFlag(frameRenderingConfiguration, FrameRenderingConfiguration.Stereo))
            {
                var desc = XRSettings.eyeTextureDesc;
                desc.depthBufferBits = 0;
                desc.colorFormat = RenderTextureFormat.R8;
                cmd.GetTemporaryRT(m_ScreenSpaceShadowMapRTID, desc, FilterMode.Bilinear);
            }
            else
            {
                cmd.GetTemporaryRT(m_ScreenSpaceShadowMapRTID, m_CurrCamera.pixelWidth, m_CurrCamera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
            }

            // Note: The source isn't actually 'used', but there's an engine peculiarity (bug) that 
            // doesn't like null sources when trying to determine a stereo-ized blit.  So for proper
            // stereo functionality, we use the screen-space shadow map as the source (until we have
            // a better solution).
            // An alternative would be DrawProcedural, but that would require further changes in the shader.
            cmd.Blit(m_ScreenSpaceShadowMapRT, m_ScreenSpaceShadowMapRT, m_ScreenSpaceShadowsMaterial);

            StartStereoRendering(ref context, frameRenderingConfiguration);

            context.ExecuteCommandBuffer(cmd);

            StopStereoRendering(ref context, frameRenderingConfiguration);

            CommandBufferPool.Release(cmd);
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

            StartStereoRendering(ref context, frameRenderingConfiguration);

            context.DrawRenderers(m_CullResults.visibleRenderers, ref opaqueDrawSettings, opaqueFilterSettings);

            StopStereoRendering(ref context, frameRenderingConfiguration);
        }

        private void ForwardPass(List<VisibleLight> visibleLights, FrameRenderingConfiguration frameRenderingConfiguration, ref ScriptableRenderContext context, ref LightData lightData, bool stereoEnabled)
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
            if (!m_RequireDepthTexture)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("After Opaque");
            cmd.SetGlobalTexture(CameraRenderTargetID.depth, m_DepthRT);

            bool setRenderTarget = false;
            RenderTargetIdentifier depthRT = m_DepthRT;

            // TODO: There's currently an issue in the PostFX stack that has a one frame delay when an effect is enabled/disabled
            // when an effect is disabled, HasOpaqueOnlyEffects returns true in the first frame, however inside render the effect
            // state is update, causing RenderPostProcess here to not blit to FinalColorRT. Until the next frame the RT will have garbage.
            if (LightweightUtils.HasFlag(config, FrameRenderingConfiguration.BeforeTransparentPostProcess))
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

            if (LightweightUtils.HasFlag(config, FrameRenderingConfiguration.DepthCopy))
            {
                CopyTexture(cmd, m_DepthRT, m_CopyDepth, m_CopyDepthMaterial);
                depthRT = m_CopyDepth;
                setRenderTarget = true;
            }

            if (setRenderTarget)
                SetRenderTarget(cmd, m_CurrCameraColorRT, depthRT);
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

        private void BuildShadowSettings()
        {
            m_ShadowSettings = ShadowSettings.Default;
            m_ShadowSettings.directionalLightCascadeCount = m_Asset.CascadeCount;

            m_ShadowSettings.shadowAtlasWidth = m_Asset.ShadowAtlasResolution;
            m_ShadowSettings.shadowAtlasHeight = m_Asset.ShadowAtlasResolution;
            m_ShadowSettings.maxShadowDistance = m_Asset.ShadowDistance;
            m_ShadowSettings.renderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;

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

        private void SetupFrameRenderingConfiguration(out FrameRenderingConfiguration configuration, bool shadows, bool stereoEnabled, bool sceneViewCamera)
        {
            configuration = (stereoEnabled) ? FrameRenderingConfiguration.Stereo : FrameRenderingConfiguration.None;
            if (stereoEnabled && XRSettings.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
                m_IntermediateTextureArray = true;
            else
                m_IntermediateTextureArray = false;

            bool hdrEnabled = m_Asset.SupportsHDR && m_CurrCamera.allowHDR;
            bool intermediateTexture = m_CurrCamera.targetTexture != null || m_CurrCamera.cameraType == CameraType.SceneView ||
                m_Asset.RenderScale < 1.0f || hdrEnabled;

            m_ColorFormat = hdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            m_RequireCopyColor = false;
            m_DepthRenderBuffer = false;
            m_CameraPostProcessLayer = m_CurrCamera.GetComponent<PostProcessLayer>();

            bool msaaEnabled = m_CurrCamera.allowMSAA && m_Asset.MSAASampleCount > 1 && (m_CurrCamera.targetTexture == null || m_CurrCamera.targetTexture.antiAliasing > 1);

            // TODO: PostProcessing and SoftParticles are currently not support for VR
            bool postProcessEnabled = m_CameraPostProcessLayer != null && m_CameraPostProcessLayer.enabled && !stereoEnabled;
            m_RequireDepthTexture = m_Asset.RequireDepthTexture && !stereoEnabled;
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
                m_RequireDepthTexture = true;

            if (shadows)
            {
                m_RequireDepthTexture = true;

                if (!msaaEnabled)
                    intermediateTexture = true;
            }

            if (msaaEnabled)
            {
                configuration |= FrameRenderingConfiguration.Msaa;
                intermediateTexture = intermediateTexture || !LightweightUtils.PlatformSupportsMSAABackBuffer();
            }

            if (m_RequireDepthTexture)
            {
                // If msaa is enabled we don't use a depth renderbuffer as we might not have support to Texture2DMS to resolve depth.
                // Instead we use a depth prepass and whenever depth is needed we use the 1 sample depth from prepass.
                // Screen space shadows require depth before opaque shading.
                if (!msaaEnabled && !shadows)
                {
                    bool supportsDepthCopy = m_CopyTextureSupport != CopyTextureSupport.None && m_Asset.CopyDepthShader.isSupported;
                    m_DepthRenderBuffer = true;
                    intermediateTexture = true;

                    // If requiring a camera depth texture we need separate depth as it reads/write to depth at same time
                    // Post process doesn't need the copy
                    if (!m_Asset.RequireDepthTexture && postProcessEnabled)
                        configuration |= (supportsDepthCopy) ? FrameRenderingConfiguration.DepthCopy : FrameRenderingConfiguration.DepthPrePass;
                }
                else
                {
                    configuration |= FrameRenderingConfiguration.DepthPrePass;
                }
            }

            Rect cameraRect = m_CurrCamera.rect;
            if (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f || Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f))
                configuration |= FrameRenderingConfiguration.DefaultViewport;
            else
                intermediateTexture = true;

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
                SetupIntermediateRenderTextures(cmd, renderingConfig, msaaSamples);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetupIntermediateRenderTextures(CommandBuffer cmd, FrameRenderingConfiguration renderingConfig, int msaaSamples)
        {
            RenderTextureDescriptor baseDesc;
            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
                baseDesc = XRSettings.eyeTextureDesc;
            else
                baseDesc = new RenderTextureDescriptor(m_CurrCamera.pixelWidth, m_CurrCamera.pixelHeight);

            float renderScale = (m_CurrCamera.cameraType == CameraType.Game) ? m_Asset.RenderScale : 1.0f;
            baseDesc.width = (int)((float)baseDesc.width * renderScale);
            baseDesc.height = (int)((float)baseDesc.height * renderScale);

            // TODO: Might be worth caching baseDesc for allocation of other targets (Screen-space Shadow Map?)

            if (m_RequireDepthTexture)
            {
                var depthRTDesc = baseDesc;
                depthRTDesc.colorFormat = RenderTextureFormat.Depth;
                depthRTDesc.depthBufferBits = kDepthStencilBufferBits;

                cmd.GetTemporaryRT(CameraRenderTargetID.depth, depthRTDesc, FilterMode.Bilinear);

                if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DepthCopy))
                    cmd.GetTemporaryRT(CameraRenderTargetID.depthCopy, depthRTDesc, FilterMode.Bilinear);
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

        private void SetupShaderConstants(List<VisibleLight> visibleLights, ref ScriptableRenderContext context, ref LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SetupShaderConstants");
            SetupShaderLightConstants(cmd, visibleLights, ref lightData);
            SetShaderKeywords(cmd, ref lightData, visibleLights);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void InitializeLightData(List<VisibleLight> visibleLights, out LightData lightData)
        {
            int visibleLightsCount = Math.Min(visibleLights.Count, m_Asset.MaxPixelLights);
            m_SortedLightIndexMap.Clear();

            lightData.shadowMapSampleType = LightShadows.None;

            if (visibleLightsCount <= 1)
                lightData.mainLightIndex = GetMainLight(visibleLights);
            else
                lightData.mainLightIndex = SortLights(visibleLights);

            // If we have a main light we don't shade it in the per-object light loop. We also remove it from the per-object cull list
            int mainLightPresent = (lightData.mainLightIndex >= 0) ? 1 : 0;
            int additionalPixelLightsCount = visibleLightsCount - mainLightPresent;
            int vertexLightCount = (m_Asset.SupportsVertexLight) ? Math.Min(visibleLights.Count, kMaxPerObjectLights) - additionalPixelLightsCount - mainLightPresent : 0;
            vertexLightCount = Math.Min(vertexLightCount, kMaxVertexLights);

            lightData.pixelAdditionalLightsCount = additionalPixelLightsCount;
            lightData.totalAdditionalLightsCount = additionalPixelLightsCount + vertexLightCount;

            m_MixedLightingSetup = MixedLightingSetup.None;
        }

        private int SortLights(List<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Count;

            m_VisibleLightsIDMap.Clear();
            for (int i = 0; i < totalVisibleLights; ++i)
                m_VisibleLightsIDMap.Add(visibleLights[i], i);

            // Sorts light so we have all directionals first, then local lights.
            // Directionals are sorted further by shadow, cookie and intensity
            // Locals are sorted further by shadow, cookie and distance to camera
            m_LightComparer.CurrCamera = m_CurrCamera;
            visibleLights.Sort(m_LightComparer);

            for (int i = 0; i < totalVisibleLights; ++i)
                m_SortedLightIndexMap.Add(m_VisibleLightsIDMap[visibleLights[i]]);

            return GetMainLight(visibleLights);
        }

        // How main light is decided:
        // If shadows enabled, main light is always a shadow casting light. Directional has priority over local lights.
        // Otherwise directional lights have priority based on cookie support and intensity
        private int GetMainLight(List<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Count;
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

                // Shadow lights are sorted by type (directional > puctual) and intensity
                // The first shadow light we find in the list is the main light
                if (shadowsEnabled && currLight.light.shadows != LightShadows.None && LightweightUtils.IsSupportedShadowType(currLight.lightType))
                    return i;

                // In case no shadow light is present we will return the brightest directional light
                if (currLight.lightType == LightType.Directional && brighestDirectionalIndex == -1)
                    brighestDirectionalIndex = i;
            }

            return brighestDirectionalIndex;
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

        private void SetupShaderLightConstants(CommandBuffer cmd, List<VisibleLight> lights, ref LightData lightData)
        {
            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, lights, lightData.mainLightIndex);
            SetupAdditionalListConstants(cmd, lights, ref lightData);
        }

        private void SetupMainLightConstants(CommandBuffer cmd, List<VisibleLight> lights, int lightIndex)
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
            cmd.SetGlobalVector(PerCameraBuffer._MainLightColor, lightColor);
            cmd.SetGlobalVector(PerCameraBuffer._MainLightDistanceAttenuation, lightDistanceAttenuation);
            cmd.SetGlobalVector(PerCameraBuffer._MainLightSpotDir, lightSpotDir);
            cmd.SetGlobalVector(PerCameraBuffer._MainLightSpotAttenuation, lightSpotAttenuation);
        }

        private void SetupAdditionalListConstants(CommandBuffer cmd, List<VisibleLight> lights, ref LightData lightData)
        {
            int additionalLightIndex = 0;

            if (lightData.totalAdditionalLightsCount > 0)
            {
                // We need to update per-object light list with the proper map to our global additional light buffer
                // First we initialize all lights in the map to -1 to tell the system to discard main light index and
                // remaining lights in the scene that don't fit the max additional light buffer (kMaxVisibileAdditionalLights)
                int[] perObjectLightIndexMap = m_CullResults.GetLightIndexMap();
                for (int i = 0; i < lights.Count; ++i)
                    perObjectLightIndexMap[i] = -1;

                for (int i = 0; i < lights.Count && additionalLightIndex < kMaxVisibleLights; ++i)
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
            }
            else
            {
                cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, Vector4.zero);

                // Clear to default all light cosntant data
                for (int i = 0; i < kMaxVisibleLights; ++i)
                    InitializeLightConstants(lights, -1, out m_LightPositions[additionalLightIndex],
                            out m_LightColors[additionalLightIndex],
                            out m_LightDistanceAttenuations[additionalLightIndex],
                            out m_LightSpotDirections[additionalLightIndex],
                            out m_LightSpotAttenuations[additionalLightIndex]);
            }

            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightPosition, m_LightPositions);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightColor, m_LightColors);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightDistanceAttenuation, m_LightDistanceAttenuations);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotDir, m_LightSpotDirections);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotAttenuation, m_LightSpotAttenuations);
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

        private void SetupShadowReceiverConstants(CommandBuffer cmd, VisibleLight shadowLight)
        {
            Light light = shadowLight.light;

            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < kMaxCascades; ++i)
                m_ShadowMatrices[i] = (cascadeCount >= i) ? m_ShadowSlices[i].shadowTransform : Matrix4x4.identity;

            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m33 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            m_ShadowMatrices[kMaxCascades] = noOpShadowMatrix;

            float invShadowResolution = 1.0f / m_Asset.ShadowAtlasResolution;
            float invHalfShadowResolution = 0.5f * invShadowResolution;
            cmd.SetGlobalMatrixArray(ShadowConstantBuffer._WorldToShadow, m_ShadowMatrices);
            cmd.SetGlobalVector(ShadowConstantBuffer._ShadowData, new Vector4(light.shadowStrength, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalVectorArray(ShadowConstantBuffer._DirShadowSplitSpheres, m_DirectionalShadowSplitDistances);
            cmd.SetGlobalVector(ShadowConstantBuffer._DirShadowSplitSphereRadii, m_DirectionalShadowSplitRadii);
            cmd.SetGlobalVector(ShadowConstantBuffer._ShadowOffset0, new Vector4(-invHalfShadowResolution, -invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(ShadowConstantBuffer._ShadowOffset1, new Vector4(invHalfShadowResolution, -invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(ShadowConstantBuffer._ShadowOffset2, new Vector4(-invHalfShadowResolution, invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(ShadowConstantBuffer._ShadowOffset3, new Vector4(invHalfShadowResolution, invHalfShadowResolution, 0.0f, 0.0f));
            cmd.SetGlobalVector(ShadowConstantBuffer._ShadowmapSize, new Vector4(invShadowResolution, invShadowResolution, m_Asset.ShadowAtlasResolution, m_Asset.ShadowAtlasResolution));
        }

        private void SetShaderKeywords(CommandBuffer cmd, ref LightData lightData, List<VisibleLight> visibleLights)
        {
            int vertexLightsCount = lightData.totalAdditionalLightsCount - lightData.pixelAdditionalLightsCount;

            int mainLightIndex = lightData.mainLightIndex;
            //TIM: Not used in shader for V1 to reduce keywords
            CoreUtils.SetKeyword(cmd, "_MAIN_LIGHT_DIRECTIONAL", mainLightIndex == -1 || visibleLights[mainLightIndex].lightType == LightType.Directional);
            //TIM: Not used in shader for V1 to reduce keywords
            CoreUtils.SetKeyword(cmd, "_MAIN_LIGHT_SPOT", mainLightIndex != -1 && visibleLights[mainLightIndex].lightType == LightType.Spot);

            //TIM: Not used in shader for V1 to reduce keywords
            CoreUtils.SetKeyword(cmd, "_SHADOWS_ENABLED", lightData.shadowMapSampleType != LightShadows.None);

            //TIM: Not used in shader for V1 to reduce keywords
            CoreUtils.SetKeyword(cmd, "_MAIN_LIGHT_COOKIE", mainLightIndex != -1 && LightweightUtils.IsSupportedCookieType(visibleLights[mainLightIndex].lightType) && visibleLights[mainLightIndex].light.cookie != null);

            CoreUtils.SetKeyword(cmd, "_ADDITIONAL_LIGHTS", lightData.totalAdditionalLightsCount > 0);
            CoreUtils.SetKeyword(cmd, "_MIXED_LIGHTING_SUBTRACTIVE", m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            CoreUtils.SetKeyword(cmd, "_VERTEX_LIGHTS", vertexLightsCount > 0);
            CoreUtils.SetKeyword(cmd, "SOFTPARTICLES_ON", m_RequireDepthTexture && m_Asset.RequireSoftParticles);
           
            bool linearFogModeEnabled = false;
            bool exponentialFogModeEnabled = false;
            if (RenderSettings.fog)
            {
                if (RenderSettings.fogMode == FogMode.Linear)
                    linearFogModeEnabled = true;
                else
                    exponentialFogModeEnabled = true;
            }

            CoreUtils.SetKeyword(cmd, "FOG_LINEAR", linearFogModeEnabled);
            CoreUtils.SetKeyword(cmd, "FOG_EXP2", exponentialFogModeEnabled);
        }

        private void SetShadowCollectPassKeywords(CommandBuffer cmd, VisibleLight shadowLight, ref LightData lightData)
        {
            bool cascadeShadows = shadowLight.lightType == LightType.Directional && m_Asset.CascadeCount > 1;
            CoreUtils.SetKeyword(cmd, "_SHADOWS_SOFT", lightData.shadowMapSampleType == LightShadows.Soft);
            CoreUtils.SetKeyword(cmd, "_SHADOWS_CASCADE", cascadeShadows);
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

            float shadowNearPlane = m_Asset.ShadowNearOffset;

            Matrix4x4 view, proj;
            var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);
            bool success = false;

            var cmd = CommandBufferPool.Get("Prepare Shadowmap");
            cmd.GetTemporaryRT(m_ShadowMapRTID, m_ShadowSettings.shadowAtlasWidth,
                m_ShadowSettings.shadowAtlasHeight, kDepthStencilBufferBits, FilterMode.Bilinear, m_ShadowSettings.renderTextureFormat);
            // LightweightPipeline.SetRenderTarget is meant to be used with camera targets, not shadowmaps
            CoreUtils.SetRenderTarget(cmd, m_ShadowMapRT, ClearFlag.Depth, CoreUtils.ConvertSRGBToActiveColorSpace(m_CurrCamera.backgroundColor));

            if (shadowLight.lightType == LightType.Spot)
            {
                success = cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowLightIndex, out view, out proj,
                        out settings.splitData);

                if (success)
                {
                    SetupShadowCasterConstants(cmd, ref shadowLight, proj, shadowResolution);
                    SetupShadowSliceTransform(0, shadowResolution, proj, view);
                    RenderShadowSlice(cmd, ref context, 0, proj, view, settings);
                }
            }
            else if (shadowLight.lightType == LightType.Directional)
            {
                for (int cascadeIdx = 0; cascadeIdx < m_ShadowCasterCascadesCount; ++cascadeIdx)
                {
                    success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex,
                            cascadeIdx, m_ShadowCasterCascadesCount, m_ShadowSettings.directionalLightCascades, shadowResolution, shadowNearPlane, out view, out proj,
                            out settings.splitData);

                    float cullingSphereRadius = settings.splitData.cullingSphere.w;
                    m_DirectionalShadowSplitDistances[cascadeIdx] = settings.splitData.cullingSphere;
                    m_DirectionalShadowSplitRadii[cascadeIdx] = cullingSphereRadius * cullingSphereRadius;

                    if (!success)
                        break;

                    SetupShadowCasterConstants(cmd, ref shadowLight, proj, shadowResolution);
                    SetupShadowSliceTransform(cascadeIdx, shadowResolution, proj, view);
                    RenderShadowSlice(cmd, ref context, cascadeIdx, proj, view, settings);
                }
            }
            else
            {
                Debug.LogWarning("Only spot and directional shadow casters are supported in lightweight pipeline");
            }

            CommandBufferPool.Release(cmd);
            return success;
        }

        private void SetupShadowSliceTransform(int cascadeIndex, int shadowResolution, Matrix4x4 proj, Matrix4x4 view)
        {
            if (cascadeIndex >= kMaxCascades)
            {
                Debug.LogError(String.Format("{0} is an invalid cascade index. Maximum of {1} cascades", cascadeIndex, kMaxCascades));
                return;
            }

            int atlasX = (cascadeIndex % 2) * shadowResolution;
            int atlasY = (cascadeIndex / 2) * shadowResolution;
            float atlasWidth = (float)m_ShadowSettings.shadowAtlasWidth;
            float atlasHeight = (float)m_ShadowSettings.shadowAtlasHeight;

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
            cascadeAtlas.m00 = (float)shadowResolution / atlasWidth;
            cascadeAtlas.m11 = (float)shadowResolution / atlasHeight;
            cascadeAtlas.m03 = (float)atlasX / atlasWidth;
            cascadeAtlas.m13 = (float)atlasY / atlasHeight;

            // Apply cascade scale and offset
            worldToShadow = cascadeAtlas * worldToShadow;

            m_ShadowSlices[cascadeIndex].atlasX = atlasX;
            m_ShadowSlices[cascadeIndex].atlasY = atlasY;
            m_ShadowSlices[cascadeIndex].shadowResolution = shadowResolution;
            m_ShadowSlices[cascadeIndex].shadowTransform = worldToShadow;
        }

        private void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context, int cascadeIndex,
            Matrix4x4 proj, Matrix4x4 view, DrawShadowsSettings settings)
        {
            cmd.SetViewport(new Rect(m_ShadowSlices[cascadeIndex].atlasX, m_ShadowSlices[cascadeIndex].atlasY,
                    m_ShadowSlices[cascadeIndex].shadowResolution, m_ShadowSlices[cascadeIndex].shadowResolution));
            cmd.SetViewProjectionMatrices(view, proj);
            context.ExecuteCommandBuffer(cmd);
            context.DrawShadows(ref settings);
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

        private void BeginForwardRendering(ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfig)
        {
            RenderTargetIdentifier colorRT = BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier depthRT = BuiltinRenderTextureType.None;

            StartStereoRendering(ref context, renderingConfig);

            CommandBuffer cmd = CommandBufferPool.Get("SetCameraRenderTarget");
            bool intermediateTexture = LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture);
            if (intermediateTexture)
            {
                if (!m_IsOffscreenCamera)
                    colorRT = m_CurrCameraColorRT;

                if (m_RequireDepthTexture)
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

            // If rendering to an intermediate RT we resolve viewport on blit due to offset not being supported
            // while rendering to a RT.
            if (!intermediateTexture && !LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DefaultViewport))
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
            else if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.IntermediateTexture))
            {
                Material blitMaterial = m_BlitMaterial;
                if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.Stereo))
                    blitMaterial = null;

                // If PostProcessing is enabled, it is already blit to CameraTarget.
                if (!LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.PostProcess))
                    Blit(cmd, renderingConfig, m_CurrCameraColorRT, BuiltinRenderTextureType.CameraTarget, blitMaterial);
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
            cmd.SetGlobalTexture(m_BlitTexID, sourceRT);
            if (LightweightUtils.HasFlag(renderingConfig, FrameRenderingConfiguration.DefaultViewport))
            {
                cmd.Blit(sourceRT, destRT, material);
            }
            else
            {
                if (m_BlitQuad == null)
                    m_BlitQuad = LightweightUtils.CreateQuadMesh(false);

                SetRenderTarget(cmd, destRT);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(m_CurrCamera.pixelRect);
                cmd.DrawMesh(m_BlitQuad, Matrix4x4.identity, material);
            }
        }

        private void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier sourceRT, RenderTargetIdentifier destRT, Material copyMaterial, bool forceBlit = false)
        {
            if (m_CopyTextureSupport != CopyTextureSupport.None && !forceBlit)
                cmd.CopyTexture(sourceRT, destRT);
            else
                cmd.Blit(sourceRT, destRT, copyMaterial);
        }

        private void StartStereoRendering(ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfiguration)
        {
            if (LightweightUtils.HasFlag(renderingConfiguration, FrameRenderingConfiguration.Stereo))
                context.StartMultiEye(m_CurrCamera);
        }

        private void StopStereoRendering(ref ScriptableRenderContext context, FrameRenderingConfiguration renderingConfiguration)
        {
            if (LightweightUtils.HasFlag(renderingConfiguration, FrameRenderingConfiguration.Stereo))
                context.StopMultiEye(m_CurrCamera);
        }
    }
}
