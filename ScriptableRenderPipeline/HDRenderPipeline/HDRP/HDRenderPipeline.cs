using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class GBufferManager
    {
        public const int k_MaxGbuffer = 8;

        public int gbufferCount { get; set; }

        RenderTargetIdentifier[] m_ColorMRTs;
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[k_MaxGbuffer];

        public void InitGBuffers(RenderTextureDescriptor rtDesc, RenderPipelineMaterial deferredMaterial, bool enableBakeShadowMask, CommandBuffer cmd)
        {
            // Init Gbuffer description
            gbufferCount = deferredMaterial.GetMaterialGBufferCount();
            RenderTextureFormat[] rtFormat;
            RenderTextureReadWrite[] rtReadWrite;
            deferredMaterial.GetMaterialGBufferDescription(out rtFormat, out rtReadWrite);

            rtDesc.depthBufferBits = 0;

            for (int gbufferIndex = 0; gbufferIndex < gbufferCount; ++gbufferIndex)
            {
                cmd.ReleaseTemporaryRT(HDShaderIDs._GBufferTexture[gbufferIndex]);

                rtDesc.colorFormat = rtFormat[gbufferIndex];
                rtDesc.sRGB = (rtReadWrite[gbufferIndex] != RenderTextureReadWrite.Linear);

                cmd.GetTemporaryRT(HDShaderIDs._GBufferTexture[gbufferIndex], rtDesc, FilterMode.Point);
                m_RTIDs[gbufferIndex] = new RenderTargetIdentifier(HDShaderIDs._GBufferTexture[gbufferIndex]);
            }

            if (enableBakeShadowMask)
            {
                cmd.ReleaseTemporaryRT(HDShaderIDs._ShadowMaskTexture);

                rtDesc.colorFormat = Builtin.GetShadowMaskBufferFormat();
                rtDesc.sRGB = (Builtin.GetShadowMaskBufferReadWrite() != RenderTextureReadWrite.Linear);

                cmd.GetTemporaryRT(HDShaderIDs._ShadowMaskTexture, rtDesc, FilterMode.Point);
                m_RTIDs[gbufferCount++] = new RenderTargetIdentifier(HDShaderIDs._ShadowMaskTexture);
            }
        }

        public RenderTargetIdentifier[] GetGBuffers()
        {
            // TODO: check with THomas or Tim if wa can simply return m_ColorMRTs with null for extra RT
            if (m_ColorMRTs == null || m_ColorMRTs.Length != gbufferCount)
                m_ColorMRTs = new RenderTargetIdentifier[gbufferCount];

            for (int index = 0; index < gbufferCount; index++)
            {
                m_ColorMRTs[index] = m_RTIDs[index];
            }

            return m_ColorMRTs;
        }
    }

    public class DBufferManager
    {
        public const int k_MaxDbuffer = 4;

        public int dbufferCount { get; set; }
        public int vsibleDecalCount { get; set; }

        RenderTargetIdentifier[] m_ColorMRTs;
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[k_MaxDbuffer];

        public void InitDBuffers(RenderTextureDescriptor rtDesc,  CommandBuffer cmd)
        {
            dbufferCount = Decal.GetMaterialDBufferCount();
            RenderTextureFormat[] rtFormat;
            RenderTextureReadWrite[] rtReadWrite;
            Decal.GetMaterialDBufferDescription(out rtFormat, out rtReadWrite);

            rtDesc.depthBufferBits = 0;

            for (int dbufferIndex = 0; dbufferIndex < dbufferCount; ++dbufferIndex)
            {
                cmd.ReleaseTemporaryRT(HDShaderIDs._DBufferTexture[dbufferIndex]);

                rtDesc.colorFormat = rtFormat[dbufferIndex];
                rtDesc.sRGB = (rtReadWrite[dbufferIndex] != RenderTextureReadWrite.Linear);

                cmd.GetTemporaryRT(HDShaderIDs._DBufferTexture[dbufferIndex], rtDesc, FilterMode.Point);
                m_RTIDs[dbufferIndex] = new RenderTargetIdentifier(HDShaderIDs._DBufferTexture[dbufferIndex]);
            }
        }

        public RenderTargetIdentifier[] GetDBuffers()
        {
            if (m_ColorMRTs == null || m_ColorMRTs.Length != dbufferCount)
                m_ColorMRTs = new RenderTargetIdentifier[dbufferCount];

            for (int index = 0; index < dbufferCount; index++)
            {
                m_ColorMRTs[index] = m_RTIDs[index];
            }

            return m_ColorMRTs;
        }

		public void ClearNormalTarget(Color clearColor, CommandBuffer cmd)
		{
			// index 1 is normals
			CoreUtils.SetRenderTarget(cmd, m_ColorMRTs[1], ClearFlag.Color, clearColor);
		}

        public void PushGlobalParams(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(HDShaderIDs._EnableDBuffer, vsibleDecalCount > 0 ? 1 : 0);
        }
    }


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

        public bool IsInternalDiffusionProfile(DiffusionProfileSettings profile)
        {
            return m_InternalSSSAsset == profile;
        }

        readonly RenderPipelineMaterial m_DeferredMaterial;
        readonly List<RenderPipelineMaterial> m_MaterialList = new List<RenderPipelineMaterial>();

        readonly GBufferManager m_GbufferManager = new GBufferManager();
        readonly DBufferManager m_DbufferManager = new DBufferManager();
        readonly SubsurfaceScatteringManager m_SSSBufferManager = new SubsurfaceScatteringManager();

        // Renderer Bake configuration can vary depends on if shadow mask is enabled or no
        RendererConfiguration m_currentRendererConfigurationBakedLighting = HDUtils.k_RendererConfigurationBakedLighting;
        Material m_CopyStencilForNoLighting;
        GPUCopy m_GPUCopy;

        IBLFilterGGX m_IBLFilterGGX = null;

        ComputeShader m_GaussianPyramidCS { get { return m_Asset.renderPipelineResources.gaussianPyramidCS; } }
        int m_GaussianPyramidKernel;
        ComputeShader m_DepthPyramidCS { get { return m_Asset.renderPipelineResources.depthPyramidCS; } }
        int m_DepthPyramidKernel;

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

        // Various buffer
        readonly int m_CameraColorBuffer;
        readonly int m_CameraSssDiffuseLightingBuffer;
        readonly int m_ShadowMaskBuffer;
        readonly int m_VelocityBuffer;
        readonly int m_DistortionBuffer;
        readonly int m_GaussianPyramidColorBuffer;
        readonly int m_DepthPyramidBuffer;

        readonly int m_DeferredShadowBuffer;

        // 'm_CameraColorBuffer' does not contain diffuse lighting of SSS materials until the SSS pass. It is stored within 'm_CameraSssDiffuseLightingBuffer'.
        readonly RenderTargetIdentifier m_CameraColorBufferRT;
        readonly RenderTargetIdentifier m_CameraSssDiffuseLightingBufferRT;
        readonly RenderTargetIdentifier m_VelocityBufferRT;
        readonly RenderTargetIdentifier m_DistortionBufferRT;
        readonly RenderTargetIdentifier m_GaussianPyramidColorBufferRT;
        readonly RenderTargetIdentifier m_DepthPyramidBufferRT;
        RenderTextureDescriptor m_GaussianPyramidColorBufferDesc;
        RenderTextureDescriptor m_DepthPyramidBufferDesc;

        readonly RenderTargetIdentifier m_DeferredShadowBufferRT;

        RenderTexture m_CameraDepthStencilBuffer;
        RenderTexture m_CameraDepthBufferCopy;
        RenderTexture m_CameraStencilBufferCopy;

        RenderTargetIdentifier m_CameraDepthStencilBufferRT;
        RenderTargetIdentifier m_CameraDepthBufferCopyRT;
        RenderTargetIdentifier m_CameraStencilBufferCopyRT;

        // The pass "SRPDefaultUnlit" is a fall back to legacy unlit rendering and is required to support unity 2d + unity UI that render in the scene.
        ShaderPassName[] m_ForwardAndForwardOnlyPassNames = { new ShaderPassName(), new ShaderPassName(), HDShaderPassNames.s_SRPDefaultUnlitName };
        ShaderPassName[] m_ForwardOnlyPassNames = { new ShaderPassName(), HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllTransparentPassNames = {  HDShaderPassNames.s_TransparentBackfaceName,
                                                        HDShaderPassNames.s_ForwardOnlyName,
                                                        HDShaderPassNames.s_ForwardName,
                                                        HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllTransparentDebugDisplayPassNames = {  HDShaderPassNames.s_TransparentBackfaceDebugDisplayName,
                                                                    HDShaderPassNames.s_ForwardOnlyDebugDisplayName,
                                                                    HDShaderPassNames.s_ForwardDebugDisplayName,
                                                                    HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllForwardDebugDisplayPassNames = {  HDShaderPassNames.s_TransparentBackfaceDebugDisplayName,
                                                                HDShaderPassNames.s_ForwardOnlyDebugDisplayName,
                                                                HDShaderPassNames.s_ForwardDebugDisplayName };

        ShaderPassName[] m_DepthOnlyAndDepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_DepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName };
        ShaderPassName[] m_DepthOnlyPassNames = { HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_TransparentDepthPrepassNames = { HDShaderPassNames.s_TransparentDepthPrepassName };
        ShaderPassName[] m_TransparentDepthPostpassNames = { HDShaderPassNames.s_TransparentDepthPostpassName };
        ShaderPassName[] m_ForwardErrorPassNames = { HDShaderPassNames.s_AlwaysName, HDShaderPassNames.s_ForwardBaseName, HDShaderPassNames.s_DeferredName, HDShaderPassNames.s_PrepassBaseName, HDShaderPassNames.s_VertexName, HDShaderPassNames.s_VertexLMRGBMName, HDShaderPassNames.s_VertexLMName };
        ShaderPassName[] m_SinglePassName = new ShaderPassName[1];

        RenderTargetIdentifier[] m_MRTCache2 = new RenderTargetIdentifier[2];

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

        FrameSettings m_FrameSettings; // Init every frame

        // For debug
        int m_DebugColorPickerRT;
        int m_DebugFullScreenTempRT;
        bool m_FullScreenDebugPushed;

        public HDRenderPipeline(HDRenderPipelineAsset asset)
        {
            SetRenderingFeatures();

            m_Asset = asset;
            m_GPUCopy = new GPUCopy(asset.renderPipelineResources.copyChannelCS);
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

            m_CameraColorBuffer = HDShaderIDs._CameraColorTexture;
            m_CameraColorBufferRT = new RenderTargetIdentifier(m_CameraColorBuffer);
            m_CameraSssDiffuseLightingBuffer = HDShaderIDs._CameraSssDiffuseLightingBuffer;
            m_CameraSssDiffuseLightingBufferRT = new RenderTargetIdentifier(m_CameraSssDiffuseLightingBuffer);

            m_SSSBufferManager.Build(asset);

            // Initialize various compute shader resources
            m_applyDistortionKernel = m_applyDistortionCS.FindKernel("KMain");

            // General material
            m_CopyStencilForNoLighting = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.copyStencilBuffer);
            m_CopyStencilForNoLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
            m_CopyStencilForNoLighting.SetInt(HDShaderIDs._StencilMask, (int)StencilBitMask.LightingMask);
            m_CameraMotionVectorsMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.cameraMotionVectors);

            InitializeDebugMaterials();

            m_VelocityBuffer = HDShaderIDs._VelocityTexture;
            m_VelocityBufferRT = new RenderTargetIdentifier(m_VelocityBuffer);

            m_DistortionBuffer = HDShaderIDs._DistortionTexture;
            m_DistortionBufferRT = new RenderTargetIdentifier(m_DistortionBuffer);

            m_GaussianPyramidKernel = m_GaussianPyramidCS.FindKernel("KMain");
            m_GaussianPyramidColorBuffer = HDShaderIDs._GaussianPyramidColorTexture;
            m_GaussianPyramidColorBufferRT = new RenderTargetIdentifier(m_GaussianPyramidColorBuffer);
            m_GaussianPyramidColorBufferDesc = new RenderTextureDescriptor(2, 2, RenderTextureFormat.ARGBHalf, 0)
            {
                useMipMap = true,
                autoGenerateMips = false
            };

            m_DepthPyramidKernel = m_DepthPyramidCS.FindKernel("KMain");
            m_DepthPyramidBuffer = HDShaderIDs._PyramidDepthTexture;
            m_DepthPyramidBufferRT = new RenderTargetIdentifier(m_DepthPyramidBuffer);
            m_DepthPyramidBufferDesc = new RenderTextureDescriptor(2, 2, RenderTextureFormat.RFloat, 0)
            {
                useMipMap = true,
                autoGenerateMips = false
            };

            m_DeferredShadowBuffer = HDShaderIDs._DeferredShadowTexture;
            m_DeferredShadowBufferRT = new RenderTargetIdentifier(m_DeferredShadowBuffer);

            m_MaterialList.ForEach(material => material.Build(asset));

            m_IBLFilterGGX = new IBLFilterGGX(asset.renderPipelineResources);

            m_LightLoop.Build(asset, m_ShadowSettings, m_IBLFilterGGX);

            m_SkyManager.Build(asset, m_IBLFilterGGX);

            m_VolumetricLightingModule.Build(asset);

            m_DebugDisplaySettings.RegisterDebug();
            FrameSettings.RegisterDebug("Default Camera", m_Asset.GetFrameSettings());

            m_DebugColorPickerRT = HDShaderIDs._DebugColorPickerTexture;
            m_DebugFullScreenTempRT = HDShaderIDs._DebugFullScreenTexture;

            // For debugging
            MousePositionDebug.instance.Build();

            InitializeRenderStateBlocks();
        }

        void SetRenderingFeatures()
        {
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

            m_LightLoop.Cleanup();

            // For debugging
            MousePositionDebug.instance.Cleanup();

            m_MaterialList.ForEach(material => material.Cleanup());

            CoreUtils.Destroy(m_CopyStencilForNoLighting);
            CoreUtils.Destroy(m_CameraMotionVectorsMaterial);

            CoreUtils.Destroy(m_DebugViewMaterialGBuffer);
            CoreUtils.Destroy(m_DebugViewMaterialGBufferShadowMask);
            CoreUtils.Destroy(m_DebugDisplayLatlong);
            CoreUtils.Destroy(m_DebugFullScreen);
            CoreUtils.Destroy(m_DebugColorPicker);
            CoreUtils.Destroy(m_Blit);
            CoreUtils.Destroy(m_ErrorMaterial);

            m_SSSBufferManager.Cleanup();
            m_SkyManager.Cleanup();
            m_VolumetricLightingModule.Cleanup();

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
        }

        void CreateDepthStencilBuffer(HDCamera hdCamera)
        {
            if (m_CameraDepthStencilBuffer != null)
                m_CameraDepthStencilBuffer.Release();

            m_CameraDepthStencilBuffer = CoreUtils.CreateRenderTexture(hdCamera.renderTextureDesc, 24, RenderTextureFormat.Depth);
            m_CameraDepthStencilBuffer.filterMode = FilterMode.Point;
            m_CameraDepthStencilBuffer.Create();
            m_CameraDepthStencilBufferRT = new RenderTargetIdentifier(m_CameraDepthStencilBuffer);

            if (NeedDepthBufferCopy())
            {
                if (m_CameraDepthBufferCopy != null)
                    m_CameraDepthBufferCopy.Release();

                m_CameraDepthBufferCopy = CoreUtils.CreateRenderTexture(hdCamera.renderTextureDesc, 24, RenderTextureFormat.Depth);
                m_CameraDepthBufferCopy.filterMode = FilterMode.Point;
                m_CameraDepthBufferCopy.Create();
                m_CameraDepthBufferCopyRT = new RenderTargetIdentifier(m_CameraDepthBufferCopy);
            }

            if (NeedStencilBufferCopy())
            {
                if (m_CameraStencilBufferCopy != null)
                    m_CameraStencilBufferCopy.Release();

                m_CameraStencilBufferCopy = CoreUtils.CreateRenderTexture(hdCamera.renderTextureDesc, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear); // DXGI_FORMAT_R8_UINT is not supported by Unity
                m_CameraStencilBufferCopy.filterMode = FilterMode.Point;
                m_CameraStencilBufferCopy.Create();
                m_CameraStencilBufferCopyRT = new RenderTargetIdentifier(m_CameraStencilBufferCopy);
            }
        }

        void Resize(HDCamera hdCamera)
        {
            // TODO: Detect if renderdoc just load and force a resize in this case, as often renderdoc require to realloc resource.

            // TODO: This is the wrong way to handle resize/allocation. We can have several different camera here, mean that the loop on camera will allocate and deallocate
            // the below buffer which is bad. Best is to have a set of buffer for each camera that is persistent and reallocate resource if need
            // For now consider we have only one camera that go to this code, the main one.
            var desc = hdCamera.renderTextureDesc;
            var texWidth = desc.width;
            var texHeight = desc.height;

            bool resolutionChanged = (texWidth != m_CurrentWidth) || (texHeight != m_CurrentHeight);

            if (resolutionChanged || m_CameraDepthStencilBuffer == null)
            {
                CreateDepthStencilBuffer(hdCamera);
                m_SSSBufferManager.Resize(hdCamera);
            }

            if (resolutionChanged || m_LightLoop.NeedResize())
            {
                if (m_CurrentWidth > 0 && m_CurrentHeight > 0)
                    m_LightLoop.ReleaseResolutionDependentBuffers();

                m_LightLoop.AllocResolutionDependentBuffers(texWidth, texHeight);
            }

            // Warning: (resolutionChanged == false) if you open a new Editor tab of the same size!
            m_VolumetricLightingModule.ResizeVBuffer(hdCamera, texWidth, texHeight);

            // update recorded window resolution
            m_CurrentWidth = texWidth;
            m_CurrentHeight = texHeight;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, DiffusionProfileSettings sssParameters)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", CustomSamplerId.PushGlobalParameters.GetSampler()))
            {
                hdCamera.SetupGlobalParams(cmd);

                m_SSSBufferManager.PushGlobalParams(cmd, sssParameters, m_FrameSettings);

                m_DbufferManager.PushGlobalParams(cmd);

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

        RenderTargetIdentifier GetDepthTexture()
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
                        cmd.CopyTexture(m_CameraDepthStencilBufferRT, m_CameraDepthBufferCopyRT);
                    }
                }
            }
        }

        public void UpdateShadowSettings()
        {
            var shadowSettings = VolumeManager.instance.stack.GetComponent<HDShadowSettings>();

            m_ShadowSettings.maxShadowDistance = shadowSettings.maxShadowDistance;
            //m_ShadowSettings.directionalLightNearPlaneOffset = commonSettings.shadowNearPlaneOffset;
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

                    // TODO: Float HDCamera setup higher in order to pass stereo into GetCullingParameters
                    ScriptableCullingParameters cullingParams;
                    if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    {
                        renderContext.Submit();
                        continue;
                    }

                    m_LightLoop.UpdateCullingParameters(ref cullingParams);

#if UNITY_EDITOR
                    // emit scene view UI
                    if (camera.cameraType == CameraType.SceneView)
                    {
                        ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                    }
#endif
                    // decal system needs to be updated with current camera
                    if (m_FrameSettings.enableDBuffer)
                        DecalSystem.instance.BeginCull(camera);

                    ReflectionSystem.PrepareCull(camera, m_ReflectionProbeCullResults);

                    using (new ProfilingSample(cmd, "CullResults.Cull", CustomSamplerId.CullResultsCull.GetSampler()))
                    {
                        CullResults.Cull(ref cullingParams, renderContext, ref m_CullResults);
                    }

                    m_ReflectionProbeCullResults.Cull();

                    m_DbufferManager.vsibleDecalCount = 0;
                    if (m_FrameSettings.enableDBuffer)
                    {
                        m_DbufferManager.vsibleDecalCount = DecalSystem.instance.QueryCullResults();
                        DecalSystem.instance.EndCull();
                    }

                    if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled())
                    {
                        m_CurrentDebugDisplaySettings.UpdateMaterials();
                    }

                    renderContext.SetupCameraProperties(camera);

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
                            CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.Color | ClearFlag.Depth);
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

                    InitAndClearBuffer(hdCamera, enableBakeShadowMask, cmd);

                    bool forcePrepassForDecals = m_DbufferManager.vsibleDecalCount > 0;
                    RenderDepthPrepass(m_CullResults, hdCamera, renderContext, cmd, forcePrepassForDecals);

                    RenderObjectsVelocity(m_CullResults, hdCamera, renderContext, cmd);

                    RenderDBuffer(hdCamera, renderContext, cmd);

                    RenderGBuffer(m_CullResults, hdCamera, renderContext, cmd);

                    // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
                    CopyDepthBufferIfNeeded(cmd);

                    RenderCameraVelocity(m_CullResults, hdCamera, renderContext, cmd);

                    // Depth texture is now ready, bind it.
                    cmd.SetGlobalTexture(HDShaderIDs._MainDepthTexture, GetDepthTexture());

                    // Caution: We require sun light here as some skies use the sun light to render, it means that UpdateSkyEnvironment must be called after PrepareLightsForGPU.
                    // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
                    UpdateSkyEnvironment(hdCamera, cmd);

                    RenderPyramidDepth(hdCamera, cmd, renderContext, FullScreenDebugMode.DepthPyramid);


                    if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled())
                    {
                        RenderDebugViewMaterial(m_CullResults, hdCamera, renderContext, cmd);

                        PushColorPickerDebugTexture(cmd, m_CameraColorBufferRT, hdCamera);
                    }
                    else
                    {
                        using (new ProfilingSample(cmd, "Render SSAO", CustomSamplerId.RenderSSAO.GetSampler()))
                        {
                            // TODO: Everything here (SSAO, Shadow, Build light list, deferred shadow, material and light classification can be parallelize with Async compute)
                            RenderSSAO(cmd, hdCamera, renderContext, postProcessLayer);
                        }

                        GPUFence buildGPULightListsCompleteFence = new GPUFence();
                        if (m_FrameSettings.enableAsyncCompute)
                        {
                            GPUFence startFence = cmd.CreateGPUFence();
                            renderContext.ExecuteCommandBuffer(cmd);
                            cmd.Clear();

                            buildGPULightListsCompleteFence = m_LightLoop.BuildGPULightListsAsyncBegin(hdCamera, renderContext, m_CameraDepthStencilBufferRT, m_CameraStencilBufferCopyRT, startFence, m_SkyManager.IsSkyValid());
                        }

                        using (new ProfilingSample(cmd, "Render shadows", CustomSamplerId.RenderShadows.GetSampler()))
                        {
                            m_LightLoop.RenderShadows(renderContext, cmd, m_CullResults);
                            // TODO: check if statement below still apply
                            renderContext.SetupCameraProperties(camera); // Need to recall SetupCameraProperties after RenderShadows as it modify our view/proj matrix
                        }

                        using (new ProfilingSample(cmd, "Deferred directional shadows", CustomSamplerId.RenderDeferredDirectionalShadow.GetSampler()))
                        {
                            cmd.ReleaseTemporaryRT(m_DeferredShadowBuffer);
                            CoreUtils.CreateCmdTemporaryRT(cmd, m_DeferredShadowBuffer, hdCamera.renderTextureDesc, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, true);
                            m_LightLoop.RenderDeferredDirectionalShadow(hdCamera, m_DeferredShadowBufferRT, GetDepthTexture(), cmd);
                            PushFullScreenDebugTexture(cmd, m_DeferredShadowBuffer, hdCamera, FullScreenDebugMode.DeferredShadows);
                        }

                        // TODO: Move this code inside LightLoop
                        if (m_LightLoop.GetFeatureVariantsEnabled())
                        {
                            // For material classification we use compute shader and so can't read into the stencil, so prepare it.
                            using (new ProfilingSample(cmd, "Clear and copy stencil texture", CustomSamplerId.ClearAndCopyStencilTexture.GetSampler()))
                            {
                                CoreUtils.SetRenderTarget(cmd, m_CameraStencilBufferCopyRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);

                                // In the material classification shader we will simply test is we are no lighting
                                // Use ShaderPassID 1 => "Pass 1 - Write 1 if value different from stencilRef to output"
                                CoreUtils.DrawFullScreen(cmd, m_CopyStencilForNoLighting, m_CameraStencilBufferCopyRT, m_CameraDepthStencilBufferRT, null, 1);
                            }
                        }

                        if (m_FrameSettings.enableAsyncCompute)
                        {
                            m_LightLoop.BuildGPULightListAsyncEnd(camera, cmd, buildGPULightListsCompleteFence);
                        }
                        else
                        {
                            using (new ProfilingSample(cmd, "Build Light list", CustomSamplerId.BuildLightList.GetSampler()))
                            {
                                m_LightLoop.BuildGPULightLists(hdCamera, cmd, m_CameraDepthStencilBufferRT, m_CameraStencilBufferCopyRT, m_SkyManager.IsSkyValid());
                            }
                        }

                        // Render the volumetric lighting.
                        // The pass requires the volume properties, the light list and the shadows, and can run async.
                        m_VolumetricLightingModule.VolumetricLightingPass(hdCamera, cmd, m_FrameSettings);

                        RenderDeferredLighting(hdCamera, cmd);

                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Opaque);
                        RenderForwardError(m_CullResults, camera, renderContext, cmd, ForwardPass.Opaque);

                        // SSS pass here handle both SSS material from deferred and forward
                        m_SSSBufferManager.SubsurfaceScatteringPass(hdCamera, cmd, diffusionProfileSettings, m_FrameSettings,
                                                                    m_CameraColorBufferRT, m_CameraSssDiffuseLightingBufferRT, m_CameraDepthStencilBufferRT, GetDepthTexture());

                        RenderSky(hdCamera, cmd);

                        // Render pre refraction objects
                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.PreRefraction);
                        RenderForwardError(m_CullResults, camera, renderContext, cmd, ForwardPass.PreRefraction);

                        RenderGaussianPyramidColor(hdCamera, cmd, renderContext, true);

                        // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Transparent);
                        RenderForwardError(m_CullResults, camera, renderContext, cmd, ForwardPass.Transparent);

                        // Fill depth buffer to reduce artifact for transparent object during postprocess
                        RenderTransparentDepthPostpass(m_CullResults, camera, renderContext, cmd, ForwardPass.Transparent);

                        RenderGaussianPyramidColor(hdCamera, cmd, renderContext, false);

                        AccumulateDistortion(m_CullResults, hdCamera, renderContext, cmd);
                        RenderDistortion(cmd, m_Asset.renderPipelineResources, hdCamera);

                        PushFullScreenDebugTexture(cmd, m_CameraColorBuffer, hdCamera, FullScreenDebugMode.NanTracker);
                        PushColorPickerDebugTexture(cmd, m_CameraColorBufferRT, hdCamera);

                        // The final pass either postprocess of Blit will flip the screen (as it is reverse by default due to Unity openGL legacy)
                        // Postprocess system (that doesn't use cmd.Blit) handle it with configuration (and do not flip in SceneView) or it is automatically done in Blit

                        // Final blit
                        if (m_FrameSettings.enablePostprocess && CoreUtils.IsPostProcessingActive(postProcessLayer))
                        {
                            RenderPostProcess(hdCamera, cmd, postProcessLayer);
                        }
                        else
                        {
                            using (new ProfilingSample(cmd, "Blit to final RT", CustomSamplerId.BlitToFinalRT.GetSampler()))
                            {
                                // This Blit will flip the screen anything other than openGL
                                // Simple blit
                                cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                            }
                        }
                    }

                    // Caution: RenderDebug need to take into account that we have flip the screen (so anything capture before the flip will be flipped)
                    RenderDebug(hdCamera, cmd);

                    // Make sure to unbind every render texture here because in the next iteration of the loop we might have to reallocate render texture (if the camera size is different)
                    cmd.SetRenderTarget(new RenderTargetIdentifier(-1), new RenderTargetIdentifier(-1));

