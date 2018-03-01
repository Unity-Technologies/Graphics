using System.Collections.Generic;
using UnityEngine.Rendering;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.GlobalIllumination;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDRenderPipeline : RenderPipeline
    {
        enum ForwardPass
        {
            Opaque,
            PreRefraction,
            Transparent
        }

        static readonly string[] k_ForwardPassDebugName =
        {
            "Forward Opaque Debug",
            "Forward PreRefraction Debug",
            "Forward Transparent Debug"
        };

        static readonly string[] k_ForwardPassName =
        {
            "Forward Opaque",
            "Forward PreRefraction",
            "Forward Transparent"
        };

        readonly HDRenderPipelineAsset m_Asset;

        DiffusionProfileSettings m_InternalSSSAsset;
        public DiffusionProfileSettings diffusionProfileSettings
        {
            get
            {
                // If no SSS asset is set, build / reuse an internal one for simplicity
                var asset = m_Asset.diffusionProfileSettings;

                if (asset == null)
                {
                    if (m_InternalSSSAsset == null)
                        m_InternalSSSAsset = ScriptableObject.CreateInstance<DiffusionProfileSettings>();

                    asset = m_InternalSSSAsset;
                }

                return asset;
            }
        }
        public RenderPipelineSettings renderPipelineSettings { get { return m_Asset.renderPipelineSettings; } }

        public bool IsInternalDiffusionProfile(DiffusionProfileSettings profile)
        {
            return m_InternalSSSAsset == profile;
        }

        readonly RenderPipelineMaterial m_DeferredMaterial;
        readonly List<RenderPipelineMaterial> m_MaterialList = new List<RenderPipelineMaterial>();

        readonly GBufferManager m_GbufferManager;
        readonly DBufferManager m_DbufferManager;
        readonly SubsurfaceScatteringManager m_SSSBufferManager = new SubsurfaceScatteringManager();

        // Renderer Bake configuration can vary depends on if shadow mask is enabled or no
        RendererConfiguration m_currentRendererConfigurationBakedLighting = HDUtils.k_RendererConfigurationBakedLighting;
        Material m_CopyStencilForNoLighting;
        Material m_CopyDepth;
        GPUCopy m_GPUCopy;
        BufferPyramid m_BufferPyramid;

        IBLFilterGGX m_IBLFilterGGX = null;

        ComputeShader m_applyDistortionCS { get { return m_Asset.renderPipelineResources.applyDistortionCS; } }
        int m_applyDistortionKernel;

        Material m_CameraMotionVectorsMaterial;

        // Debug material
        Material m_DebugViewMaterialGBuffer;
        Material m_DebugViewMaterialGBufferShadowMask;
        Material m_currentDebugViewMaterialGBuffer;
        Material m_DebugDisplayLatlong;
        Material m_DebugFullScreen;
        Material m_DebugColorPicker;
        Material m_Blit;
        Material m_ErrorMaterial;

        RenderTargetIdentifier[] m_MRTCache2 = new RenderTargetIdentifier[2];

        // 'm_CameraColorBuffer' does not contain diffuse lighting of SSS materials until the SSS pass. It is stored within 'm_CameraSssDiffuseLightingBuffer'.
        RTHandle m_CameraColorBuffer;
        RTHandle m_CameraSssDiffuseLightingBuffer;

        RTHandle m_CameraDepthStencilBuffer;
        RTHandle m_CameraDepthBufferCopy;
        RTHandle m_CameraStencilBufferCopy;

        RTHandle m_VelocityBuffer;
        RTHandle m_DeferredShadowBuffer;
        RTHandle m_AmbientOcclusionBuffer;
        RTHandle m_DistortionBuffer;

        // The pass "SRPDefaultUnlit" is a fall back to legacy unlit rendering and is required to support unity 2d + unity UI that render in the scene.
        ShaderPassName[] m_ForwardAndForwardOnlyPassNames = { new ShaderPassName(), new ShaderPassName(), HDShaderPassNames.s_SRPDefaultUnlitName };
        ShaderPassName[] m_ForwardOnlyPassNames = { new ShaderPassName(), HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllTransparentPassNames = {  HDShaderPassNames.s_TransparentBackfaceName,
                                                        HDShaderPassNames.s_ForwardOnlyName,
                                                        HDShaderPassNames.s_ForwardName,
                                                        HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllForwardOpaquePassNames = {    HDShaderPassNames.s_ForwardOnlyName,
                                                            HDShaderPassNames.s_ForwardName,
                                                            HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_DepthOnlyAndDepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_DepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName };
        ShaderPassName[] m_DepthOnlyPassNames = { HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_TransparentDepthPrepassNames = { HDShaderPassNames.s_TransparentDepthPrepassName };
        ShaderPassName[] m_TransparentDepthPostpassNames = { HDShaderPassNames.s_TransparentDepthPostpassName };
        ShaderPassName[] m_ForwardErrorPassNames = { HDShaderPassNames.s_AlwaysName, HDShaderPassNames.s_ForwardBaseName, HDShaderPassNames.s_DeferredName, HDShaderPassNames.s_PrepassBaseName, HDShaderPassNames.s_VertexName, HDShaderPassNames.s_VertexLMRGBMName, HDShaderPassNames.s_VertexLMName };
        ShaderPassName[] m_SinglePassName = new ShaderPassName[1];

        // Stencil usage in HDRenderPipeline.
        // Currently we use only 2 bits to identify the kind of lighting that is expected from the render pipeline
        // Usage is define in LightDefinitions.cs
        [Flags]
        public enum StencilBitMask
        {
            Clear           = 0,             // 0x0
            LightingMask    = 7,             // 0x7  - 3 bit
            ObjectVelocity  = 128,           // 0x80 - 1 bit
            All             = 255            // 0xFF - 8 bit
        }

        RenderStateBlock m_DepthStateOpaque;
        RenderStateBlock m_DepthStateOpaqueWithPrepass;

        // Detect when windows size is changing
        int m_CurrentWidth;
        int m_CurrentHeight;

        // Use to detect frame changes
        int m_FrameCount;

        public int GetCurrentShadowCount() { return m_LightLoop.GetCurrentShadowCount(); }
        public int GetShadowAtlasCount() { return m_LightLoop.GetShadowAtlasCount(); }

        readonly SkyManager m_SkyManager = new SkyManager();
        readonly LightLoop m_LightLoop = new LightLoop();
        readonly ShadowSettings m_ShadowSettings = new ShadowSettings();
        readonly VolumetricLightingModule m_VolumetricLightingModule = new VolumetricLightingModule();

        // Debugging
        MaterialPropertyBlock m_SharedPropertyBlock = new MaterialPropertyBlock();
        DebugDisplaySettings m_DebugDisplaySettings = new DebugDisplaySettings();
        public DebugDisplaySettings debugDisplaySettings { get { return m_DebugDisplaySettings; } }
        static DebugDisplaySettings s_NeutralDebugDisplaySettings = new DebugDisplaySettings();
        DebugDisplaySettings m_CurrentDebugDisplaySettings;
        RTHandle                        m_DebugColorPickerBuffer;
        RTHandle                        m_DebugFullScreenTempBuffer;
        bool                            m_FullScreenDebugPushed;

        public Material GetBlitMaterial() { return m_Blit; }

        FrameSettings m_FrameSettings; // Init every frame

        public HDRenderPipeline(HDRenderPipelineAsset asset)
        {
            SetRenderingFeatures();

            m_Asset = asset;
            m_GPUCopy = new GPUCopy(asset.renderPipelineResources.copyChannelCS);
            m_BufferPyramid = new BufferPyramid(
                asset.renderPipelineResources.colorPyramidCS,
                asset.renderPipelineResources.depthPyramidCS,
                m_GPUCopy);

            EncodeBC6H.DefaultInstance = EncodeBC6H.DefaultInstance ?? new EncodeBC6H(asset.renderPipelineResources.encodeBC6HCS);

            m_ReflectionProbeCullResults = new ReflectionProbeCullResults(asset.reflectionSystemParameters);
            ReflectionSystem.SetParameters(asset.reflectionSystemParameters);

            // Scan material list and assign it
            m_MaterialList = HDUtils.GetRenderPipelineMaterialList();
            // Find first material that have non 0 Gbuffer count and assign it as deferredMaterial
            m_DeferredMaterial = null;
            foreach (var material in m_MaterialList)
            {
                if (material.GetMaterialGBufferCount() > 0)
                    m_DeferredMaterial = material;
            }

            // TODO: Handle the case of no Gbuffer material
            // TODO: I comment the assert here because m_DeferredMaterial for whatever reasons contain the correct class but with a "null" in the name instead of the real name and then trigger the assert
            // whereas it work. Don't know what is happening, DebugDisplay use the same code and name is correct there.
            // Debug.Assert(m_DeferredMaterial != null);

            m_GbufferManager = new GBufferManager(m_DeferredMaterial, m_Asset.renderPipelineSettings.supportShadowMask);
            m_DbufferManager = new DBufferManager();

            m_SSSBufferManager.Build(asset);

            // Initialize various compute shader resources
            m_applyDistortionKernel = m_applyDistortionCS.FindKernel("KMain");

            // General material
            m_CopyStencilForNoLighting = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.copyStencilBuffer);
            m_CopyStencilForNoLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
            m_CopyStencilForNoLighting.SetInt(HDShaderIDs._StencilMask, (int)StencilBitMask.LightingMask);
            m_CameraMotionVectorsMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.cameraMotionVectors);

            m_CopyDepth = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.copyDepthBuffer);

            InitializeDebugMaterials();

            m_MaterialList.ForEach(material => material.Build(asset));

            m_IBLFilterGGX = new IBLFilterGGX(asset.renderPipelineResources);

            m_LightLoop.Build(asset, m_ShadowSettings, m_IBLFilterGGX);

            m_SkyManager.Build(asset, m_IBLFilterGGX);

            m_VolumetricLightingModule.Build(asset);

            m_DebugDisplaySettings.RegisterDebug();
#if UNITY_EDITOR
            // We don't need the debug of Default camera at runtime (each camera have its own debug settings)
            FrameSettings.RegisterDebug("Default Camera", m_Asset.GetFrameSettings());
#endif

            InitializeRenderTextures();

            // For debugging
            MousePositionDebug.instance.Build();

            InitializeRenderStateBlocks();
        }

        void InitializeRenderTextures()
        {
            // Initial state of the RTHandle system.
            // Tells the system that we will require MSAA or not so that we can avoid wasteful render texture allocation.
            // TODO: Might want to initialize to at least the window resolution to avoid un-necessary re-alloc in the player
            RTHandle.Initialize(1, 1, m_Asset.renderPipelineSettings.supportMSAA, m_Asset.renderPipelineSettings.msaaSampleCount);

            if(!m_Asset.renderPipelineSettings.supportForwardOnly)
                m_GbufferManager.CreateBuffers();

            if (m_Asset.renderPipelineSettings.supportDBuffer)
                m_DbufferManager.CreateBuffers();

            m_BufferPyramid.CreateBuffers();

            m_SSSBufferManager.InitSSSBuffers(m_GbufferManager, m_Asset.renderPipelineSettings);

            m_CameraColorBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB : false, enableRandomWrite: true, enableMSAA: true, name : "CameraColor");
            m_CameraSssDiffuseLightingBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RGB111110Float, sRGB: false, enableRandomWrite: true, enableMSAA: true, name: "CameraSSSDiffuseLighting");

            m_CameraDepthStencilBuffer = RTHandle.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth24, colorFormat: RenderTextureFormat.Depth, filterMode: FilterMode.Point, bindTextureMS: true, enableMSAA: true, name: "CameraDepthStencil");

            if (NeedDepthBufferCopy())
            {
                m_CameraDepthBufferCopy = RTHandle.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth24, colorFormat: RenderTextureFormat.Depth, filterMode: FilterMode.Point, bindTextureMS: true, enableMSAA: true, name: "CameraDepthStencilCopy");
            }

            // Technically we won't need this buffer in some cases, but nothing that we can determine at init time.
            m_CameraStencilBufferCopy = RTHandle.Alloc(Vector2.one, depthBufferBits: DepthBits.None, colorFormat: RenderTextureFormat.R8, sRGB: false, filterMode: FilterMode.Point, enableMSAA: true, name: "CameraStencilCopy"); // DXGI_FORMAT_R8_UINT is not supported by Unity

            if (m_Asset.renderPipelineSettings.supportSSAO)
            {
                m_AmbientOcclusionBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Bilinear, colorFormat: RenderTextureFormat.R8, sRGB: false, enableRandomWrite: true, name: "AmbientOcclusion");
            }

            if (m_Asset.renderPipelineSettings.supportMotionVectors)
            {
                m_VelocityBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: Builtin.GetVelocityBufferFormat(), sRGB: Builtin.GetVelocityBufferSRGBFlag(), enableMSAA: true, name: "Velocity");
            }

            m_DistortionBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: Builtin.GetDistortionBufferFormat(), sRGB: Builtin.GetDistortionBufferSRGBFlag(), name: "Distortion");

            // TODO: For MSAA, we'll need to add a Draw path in order to support MSAA properly
            m_DeferredShadowBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGB32, sRGB: false, enableRandomWrite: true, name: "DeferredShadow");

            if (Debug.isDebugBuild)
            {
                m_DebugColorPickerBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, name: "DebugColorPicker");
                m_DebugFullScreenTempBuffer = RTHandle.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, name: "DebugFullScreen");
            }
        }

        void DestroyRenderTextures()
        {
            m_GbufferManager.DestroyBuffers();
            m_DbufferManager.DestroyBuffers();
            m_BufferPyramid.DestroyBuffers();

            RTHandle.Release(m_CameraColorBuffer);
            RTHandle.Release(m_CameraSssDiffuseLightingBuffer);

            RTHandle.Release(m_CameraDepthStencilBuffer);
            RTHandle.Release(m_CameraDepthBufferCopy);
            RTHandle.Release(m_CameraStencilBufferCopy);

            RTHandle.Release(m_AmbientOcclusionBuffer);
            RTHandle.Release(m_VelocityBuffer);
            RTHandle.Release(m_DistortionBuffer);
            RTHandle.Release(m_DeferredShadowBuffer);

            RTHandle.Release(m_DebugColorPickerBuffer);
            RTHandle.Release(m_DebugFullScreenTempBuffer);
        }


        void SetRenderingFeatures()
        {
            // Set subshader pipeline tag
            Shader.globalRenderPipeline = "HDRenderPipeline";

            // HD use specific GraphicsSettings
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeSupportFlags = SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation,
                defaultMixedLightingMode = SupportedRenderingFeatures.LightmapMixedBakeMode.IndirectOnly,
                supportedMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeMode.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeMode.Shadowmask,
                supportedLightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                supportedLightmapsModes = LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional,
                rendererSupportsLightProbeProxyVolumes = true,
                rendererSupportsMotionVectors = true,
                rendererSupportsReceiveShadows = true,
                rendererSupportsReflectionProbes = true
            };

            Lightmapping.SetDelegate(GlobalIlluminationUtils.hdLightsDelegate);

#if UNITY_EDITOR
            SceneViewDrawMode.SetupDrawMode();

            if (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                Debug.LogError("High Definition Render Pipeline doesn't support Gamma mode, change to Linear mode");
            }
#endif
        }

        void UnsetRenderingFeatures()
        {
            Shader.globalRenderPipeline = "";

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

            Lightmapping.ResetDelegate();
        }

        void InitializeDebugMaterials()
        {
            m_DebugViewMaterialGBuffer = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugViewMaterialGBufferShader);
            m_DebugViewMaterialGBufferShadowMask = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugViewMaterialGBufferShader);
            m_DebugViewMaterialGBufferShadowMask.EnableKeyword("SHADOWS_SHADOWMASK");
            m_DebugDisplayLatlong = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugDisplayLatlongShader);
            m_DebugFullScreen = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugFullScreenShader);
            m_DebugColorPicker = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugColorPickerShader);
            m_Blit = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.blit);
            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");
        }

        void InitializeRenderStateBlocks()
        {
            m_DepthStateOpaque = new RenderStateBlock
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };

            // When doing a prepass, we don't need to write the depth anymore.
            // Moreover, we need to use DepthEqual because for alpha tested materials we don't do the clip in the shader anymore (otherwise HiZ does not work on PS4)
            m_DepthStateOpaqueWithPrepass = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.Equal),
                mask = RenderStateMask.Depth
            };
        }

        public void OnSceneLoad()
        {
            // Recreate the textures which went NULL
            m_MaterialList.ForEach(material => material.Build(m_Asset));
        }

        public override void Dispose()
        {
            base.Dispose();

            m_DebugDisplaySettings.UnregisterDebug();

            m_LightLoop.Cleanup();

            // For debugging
            MousePositionDebug.instance.Cleanup();

            DecalSystem.instance.Cleanup();

            m_MaterialList.ForEach(material => material.Cleanup());

            CoreUtils.Destroy(m_CopyStencilForNoLighting);
            CoreUtils.Destroy(m_CameraMotionVectorsMaterial);

            CoreUtils.Destroy(m_DebugViewMaterialGBuffer);
            CoreUtils.Destroy(m_DebugViewMaterialGBufferShadowMask);
            CoreUtils.Destroy(m_DebugDisplayLatlong);
            CoreUtils.Destroy(m_DebugFullScreen);
            CoreUtils.Destroy(m_DebugColorPicker);
            CoreUtils.Destroy(m_Blit);
            CoreUtils.Destroy(m_CopyDepth);
            CoreUtils.Destroy(m_ErrorMaterial);

            m_SSSBufferManager.Cleanup();
            m_SkyManager.Cleanup();
            m_VolumetricLightingModule.Cleanup();

            DestroyRenderTextures();

            UnsetRenderingFeatures();

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif
        }

        void Resize(HDCamera hdCamera)
        {
            bool resolutionChanged = (hdCamera.actualWidth != m_CurrentWidth) || (hdCamera.actualHeight != m_CurrentHeight);

            if (resolutionChanged || m_LightLoop.NeedResize())
            {
                if (m_CurrentWidth > 0 && m_CurrentHeight > 0)
                    m_LightLoop.ReleaseResolutionDependentBuffers();

                m_LightLoop.AllocResolutionDependentBuffers((int)hdCamera.screenSize.x, (int)hdCamera.screenSize.y, m_FrameSettings.enableStereo);
            }

            // Warning: (resolutionChanged == false) if you open a new Editor tab of the same size!
            m_VolumetricLightingModule.ResizeVBuffer(hdCamera, hdCamera.actualWidth, hdCamera.actualHeight);

            // update recorded window resolution
            m_CurrentWidth = hdCamera.actualWidth;
            m_CurrentHeight = hdCamera.actualHeight;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, DiffusionProfileSettings sssParameters)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", CustomSamplerId.PushGlobalParameters.GetSampler()))
            {
                hdCamera.SetupGlobalParams(cmd);
                if (m_FrameSettings.enableStereo)
                    hdCamera.SetupGlobalStereoParams(cmd);

                m_SSSBufferManager.PushGlobalParams(cmd, sssParameters, m_FrameSettings);

                m_DbufferManager.PushGlobalParams(cmd, m_FrameSettings);

                m_VolumetricLightingModule.PushGlobalParams(hdCamera, cmd);
            }
        }

        bool NeedDepthBufferCopy()
        {
            // For now we consider only PS4 to be able to read from a bound depth buffer.
            // TODO: test/implement for other platforms.
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4 &&
                    SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOne &&
                    SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOneD3D12;
        }

        bool NeedStencilBufferCopy()
        {
            // Currently, Unity does not offer a way to bind the stencil buffer as a texture in a compute shader.
            // Therefore, it's manually copied using a pixel shader.
            return m_LightLoop.GetFeatureVariantsEnabled();
        }

        RTHandle GetDepthTexture()
        {
            return NeedDepthBufferCopy() ? m_CameraDepthBufferCopy : m_CameraDepthStencilBuffer;
        }

        void CopyDepthBufferIfNeeded(CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, NeedDepthBufferCopy() ? "Copy DepthBuffer" : "Set DepthBuffer", CustomSamplerId.CopySetDepthBuffer.GetSampler()))
            {
                if (NeedDepthBufferCopy())
                {
                    using (new ProfilingSample(cmd, "Copy depth-stencil buffer", CustomSamplerId.CopyDepthStencilbuffer.GetSampler()))
                    {
                        cmd.CopyTexture(m_CameraDepthStencilBuffer, m_CameraDepthBufferCopy);
                    }
                }
            }
        }

        public void UpdateShadowSettings()
        {
            var shadowSettings = VolumeManager.instance.stack.GetComponent<HDShadowSettings>();

            m_ShadowSettings.maxShadowDistance = shadowSettings.maxShadowDistance;
            //m_ShadowSettings.directionalLightNearPlaneOffset = commonSettings.shadowNearPlaneOffset;

            m_ShadowSettings.enabled = m_FrameSettings.enableShadow;
        }

        public void ConfigureForShadowMask(bool enableBakeShadowMask, CommandBuffer cmd)
        {
            // Globally enable (for GBuffer shader and forward lit (opaque and transparent) the keyword SHADOWS_SHADOWMASK
            CoreUtils.SetKeyword(cmd, "SHADOWS_SHADOWMASK", enableBakeShadowMask);

            // Configure material to use depends on shadow mask option
            m_currentRendererConfigurationBakedLighting = enableBakeShadowMask ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;
            m_currentDebugViewMaterialGBuffer = enableBakeShadowMask ? m_DebugViewMaterialGBufferShadowMask : m_DebugViewMaterialGBuffer;
        }

        CullResults m_CullResults;
        ReflectionProbeCullResults m_ReflectionProbeCullResults;
        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            base.Render(renderContext, cameras);
            RenderPipeline.BeginFrameRendering(cameras);

            if (m_FrameCount != Time.frameCount)
            {
                HDCamera.CleanUnused();
                m_FrameCount = Time.frameCount;
            }

            // TODO: Render only visible probes
            var isReflection = cameras.Any(c => c.cameraType == CameraType.Reflection);
            if (!isReflection)
                ReflectionSystem.RenderAllRealtimeProbes(ReflectionProbeType.PlanarReflection);

            // We first update the state of asset frame settings as they can be use by various camera
            // but we keep the dirty state to correctly reset other camera that use RenderingPath.Default.
            bool assetFrameSettingsIsDirty = m_Asset.frameSettingsIsDirty;
            m_Asset.UpdateDirtyFrameSettings();

            foreach (var camera in cameras)
            {
                if (camera == null)
                    continue;

                RenderPipeline.BeginCameraRendering(camera);

                if (camera.cameraType != CameraType.Reflection)
                    // TODO: Render only visible probes
                    ReflectionSystem.RenderAllRealtimeViewerDependentProbesFor(ReflectionProbeType.PlanarReflection, camera);

                // First, get aggregate of frame settings base on global settings, camera frame settings and debug settings
                // Note: the SceneView camera will never have additionalCameraData
                var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();

                // Init effective frame settings of each camera
                // Each camera have its own debug frame settings control from the debug windows
                // debug frame settings can't be aggregate with frame settings (i.e we can't aggregate forward only control for example)
                // so debug settings (when use) are the effective frame settings
                // To be able to have this behavior we init effective frame settings with serialized frame settings and copy
                // debug settings change on top of it. Each time frame settings are change in the editor, we reset all debug settings
                // to stay in sync. The loop below allow to update all frame settings correctly and is required because
                // camera can rely on default frame settings from the HDRendeRPipelineAsset
                FrameSettings srcFrameSettings;
                if (additionalCameraData)
                {
                    additionalCameraData.UpdateDirtyFrameSettings(assetFrameSettingsIsDirty, m_Asset.GetFrameSettings());
                    srcFrameSettings = additionalCameraData.GetFrameSettings();
                }
                else
                {
                    srcFrameSettings = m_Asset.GetFrameSettings();
                }
                // Get the effective frame settings for this camera taking into account the global setting and camera type
                FrameSettings.InitializeFrameSettings(camera, m_Asset.GetRenderPipelineSettings(), srcFrameSettings, ref m_FrameSettings);

                // This is the main command buffer used for the frame.
                var cmd = CommandBufferPool.Get("");

                // Init material if needed
                // TODO: this should be move outside of the camera loop but we have no command buffer, ask details to Tim or Julien to do this
                if (!m_IBLFilterGGX.IsInitialized())
                    m_IBLFilterGGX.Initialize(cmd);

                foreach (var material in m_MaterialList)
                    material.RenderInit(cmd);

                using (new ProfilingSample(cmd, "HDRenderPipeline::Render", CustomSamplerId.HDRenderPipelineRender.GetSampler()))
                {
                    // Do anything we need to do upon a new frame.
                    m_LightLoop.NewFrame(m_FrameSettings);

                    // If we render a reflection view or a preview we should not display any debug information
                    // This need to be call before ApplyDebugDisplaySettings()
                    if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                    {
                        // Neutral allow to disable all debug settings
                        m_CurrentDebugDisplaySettings = s_NeutralDebugDisplaySettings;
                    }
                    else
                    {
                        m_CurrentDebugDisplaySettings = m_DebugDisplaySettings;

                        using (new ProfilingSample(cmd, "Volume Update", CustomSamplerId.VolumeUpdate.GetSampler()))
                        {
                            LayerMask layerMask = -1;
                            if (additionalCameraData != null)
                            {
                                layerMask = additionalCameraData.volumeLayerMask;
                            }
                            else
                            {
                                // Temporary hack. For scene view, by default, we don't want to have the lighting override layers in the current sky.
                                // This is arbitrary and should be editable in the scene view somehow.
                                if (camera.cameraType == CameraType.SceneView)
                                {
                                    layerMask = (-1 & ~m_Asset.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask);
                                }
                            }
                            VolumeManager.instance.Update(camera.transform, layerMask);
                        }
                    }

                    var postProcessLayer = camera.GetComponent<PostProcessLayer>();
                    var hdCamera = HDCamera.Get(camera, postProcessLayer, m_FrameSettings);

                    Resize(hdCamera);

                    ApplyDebugDisplaySettings(hdCamera, cmd);
                    UpdateShadowSettings();
                    m_SkyManager.UpdateCurrentSkySettings(hdCamera);

                    ScriptableCullingParameters cullingParams;
                    if (!CullResults.GetCullingParameters(camera, m_FrameSettings.enableStereo, out cullingParams))
                    {
                        renderContext.Submit();
                        continue;
                    }

                    m_LightLoop.UpdateCullingParameters(ref cullingParams);
                    hdCamera.UpdateStereoDependentState(m_FrameSettings, ref cullingParams);

#if UNITY_EDITOR
                    // emit scene view UI
                    if (camera.cameraType == CameraType.SceneView)
                    {
                        ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                    }
#endif

                    if (m_FrameSettings.enableDBuffer)
                    {
                        // decal system needs to be updated with current camera, it needs it to set up culling and light list generation parameters
                        DecalSystem.instance.CurrentCamera = camera;
                        DecalSystem.instance.BeginCull();
                    }

                    ReflectionSystem.PrepareCull(camera, m_ReflectionProbeCullResults);

                    using (new ProfilingSample(cmd, "CullResults.Cull", CustomSamplerId.CullResultsCull.GetSampler()))
                    {
                        CullResults.Cull(ref cullingParams, renderContext, ref m_CullResults);
                    }

                    m_ReflectionProbeCullResults.Cull();

                    m_DbufferManager.vsibleDecalCount = 0;
                    using (new ProfilingSample(cmd, "DBufferPrepareDrawData", CustomSamplerId.DBufferPrepareDrawData.GetSampler()))
                    {
                        if (m_FrameSettings.enableDBuffer)
                        {
                            DecalSystem.instance.EndCull();
                            m_DbufferManager.vsibleDecalCount = DecalSystem.m_DecalsVisibleThisFrame;
                            DecalSystem.instance.UpdateCachedMaterialData(cmd);     // textures, alpha or fade distances could've changed
                            DecalSystem.instance.CreateDrawData();                  // prepare data is separate from draw
                        }
                    }
                    renderContext.SetupCameraProperties(camera, m_FrameSettings.enableStereo);

                    PushGlobalParams(hdCamera, cmd, diffusionProfileSettings);

                    // TODO: Find a correct place to bind these material textures
                    // We have to bind the material specific global parameters in this mode
                    m_MaterialList.ForEach(material => material.Bind());

                    if (additionalCameraData && additionalCameraData.renderingPath == HDAdditionalCameraData.RenderingPath.Unlit)
                    {
                        // TODO: Add another path dedicated to planar reflection / real time cubemap that implement simpler lighting
                        // It is up to the users to only send unlit object for this camera path

                        using (new ProfilingSample(cmd, "Forward", CustomSamplerId.Forward.GetSampler()))
                        {
                            HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_CameraDepthStencilBuffer, ClearFlag.Color | ClearFlag.Depth);
                            RenderOpaqueRenderList(m_CullResults, camera, renderContext, cmd, HDShaderPassNames.s_ForwardName);
                            RenderTransparentRenderList(m_CullResults, camera, renderContext, cmd, HDShaderPassNames.s_ForwardName);
                        }

                        renderContext.ExecuteCommandBuffer(cmd);
                        CommandBufferPool.Release(cmd);
                        renderContext.Submit();
                        continue;
                    }

                    // Note: Legacy Unity behave like this for ShadowMask
                    // When you select ShadowMask in Lighting panel it recompile shaders on the fly with the SHADOW_MASK keyword.
                    // However there is no C# function that we can query to know what mode have been select in Lighting Panel and it will be wrong anyway. Lighting Panel setup what will be the next bake mode. But until light is bake, it is wrong.
                    // Currently to know if you need shadow mask you need to go through all visible lights (of CullResult), check the LightBakingOutput struct and look at lightmapBakeType/mixedLightingMode. If one light have shadow mask bake mode, then you need shadow mask features (i.e extra Gbuffer).
                    // It mean that when we build a standalone player, if we detect a light with bake shadow mask, we generate all shader variant (with and without shadow mask) and at runtime, when a bake shadow mask light is visible, we dynamically allocate an extra GBuffer and switch the shader.
                    // So the first thing to do is to go through all the light: PrepareLightsForGPU
                    bool enableBakeShadowMask;
                    using (new ProfilingSample(cmd, "TP_PrepareLightsForGPU", CustomSamplerId.TPPrepareLightsForGPU.GetSampler()))
                    {
                        enableBakeShadowMask = m_LightLoop.PrepareLightsForGPU(cmd, m_ShadowSettings, m_CullResults, m_ReflectionProbeCullResults, camera) && m_FrameSettings.enableShadowMask;
                    }
                    ConfigureForShadowMask(enableBakeShadowMask, cmd);

                    StartStereoRendering(renderContext, hdCamera.camera);

                    ClearBuffers(hdCamera, cmd);

                    // TODO: Add stereo occlusion mask

                    bool forcePrepassForDecals = m_DbufferManager.vsibleDecalCount > 0;
                    RenderDepthPrepass(m_CullResults, hdCamera, renderContext, cmd, forcePrepassForDecals);

                    RenderObjectsVelocity(m_CullResults, hdCamera, renderContext, cmd);

                    RenderDBuffer(hdCamera, cmd);

					RenderGBuffer(m_CullResults, hdCamera, enableBakeShadowMask, renderContext, cmd);

                    // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
                    CopyDepthBufferIfNeeded(cmd);

                    RenderCameraVelocity(m_CullResults, hdCamera, renderContext, cmd);

                    // Depth texture is now ready, bind it.
                    cmd.SetGlobalTexture(HDShaderIDs._MainDepthTexture, GetDepthTexture());

                    // Caution: We require sun light here as some skies use the sun light to render, it means that UpdateSkyEnvironment must be called after PrepareLightsForGPU.
                    // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
                    UpdateSkyEnvironment(hdCamera, cmd);

                    RenderPyramidDepth(hdCamera, cmd, renderContext, FullScreenDebugMode.DepthPyramid);

                    StopStereoRendering(renderContext, hdCamera.camera);

                    if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled())
                    {
                        RenderDebugViewMaterial(m_CullResults, hdCamera, renderContext, cmd);

                        PushColorPickerDebugTexture(cmd, m_CameraColorBuffer, hdCamera);
                    }
                    else
                    {
                        StartStereoRendering(renderContext, hdCamera.camera);

                        using (new ProfilingSample(cmd, "Render SSAO", CustomSamplerId.RenderSSAO.GetSampler()))
                        {
                            // TODO: Everything here (SSAO, Shadow, Build light list, deferred shadow, material and light classification can be parallelize with Async compute)
                            RenderSSAO(cmd, hdCamera, renderContext, postProcessLayer);
                        }

                        // Clear and copy the stencil texture needs to be moved to before we invoke the async light list build,
                        // otherwise the async compute queue can end up using that texture before the graphics queue is done with it.
                        // TODO: Move this code inside LightLoop
                        if (m_LightLoop.GetFeatureVariantsEnabled())
                        {
                            // For material classification we use compute shader and so can't read into the stencil, so prepare it.
                            using (new ProfilingSample(cmd, "Clear and copy stencil texture", CustomSamplerId.ClearAndCopyStencilTexture.GetSampler()))
                            {
                                HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraStencilBufferCopy, ClearFlag.Color, CoreUtils.clearColorAllBlack);

                                // In the material classification shader we will simply test is we are no lighting
                                // Use ShaderPassID 1 => "Pass 1 - Write 1 if value different from stencilRef to output"
                                HDUtils.DrawFullScreen(cmd, hdCamera, m_CopyStencilForNoLighting, m_CameraStencilBufferCopy, m_CameraDepthStencilBuffer, null, 1);
                            }
                        }

                        StopStereoRendering(renderContext, hdCamera.camera);

                        GPUFence buildGPULightListsCompleteFence = new GPUFence();
                        if (m_FrameSettings.enableAsyncCompute)
                        {
                            GPUFence startFence = cmd.CreateGPUFence();
                            renderContext.ExecuteCommandBuffer(cmd);
                            cmd.Clear();

                            buildGPULightListsCompleteFence = m_LightLoop.BuildGPULightListsAsyncBegin(hdCamera, renderContext, m_CameraDepthStencilBuffer, m_CameraStencilBufferCopy, startFence, m_SkyManager.IsSkyValid());
                        }

                        using (new ProfilingSample(cmd, "Render shadows", CustomSamplerId.RenderShadows.GetSampler()))
                        {
                            m_LightLoop.RenderShadows(renderContext, cmd, m_CullResults);
                            // TODO: check if statement below still apply
                            renderContext.SetupCameraProperties(camera, m_FrameSettings.enableStereo); // Need to recall SetupCameraProperties after RenderShadows as it modify our view/proj matrix
                        }

                        using (new ProfilingSample(cmd, "Deferred directional shadows", CustomSamplerId.RenderDeferredDirectionalShadow.GetSampler()))
                        {
                            // When debug is enabled we need to clear otherwise we may see non-shadows areas with stale values.
                            if(m_CurrentDebugDisplaySettings.fullScreenDebugMode == FullScreenDebugMode.DeferredShadows)
                            {
                                HDUtils.SetRenderTarget(cmd, hdCamera, m_DeferredShadowBuffer, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                            }

                            m_LightLoop.RenderDeferredDirectionalShadow(hdCamera, m_DeferredShadowBuffer, GetDepthTexture(), cmd);
                            PushFullScreenDebugTexture(cmd, m_DeferredShadowBuffer, hdCamera, FullScreenDebugMode.DeferredShadows);
                        }

                        if (m_FrameSettings.enableAsyncCompute)
                        {
                            m_LightLoop.BuildGPULightListAsyncEnd(hdCamera, cmd, buildGPULightListsCompleteFence);
                        }
                        else
                        {
                            using (new ProfilingSample(cmd, "Build Light list", CustomSamplerId.BuildLightList.GetSampler()))
                            {
                                m_LightLoop.BuildGPULightLists(hdCamera, cmd, m_CameraDepthStencilBuffer, m_CameraStencilBufferCopy, m_SkyManager.IsSkyValid());
                            }
                        }

                        // The pass only requires the volume properties, and can run async.
                        m_VolumetricLightingModule.VoxelizeDensityVolumes(hdCamera, cmd);

                        // Render the volumetric lighting.
                        // The pass requires the volume properties, the light list and the shadows, and can run async.
                        m_VolumetricLightingModule.VolumetricLightingPass(hdCamera, cmd, m_FrameSettings);

                        RenderDeferredLighting(hdCamera, cmd);

                        // Might float this higher if we enable stereo w/ deferred
                        StartStereoRendering(renderContext, hdCamera.camera);

                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Opaque);
                        RenderForwardError(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Opaque);

                        // SSS pass here handle both SSS material from deferred and forward
                        m_SSSBufferManager.SubsurfaceScatteringPass(hdCamera, cmd, diffusionProfileSettings, m_FrameSettings,
                                                                    m_CameraColorBuffer, m_CameraSssDiffuseLightingBuffer, m_CameraDepthStencilBuffer, GetDepthTexture());

                        RenderSky(hdCamera, cmd);

                        // Render pre refraction objects
                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.PreRefraction);
                        RenderForwardError(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.PreRefraction);

                        RenderGaussianPyramidColor(hdCamera, cmd, renderContext, true);

                        // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Transparent);
                        RenderForwardError(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Transparent);

                        // Fill depth buffer to reduce artifact for transparent object during postprocess
                        RenderTransparentDepthPostpass(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Transparent);

                        RenderGaussianPyramidColor(hdCamera, cmd, renderContext, false);

                        AccumulateDistortion(m_CullResults, hdCamera, renderContext, cmd);
                        RenderDistortion(cmd, m_Asset.renderPipelineResources, hdCamera);

                        StopStereoRendering(renderContext, hdCamera.camera);

                        PushFullScreenDebugTexture(cmd, m_CameraColorBuffer, hdCamera, FullScreenDebugMode.NanTracker);
                        PushColorPickerDebugTexture(cmd, m_CameraColorBuffer, hdCamera);

                        // The final pass either postprocess of Blit will flip the screen (as it is reverse by default due to Unity openGL legacy)
                        // Postprocess system (that doesn't use cmd.Blit) handle it with configuration (and do not flip in SceneView) or it is automatically done in Blit

                        StartStereoRendering(renderContext, hdCamera.camera);

                        // Final blit
                        if (m_FrameSettings.enablePostprocess && CoreUtils.IsPostProcessingActive(postProcessLayer))
                        {
                            RenderPostProcess(hdCamera, cmd, postProcessLayer);
                        }
                        else
                        {
                            using (new ProfilingSample(cmd, "Blit to final RT", CustomSamplerId.BlitToFinalRT.GetSampler()))
                            {
                                // This Blit will flip the screen on anything other than openGL
                                HDUtils.BlitCameraTexture(cmd, hdCamera, m_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget);
                            }
                        }

                        StopStereoRendering(renderContext, hdCamera.camera);
                        // Pushes to XR headset and/or display mirror
                        if (m_FrameSettings.enableStereo)
                            renderContext.StereoEndRender(hdCamera.camera);
                    }