#if UNITY_EDITOR
                    // We still need to bind correctly default camera target with our depth buffer in case we are currently rendering scene view. It should be the last camera here

                    // bind depth surface for editor grid/gizmo/selection rendering
                    if (camera.cameraType == CameraType.SceneView)
                        cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, m_CameraDepthStencilBufferRT);
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
                cmd.ReleaseTemporaryRT(m_DistortionBuffer);
                CoreUtils.CreateCmdTemporaryRT(cmd, m_DistortionBuffer, hdCamera.renderTextureDesc, 0, FilterMode.Point, Builtin.GetDistortionBufferFormat(), Builtin.GetDistortionBufferReadWrite());
                cmd.SetRenderTarget(m_DistortionBufferRT, m_CameraDepthStencilBufferRT);
                cmd.ClearRenderTarget(false, true, Color.clear);

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
                var size = new Vector4(hdCamera.screenSize.x, hdCamera.screenSize.y, 1f / hdCamera.screenSize.x, 1f / hdCamera.screenSize.y);
                uint x, y, z;
                m_applyDistortionCS.GetKernelThreadGroupSizes(m_applyDistortionKernel, out x, out y, out z);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._DistortionTexture, m_DistortionBufferRT);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._GaussianPyramidColorTexture, m_GaussianPyramidColorBufferRT);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._CameraColorTexture, m_CameraColorBufferRT);
                cmd.SetComputeVectorParam(m_applyDistortionCS, HDShaderIDs._Size, size);
                cmd.SetComputeVectorParam(m_applyDistortionCS, HDShaderIDs._ZBufferParams, Shader.GetGlobalVector(HDShaderIDs._ZBufferParams));
                cmd.SetComputeVectorParam(m_applyDistortionCS, HDShaderIDs._GaussianPyramidColorMipSize, Shader.GetGlobalVector(HDShaderIDs._GaussianPyramidColorMipSize));

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
                CoreUtils.SetRenderTarget(cmd, m_CameraDepthStencilBufferRT);
                if (forcePrepass || (addFullDepthPrepass && !addAlphaTestedOnly)) // Always true in case of forward rendering, use in case of deferred rendering if requesting a full depth prepass
                {
                    // We render first the opaque object as opaque alpha tested are more costly to render and could be reject by early-z (but not Hi-z as it is disable with clip instruction)
                    // This is handled automatically with the RenderQueue value (OpaqueAlphaTested have a different value and thus are sorted after Opaque)
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthOnlyAndDepthForwardOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque, m_DepthStateOpaque);
                }
                else // Deferred rendering with partial depth prepass
                {
                    // We always do a DepthForwardOnly pass with all the opaque (including alpha test)
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthForwardOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque, m_DepthStateOpaque);

                    // Render Alpha test only if requested
                    if (addAlphaTestedOnly)
                    {
                        var renderQueueRange = new RenderQueueRange { min = (int)RenderQueue.AlphaTest, max = (int)RenderQueue.GeometryLast - 1 };
                        RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthOnlyPassNames, 0, renderQueueRange, m_DepthStateOpaque);
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
        void RenderGBuffer(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (m_FrameSettings.enableForwardRenderingOnly)
                return;

            var camera = hdCamera.camera;

            using (new ProfilingSample(cmd, m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ? "GBufferDebugDisplay" : "GBuffer", CustomSamplerId.GBuffer.GetSampler()))
            {
                // setup GBuffer for rendering
                CoreUtils.SetRenderTarget(cmd, m_GbufferManager.GetGBuffers(), m_CameraDepthStencilBufferRT);

                // Render opaque objects into GBuffer
                if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled())
                {
                    // When doing debug display, the shader has the clip instruction regardless of the depth prepass so we can use regular depth test.
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferDebugDisplayName, m_currentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_AllOpaque, m_DepthStateOpaque);
                }
                else
                {
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
                }
            }
        }

        void RenderDBuffer(HDCamera camera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!m_FrameSettings.enableDBuffer)
                return;

            using (new ProfilingSample(cmd, "DBuffer", CustomSamplerId.DBuffer.GetSampler()))
            {
                // We need to copy depth buffer texture if we want to bind it at this stage
                CopyDepthBufferIfNeeded(cmd);

                // Depth texture is now ready, bind it.
                cmd.SetGlobalTexture(HDShaderIDs._MainDepthTexture, GetDepthTexture());

				// for alpha compositing, color is cleared to 0, alpha to 1
				// https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html

				Color clearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
				CoreUtils.SetRenderTarget(cmd, m_DbufferManager.GetDBuffers(), m_CameraDepthStencilBufferRT, ClearFlag.Color, clearColor);

				// we need to do a separate clear for normals, because they are cleared to a different color
				Color clearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
				m_DbufferManager.ClearNormalTarget(clearColorNormal, cmd);

				CoreUtils.SetRenderTarget(cmd, m_DbufferManager.GetDBuffers(), m_CameraDepthStencilBufferRT); // do not clear anymore
                DecalSystem.instance.Render(renderContext, camera, cmd);
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
                        CoreUtils.DrawFullScreen(cmd, m_currentDebugViewMaterialGBuffer, m_CameraColorBufferRT);
                    }
                }
                else
                {
                    CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.All, CoreUtils.clearColorAllBlack);
                    // Render Opaque forward
                    RenderOpaqueRenderList(cull, hdCamera.camera, renderContext, cmd, m_AllForwardDebugDisplayPassNames, m_currentRendererConfigurationBakedLighting);

                    // Render forward transparent
                    RenderTransparentRenderList(cull, hdCamera.camera, renderContext, cmd, m_AllForwardDebugDisplayPassNames, m_currentRendererConfigurationBakedLighting);
                }
            }

            // Last blit
            {
                using (new ProfilingSample(cmd, "Blit DebugView Material Debug", CustomSamplerId.BlitDebugViewMaterialDebug.GetSampler()))
                {
                    // This Blit will flip the screen anything other than openGL
                    cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
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
                    cmd.ReleaseTemporaryRT(HDShaderIDs._AmbientOcclusionTexture);
                    CoreUtils.CreateCmdTemporaryRT(cmd, HDShaderIDs._AmbientOcclusionTexture, hdCamera.renderTextureDesc, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.Linear, msaaSamples: 1, enableRandomWrite: true);
                    postProcessLayer.BakeMSVOMap(cmd, camera, HDShaderIDs._AmbientOcclusionTexture, GetDepthTexture(), true);
                    cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(settings.color.value.r, settings.color.value.g, settings.color.value.b, settings.directLightingStrength.value));
                    PushFullScreenDebugTexture(cmd, HDShaderIDs._AmbientOcclusionTexture, hdCamera, FullScreenDebugMode.SSAO);
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

            m_MRTCache2[0] = m_CameraColorBufferRT;
            m_MRTCache2[1] = m_CameraSssDiffuseLightingBufferRT;
            var depthTexture = GetDepthTexture();

            var options = new LightLoop.LightingPassOptions();

            if (m_FrameSettings.enableSubsurfaceScattering)
            {
                // Output split lighting for materials asking for it (masked in the stencil buffer)
                options.outputSplitLighting = true;

                m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_CameraDepthStencilBufferRT, depthTexture, options);
            }

            // Output combined lighting for all the other materials.
            options.outputSplitLighting = false;

            m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_CameraDepthStencilBufferRT, depthTexture, options);
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

            m_SkyManager.RenderSky(hdCamera, m_LightLoop.GetCurrentSunLight(), m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, cmd);

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
                        RenderTargetIdentifier[] m_MRTWithSSS = new RenderTargetIdentifier[2 + m_SSSBufferManager.sssBufferCount];
                        m_MRTWithSSS[0] = m_CameraColorBufferRT; // Store the specular color
                        m_MRTWithSSS[1] = m_CameraSssDiffuseLightingBufferRT;
                        for (int i = 0; i < m_SSSBufferManager.sssBufferCount; ++i)
                        {
                            m_MRTWithSSS[i + 2] = m_SSSBufferManager.GetSSSBuffers(i);
                        }

                        CoreUtils.SetRenderTarget(cmd, m_MRTWithSSS, m_CameraDepthStencilBufferRT);
                    }
                    else
                    {
                        CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);
                    }

                    if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled())
                    {
                        m_ForwardAndForwardOnlyPassNames[0] = m_ForwardOnlyPassNames[0] = HDShaderPassNames.s_ForwardOnlyDebugDisplayName;
                        m_ForwardAndForwardOnlyPassNames[1] = HDShaderPassNames.s_ForwardDebugDisplayName;
                    }
                    else
                    {
                        m_ForwardAndForwardOnlyPassNames[0] = m_ForwardOnlyPassNames[0] = HDShaderPassNames.s_ForwardOnlyName;
                        m_ForwardAndForwardOnlyPassNames[1] = HDShaderPassNames.s_ForwardName;
                    }

                    var passNames = m_FrameSettings.enableForwardRenderingOnly ? m_ForwardAndForwardOnlyPassNames : m_ForwardOnlyPassNames;
                    // Forward opaque material always have a prepass (whether or not we use deferred, whether or not there is option like alpha test only) so we pass the right depth state here.
                    RenderOpaqueRenderList(cullResults, camera, renderContext, cmd, passNames, m_currentRendererConfigurationBakedLighting, null, m_DepthStateOpaqueWithPrepass);
                }
                else
                {
                    CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);

                    var passNames = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ? m_AllTransparentDebugDisplayPassNames : m_AllTransparentPassNames;
                    RenderTransparentRenderList(cullResults, camera, renderContext, cmd, passNames, m_currentRendererConfigurationBakedLighting, pass == ForwardPass.PreRefraction ? HDRenderQueue.k_RenderQueue_PreRefraction : HDRenderQueue.k_RenderQueue_Transparent);
                }
            }
        }

        // This is use to Display legacy shader with an error shader
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void RenderForwardError(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
        {
            using (new ProfilingSample(cmd, "Render Forward Error", CustomSamplerId.RenderForwardError.GetSampler()))
            {
                CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);

                if (pass == ForwardPass.Opaque)
                {
                    RenderOpaqueRenderList(cullResults, camera, renderContext, cmd, m_ForwardErrorPassNames, 0, null, null, m_ErrorMaterial);
                }
                else
                {
                    RenderTransparentRenderList(cullResults, camera, renderContext, cmd, m_ForwardErrorPassNames, 0, pass == ForwardPass.PreRefraction ? HDRenderQueue.k_RenderQueue_PreRefraction : HDRenderQueue.k_RenderQueue_Transparent, null, m_ErrorMaterial);
                }
            }
        }

        void RenderTransparentDepthPostpass(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
        {
            if (!m_FrameSettings.enableTransparentPostpass)
                return;

            using (new ProfilingSample(cmd, "Render Transparent Depth Post ", CustomSamplerId.TransparentDepthPostpass.GetSampler()))
            {
                CoreUtils.SetRenderTarget(cmd, m_CameraDepthStencilBufferRT);
                RenderTransparentRenderList(cullResults, camera, renderContext, cmd, m_TransparentDepthPostpassNames);
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

                cmd.SetRenderTarget(m_VelocityBufferRT, m_CameraDepthStencilBufferRT);

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


                CoreUtils.DrawFullScreen(cmd, m_CameraMotionVectorsMaterial, m_VelocityBufferRT, m_CameraDepthStencilBufferRT, null, 0);

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
            {
                var colorPyramidDesc = m_GaussianPyramidColorBufferDesc;
                var pyramidSideSize = GetPyramidSize(colorPyramidDesc);

                // The Gaussian pyramid compute works in blocks of 8x8 so make sure the last lod has a
                // minimum size of 8x8
                int lodCount = Mathf.FloorToInt(Mathf.Log(pyramidSideSize, 2f) - 3f);
                if (lodCount > HDShaderIDs._GaussianPyramidColorMips.Length)
                {
                    Debug.LogWarningFormat("Cannot compute all mipmaps of the color pyramid, max texture size supported: {0}", (2 << HDShaderIDs._GaussianPyramidColorMips.Length).ToString());
                    lodCount = HDShaderIDs._GaussianPyramidColorMips.Length;
                }

                cmd.SetGlobalVector(HDShaderIDs._GaussianPyramidColorMipSize, new Vector4(pyramidSideSize, pyramidSideSize, lodCount, 0));

                cmd.SetGlobalTexture(HDShaderIDs._BlitTexture, m_CameraColorBufferRT);
                CoreUtils.DrawFullScreen(cmd, m_Blit, m_GaussianPyramidColorBufferRT, null, 1); // Bilinear filtering

                var last = m_GaussianPyramidColorBuffer;

                colorPyramidDesc.sRGB = false;
                colorPyramidDesc.enableRandomWrite = true;
                colorPyramidDesc.useMipMap = false;

                for (int i = 0; i < lodCount; i++)
                {
                    colorPyramidDesc.width = colorPyramidDesc.width >> 1;
                    colorPyramidDesc.height = colorPyramidDesc.height >> 1;

                    // TODO: Add proper stereo support to the compute job

                    cmd.ReleaseTemporaryRT(HDShaderIDs._GaussianPyramidColorMips[i + 1]);
                    cmd.GetTemporaryRT(HDShaderIDs._GaussianPyramidColorMips[i + 1], colorPyramidDesc, FilterMode.Bilinear);
                    cmd.SetComputeTextureParam(m_GaussianPyramidCS, m_GaussianPyramidKernel, "_Source", last);
                    cmd.SetComputeTextureParam(m_GaussianPyramidCS, m_GaussianPyramidKernel, "_Result", HDShaderIDs._GaussianPyramidColorMips[i + 1]);
                    cmd.SetComputeVectorParam(m_GaussianPyramidCS, "_Size", new Vector4(colorPyramidDesc.width, colorPyramidDesc.height, 1f / colorPyramidDesc.width, 1f / colorPyramidDesc.height));
                    cmd.DispatchCompute(m_GaussianPyramidCS, m_GaussianPyramidKernel, colorPyramidDesc.width / 8, colorPyramidDesc.height / 8, 1);
                    cmd.CopyTexture(HDShaderIDs._GaussianPyramidColorMips[i + 1], 0, 0, m_GaussianPyramidColorBufferRT, 0, i + 1);

                    last = HDShaderIDs._GaussianPyramidColorMips[i + 1];
                }

                PushFullScreenDebugTextureMip(cmd, m_GaussianPyramidColorBufferRT, lodCount, m_GaussianPyramidColorBufferDesc, hdCamera, isPreRefraction ? FullScreenDebugMode.PreRefractionColorPyramid : FullScreenDebugMode.FinalColorPyramid);

                cmd.SetGlobalTexture(HDShaderIDs._GaussianPyramidColorTexture, m_GaussianPyramidColorBuffer);

                for (int i = 0; i < lodCount; i++)
                    cmd.ReleaseTemporaryRT(HDShaderIDs._GaussianPyramidColorMips[i + 1]);

                // TODO: Why don't we use _GaussianPyramidColorMips[0]?
            }
        }

        void RenderPyramidDepth(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, FullScreenDebugMode debugMode)
        {
            if (!m_FrameSettings.enableRoughRefraction)
                return;

            using (new ProfilingSample(cmd, "Pyramid Depth", CustomSamplerId.PyramidDepth.GetSampler()))
            {
                var depthPyramidDesc = m_DepthPyramidBufferDesc;
                var pyramidSideSize = GetPyramidSize(depthPyramidDesc);

                // The gaussian pyramid compute works in blocks of 8x8 so make sure the last lod has a
                // minimum size of 8x8
                int lodCount = Mathf.FloorToInt(Mathf.Log(pyramidSideSize, 2f) - 3f);
                if (lodCount > HDShaderIDs._DepthPyramidMips.Length)
                {
                    Debug.LogWarningFormat("Cannot compute all mipmaps of the depth pyramid, max texture size supported: {0}", (2 << HDShaderIDs._DepthPyramidMips.Length).ToString());
                    lodCount = HDShaderIDs._DepthPyramidMips.Length;
                }

                cmd.SetGlobalVector(HDShaderIDs._DepthPyramidMipSize, new Vector4(pyramidSideSize, pyramidSideSize, lodCount, 0));

                cmd.ReleaseTemporaryRT(HDShaderIDs._DepthPyramidMips[0]);

                depthPyramidDesc.sRGB = false;
                depthPyramidDesc.enableRandomWrite = true;
                depthPyramidDesc.useMipMap = false;

                cmd.GetTemporaryRT(HDShaderIDs._DepthPyramidMips[0], depthPyramidDesc, FilterMode.Bilinear);
                m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, GetDepthTexture(), HDShaderIDs._DepthPyramidMips[0], new Vector2(depthPyramidDesc.width, depthPyramidDesc.height));
                cmd.CopyTexture(HDShaderIDs._DepthPyramidMips[0], 0, 0, m_DepthPyramidBufferRT, 0, 0);

                for (int i = 0; i < lodCount; i++)
                {
                    var srcMipWidth = depthPyramidDesc.width;
                    var srcMipHeight = depthPyramidDesc.height;
                    depthPyramidDesc.width = srcMipWidth >> 1;
                    depthPyramidDesc.height = srcMipHeight >> 1;

                    cmd.ReleaseTemporaryRT(HDShaderIDs._DepthPyramidMips[i + 1]);
                    cmd.GetTemporaryRT(HDShaderIDs._DepthPyramidMips[i + 1], depthPyramidDesc, FilterMode.Bilinear);

                    cmd.SetComputeTextureParam(m_DepthPyramidCS, m_DepthPyramidKernel, "_Source", HDShaderIDs._DepthPyramidMips[i]);
                    cmd.SetComputeTextureParam(m_DepthPyramidCS, m_DepthPyramidKernel, "_Result", HDShaderIDs._DepthPyramidMips[i + 1]);
                    cmd.SetComputeVectorParam(m_DepthPyramidCS, "_SrcSize", new Vector4(srcMipWidth, srcMipHeight, 1f / srcMipWidth, 1f / srcMipHeight));

                    cmd.DispatchCompute(m_DepthPyramidCS, m_DepthPyramidKernel, depthPyramidDesc.width / 8, depthPyramidDesc.height / 8, 1);

                    cmd.CopyTexture(HDShaderIDs._DepthPyramidMips[i + 1], 0, 0, m_DepthPyramidBufferRT, 0, i + 1);
                }

                PushFullScreenDebugDepthMip(cmd, m_DepthPyramidBufferRT, lodCount, m_DepthPyramidBufferDesc, hdCamera, debugMode);

                cmd.SetGlobalTexture(HDShaderIDs._PyramidDepthTexture, m_DepthPyramidBuffer);

                for (int i = 0; i < lodCount + 1; i++)
                    cmd.ReleaseTemporaryRT(HDShaderIDs._DepthPyramidMips[i]);
            }
        }

        void RenderPostProcess(HDCamera hdcamera, CommandBuffer cmd, PostProcessLayer layer)
        {
            using (new ProfilingSample(cmd, "Post-processing", CustomSamplerId.PostProcessing.GetSampler()))
            {
                // Note: Here we don't use GetDepthTexture() to get the depth texture but m_CameraDepthStencilBuffer as the Forward transparent pass can
                // write extra data to deal with DOF/MB
                cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_CameraDepthStencilBuffer);
                cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, m_VelocityBufferRT);

                var context = hdcamera.postprocessRenderContext;
                context.Reset();
                context.source = m_CameraColorBufferRT;
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
                m_ShadowSettings.enabled = m_FrameSettings.enableShadow;

                var lightingDebugSettings = m_CurrentDebugDisplaySettings.lightingDebugSettings;
                var debugAlbedo = new Vector4(lightingDebugSettings.debugLightingAlbedo.r, lightingDebugSettings.debugLightingAlbedo.g, lightingDebugSettings.debugLightingAlbedo.b, 0.0f);
                var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);

                cmd.SetGlobalInt(HDShaderIDs._DebugViewMaterial, (int)m_CurrentDebugDisplaySettings.GetDebugMaterialIndex());
                cmd.SetGlobalInt(HDShaderIDs._DebugLightingMode, (int)m_CurrentDebugDisplaySettings.GetDebugLightingMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugMipMapMode, (int)m_CurrentDebugDisplaySettings.GetDebugMipMapMode());
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingAlbedo, debugAlbedo);

                cmd.SetGlobalVector(HDShaderIDs._DebugLightingSmoothness, debugSmoothness);

                Vector2 mousePixelCoord = MousePositionDebug.instance.GetMousePosition(hdCamera.screenSize.y);
                var mouseParam = new Vector4(mousePixelCoord.x, mousePixelCoord.y, mousePixelCoord.x / hdCamera.screenSize.x, mousePixelCoord.y / hdCamera.screenSize.y);
                cmd.SetGlobalVector(HDShaderIDs._MousePixelCoord, mouseParam);

                cmd.SetGlobalTexture(HDShaderIDs._DebugFont, m_Asset.renderPipelineResources.debugFontTexture);
            }
        }

        // allowFlip is false if we call the function after the FinalPass or cmd.Blit that flip the screen
        public void PushColorPickerDebugTexture(CommandBuffer cmd, RenderTargetIdentifier textureID, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
            {
                cmd.ReleaseTemporaryRT(m_DebugColorPickerRT);
                CoreUtils.CreateCmdTemporaryRT(cmd, m_DebugColorPickerRT, hdCamera.renderTextureDesc, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                cmd.Blit(textureID, m_DebugColorPickerRT);
            }
        }

        public void PushFullScreenDebugTexture(CommandBuffer cmd, RenderTargetIdentifier textureID, HDCamera hdCamera, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed, when we render the result in RenderDebug the temporary RT will not exist.
                cmd.ReleaseTemporaryRT(m_DebugFullScreenTempRT);
                CoreUtils.CreateCmdTemporaryRT(cmd, m_DebugFullScreenTempRT, hdCamera.renderTextureDesc, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                cmd.Blit(textureID, m_DebugFullScreenTempRT);
            }
        }

        void PushFullScreenDebugTextureMip(CommandBuffer cmd, RenderTargetIdentifier textureID, int lodCount, RenderTextureDescriptor desc, HDCamera hdCamera, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.fullscreenDebugMip * (lodCount));

                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed, when we render the result in RenderDebug the temporary RT will not exist.
                cmd.ReleaseTemporaryRT(m_DebugFullScreenTempRT);

                desc.width = desc.width >> mipIndex;
                desc.height = desc.height >> mipIndex;
                CoreUtils.CreateCmdTemporaryRT(cmd, m_DebugFullScreenTempRT, desc, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

                cmd.CopyTexture(textureID, 0, mipIndex, m_DebugFullScreenTempRT, 0, 0); // TODO: Support tex arrays
            }
        }

        void PushFullScreenDebugDepthMip(CommandBuffer cmd, RenderTargetIdentifier textureID, int lodCount, RenderTextureDescriptor desc, HDCamera hdCamera, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.fullscreenDebugMip * (lodCount));

                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no fullscreen debug is pushed, when we render the result in RenderDebug the temporary RT will not exist.
                cmd.ReleaseTemporaryRT(m_DebugFullScreenTempRT);

                desc.width = desc.width >> mipIndex;
                desc.height = desc.height >> mipIndex;
                CoreUtils.CreateCmdTemporaryRT(cmd, m_DebugFullScreenTempRT, desc, 0, FilterMode.Point, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

                cmd.CopyTexture(textureID, 0, mipIndex, m_DebugFullScreenTempRT, 0, 0); // TODO: Support tex arrays
            }
        }

        public void PushFullScreenDebugTexture(CommandBuffer cb, int textureID, HDCamera hdCamera, FullScreenDebugMode debugMode)
        {
            PushFullScreenDebugTexture(cb, new RenderTargetIdentifier(textureID), hdCamera, debugMode);
        }

        void RenderDebug(HDCamera hdCamera, CommandBuffer cmd)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return;

            using (new ProfilingSample(cmd, "Render Debug", CustomSamplerId.RenderDebug.GetSampler()))
            {
                // We make sure the depth buffer is bound because we need it to write depth at near plane for overlays otherwise the editor grid end up visible in them.
                CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, m_CameraDepthStencilBufferRT);

                // First render full screen debug texture
                if (m_CurrentDebugDisplaySettings.fullScreenDebugMode != FullScreenDebugMode.None && m_FullScreenDebugPushed)
                {
                    m_FullScreenDebugPushed = false;
                    cmd.SetGlobalTexture(HDShaderIDs._DebugFullScreenTexture, m_DebugFullScreenTempRT);
                    // TODO: Replace with command buffer call when available
                    m_DebugFullScreen.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)m_CurrentDebugDisplaySettings.fullScreenDebugMode);
                    // Everything we have capture is flipped (as it happen before FinalPass/postprocess/Blit. So if we are not in SceneView
                    // (i.e. we have perform a flip, we need to flip the input texture)
                    m_DebugFullScreen.SetFloat(HDShaderIDs._RequireToFlipInputTexture, hdCamera.camera.cameraType != CameraType.SceneView ? 1.0f : 0.0f);
                    CoreUtils.DrawFullScreen(cmd, m_DebugFullScreen, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);

                    PushColorPickerDebugTexture(cmd, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget, hdCamera);
                }

                // Then overlays
                float x = 0;
                float overlayRatio = m_CurrentDebugDisplaySettings.debugOverlayRatio;
                float overlaySize = Math.Min(hdCamera.camera.pixelHeight, hdCamera.camera.pixelWidth) * overlayRatio;
                float y = hdCamera.camera.pixelHeight - overlaySize;

                var lightingDebug = m_CurrentDebugDisplaySettings.lightingDebugSettings;

                if (lightingDebug.displaySkyReflection)
                {
                    var skyReflection = m_SkyManager.skyReflection;
                    m_SharedPropertyBlock.SetTexture(HDShaderIDs._InputCubemap, skyReflection);
                    m_SharedPropertyBlock.SetFloat(HDShaderIDs._Mipmap, lightingDebug.skyReflectionMipmap);
                    cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                    cmd.DrawProcedural(Matrix4x4.identity, m_DebugDisplayLatlong, 0, MeshTopology.Triangles, 3, 1, m_SharedPropertyBlock);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.camera.pixelWidth);
                }

                m_LightLoop.RenderDebugOverlay(hdCamera, cmd, m_CurrentDebugDisplaySettings, ref x, ref y, overlaySize, hdCamera.camera.pixelWidth);

                if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
                {
                    ColorPickerDebugSettings colorPickerDebugSettings = m_CurrentDebugDisplaySettings.colorPickerDebugSettings;

                    // Here we have three cases:
                    // - Material debug is enabled, this is the buffer we display
                    // - Otherwise we display the HDR buffer before postprocess and distortion
                    // - If fullscreen debug is enabled we always use it

                    cmd.SetGlobalTexture(HDShaderIDs._DebugColorPickerTexture, m_DebugColorPickerRT); // No SetTexture with RenderTarget identifier... so use SetGlobalTexture
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
                    CoreUtils.DrawFullScreen(cmd, m_DebugColorPicker, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);
                }
            }
        }

        void InitAndClearBuffer(HDCamera hdCamera, bool enableBakeShadowMask, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "InitAndClearBuffer", CustomSamplerId.InitAndClearBuffer.GetSampler()))
            {
                // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.
                // Clear depth/stencil and init buffers
                using (new ProfilingSample(cmd, "InitGBuffers and clear Depth/Stencil", CustomSamplerId.InitGBuffersAndClearDepthStencil.GetSampler()))
                {
                    // Init buffer
                    // With scriptable render loop we must allocate ourself depth and color buffer (We must be independent of backbuffer for now, hope to fix that later).
                    // Also we manage ourself the HDR format, here allocating fp16 directly.
                    // With scriptable render loop we can allocate temporary RT in a command buffer, they will not be release with ExecuteCommandBuffer
                    // These temporary surface are release automatically at the end of the scriptable render pipeline if not release explicitly

                    cmd.ReleaseTemporaryRT(m_CameraColorBuffer);
                    cmd.ReleaseTemporaryRT(m_CameraSssDiffuseLightingBuffer);
                    CoreUtils.CreateCmdTemporaryRT(cmd, m_CameraColorBuffer,              hdCamera.renderTextureDesc, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf,       RenderTextureReadWrite.Linear, 1, true); // Enable UAV
                    CoreUtils.CreateCmdTemporaryRT(cmd, m_CameraSssDiffuseLightingBuffer, hdCamera.renderTextureDesc, 0, FilterMode.Point, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, 1, true); // Enable UAV


                    // Color and depth pyramids
                    m_GaussianPyramidColorBufferDesc = BuildPyramidDescriptor(hdCamera, PyramidType.Color, m_FrameSettings.enableStereo);
                    cmd.ReleaseTemporaryRT(m_GaussianPyramidColorBuffer);
                    cmd.GetTemporaryRT(m_GaussianPyramidColorBuffer, m_GaussianPyramidColorBufferDesc, FilterMode.Trilinear);

                    m_DepthPyramidBufferDesc = BuildPyramidDescriptor(hdCamera, PyramidType.Depth, m_FrameSettings.enableStereo);

                    cmd.ReleaseTemporaryRT(m_DepthPyramidBuffer);
                    cmd.GetTemporaryRT(m_DepthPyramidBuffer, m_DepthPyramidBufferDesc, FilterMode.Trilinear);
                    // End

                    if (!m_FrameSettings.enableForwardRenderingOnly)
                    {
                        m_GbufferManager.InitGBuffers(hdCamera.renderTextureDesc, m_DeferredMaterial, enableBakeShadowMask, cmd);
                        m_SSSBufferManager.InitSSSBuffersFromGBuffer(m_GbufferManager);
                    }
                    else
                    {
                        // We need to allocate target for SSS
                        m_SSSBufferManager.InitSSSBuffers(hdCamera.renderTextureDesc, cmd);
                    }

                    m_DbufferManager.InitDBuffers(hdCamera.renderTextureDesc, cmd);

                    if (m_FrameSettings.enableMotionVectors || m_FrameSettings.enableObjectMotionVectors)
                    {
                        // Note: We don't need to clear this buffer, it will be fill entirely by the rendering into the velocity buffer
                        cmd.ReleaseTemporaryRT(m_VelocityBuffer);
                        CoreUtils.CreateCmdTemporaryRT(cmd, m_VelocityBuffer, hdCamera.renderTextureDesc, 0, FilterMode.Point, Builtin.GetVelocityBufferFormat(), Builtin.GetVelocityBufferReadWrite());
                    }

                    CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.Depth);
                }

                // Clear the diffuse SSS lighting target
                using (new ProfilingSample(cmd, "Clear SSS diffuse target", CustomSamplerId.ClearSSSDiffuseTarget.GetSampler()))
                {
                    CoreUtils.SetRenderTarget(cmd, m_CameraSssDiffuseLightingBufferRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                }

                // TODO: As we are in development and have not all the setup pass we still clear the color in emissive buffer and gbuffer, but this will be removed later.

                // Clear the HDR target
                using (new ProfilingSample(cmd, "Clear HDR target", CustomSamplerId.ClearHDRTarget.GetSampler()))
                {
                    Color clearColor = hdCamera.camera.backgroundColor.linear; // Need it in linear because we clear a linear fp16 texture.
                    CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.Color, clearColor);
                }

                // Clear GBuffers
                if (!m_FrameSettings.enableForwardRenderingOnly)
                {
                    using (new ProfilingSample(cmd, "Clear GBuffer", CustomSamplerId.ClearGBuffer.GetSampler()))
                    {
                        CoreUtils.SetRenderTarget(cmd, m_GbufferManager.GetGBuffers(), m_CameraDepthStencilBufferRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }
                // END TEMP
            }
        }

        static int CalculatePyramidSize(int w, int h)
        {
            return Mathf.ClosestPowerOfTwo(Mathf.Min(w, h));
        }

        static int GetPyramidSize(RenderTextureDescriptor pyramidDesc)
        {
            // The monoscopic pyramid texture has both the width and height
            // matching, so the pyramid size could be either dimension.
            // However, with stereo double-wide rendering, we will arrange
            // two pyramid textures next to each other inside the double-wide
            // texture.  The whole texture width will no longer be representative
            // of the pyramid size, but the height still corresponds to the pyramid.
            return pyramidDesc.height;
        }

        public enum PyramidType
        {
            Color = 0,
            Depth = 1
        }

        static RenderTextureDescriptor BuildPyramidDescriptor(HDCamera hdCamera, PyramidType pyramidType, bool stereoEnabled)
        {
            var desc = hdCamera.renderTextureDesc;
            desc.colorFormat = (pyramidType == PyramidType.Color) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.RFloat;
            desc.depthBufferBits = 0;
            desc.useMipMap = true;
            desc.autoGenerateMips = false;

            var pyramidSize = CalculatePyramidSize((int)hdCamera.screenSize.x, (int)hdCamera.screenSize.y);

            // for stereo double-wide, each half of the texture will represent a single eye's pyramid
            //var widthModifier = 1;
            //if (stereoEnabled && (desc.dimension != TextureDimension.Tex2DArray))
            //    widthModifier = 2; // double-wide

            //desc.width = pyramidSize * widthModifier;
            desc.width = pyramidSize;
            desc.height = pyramidSize;

            return desc;
        }
    }
}