#if UNITY_EDITOR
                    // During rendering we use our own depth buffer instead of the one provided by the scene view (because we need to be able to control its life cycle)
                    // In order for scene view gizmos/icons etc to be depth test correctly, we need to copy the content of our own depth buffer into the scene view depth buffer.
                    // On subtlety here is that our buffer can be bigger than the camera one so we need to copy only the corresponding portion
                    // (it's handled automatically by the copy shader because it uses a load in pxiel coordinates based on the target).
                    // This copy will also have the effect of re-binding this depth buffer correctly for subsequent editor rendering.

                    // NOTE: This needs to be done before the call to RenderDebug because debug overlays need to update the depth for the scene view as well.
                    // Make sure RenderDebug does not change the current Render Target
                    if (camera.cameraType == CameraType.SceneView)
                    {
                        using (new ProfilingSample(cmd, "Copy Depth For SceneView", CustomSamplerId.CopyDepthForSceneView.GetSampler()))
                        {
                            m_CopyDepth.SetTexture(HDShaderIDs._InputDepth, m_CameraDepthStencilBuffer);
                            cmd.Blit(null, BuiltinRenderTextureType.CameraTarget, m_CopyDepth);
                        }
                    }
#endif
                    // Caution: RenderDebug need to take into account that we have flip the screen (so anything capture before the flip will be flipped)
                    RenderDebug(hdCamera, cmd);

#if UNITY_EDITOR
                    // We need to make sure the viewport is correctly set for the editor rendering. It might have been changed by debug overlay rendering just before.
                    cmd.SetViewport(new Rect(0.0f, 0.0f, hdCamera.actualWidth, hdCamera.actualHeight));
#endif
                }

                // Caution: ExecuteCommandBuffer must be outside of the profiling bracket
                renderContext.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
                renderContext.Submit();
            } // For each camera
        }

        void RenderOpaqueRenderList(CullResults cull,
                                    Camera camera,
                                    ScriptableRenderContext renderContext,
                                    CommandBuffer cmd,
                                    ShaderPassName passName,
                                    RendererConfiguration rendererConfiguration = 0,
                                    RenderQueueRange? inRenderQueueRange = null,
                                    RenderStateBlock? stateBlock = null,
                                    Material overrideMaterial = null)
        {
            m_SinglePassName[0] = passName;
            RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_SinglePassName, rendererConfiguration, inRenderQueueRange, stateBlock, overrideMaterial);
        }

        void RenderOpaqueRenderList(CullResults cull,
                                    Camera camera,
                                    ScriptableRenderContext renderContext,
                                    CommandBuffer cmd,
                                    ShaderPassName[] passNames,
                                    RendererConfiguration rendererConfiguration = 0,
                                    RenderQueueRange? inRenderQueueRange = null,
                                    RenderStateBlock? stateBlock = null,
                                    Material overrideMaterial = null)
        {
            if (!m_FrameSettings.enableOpaqueObjects)
                return;

            // This is done here because DrawRenderers API lives outside command buffers so we need to make call this before doing any DrawRenders
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var drawSettings = new DrawRendererSettings(camera, HDShaderPassNames.s_EmptyName)
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CommonOpaque }
            };

            for (int i = 0; i < passNames.Length; ++i)
            {
                drawSettings.SetShaderPassName(i, passNames[i]);
            }

            if (overrideMaterial != null)
                drawSettings.SetOverrideMaterial(overrideMaterial, 0);

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = inRenderQueueRange == null ? HDRenderQueue.k_RenderQueue_AllOpaque : inRenderQueueRange.Value
            };

            if (stateBlock == null)
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
            else
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings, stateBlock.Value);
        }

        void RenderTransparentRenderList(CullResults cull,
                                         Camera camera,
                                         ScriptableRenderContext renderContext,
                                         CommandBuffer cmd,
                                         ShaderPassName passName,
                                         RendererConfiguration rendererConfiguration = 0,
                                         RenderQueueRange? inRenderQueueRange = null,
                                         RenderStateBlock? stateBlock = null,
                                         Material overrideMaterial = null)
        {
            m_SinglePassName[0] = passName;
            RenderTransparentRenderList(cull, camera, renderContext, cmd, m_SinglePassName,
                                        rendererConfiguration, inRenderQueueRange, stateBlock, overrideMaterial);
        }

        void RenderTransparentRenderList(CullResults cull,
                                         Camera camera,
                                         ScriptableRenderContext renderContext,
                                         CommandBuffer cmd,
                                         ShaderPassName[] passNames,
                                         RendererConfiguration rendererConfiguration = 0,
                                         RenderQueueRange? inRenderQueueRange = null,
                                         RenderStateBlock? stateBlock = null,
                                         Material overrideMaterial = null
                                         )
        {
            if (!m_FrameSettings.enableTransparentObjects)
                return;

            // This is done here because DrawRenderers API lives outside command buffers so we need to make call this before doing any DrawRenders
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var drawSettings = new DrawRendererSettings(camera, HDShaderPassNames.s_EmptyName)
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CommonTransparent }
            };

            for (int i = 0; i < passNames.Length; ++i)
            {
                drawSettings.SetShaderPassName(i, passNames[i]);
            }

            if (overrideMaterial != null)
                drawSettings.SetOverrideMaterial(overrideMaterial, 0);

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = inRenderQueueRange == null ? HDRenderQueue.k_RenderQueue_AllTransparent : inRenderQueueRange.Value
            };

            if (stateBlock == null)
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
            else
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings, stateBlock.Value);
        }

        void AccumulateDistortion(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!m_FrameSettings.enableDistortion)
                return;

            using (new ProfilingSample(cmd, "Distortion", CustomSamplerId.Distortion.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, hdCamera, m_DistortionBuffer, m_CameraDepthStencilBuffer, ClearFlag.Color, Color.clear);

                // Only transparent object can render distortion vectors
                RenderTransparentRenderList(cullResults, hdCamera.camera, renderContext, cmd, HDShaderPassNames.s_DistortionVectorsName);
            }
        }

        void RenderDistortion(CommandBuffer cmd, RenderPipelineResources resources, HDCamera hdCamera)
        {
            if (!m_FrameSettings.enableDistortion)
                return;

            using (new ProfilingSample(cmd, "ApplyDistortion", CustomSamplerId.ApplyDistortion.GetSampler()))
            {
                Vector2 pyramidScale = m_BufferPyramid.GetPyramidToScreenScale(hdCamera);

                // Need to account for the fact that the gaussian pyramid is actually rendered inside the camera viewport in a square texture so we mutiply by the PyramidToScreen scale
                var size = new Vector4(hdCamera.screenSize.x, hdCamera.screenSize.y, pyramidScale.x / hdCamera.screenSize.x, pyramidScale.y / hdCamera.screenSize.y);
                uint x, y, z;
                m_applyDistortionCS.GetKernelThreadGroupSizes(m_applyDistortionKernel, out x, out y, out z);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._DistortionTexture, m_DistortionBuffer);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._GaussianPyramidColorTexture, m_BufferPyramid.colorPyramid);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._CameraColorTexture, m_CameraColorBuffer);
                cmd.SetComputeVectorParam(m_applyDistortionCS, HDShaderIDs._Size, size);
                cmd.SetComputeVectorParam(m_applyDistortionCS, HDShaderIDs._ZBufferParams, Shader.GetGlobalVector(HDShaderIDs._ZBufferParams));

                cmd.DispatchCompute(m_applyDistortionCS, m_applyDistortionKernel, Mathf.CeilToInt(size.x / x), Mathf.CeilToInt(size.y / y), 1);
            }
        }

        // RenderDepthPrepass render both opaque and opaque alpha tested based on engine configuration.
        // Forward only renderer: We always render everything
        // Deferred renderer: We render a depth prepass only if engine request it. We can decide if we render everything or only opaque alpha tested object.
        // Forward opaque with deferred renderer (DepthForwardOnly pass): We always render everything
        void RenderDepthPrepass(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd, bool forcePrepass)
        {
            // In case of deferred renderer, we can have forward opaque material. These materials need to be render in the depth buffer to correctly build the light list.
            // And they will tag the stencil to not be lit during the deferred lighting pass.

            // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
            // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
            // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.

            // In case of forward only rendering we have a depth prepass. In case of deferred renderer, it is optional
            bool addFullDepthPrepass = m_FrameSettings.enableForwardRenderingOnly || m_FrameSettings.enableDepthPrepassWithDeferredRendering;
            bool addAlphaTestedOnly = !m_FrameSettings.enableForwardRenderingOnly && m_FrameSettings.enableDepthPrepassWithDeferredRendering && m_FrameSettings.enableAlphaTestOnlyInDeferredPrepass;

            var camera = hdCamera.camera;

            using (new ProfilingSample(cmd, addAlphaTestedOnly ? "Depth Prepass alpha test" : "Depth Prepass", CustomSamplerId.DepthPrepass.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraDepthStencilBuffer);
                if (forcePrepass || (addFullDepthPrepass && !addAlphaTestedOnly)) // Always true in case of forward rendering, use in case of deferred rendering if requesting a full depth prepass
                {
                    // We render first the opaque object as opaque alpha tested are more costly to render and could be reject by early-z (but not Hi-z as it is disable with clip instruction)
                    // This is handled automatically with the RenderQueue value (OpaqueAlphaTested have a different value and thus are sorted after Opaque)
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthOnlyAndDepthForwardOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque);
                }
                else // Deferred rendering with partial depth prepass
                {
                    // We always do a DepthForwardOnly pass with all the opaque (including alpha test)
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthForwardOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque);

                    // Render Alpha test only if requested
                    if (addAlphaTestedOnly)
                    {
                        var renderQueueRange = new RenderQueueRange { min = (int)RenderQueue.AlphaTest, max = (int)RenderQueue.GeometryLast - 1 };
                        RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthOnlyPassNames, 0, renderQueueRange);
                    }
                }
            }

            if (m_FrameSettings.enableTransparentPrepass)
            {
                // Render transparent depth prepass after opaque one
                using (new ProfilingSample(cmd, "Transparent Depth Prepass", CustomSamplerId.TransparentDepthPrepass.GetSampler()))
                {
                    RenderTransparentRenderList(cull, camera, renderContext, cmd, m_TransparentDepthPrepassNames);
                }
            }
        }

        // RenderGBuffer do the gbuffer pass. This is solely call with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque apha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(CullResults cull, HDCamera hdCamera, bool enableShadowMask, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (m_FrameSettings.enableForwardRenderingOnly)
                return;

            var camera = hdCamera.camera;

            using (new ProfilingSample(cmd, m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ? "GBuffer Debug" : "GBuffer", CustomSamplerId.GBuffer.GetSampler()))
            {
                // setup GBuffer for rendering
                HDUtils.SetRenderTarget(cmd, hdCamera, m_GbufferManager.GetBuffersRTI(enableShadowMask), m_CameraDepthStencilBuffer);

                // Render opaque objects into GBuffer
                if (m_FrameSettings.enableDepthPrepassWithDeferredRendering)
                {
                    // When using depth prepass for opaque alpha test only we need to use regular depth test for normal opaque objects.
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferName, m_currentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_OpaqueNoAlphaTest, m_FrameSettings.enableAlphaTestOnlyInDeferredPrepass ? m_DepthStateOpaque : m_DepthStateOpaqueWithPrepass);
                    // but for opaque alpha tested object we use a depth equal and no depth write. And we rely on the shader pass GbufferWithDepthPrepass
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferWithPrepassName, m_currentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_OpaqueAlphaTest, m_DepthStateOpaqueWithPrepass);
                }
                else
                {
                    // No depth prepass, use regular depth test - Note that we will render opaque then opaque alpha tested (based on the RenderQueue system)
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferName, m_currentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_AllOpaque, m_DepthStateOpaque);
                }

                m_GbufferManager.BindBufferAsTextures(cmd);
            }
        }

        void RenderDBuffer(HDCamera camera, CommandBuffer cmd)
        {
            if (!m_FrameSettings.enableDBuffer)
                return;

            using (new ProfilingSample(cmd, "DBufferRender", CustomSamplerId.DBufferRender.GetSampler()))
            {
                // We need to copy depth buffer texture if we want to bind it at this stage
                CopyDepthBufferIfNeeded(cmd);

                // Depth texture is now ready, bind it.
                cmd.SetGlobalTexture(HDShaderIDs._MainDepthTexture, GetDepthTexture());

				// for alpha compositing, color is cleared to 0, alpha to 1
				// https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html

				Color clearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
				HDUtils.SetRenderTarget(cmd, camera, m_DbufferManager.GetBuffersRTI(), m_CameraDepthStencilBuffer, ClearFlag.Color, clearColor);

				// we need to do a separate clear for normals, because they are cleared to a different color
				Color clearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
				m_DbufferManager.ClearNormalTargetAndHTile(cmd, camera, clearColorNormal);

				HDUtils.SetRenderTarget(cmd, camera, m_DbufferManager.GetBuffersRTI(), m_CameraDepthStencilBuffer); // do not clear anymore
                m_DbufferManager.SetHTile(m_DbufferManager.bufferCount, cmd);
                DecalSystem.instance.RenderIntoDBuffer(cmd);
                m_DbufferManager.UnSetHTile(cmd);
                m_DbufferManager.SetHTileTexture(cmd);  // mask per 8x8 tile used for optimization when looking up dbuffer values
            }
        }

        void RenderDebugViewMaterial(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "DisplayDebug ViewMaterial", CustomSamplerId.DisplayDebugViewMaterial.GetSampler()))
            {
                if (m_CurrentDebugDisplaySettings.materialDebugSettings.IsDebugGBufferEnabled() && !m_FrameSettings.enableForwardRenderingOnly)
                {
                    using (new ProfilingSample(cmd, "DebugViewMaterialGBuffer", CustomSamplerId.DebugViewMaterialGBuffer.GetSampler()))
                    {
                        HDUtils.DrawFullScreen(cmd, hdCamera, m_currentDebugViewMaterialGBuffer, m_CameraColorBuffer);
                    }
                }
                else
                {
                    // When rendering debug material we shouldn't rely on a depth prepass for optimizing the alpha clip test. As it is control on the material inspector side
                    // we must override the state here.

                    HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_CameraDepthStencilBuffer, ClearFlag.All, CoreUtils.clearColorAllBlack);
                    // Render Opaque forward
                    RenderOpaqueRenderList(cull, hdCamera.camera, renderContext, cmd, m_AllForwardOpaquePassNames, m_currentRendererConfigurationBakedLighting, stateBlock: m_DepthStateOpaque);

                    // Render forward transparent
                    RenderTransparentRenderList(cull, hdCamera.camera, renderContext, cmd, m_AllTransparentPassNames, m_currentRendererConfigurationBakedLighting, stateBlock: m_DepthStateOpaque);
                }
            }

            // Last blit
            {
                using (new ProfilingSample(cmd, "Blit DebugView Material Debug", CustomSamplerId.BlitDebugViewMaterialDebug.GetSampler()))
                {
                    // This Blit will flip the screen anything other than openGL
                    HDUtils.BlitCameraTexture(cmd, hdCamera, m_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget);
                }
            }
        }

        void RenderSSAO(CommandBuffer cmd, HDCamera hdCamera, ScriptableRenderContext renderContext, PostProcessLayer postProcessLayer)
        {
            var camera = hdCamera.camera;

            // Apply SSAO from PostProcessLayer
            if (m_FrameSettings.enableSSAO && postProcessLayer != null && postProcessLayer.enabled)
            {
                var settings = postProcessLayer.GetSettings<AmbientOcclusion>();

                if (settings.IsEnabledAndSupported(null))
                {
                    postProcessLayer.BakeMSVOMap(cmd, camera, m_AmbientOcclusionBuffer, GetDepthTexture(), true);

                    cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, m_AmbientOcclusionBuffer);
                    cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(settings.color.value.r, settings.color.value.g, settings.color.value.b, settings.directLightingStrength.value));
                    PushFullScreenDebugTexture(cmd, m_AmbientOcclusionBuffer, hdCamera, FullScreenDebugMode.SSAO);
                    return;
                }
            }

            // No AO applied - neutral is black, see the comment in the shaders
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, RuntimeUtilities.blackTexture);
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
        }

        void RenderDeferredLighting(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (m_FrameSettings.enableForwardRenderingOnly)
                return;

            m_MRTCache2[0] = m_CameraColorBuffer;
            m_MRTCache2[1] = m_CameraSssDiffuseLightingBuffer;
            var depthTexture = GetDepthTexture();

            var options = new LightLoop.LightingPassOptions();

            if (m_FrameSettings.enableSubsurfaceScattering)
            {
                // Output split lighting for materials asking for it (masked in the stencil buffer)
                options.outputSplitLighting = true;

                m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_CameraDepthStencilBuffer, depthTexture, options);
            }

            // Output combined lighting for all the other materials.
            options.outputSplitLighting = false;

            m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_CameraDepthStencilBuffer, depthTexture, options);
        }

        void UpdateSkyEnvironment(HDCamera hdCamera, CommandBuffer cmd)
        {
            m_SkyManager.UpdateEnvironment(hdCamera, m_LightLoop.GetCurrentSunLight(), cmd);
        }

        void RenderSky(HDCamera hdCamera, CommandBuffer cmd)
        {
            // Rendering the sky is the first time in the frame where we need fog parameters so we push them here for the whole frame.
            var visualEnv = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            visualEnv.PushFogShaderParameters(cmd, m_FrameSettings);

            m_SkyManager.RenderSky(hdCamera, m_LightLoop.GetCurrentSunLight(), m_CameraColorBuffer, m_CameraDepthStencilBuffer, cmd);

            if (visualEnv.fogType != FogType.None || m_VolumetricLightingModule.preset != VolumetricLightingModule.VolumetricLightingPreset.Off)
                m_SkyManager.RenderOpaqueAtmosphericScattering(cmd);
        }

        public Texture2D ExportSkyToTexture()
        {
            return m_SkyManager.ExportSkyToTexture();
        }

        // Render forward is use for both transparent and opaque objects. In case of deferred we can still render opaque object in forward.
        void RenderForward(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
        {
            // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
            // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
            // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.
            // The RenderForward pass will render the appropriate pass depends on the engine settings. In case of forward only rendering, both "Forward" pass and "ForwardOnly" pass
            // material will be render for both transparent and opaque. In case of deferred, both path are used for transparent but only "ForwardOnly" is use for opaque.
            // (Thus why "Forward" and "ForwardOnly" are exclusive, else they will render two times"

            string profileName;
            if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled())
            {
                profileName = k_ForwardPassDebugName[(int)pass];
            }
            else
            {
                profileName = k_ForwardPassName[(int)pass];
            }

            using (new ProfilingSample(cmd, profileName, CustomSamplerId.ForwardPassName.GetSampler()))
            {
                var camera = hdCamera.camera;

                m_LightLoop.RenderForward(camera, cmd, pass == ForwardPass.Opaque);

                if (pass == ForwardPass.Opaque)
                {
                    // In case of forward SSS we will bind all the required target. It is up to the shader to write into it or not.
                    if (m_FrameSettings.enableSubsurfaceScattering)
                    {
                        RenderTargetIdentifier[] m_MRTWithSSS =
                            new RenderTargetIdentifier[2 + m_SSSBufferManager.sssBufferCount];
                        m_MRTWithSSS[0] = m_CameraColorBuffer; // Store the specular color
                        m_MRTWithSSS[1] = m_CameraSssDiffuseLightingBuffer;
                        for (int i = 0; i < m_SSSBufferManager.sssBufferCount; ++i)
                        {
                            m_MRTWithSSS[i + 2] = m_SSSBufferManager.GetSSSBuffer(i);
                        }

                        HDUtils.SetRenderTarget(cmd, hdCamera, m_MRTWithSSS, m_CameraDepthStencilBuffer);
                    }
                    else
                    {
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_CameraDepthStencilBuffer);
                    }

                    m_ForwardAndForwardOnlyPassNames[0] = m_ForwardOnlyPassNames[0] =
                        HDShaderPassNames.s_ForwardOnlyName;
                    m_ForwardAndForwardOnlyPassNames[1] = HDShaderPassNames.s_ForwardName;

                    var passNames = m_FrameSettings.enableForwardRenderingOnly
                        ? m_ForwardAndForwardOnlyPassNames
                        : m_ForwardOnlyPassNames;
                    RenderOpaqueRenderList(cullResults, camera, renderContext, cmd, passNames, m_currentRendererConfigurationBakedLighting);
                }
                else
                {
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_CameraDepthStencilBuffer);
                    if (m_FrameSettings.enableDBuffer) // enable d-buffer flag value is being interpreted more like enable decals in general now that we have clustered
                    {
                        DecalSystem.instance.SetAtlas(cmd); // for clustered decals
                    }
                    RenderTransparentRenderList(cullResults, camera, renderContext, cmd, m_AllTransparentPassNames, m_currentRendererConfigurationBakedLighting, pass == ForwardPass.PreRefraction ? HDRenderQueue.k_RenderQueue_PreRefraction : HDRenderQueue.k_RenderQueue_Transparent);
                }
            }
        }

        // This is use to Display legacy shader with an error shader
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void RenderForwardError(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
        {
            using (new ProfilingSample(cmd, "Render Forward Error", CustomSamplerId.RenderForwardError.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_CameraDepthStencilBuffer);

                if (pass == ForwardPass.Opaque)
                {
                    RenderOpaqueRenderList(cullResults, hdCamera.camera, renderContext, cmd, m_ForwardErrorPassNames, 0, null, null, m_ErrorMaterial);
                }
                else
                {
                    RenderTransparentRenderList(cullResults, hdCamera.camera, renderContext, cmd, m_ForwardErrorPassNames, 0, pass == ForwardPass.PreRefraction ? HDRenderQueue.k_RenderQueue_PreRefraction : HDRenderQueue.k_RenderQueue_Transparent, null, m_ErrorMaterial);
                }
            }
        }

        void RenderTransparentDepthPostpass(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
        {
            if (!m_FrameSettings.enableTransparentPostpass)
                return;

            using (new ProfilingSample(cmd, "Render Transparent Depth Post ", CustomSamplerId.TransparentDepthPostpass.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraDepthStencilBuffer);
                RenderTransparentRenderList(cullResults, hdCamera.camera, renderContext, cmd, m_TransparentDepthPostpassNames);
            }
        }

        void RenderObjectsVelocity(CullResults cullResults, HDCamera hdcamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!m_FrameSettings.enableMotionVectors || !m_FrameSettings.enableObjectMotionVectors)
                return;

            using (new ProfilingSample(cmd, "Objects Velocity", CustomSamplerId.ObjectsVelocity.GetSampler()))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdcamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                HDUtils.SetRenderTarget(cmd, hdcamera, m_VelocityBuffer, m_CameraDepthStencilBuffer);
                RenderOpaqueRenderList(cullResults, hdcamera.camera, renderContext, cmd, HDShaderPassNames.s_MotionVectorsName, RendererConfiguration.PerObjectMotionVectors);
            }
        }

        void RenderCameraVelocity(CullResults cullResults, HDCamera hdcamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!m_FrameSettings.enableMotionVectors)
                return;

            using (new ProfilingSample(cmd, "Camera Velocity", CustomSamplerId.CameraVelocity.GetSampler()))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdcamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                HDUtils.DrawFullScreen(cmd, hdcamera, m_CameraMotionVectorsMaterial, m_VelocityBuffer, m_CameraDepthStencilBuffer, null, 0);
                PushFullScreenDebugTexture(cmd, m_VelocityBuffer, hdcamera, FullScreenDebugMode.MotionVectors);
            }
        }

        void RenderGaussianPyramidColor(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, bool isPreRefraction)
        {
            if (isPreRefraction)
            {
                if (!m_FrameSettings.enableRoughRefraction)
                    return;
            }
            else
            {
                // TODO: This final Gaussian pyramid can be reuse by Bloom and SSR in the future, so disable it only if there is no postprocess AND no distortion
                if (!m_FrameSettings.enableDistortion && !m_FrameSettings.enablePostprocess && !m_FrameSettings.enableSSR)
                    return;
            }

            using (new ProfilingSample(cmd, "Gaussian Pyramid Color", CustomSamplerId.GaussianPyramidColor.GetSampler()))
                m_BufferPyramid.RenderColorPyramid(hdCamera, cmd, renderContext, m_CameraColorBuffer);

            Vector2 pyramidScale = m_BufferPyramid.GetPyramidToScreenScale(hdCamera);
            PushFullScreenDebugTextureMip(cmd, m_BufferPyramid.colorPyramid, m_BufferPyramid.GetPyramidLodCount(hdCamera), new Vector4(pyramidScale.x, pyramidScale.y, 0.0f, 0.0f), hdCamera, isPreRefraction ? FullScreenDebugMode.PreRefractionColorPyramid : FullScreenDebugMode.FinalColorPyramid);
        }

        void RenderPyramidDepth(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, FullScreenDebugMode debugMode)
        {
            if (!m_FrameSettings.enableRoughRefraction)
                return;

            using (new ProfilingSample(cmd, "Pyramid Depth", CustomSamplerId.PyramidDepth.GetSampler()))
                m_BufferPyramid.RenderDepthPyramid(hdCamera, cmd, renderContext, GetDepthTexture());

            Vector2 pyramidScale = m_BufferPyramid.GetPyramidToScreenScale(hdCamera);
            PushFullScreenDebugTextureMip(cmd, m_BufferPyramid.depthPyramid, m_BufferPyramid.GetPyramidLodCount(hdCamera), new Vector4(pyramidScale.x, pyramidScale.y, 0.0f, 0.0f), hdCamera, debugMode);

            cmd.SetGlobalTexture(HDShaderIDs._PyramidDepthTexture, m_BufferPyramid.depthPyramid);
        }

        void RenderPostProcess(HDCamera hdcamera, CommandBuffer cmd, PostProcessLayer layer)
        {
            using (new ProfilingSample(cmd, "Post-processing", CustomSamplerId.PostProcessing.GetSampler()))
            {
                RenderTargetIdentifier source = m_CameraColorBuffer;

#if UNITY_EDITOR
                bool tempHACK = true;
#else
                // In theory in the player the only place where we have post process is the main camera with the RTHandle reference size, so we won't need to copy.
                bool tempHACK = false;
#endif
                if (tempHACK)
                {
                    // TEMPORARY:
                    // Since we don't render to the full render textures, we need to feed the post processing stack with the right scale/bias.
                    // This feature not being implemented yet, we'll just copy the relevant buffers into an appropriately sized RT.
                    cmd.ReleaseTemporaryRT(HDShaderIDs._CameraDepthTexture);
                    cmd.ReleaseTemporaryRT(HDShaderIDs._CameraMotionVectorsTexture);
                    cmd.ReleaseTemporaryRT(HDShaderIDs._CameraColorTexture);

                    cmd.GetTemporaryRT(HDShaderIDs._CameraDepthTexture, hdcamera.actualWidth, hdcamera.actualHeight, m_CameraDepthStencilBuffer.rt.depth, FilterMode.Point, m_CameraDepthStencilBuffer.rt.format);
                    m_CopyDepth.SetTexture(HDShaderIDs._InputDepth, m_CameraDepthStencilBuffer);
                    cmd.Blit(null, HDShaderIDs._CameraDepthTexture, m_CopyDepth);
                    if (m_VelocityBuffer != null)
                    {
                        cmd.GetTemporaryRT(HDShaderIDs._CameraMotionVectorsTexture, hdcamera.actualWidth, hdcamera.actualHeight, 0, FilterMode.Point, m_VelocityBuffer.rt.format);
                        HDUtils.BlitCameraTexture(cmd, hdcamera, m_VelocityBuffer, HDShaderIDs._CameraMotionVectorsTexture);
                    }
                    cmd.GetTemporaryRT(HDShaderIDs._CameraColorTexture, hdcamera.actualWidth, hdcamera.actualHeight, 0, FilterMode.Point, m_CameraColorBuffer.rt.format);
                    HDUtils.BlitCameraTexture(cmd, hdcamera, m_CameraColorBuffer, HDShaderIDs._CameraColorTexture);
                    source = HDShaderIDs._CameraColorTexture;
                }
                else
                {
                    // Note: Here we don't use GetDepthTexture() to get the depth texture but m_CameraDepthStencilBuffer as the Forward transparent pass can
                    // write extra data to deal with DOF/MB
                    cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_CameraDepthStencilBuffer);
                    cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, m_VelocityBuffer);
                }

                var context = hdcamera.postprocessRenderContext;
                context.Reset();
                context.source = source;
                context.destination = BuiltinRenderTextureType.CameraTarget;
                context.command = cmd;
                context.camera = hdcamera.camera;
                context.sourceFormat = RenderTextureFormat.ARGBHalf;
                context.flip = true;

                layer.Render(context);
            }
        }

        public void ApplyDebugDisplaySettings(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ||
                m_CurrentDebugDisplaySettings.fullScreenDebugMode != FullScreenDebugMode.None ||
                m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
            {
                // enable globally the keyword DEBUG_DISPLAY on shader that support it with multicompile
                cmd.EnableShaderKeyword("DEBUG_DISPLAY");

                // This is for texture streaming
                m_CurrentDebugDisplaySettings.UpdateMaterials();

                var lightingDebugSettings = m_CurrentDebugDisplaySettings.lightingDebugSettings;
                var debugAlbedo = new Vector4(lightingDebugSettings.overrideAlbedo ? 1.0f : 0.0f, lightingDebugSettings.overrideAlbedoValue.r, lightingDebugSettings.overrideAlbedoValue.g, lightingDebugSettings.overrideAlbedoValue.b);
                var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);
                var debugNormal = new Vector4(lightingDebugSettings.overrideNormal ? 1.0f : 0.0f, 0.0f, 0.0f, 0.0f);

                cmd.SetGlobalInt(HDShaderIDs._DebugViewMaterial, (int)m_CurrentDebugDisplaySettings.GetDebugMaterialIndex());
                cmd.SetGlobalInt(HDShaderIDs._DebugLightingMode, (int)m_CurrentDebugDisplaySettings.GetDebugLightingMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugMipMapMode, (int)m_CurrentDebugDisplaySettings.GetDebugMipMapMode());

                cmd.SetGlobalVector(HDShaderIDs._DebugLightingAlbedo, debugAlbedo);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingSmoothness, debugSmoothness);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingNormal, debugNormal);

                cmd.SetGlobalVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));

                cmd.SetGlobalTexture(HDShaderIDs._DebugFont, m_Asset.renderPipelineResources.debugFontTexture);
            }
            else
            {
                // TODO: Be sure that if there is no change in the state of this keyword, it doesn't imply any work on CPU side! else we will need to save the sate somewher
                cmd.DisableShaderKeyword("DEBUG_DISPLAY");
            }
        }

        public void PushColorPickerDebugTexture(CommandBuffer cmd, RTHandle textureID, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
            {
                HDUtils.BlitCameraTexture(cmd, hdCamera, textureID, m_DebugColorPickerBuffer);
            }
        }

        // TODO TEMP: Not sure I want to keep this special case. Gotta see how to get rid of it (not sure it will work correctly for non-full viewports.
        public void PushColorPickerDebugTexture(CommandBuffer cmd, RenderTargetIdentifier textureID, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
            {
                HDUtils.BlitCameraTexture(cmd, hdCamera, textureID, m_DebugColorPickerBuffer);
            }
        }

        public void PushFullScreenDebugTexture(CommandBuffer cmd, RTHandle textureID, HDCamera hdCamera, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
                HDUtils.BlitCameraTexture(cmd, hdCamera, textureID, m_DebugFullScreenTempBuffer);
            }
        }

        void PushFullScreenDebugTextureMip(CommandBuffer cmd, RTHandle texture, int lodCount, Vector4 scaleBias, HDCamera hdCamera, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.fullscreenDebugMip * (lodCount));

                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
                HDUtils.BlitCameraTexture(cmd, hdCamera, texture, m_DebugFullScreenTempBuffer, scaleBias, mipIndex);
            }
        }

        void RenderDebug(HDCamera hdCamera, CommandBuffer cmd)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return;

            using (new ProfilingSample(cmd, "Render Debug", CustomSamplerId.RenderDebug.GetSampler()))
            {
                // First render full screen debug texture
                if (m_CurrentDebugDisplaySettings.fullScreenDebugMode != FullScreenDebugMode.None && m_FullScreenDebugPushed)
                {
                    m_FullScreenDebugPushed = false;
                    cmd.SetGlobalTexture(HDShaderIDs._DebugFullScreenTexture, m_DebugFullScreenTempBuffer);
                    // TODO: Replace with command buffer call when available
                    m_DebugFullScreen.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)m_CurrentDebugDisplaySettings.fullScreenDebugMode);
                    // Everything we have capture is flipped (as it happen before FinalPass/postprocess/Blit. So if we are not in SceneView
                    // (i.e. we have perform a flip, we need to flip the input texture)
                    m_DebugFullScreen.SetFloat(HDShaderIDs._RequireToFlipInputTexture, hdCamera.camera.cameraType != CameraType.SceneView ? 1.0f : 0.0f);
                    HDUtils.DrawFullScreen(cmd, hdCamera, m_DebugFullScreen, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);

                    PushColorPickerDebugTexture(cmd, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget, hdCamera);
                }

                // Then overlays
                float x = 0;
                float overlayRatio = m_CurrentDebugDisplaySettings.debugOverlayRatio;
                float overlaySize = Math.Min(hdCamera.actualHeight, hdCamera.actualWidth) * overlayRatio;
                float y = hdCamera.actualHeight - overlaySize;

                var lightingDebug = m_CurrentDebugDisplaySettings.lightingDebugSettings;

                if (lightingDebug.displaySkyReflection)
                {
                    var skyReflection = m_SkyManager.skyReflection;
                    m_SharedPropertyBlock.SetTexture(HDShaderIDs._InputCubemap, skyReflection);
                    m_SharedPropertyBlock.SetFloat(HDShaderIDs._Mipmap, lightingDebug.skyReflectionMipmap);
                    cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                    cmd.DrawProcedural(Matrix4x4.identity, m_DebugDisplayLatlong, 0, MeshTopology.Triangles, 3, 1, m_SharedPropertyBlock);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                }

                m_LightLoop.RenderDebugOverlay(hdCamera, cmd, m_CurrentDebugDisplaySettings, ref x, ref y, overlaySize, hdCamera.actualWidth);

                if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
                {
                    ColorPickerDebugSettings colorPickerDebugSettings = m_CurrentDebugDisplaySettings.colorPickerDebugSettings;

                    // Here we have three cases:
                    // - Material debug is enabled, this is the buffer we display
                    // - Otherwise we display the HDR buffer before postprocess and distortion
                    // - If fullscreen debug is enabled we always use it

                    cmd.SetGlobalTexture(HDShaderIDs._DebugColorPickerTexture, m_DebugColorPickerBuffer); // No SetTexture with RenderTarget identifier... so use SetGlobalTexture
                    // TODO: Replace with command buffer call when available
                    m_DebugColorPicker.SetColor(HDShaderIDs._ColorPickerFontColor, colorPickerDebugSettings.fontColor);
                    var colorPickerParam = new Vector4(colorPickerDebugSettings.colorThreshold0, colorPickerDebugSettings.colorThreshold1, colorPickerDebugSettings.colorThreshold2, colorPickerDebugSettings.colorThreshold3);
                    m_DebugColorPicker.SetVector(HDShaderIDs._ColorPickerParam, colorPickerParam);
                    m_DebugColorPicker.SetInt(HDShaderIDs._ColorPickerMode, (int)colorPickerDebugSettings.colorPickerMode);
                    // The material display debug perform sRGBToLinear conversion as the final blit currently hardcode a linearToSrgb conversion. As when we read with color picker this is not done,
                    // we perform it inside the color picker shader. But we shouldn't do it for HDR buffer.
                    m_DebugColorPicker.SetFloat(HDShaderIDs._ApplyLinearToSRGB, m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled() ? 1.0f : 0.0f);
                    // Everything we have capture is flipped (as it happen before FinalPass/postprocess/Blit. So if we are not in SceneView
                    // (i.e. we have perform a flip, we need to flip the input texture) + we need to handle the case were we debug a fullscreen pass that have already perform the flip
                    m_DebugColorPicker.SetFloat(HDShaderIDs._RequireToFlipInputTexture, hdCamera.camera.cameraType != CameraType.SceneView ? 1.0f : 0.0f);
                    HDUtils.DrawFullScreen(cmd, hdCamera, m_DebugColorPicker, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);
                }
            }
        }

        void ClearBuffers(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "ClearBuffers", CustomSamplerId.ClearBuffers.GetSampler()))
            {
                // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.
                // Clear depth/stencil and init buffers
                using (new ProfilingSample(cmd, "Clear Depth/Stencil", CustomSamplerId.ClearDepthStencil.GetSampler()))
                {
                    if (hdCamera.clearDepth)
                    {
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_CameraDepthStencilBuffer, ClearFlag.Depth);
                    }
                }

                // Clear the HDR target
                using (new ProfilingSample(cmd, "Clear HDR target", CustomSamplerId.ClearHDRTarget.GetSampler()))
                {
                    if (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.BackgroundColor ||
                        // If we want the sky but the sky don't exist, still clear with background color
                        (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky && !m_SkyManager.IsSkyValid()) ||
                        // Special handling for Preview we force to clear with background color (i.e black)
                        // Note that the sky use in this case is the last one setup. If there is no scene or game, there is no sky use as reflection in the preview
                        hdCamera.camera.cameraType == CameraType.Preview
                        )
                    {
                        Color clearColor = hdCamera.backgroundColorHDR;
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_CameraDepthStencilBuffer, ClearFlag.Color, clearColor);
                    }
                }

                // Clear the diffuse SSS lighting target
                using (new ProfilingSample(cmd, "Clear SSS diffuse target", CustomSamplerId.ClearSSSDiffuseTarget.GetSampler()))
                {
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraSssDiffuseLightingBuffer, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                }

                // TODO: As we are in development and have not all the setup pass we still clear the color in emissive buffer and gbuffer, but this will be removed later.

                // Clear GBuffers
                if (!m_FrameSettings.enableForwardRenderingOnly)
                {
                    using (new ProfilingSample(cmd, "Clear GBuffer", CustomSamplerId.ClearGBuffer.GetSampler()))
                    {
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_GbufferManager.GetBuffersRTI(), m_CameraDepthStencilBuffer, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }
                // END TEMP
            }
        }

        void StartStereoRendering(ScriptableRenderContext renderContext, Camera cam)
        {
            if (m_FrameSettings.enableStereo)
                renderContext.StartMultiEye(cam);
        }

        void StopStereoRendering(ScriptableRenderContext renderContext, Camera cam)
        {
            if (m_FrameSettings.enableStereo)
                renderContext.StopMultiEye(cam);
        }
    }
}
