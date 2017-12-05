using System.Collections.Generic;
using UnityEngine.Rendering;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class GBufferManager
    {
        public const int k_MaxGbuffer = 8;

        public int gbufferCount { get; set; }

        RenderTargetIdentifier[] m_ColorMRTs;
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[k_MaxGbuffer];

        public void InitGBuffers(int width, int height, RenderPipelineMaterial deferredMaterial, bool enableBakeShadowMask, CommandBuffer cmd)
        {
            // Init Gbuffer description
            gbufferCount = deferredMaterial.GetMaterialGBufferCount();
            RenderTextureFormat[] rtFormat;
            RenderTextureReadWrite[] rtReadWrite;
            deferredMaterial.GetMaterialGBufferDescription(out rtFormat, out rtReadWrite);

            for (int gbufferIndex = 0; gbufferIndex < gbufferCount; ++gbufferIndex)
            {
                cmd.ReleaseTemporaryRT(HDShaderIDs._GBufferTexture[gbufferIndex]);
                cmd.GetTemporaryRT(HDShaderIDs._GBufferTexture[gbufferIndex], width, height, 0, FilterMode.Point, rtFormat[gbufferIndex], rtReadWrite[gbufferIndex]);
                m_RTIDs[gbufferIndex] = new RenderTargetIdentifier(HDShaderIDs._GBufferTexture[gbufferIndex]);
            }

            if (enableBakeShadowMask)
            {
                cmd.ReleaseTemporaryRT(HDShaderIDs._ShadowMaskTexture);
                cmd.GetTemporaryRT(HDShaderIDs._ShadowMaskTexture, width, height, 0, FilterMode.Point, Builtin.GetShadowMaskBufferFormat(), Builtin.GetShadowMaskBufferReadWrite());
                m_RTIDs[gbufferCount++] = new RenderTargetIdentifier(HDShaderIDs._ShadowMaskTexture);
            }

            if (ShaderConfig.s_VelocityInGbuffer == 1)
            {
                // If velocity is in GBuffer then it is in the last RT. Assign a different name to it.
                cmd.ReleaseTemporaryRT(HDShaderIDs._VelocityTexture);
                cmd.GetTemporaryRT(HDShaderIDs._VelocityTexture, width, height, 0, FilterMode.Point, Builtin.GetVelocityBufferFormat(), Builtin.GetVelocityBufferReadWrite());
                m_RTIDs[gbufferCount++] = new RenderTargetIdentifier(HDShaderIDs._VelocityTexture);
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

    public partial class HDRenderPipeline : RenderPipeline
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

        static readonly RenderQueueRange k_RenderQueue_PreRefraction = new RenderQueueRange { min = (int)HDRenderQueue.PreRefraction, max = (int)HDRenderQueue.Transparent - 1 };
        static readonly RenderQueueRange k_RenderQueue_Transparent = new RenderQueueRange { min = (int)HDRenderQueue.Transparent, max = (int)HDRenderQueue.Overlay - 1};

        readonly HDRenderPipelineAsset m_Asset;

        readonly RenderPipelineMaterial m_DeferredMaterial;
        readonly List<RenderPipelineMaterial> m_MaterialList = new List<RenderPipelineMaterial>();

        readonly GBufferManager m_GbufferManager = new GBufferManager();

        // Renderer Bake configuration can vary depends on if shadow mask is enabled or no
        RendererConfiguration m_currentRendererConfigurationBakedLighting = HDUtils.k_RendererConfigurationBakedLighting;
        Material m_CopyStencilForSplitLighting;
        Material m_CopyStencilForRegularLighting;
        GPUCopy m_GPUCopy;

        IBLFilterGGX m_IBLFilterGGX = null;

        // Various set of material use in render loop
        ComputeShader m_SubsurfaceScatteringCS { get { return m_Asset.renderPipelineResources.subsurfaceScatteringCS; } }
        int m_SubsurfaceScatteringKernel;
        Material m_CombineLightingPass;
        // Old SSS Model >>>
        Material m_SssVerticalFilterPass;
        Material m_SssHorizontalFilterAndCombinePass;
        // <<< Old SSS Model

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
        Material m_ErrorMaterial;

        // Various buffer
        readonly int m_CameraColorBuffer;
        readonly int m_CameraSssDiffuseLightingBuffer;
        // Old SSS Model >>>
        readonly int m_CameraFilteringBuffer;
        // <<< Old SSS Model
        readonly int m_ShadowMaskBuffer;
        readonly int m_VelocityBuffer;
        readonly int m_DistortionBuffer;
        readonly int m_GaussianPyramidColorBuffer;
        readonly int m_DepthPyramidBuffer;

        readonly int m_DeferredShadowBuffer;

        // 'm_CameraColorBuffer' does not contain diffuse lighting of SSS materials until the SSS pass. It is stored within 'm_CameraSssDiffuseLightingBuffer'.
        readonly RenderTargetIdentifier m_CameraColorBufferRT;
        readonly RenderTargetIdentifier m_CameraSssDiffuseLightingBufferRT;
        // Old SSS Model >>>
        readonly RenderTargetIdentifier m_CameraFilteringBufferRT;
        // <<< Old SSS Model
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
        RenderTexture m_HTile;                   // If the hardware does not expose it, we compute our own, optimized to only contain the SSS bit

        RenderTargetIdentifier m_CameraDepthStencilBufferRT;
        RenderTargetIdentifier m_CameraDepthBufferCopyRT;
        RenderTargetIdentifier m_CameraStencilBufferCopyRT;
        RenderTargetIdentifier m_HTileRT;

        static CustomSampler[] m_samplers = new CustomSampler[(int)CustomSamplerId.Max];

        // The pass "SRPDefaultUnlit" is a fall back to legacy unlit rendering and is required to support unity 2d + unity UI that render in the scene.
        ShaderPassName[] m_ForwardAndForwardOnlyPassNames = { new ShaderPassName(), new ShaderPassName(), HDShaderPassNames.s_SRPDefaultUnlitName};
        ShaderPassName[] m_ForwardOnlyPassNames = { new ShaderPassName(), HDShaderPassNames.s_SRPDefaultUnlitName};

        ShaderPassName[] m_AllTransparentPassNames = {  HDShaderPassNames.s_TransparentBackfaceName,
                                                        HDShaderPassNames.s_ForwardOnlyName,
                                                        HDShaderPassNames.s_ForwardName,
                                                        HDShaderPassNames.s_TransparentDepthPostpassName,
                                                        HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllTransparentDebugDisplayPassNames = {  HDShaderPassNames.s_TransparentBackfaceDebugDisplayName,
                                                                    HDShaderPassNames.s_ForwardOnlyDebugDisplayName,
                                                                    HDShaderPassNames.s_ForwardDebugDisplayName,
                                                                    HDShaderPassNames.s_TransparentDepthPostpassName,
                                                                    HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllForwardDebugDisplayPassNames = {  HDShaderPassNames.s_TransparentBackfaceDebugDisplayName,
                                                                HDShaderPassNames.s_ForwardOnlyDebugDisplayName,
                                                                HDShaderPassNames.s_ForwardDebugDisplayName };

        ShaderPassName[] m_DepthOnlyAndDepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_DepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName };
        ShaderPassName[] m_DepthOnlyPassNames = { HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_TransparentDepthOnlyPassNames = { HDShaderPassNames.s_TransparentDepthPrepassName };
        ShaderPassName[] m_ForwardErrorPassNames = { HDShaderPassNames.s_AlwaysName, HDShaderPassNames.s_ForwardBaseName, HDShaderPassNames.s_DeferredName, HDShaderPassNames.s_PrepassBaseName, HDShaderPassNames.s_VertexName, HDShaderPassNames.s_VertexLMRGBMName, HDShaderPassNames.s_VertexLMName };
        ShaderPassName[] m_SinglePassName = new ShaderPassName[1];

        RenderTargetIdentifier[] m_MRTCache2 = new RenderTargetIdentifier[2];

        // Stencil usage in HDRenderPipeline.
        // Currently we use only 2 bits to identify the kind of lighting that is expected from the render pipeline
        // Usage is define in LightDefinitions.cs
        [Flags]
        public enum StencilBitMask
        {
            Clear    = 0,                    // 0x0
            Lighting = 3,                    // 0x3  - 2 bit
            All      = 255                   // 0xFF - 8 bit
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

        // Debugging
        MaterialPropertyBlock m_SharedPropertyBlock = new MaterialPropertyBlock();
        DebugDisplaySettings m_DebugDisplaySettings = new DebugDisplaySettings();
        static DebugDisplaySettings s_NeutralDebugDisplaySettings = new DebugDisplaySettings();
        DebugDisplaySettings m_CurrentDebugDisplaySettings;

        int m_DebugFullScreenTempRT;
        bool m_FullScreenDebugPushed;

        SubsurfaceScatteringSettings m_InternalSSSAsset;
        public SubsurfaceScatteringSettings sssSettings
        {
            get
            {
                // If no SSS asset is set, build / reuse an internal one for simplicity
                var asset = m_Asset.sssSettings;

                if (asset == null)
                {
                    if (m_InternalSSSAsset == null)
                        m_InternalSSSAsset = ScriptableObject.CreateInstance<SubsurfaceScatteringSettings>();

                    asset = m_InternalSSSAsset;
                }

                return asset;
            }
        }

        CommonSettings.Settings m_CommonSettings = CommonSettings.Settings.s_Defaultsettings;
        SkySettings m_SkySettings = null;

        static public CustomSampler   GetSampler(CustomSamplerId id)
        {
            return m_samplers[(int)id];
        }

        public CommonSettings.Settings commonSettingsToUse
        {
            get
            {
                if (CommonSettingsSingleton.overrideSettings)
                    return CommonSettingsSingleton.overrideSettings.settings;

                return m_CommonSettings;
            }
        }

        public SkySettings skySettingsToUse
        {
            get
            {
                if (SkySettingsSingleton.overrideSettings)
                    return SkySettingsSingleton.overrideSettings;

                return m_SkySettings;
            }
        }

        public HDRenderPipeline(HDRenderPipelineAsset asset)
        {
            m_Asset = asset;
            m_GPUCopy = new GPUCopy(asset.renderPipelineResources.copyChannelCS);

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

            m_CameraColorBuffer                = HDShaderIDs._CameraColorTexture;
            m_CameraColorBufferRT              = new RenderTargetIdentifier(m_CameraColorBuffer);
            m_CameraSssDiffuseLightingBuffer   = HDShaderIDs._CameraSssDiffuseLightingBuffer;
            m_CameraSssDiffuseLightingBufferRT = new RenderTargetIdentifier(m_CameraSssDiffuseLightingBuffer);
            m_CameraFilteringBuffer            = HDShaderIDs._CameraFilteringBuffer;
            m_CameraFilteringBufferRT          = new RenderTargetIdentifier(m_CameraFilteringBuffer);

            CreateSssMaterials();

            // Initialize various compute shader resources
            m_applyDistortionKernel = m_applyDistortionCS.FindKernel("KMain");

            m_CopyStencilForSplitLighting   = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/CopyStencilBuffer");
            m_CopyStencilForSplitLighting.EnableKeyword("EXPORT_HTILE");
            m_CopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.SplitLighting);
            m_CopyStencilForRegularLighting = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/CopyStencilBuffer");
            m_CopyStencilForRegularLighting.DisableKeyword("EXPORT_HTILE");
            m_CopyStencilForRegularLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.RegularLighting);
            m_CameraMotionVectorsMaterial   = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/CameraMotionVectors");

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
            m_DepthPyramidBuffer = HDShaderIDs._DepthPyramidTexture;
            m_DepthPyramidBufferRT = new RenderTargetIdentifier(m_DepthPyramidBuffer);
            m_DepthPyramidBufferDesc = new RenderTextureDescriptor(2, 2, RenderTextureFormat.RFloat, 0)
            {
                useMipMap = true,
                autoGenerateMips = false
            };

            m_DeferredShadowBuffer = HDShaderIDs._DeferredShadowTexture;
            m_DeferredShadowBufferRT = new RenderTargetIdentifier(m_DeferredShadowBuffer);

            m_MaterialList.ForEach(material => material.Build(asset.renderPipelineResources));

            m_IBLFilterGGX = new IBLFilterGGX(asset.renderPipelineResources);

            m_LightLoop.Build(asset.renderPipelineResources, asset.globalRenderingSettings, asset.tileSettings, asset.globalTextureSettings, asset.shadowInitParams, m_ShadowSettings, m_IBLFilterGGX);

            m_SkyManager.Build(asset.renderPipelineResources, m_IBLFilterGGX);
            m_SkyManager.skySettings = skySettingsToUse;

            m_DebugDisplaySettings.RegisterDebug();
            m_DebugFullScreenTempRT = HDShaderIDs._DebugFullScreenTexture;


            // Init all samplers
            for (int i = 0; i < (int)CustomSamplerId.Max; i++)
            {
                CustomSamplerId id = (CustomSamplerId)i;
                m_samplers[i] = CustomSampler.Create("C#_" + id.ToString());
            }

            InitializeRenderStateBlocks();

            RegisterDebug();
        }

        void RegisterDebug()
        {
            // These need to be Runtime Only because those values are held by the HDRenderPipeline asset so if user change them through the editor debug menu they might change the value in the asset without noticing it.
            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Forward Only", () => m_Asset.globalRenderingSettings.useForwardRenderingOnly, (value) => m_Asset.globalRenderingSettings.useForwardRenderingOnly = (bool)value, DebugItemFlag.RuntimeOnly);
            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Deferred Depth Prepass", () => m_Asset.globalRenderingSettings.useDepthPrepassWithDeferredRendering, (value) => m_Asset.globalRenderingSettings.useDepthPrepassWithDeferredRendering = (bool)value, DebugItemFlag.RuntimeOnly);
            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Deferred Depth Prepass ATest Only", () => m_Asset.globalRenderingSettings.renderAlphaTestOnlyInDeferredPrepass, (value) => m_Asset.globalRenderingSettings.renderAlphaTestOnlyInDeferredPrepass = (bool)value, DebugItemFlag.RuntimeOnly);

            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Enable Tile/Cluster", () => m_Asset.tileSettings.enableTileAndCluster, (value) => m_Asset.tileSettings.enableTileAndCluster = (bool)value, DebugItemFlag.RuntimeOnly);
            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Enable Big Tile", () => m_Asset.tileSettings.enableBigTilePrepass, (value) => m_Asset.tileSettings.enableBigTilePrepass = (bool)value, DebugItemFlag.RuntimeOnly);
            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Enable Compute Lighting", () => m_Asset.tileSettings.enableComputeLightEvaluation, (value) => m_Asset.tileSettings.enableComputeLightEvaluation = (bool)value, DebugItemFlag.RuntimeOnly);
            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Enable Light Classification", () => m_Asset.tileSettings.enableComputeLightVariants, (value) => m_Asset.tileSettings.enableComputeLightVariants = (bool)value, DebugItemFlag.RuntimeOnly);
            DebugMenuManager.instance.AddDebugItem<bool>("HDRP", "Enable Material Classification", () => m_Asset.tileSettings.enableComputeMaterialVariants, (value) => m_Asset.tileSettings.enableComputeMaterialVariants = (bool)value, DebugItemFlag.RuntimeOnly);
        }

        void InitializeDebugMaterials()
        {
            m_DebugViewMaterialGBuffer = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugViewMaterialGBufferShader);
            m_DebugViewMaterialGBufferShadowMask = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugViewMaterialGBufferShader);
            m_DebugViewMaterialGBufferShadowMask.EnableKeyword("SHADOWS_SHADOWMASK");
            m_DebugDisplayLatlong = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugDisplayLatlongShader);
            m_DebugFullScreen = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.debugFullScreenShader);
            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");
        }

        public void CreateSssMaterials()
        {
            m_SubsurfaceScatteringKernel = m_SubsurfaceScatteringCS.FindKernel("SubsurfaceScattering");

            CoreUtils.Destroy(m_CombineLightingPass);
            m_CombineLightingPass = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/CombineLighting");

            // Old SSS Model >>>
            CoreUtils.Destroy(m_SssVerticalFilterPass);
            m_SssVerticalFilterPass = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/SubsurfaceScattering");
            m_SssVerticalFilterPass.DisableKeyword("SSS_FILTER_HORIZONTAL_AND_COMBINE");
            m_SssVerticalFilterPass.SetFloat(HDShaderIDs._DstBlend, (float)BlendMode.Zero);

            CoreUtils.Destroy(m_SssHorizontalFilterAndCombinePass);
            m_SssHorizontalFilterAndCombinePass = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/SubsurfaceScattering");
            m_SssHorizontalFilterAndCombinePass.EnableKeyword("SSS_FILTER_HORIZONTAL_AND_COMBINE");
            m_SssHorizontalFilterAndCombinePass.SetFloat(HDShaderIDs._DstBlend, (float)BlendMode.One);
            // <<< Old SSS Model
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
            m_MaterialList.ForEach(material => material.Build(m_Asset.renderPipelineResources));
        }

        public override void Dispose()
        {
            base.Dispose();

            m_LightLoop.Cleanup();

            m_MaterialList.ForEach(material => material.Cleanup());

            CoreUtils.Destroy(m_DebugViewMaterialGBuffer);
            CoreUtils.Destroy(m_DebugViewMaterialGBufferShadowMask);
            CoreUtils.Destroy(m_DebugDisplayLatlong);
            CoreUtils.Destroy(m_DebugFullScreen);
            CoreUtils.Destroy(m_ErrorMaterial);
            CoreUtils.Destroy(m_InternalSSSAsset);

            m_SkyManager.Cleanup();

#if UNITY_EDITOR
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
#endif
        }

#if UNITY_EDITOR
        static readonly SupportedRenderingFeatures s_NeededFeatures = new SupportedRenderingFeatures()
        {
            reflectionProbeSupportFlags = SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation
        };
#endif

        void CreateDepthStencilBuffer(Camera camera)
        {
            if (m_CameraDepthStencilBuffer != null)
                m_CameraDepthStencilBuffer.Release();

            m_CameraDepthStencilBuffer = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.Depth);
            m_CameraDepthStencilBuffer.filterMode = FilterMode.Point;
            m_CameraDepthStencilBuffer.Create();
            m_CameraDepthStencilBufferRT = new RenderTargetIdentifier(m_CameraDepthStencilBuffer);

            if (NeedDepthBufferCopy())
            {
                if (m_CameraDepthBufferCopy != null)
                    m_CameraDepthBufferCopy.Release();

                m_CameraDepthBufferCopy = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.Depth);
                m_CameraDepthBufferCopy.filterMode = FilterMode.Point;
                m_CameraDepthBufferCopy.Create();
                m_CameraDepthBufferCopyRT = new RenderTargetIdentifier(m_CameraDepthBufferCopy);
            }

            if (NeedStencilBufferCopy())
            {
                if (m_CameraStencilBufferCopy != null)
                    m_CameraStencilBufferCopy.Release();

                m_CameraStencilBufferCopy = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear); // DXGI_FORMAT_R8_UINT is not supported by Unity
                m_CameraStencilBufferCopy.filterMode = FilterMode.Point;
                m_CameraStencilBufferCopy.Create();
                m_CameraStencilBufferCopyRT = new RenderTargetIdentifier(m_CameraStencilBufferCopy);
            }

            if (NeedHTileCopy())
            {
                if (m_HTile!= null)
                    m_HTile.Release();

                // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
                m_HTile = new RenderTexture((camera.pixelWidth + 7) / 8, (camera.pixelHeight + 7) / 8, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear); // DXGI_FORMAT_R8_UINT is not supported by Unity
                m_HTile.filterMode = FilterMode.Point;
                m_HTile.enableRandomWrite = true;
                m_HTile.Create();
                m_HTileRT = new RenderTargetIdentifier(m_HTile);
            }

        }

        void Resize(Camera camera)
        {
            // TODO: Detect if renderdoc just load and force a resize in this case, as often renderdoc require to realloc resource.

            // TODO: This is the wrong way to handle resize/allocation. We can have several different camera here, mean that the loop on camera will allocate and deallocate
            // the below buffer which is bad. Best is to have a set of buffer for each camera that is persistent and reallocate resource if need
            // For now consider we have only one camera that go to this code, the main one.
            m_SkyManager.skySettings = skySettingsToUse;
            m_SkyManager.Resize(camera.nearClipPlane, camera.farClipPlane); // TODO: Also a bad naming, here we just want to realloc texture if skyparameters change (useful for lookdev)

            bool resolutionChanged = camera.pixelWidth != m_CurrentWidth || camera.pixelHeight != m_CurrentHeight;

            if (resolutionChanged || m_CameraDepthStencilBuffer == null)
                CreateDepthStencilBuffer(camera);

            if (resolutionChanged || m_LightLoop.NeedResize())
            {
                if (m_CurrentWidth > 0 && m_CurrentHeight > 0)
                    m_LightLoop.ReleaseResolutionDependentBuffers();

                m_LightLoop.AllocResolutionDependentBuffers(camera.pixelWidth, camera.pixelHeight);
            }

            // update recorded window resolution
            m_CurrentWidth = camera.pixelWidth;
            m_CurrentHeight = camera.pixelHeight;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, SubsurfaceScatteringSettings sssParameters)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", GetSampler(CustomSamplerId.PushGlobalParameters)))
            {
                hdCamera.SetupGlobalParams(cmd);

                if (m_SkyManager.IsSkyValid())
                {
                    m_SkyManager.SetGlobalSkyTexture(cmd);
                    cmd.SetGlobalInt(HDShaderIDs._EnvLightSkyEnabled, 1);
                }
                else
                {
                    cmd.SetGlobalInt(HDShaderIDs._EnvLightSkyEnabled, 0);
                }

                // Broadcast SSS parameters to all shaders.
                cmd.SetGlobalInt(HDShaderIDs._EnableSSSAndTransmission, m_CurrentDebugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission ? 1 : 0);
                cmd.SetGlobalInt(HDShaderIDs._UseDisneySSS, sssParameters.useDisneySSS ? 1 : 0);
                unsafe
                {
                    // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                    // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                    uint texturingModeFlags = sssParameters.texturingModeFlags;
                    uint transmissionFlags  = sssParameters.transmissionFlags;
                    cmd.SetGlobalFloat(HDShaderIDs._TexturingModeFlags, *(float*)&texturingModeFlags);
                    cmd.SetGlobalFloat(HDShaderIDs._TransmissionFlags,  *(float*)&transmissionFlags);
                }
                cmd.SetGlobalVectorArray(HDShaderIDs._ThicknessRemaps,            sssParameters.thicknessRemaps);
                cmd.SetGlobalVectorArray(HDShaderIDs._ShapeParams,                sssParameters.shapeParams);
                cmd.SetGlobalVectorArray(HDShaderIDs._HalfRcpVariancesAndWeights, sssParameters.halfRcpVariancesAndWeights);
                cmd.SetGlobalVectorArray(HDShaderIDs._TransmissionTints,          sssParameters.transmissionTints);
            }
        }

        bool NeedDepthBufferCopy()
        {
            // For now we consider only PS4 to be able to read from a bound depth buffer.
            // TODO: test/implement for other platforms.
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4;
        }

        bool NeedStencilBufferCopy()
        {
            // Currently, Unity does not offer a way to bind the stencil buffer as a texture in a compute shader.
            // Therefore, it's manually copied using a pixel shader.
            return m_CurrentDebugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission || m_LightLoop.GetFeatureVariantsEnabled();
        }

        bool NeedHTileCopy()
        {
            // Currently, Unity does not offer a way to access the GCN HTile even on PS4 and Xbox One.
            // Therefore, it's computed in a pixel shader, and optimized to only contain the SSS bit.
            return m_CurrentDebugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission && sssSettings.useDisneySSS;
        }

        bool NeedTemporarySubsurfaceBuffer()
        {
            // Typed UAV loads from FORMAT_R16G16B16A16_FLOAT is an optional feature of Direct3D 11.
            // Most modern GPUs support it. We can avoid performing a costly copy in this case.
            // TODO: test/implement for other platforms.
            return m_CurrentDebugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission && (!sssSettings.useDisneySSS || (
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4 &&
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOne &&
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOneD3D12));
        }

        RenderTargetIdentifier GetDepthTexture()
        {
            return NeedDepthBufferCopy() ? m_CameraDepthBufferCopy : m_CameraDepthStencilBuffer;
        }

        RenderTargetIdentifier GetStencilTexture()
        {
            return NeedStencilBufferCopy() ? m_CameraStencilBufferCopyRT : m_CameraDepthStencilBufferRT;
        }

        RenderTargetIdentifier GetHTile()
        {
            // Currently, Unity does not offer a way to access the GCN HTile.
            return m_HTileRT;
        }

        void CopyDepthBufferIfNeeded(CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, NeedDepthBufferCopy() ? "Copy DepthBuffer" : "Set DepthBuffer", GetSampler(CustomSamplerId.CopySetDepthBuffer)))
            {
                if (NeedDepthBufferCopy())
                {
                    using (new ProfilingSample(cmd, "Copy depth-stencil buffer", GetSampler(CustomSamplerId.CopyDepthStencilbuffer)))
                    {
                        cmd.CopyTexture(m_CameraDepthStencilBufferRT, m_CameraDepthBufferCopyRT);
                    }
                }

                cmd.SetGlobalTexture(HDShaderIDs._MainDepthTexture, GetDepthTexture());
            }
        }

        void PrepareAndBindStencilTexture(CommandBuffer cmd)
        {
            if (NeedStencilBufferCopy())
            {
                using (new ProfilingSample(cmd, "Copy StencilBuffer", GetSampler(CustomSamplerId.CopyStencilBuffer)))
                {
                    cmd.SetRandomWriteTarget(1, GetHTile());
                    // Our method of exporting the stencil requires one pass per unique stencil value.
                    CoreUtils.DrawFullScreen(cmd, m_CopyStencilForSplitLighting,   m_CameraStencilBufferCopyRT, m_CameraDepthStencilBufferRT);
                    CoreUtils.DrawFullScreen(cmd, m_CopyStencilForRegularLighting, m_CameraStencilBufferCopyRT, m_CameraDepthStencilBufferRT);
                    cmd.ClearRandomWriteTargets();
                }
            }

            cmd.SetGlobalTexture(HDShaderIDs._HTile, GetHTile());
            cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, GetStencilTexture());
        }

        public void UpdateCommonSettings()
        {
            var commonSettings = commonSettingsToUse;

            m_ShadowSettings.maxShadowDistance = commonSettings.shadowMaxDistance;
            m_ShadowSettings.directionalLightNearPlaneOffset = commonSettings.shadowNearPlaneOffset;
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
        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            base.Render(renderContext, cameras);

#if UNITY_EDITOR
            SupportedRenderingFeatures.active = s_NeededFeatures;
#endif
            // HD use specific GraphicsSettings. This is init here.
            // TODO: This should not be set at each Frame but is there another place for these config setup ?
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;

            if (m_FrameCount != Time.frameCount)
            {
                HDCamera.CleanUnused();
                m_FrameCount = Time.frameCount;
            }

            foreach (var camera in cameras)
            {
            // This is the main command buffer used for the frame.
            var cmd = CommandBufferPool.Get("");

            using (new ProfilingSample(cmd, "HDRenderPipeline::Render", GetSampler(CustomSamplerId.HDRenderPipelineRender)))
            {


            foreach (var material in m_MaterialList)
                material.RenderInit(cmd);


            // Do anything we need to do upon a new frame.
            m_LightLoop.NewFrame();

            if (camera == null)
            {
                renderContext.Submit();
                continue;
            }

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

                using (new ProfilingSample(cmd, "Volume Update", GetSampler(CustomSamplerId.VolumeUpdate)))
                {
                    // TODO: Transform & layer should be configurable per camera
                    VolumeManager.instance.Update(camera.transform, -1);
                }
            }

            ApplyDebugDisplaySettings(cmd);
            UpdateCommonSettings();

            if (!m_IBLFilterGGX.IsInitialized())
                m_IBLFilterGGX.Initialize(cmd);

            ScriptableCullingParameters cullingParams;
            if (!CullResults.GetCullingParameters(camera, out cullingParams))
            {
                renderContext.Submit();
                continue;
            }

            m_LightLoop.UpdateCullingParameters( ref cullingParams );

#if UNITY_EDITOR
            // emit scene view UI
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            using (new ProfilingSample(cmd, "CullResults.Cull", GetSampler(CustomSamplerId.CullResultsCull)))
            {
                CullResults.Cull(ref cullingParams, renderContext,ref m_CullResults);
            }

            Resize(camera);

            renderContext.SetupCameraProperties(camera);

            var postProcessLayer = camera.GetComponent<PostProcessLayer>();
            var hdCamera = HDCamera.Get(camera, postProcessLayer);
            PushGlobalParams(hdCamera, cmd, sssSettings);

            // TODO: Find a correct place to bind these material textures
            // We have to bind the material specific global parameters in this mode
            m_MaterialList.ForEach(material => material.Bind());

            var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
            if (additionalCameraData && additionalCameraData.renderingPath == RenderingPathHDRP.Unlit)
            {
                // TODO: Add another path dedicated to planar reflection / real time cubemap that implement simpler lighting
                // It is up to the users to only send unlit object for this camera path

                using (new ProfilingSample(cmd, "Forward", GetSampler(CustomSamplerId.Forward)))
                {
                    CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.Color | ClearFlag.Depth);
                    RenderOpaqueRenderList(m_CullResults, camera, renderContext, cmd, HDShaderPassNames.s_ForwardName);
                        RenderTransparentRenderList(m_CullResults, camera, renderContext, cmd, HDShaderPassNames.s_ForwardName, false);
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
            using (new ProfilingSample(cmd, "TP_PrepareLightsForGPU", GetSampler(CustomSamplerId.TPPrepareLightsForGPU)))
            {
                enableBakeShadowMask = m_LightLoop.PrepareLightsForGPU(cmd, m_ShadowSettings, m_CullResults, camera);
            }
            ConfigureForShadowMask(enableBakeShadowMask, cmd);

            InitAndClearBuffer(hdCamera, enableBakeShadowMask, cmd);

            RenderDepthPrepass(m_CullResults, camera, renderContext, cmd);

            RenderGBuffer(m_CullResults, camera, renderContext, cmd);

            // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
            CopyDepthBufferIfNeeded(cmd);

            RenderPyramidDepth(camera, cmd, renderContext, FullScreenDebugMode.DepthPyramid);

            // Required for the SSS and the shader feature classification pass.
            PrepareAndBindStencilTexture(cmd);

            if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled())
            {
                RenderDebugViewMaterial(m_CullResults, hdCamera, renderContext, cmd);
            }
            else
            {
                using (new ProfilingSample(cmd, "Render SSAO", GetSampler(CustomSamplerId.RenderSSAO)))
                {
                    // TODO: Everything here (SSAO, Shadow, Build light list, deferred shadow, material and light classification can be parallelize with Async compute)
                    RenderSSAO(cmd, camera, renderContext, postProcessLayer);
                }

                using (new ProfilingSample(cmd, "Render shadows", GetSampler(CustomSamplerId.RenderShadows)))
                    {
                    m_LightLoop.RenderShadows(renderContext, cmd, m_CullResults);
                    // TODO: check if statement below still apply
                    renderContext.SetupCameraProperties(camera); // Need to recall SetupCameraProperties after RenderShadows as it modify our view/proj matrix
                }

                using (new ProfilingSample(cmd, "Deferred directional shadows", GetSampler(CustomSamplerId.RenderDeferredDirectionalShadow)))
                {
                    cmd.ReleaseTemporaryRT(m_DeferredShadowBuffer);
                    cmd.GetTemporaryRT(m_DeferredShadowBuffer, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1 , true);
                    m_LightLoop.RenderDeferredDirectionalShadow(hdCamera, m_DeferredShadowBufferRT, GetDepthTexture(), cmd);
                    PushFullScreenDebugTexture(cmd, m_DeferredShadowBuffer, hdCamera.camera, renderContext, FullScreenDebugMode.DeferredShadows);
                }

                using (new ProfilingSample(cmd, "Build Light list", GetSampler(CustomSamplerId.BuildLightList)))
                {
                    m_LightLoop.BuildGPULightLists(camera, cmd, m_CameraDepthStencilBufferRT, GetStencilTexture());
                }

                    // Caution: We require sun light here as some sky use the sun light to render, mean UpdateSkyEnvironment
                    // must be call after BuildGPULightLists.
                    // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
                    UpdateSkyEnvironment(hdCamera, cmd);

                RenderDeferredLighting(hdCamera, cmd);

                // We compute subsurface scattering here. Therefore, no objects rendered afterwards will exhibit SSS.
                // Currently, there is no efficient way to switch between SRT and MRT for the forward pass;
                // therefore, forward-rendered objects do not output split lighting required for the SSS pass.
                SubsurfaceScatteringPass(hdCamera, cmd, sssSettings);

                RenderForward(m_CullResults, camera, renderContext, cmd, ForwardPass.Opaque);
                RenderForwardError(m_CullResults, camera, renderContext, cmd, ForwardPass.Opaque);

                RenderSky(hdCamera, cmd);

                // Render pre refraction objects
                RenderForward(m_CullResults, camera, renderContext, cmd, ForwardPass.PreRefraction);
                RenderForwardError(m_CullResults, camera, renderContext, cmd, ForwardPass.PreRefraction);

                RenderGaussianPyramidColor(camera, cmd, renderContext, FullScreenDebugMode.PreRefractionColorPyramid);

                // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
                RenderForward(m_CullResults, camera, renderContext, cmd, ForwardPass.Transparent);
                RenderForwardError(m_CullResults, camera, renderContext, cmd, ForwardPass.Transparent);

                PushFullScreenDebugTexture(cmd, m_CameraColorBuffer, camera, renderContext, FullScreenDebugMode.NanTracker);

                // Planar and real time cubemap doesn't need post process and render in FP16
                if (camera.cameraType == CameraType.Reflection)
                {
                    using (new ProfilingSample(cmd, "Blit to final RT", GetSampler(CustomSamplerId.BlitToFinalRT)))
                    {
                        // Simple blit
                        cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                    }
                }
                else
                {
                    RenderVelocity(m_CullResults, hdCamera, renderContext, cmd); // Note we may have to render velocity earlier if we do temporalAO, temporal volumetric etc... Mean we will not take into account forward opaque in case of deferred rendering ?

                    RenderGaussianPyramidColor(camera, cmd, renderContext, FullScreenDebugMode.FinalColorPyramid);

                    // TODO: Check with VFX team.
                    // Rendering distortion here have off course lot of artifact.
                    // But resolving at each objects that write in distortion is not possible (need to sort transparent, render those that do not distort, then resolve, then etc...)
                    // Instead we chose to apply distortion at the end after we cumulate distortion vector and desired blurriness.
                    AccumulateDistortion(m_CullResults, camera, renderContext, cmd);
                    RenderDistortion(cmd, m_Asset.renderPipelineResources);

                    RenderPostProcesses(hdCamera, cmd, postProcessLayer);
                }
            }

            RenderDebug(hdCamera, cmd);

            // Make sure to unbind every render texture here because in the next iteration of the loop we might have to reallocate render texture (if the camera size is different)
            cmd.SetRenderTarget(new RenderTargetIdentifier(-1), new RenderTargetIdentifier(-1));

#if UNITY_EDITOR
            // We still need to bind correctly default camera target with our depth buffer in case we are currently rendering scene view. It should be the last camera here

            // bind depth surface for editor grid/gizmo/selection rendering
            if (camera.cameraType == CameraType.SceneView)
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, m_CameraDepthStencilBufferRT);
#endif

            renderContext.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
            renderContext.Submit();
            } // For each camera
        }

        void RenderOpaqueRenderList(CullResults             cull,
                                    Camera                  camera,
                                    ScriptableRenderContext renderContext,
                                    CommandBuffer           cmd,
                                    ShaderPassName          passName,
                                    RendererConfiguration   rendererConfiguration = 0,
                                    RenderQueueRange?       inRenderQueueRange = null,
                                    RenderStateBlock?       stateBlock = null,
                                    Material                overrideMaterial = null)
        {
            m_SinglePassName[0] = passName;
            RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_SinglePassName, rendererConfiguration, inRenderQueueRange, stateBlock, overrideMaterial);
        }

        void RenderOpaqueRenderList(CullResults             cull,
                                    Camera                  camera,
                                    ScriptableRenderContext renderContext,
                                    CommandBuffer           cmd,
                                    ShaderPassName[]        passNames,
                                    RendererConfiguration   rendererConfiguration = 0,
                                    RenderQueueRange?       inRenderQueueRange = null,
                                    RenderStateBlock?       stateBlock = null,
                                    Material                overrideMaterial = null)
        {
            if (!m_CurrentDebugDisplaySettings.renderingDebugSettings.displayOpaqueObjects)
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
                renderQueueRange = inRenderQueueRange == null
                    ? RenderQueueRange.opaque
                    : inRenderQueueRange.Value
            };

            if(stateBlock == null)
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
            else
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings, stateBlock.Value);
        }

        void RenderTransparentRenderList(CullResults             cull,
                                         Camera                  camera,
                                         ScriptableRenderContext renderContext,
                                         CommandBuffer           cmd,
                                         ShaderPassName          passName,
                                         bool                    preRefractionQueue,
                                         RendererConfiguration   rendererConfiguration = 0,
                                         RenderStateBlock?       stateBlock = null,
                                         Material                overrideMaterial = null)
        {
            m_SinglePassName[0] = passName;
            RenderTransparentRenderList(cull, camera, renderContext, cmd, m_SinglePassName, preRefractionQueue,
                                        rendererConfiguration, stateBlock, overrideMaterial);
        }

        void RenderTransparentRenderList(CullResults             cull,
                                         Camera                  camera,
                                         ScriptableRenderContext renderContext,
                                         CommandBuffer           cmd,
                                         ShaderPassName[]        passNames,
                                         bool                    preRefractionQueue,
                                         RendererConfiguration   rendererConfiguration = 0,
                                         RenderStateBlock?       stateBlock = null,
                                         Material                overrideMaterial = null
                                         )
        {
            if (!m_CurrentDebugDisplaySettings.renderingDebugSettings.displayTransparentObjects)
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
                renderQueueRange = preRefractionQueue
                    ? k_RenderQueue_PreRefraction
                    : k_RenderQueue_Transparent
            };

            if(stateBlock == null)
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
            else
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings, stateBlock.Value);
        }

        void AccumulateDistortion(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!m_CurrentDebugDisplaySettings.renderingDebugSettings.enableDistortion)
                return;

            using (new ProfilingSample(cmd, "Distortion", GetSampler(CustomSamplerId.Distortion)))
            {
                int w = camera.pixelWidth;
                int h = camera.pixelHeight;

                cmd.ReleaseTemporaryRT(m_DistortionBuffer);
                cmd.GetTemporaryRT(m_DistortionBuffer, w, h, 0, FilterMode.Point, Builtin.GetDistortionBufferFormat(), Builtin.GetDistortionBufferReadWrite());
                cmd.SetRenderTarget(m_DistortionBufferRT, m_CameraDepthStencilBufferRT);
                cmd.ClearRenderTarget(false, true, Color.clear);

                // Only transparent object can render distortion vectors
                RenderTransparentRenderList(cullResults, camera, renderContext, cmd, HDShaderPassNames.s_DistortionVectorsName, true);
                RenderTransparentRenderList(cullResults, camera, renderContext, cmd, HDShaderPassNames.s_DistortionVectorsName, false);
            }
        }

        void RenderDistortion(CommandBuffer cmd, RenderPipelineResources resources)
        {
            using (new ProfilingSample(cmd, "ApplyDistortion", GetSampler(CustomSamplerId.ApplyDistortion)))
            {
                var size = new Vector4(m_CurrentWidth, m_CurrentHeight, 1f / m_CurrentWidth, 1f / m_CurrentHeight);
                uint x, y, z;
                m_applyDistortionCS.GetKernelThreadGroupSizes(m_applyDistortionKernel, out x, out y, out z);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._DistortionTexture, m_DistortionBufferRT);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._GaussianPyramidColorTexture, m_GaussianPyramidColorBufferRT);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._CameraColorTexture, m_CameraColorBufferRT);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._DepthTexture, GetDepthTexture());
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
        void RenderDepthPrepass(CullResults cull, Camera camera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // In case of deferred renderer, we can have forward opaque material. These materials need to be render in the depth buffer to correctly build the light list.
            // And they will tag the stencil to not be lit during the deferred lighting pass.

            // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
            // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
            // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.

            // In case of forward only rendering we have a depth prepass. In case of deferred renderer, it is optional
            bool addFullDepthPrepass = m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly() || m_Asset.globalRenderingSettings.useDepthPrepassWithDeferredRendering;
            bool addAlphaTestedOnly = !m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly() && m_Asset.globalRenderingSettings.useDepthPrepassWithDeferredRendering && m_Asset.globalRenderingSettings.renderAlphaTestOnlyInDeferredPrepass;

            using (new ProfilingSample(cmd, addAlphaTestedOnly ? "Depth Prepass alpha test" : "Depth Prepass", GetSampler(CustomSamplerId.DepthPrepass)))
            {
                CoreUtils.SetRenderTarget(cmd, m_CameraDepthStencilBufferRT);
                if (addFullDepthPrepass && !addAlphaTestedOnly) // Always true in case of forward rendering, use in case of deferred rendering if requesting a full depth prepass
                {
                    // We render first the opaque object as opaque alpha tested are more costly to render and could be reject by early-z (but not Hi-z as it is disable with clip instruction)
                    // This is handled automatically with the RenderQueue value (OpaqueAlphaTested have a different value and thus are sorted after Opaque)
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthOnlyAndDepthForwardOnlyPassNames, 0, RenderQueueRange.opaque, m_DepthStateOpaque);
                }
                else // Deferred rendering with partial depth prepass
                {
                    // We always do a DepthForwardOnly pass with all the opaque (including alpha test)
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthForwardOnlyPassNames, 0, RenderQueueRange.opaque, m_DepthStateOpaque);

                    // Render Alpha test only if requested
                    if (addAlphaTestedOnly)
                    {
                        var renderQueueRange = new RenderQueueRange { min = (int)RenderQueue.AlphaTest, max = (int)RenderQueue.GeometryLast - 1 };
                        RenderOpaqueRenderList(cull, camera, renderContext, cmd, m_DepthOnlyPassNames, 0, renderQueueRange, m_DepthStateOpaque);
                    }
                }

                // Render transparent depth prepass after opaque one
                RenderTransparentRenderList(cull, camera, renderContext, cmd, m_TransparentDepthOnlyPassNames, true);
                RenderTransparentRenderList(cull, camera, renderContext, cmd, m_TransparentDepthOnlyPassNames, false);
            }
        }

        // RenderGBuffer do the gbuffer pass. This is solely call with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque apha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(CullResults cull, Camera camera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly())
                return;

            using (new ProfilingSample(cmd, m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ? "GBufferDebugDisplay" : "GBuffer", GetSampler(CustomSamplerId.GBuffer)))
            {
                // setup GBuffer for rendering
                CoreUtils.SetRenderTarget(cmd, m_GbufferManager.GetGBuffers(), m_CameraDepthStencilBufferRT);

                // Render opaque objects into GBuffer
                if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled())
                {
                    // When doing debug display, the shader has the clip instruction regardless of the depth prepass so we can use regular depth test.
                    RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferDebugDisplayName, m_currentRendererConfigurationBakedLighting, RenderQueueRange.opaque, m_DepthStateOpaque);
                }
                else
                {
                    if (m_Asset.globalRenderingSettings.useDepthPrepassWithDeferredRendering)
                    {
                        var rangeOpaqueNoAlphaTest = new RenderQueueRange { min = (int)RenderQueue.Geometry,  max = (int)RenderQueue.AlphaTest - 1    };
                        var rangeOpaqueAlphaTest   = new RenderQueueRange { min = (int)RenderQueue.AlphaTest, max = (int)RenderQueue.GeometryLast - 1 };

                        // When using depth prepass for opaque alpha test only we need to use regular depth test for normal opaque objects.
                        RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferName, m_currentRendererConfigurationBakedLighting, rangeOpaqueNoAlphaTest, m_Asset.globalRenderingSettings.renderAlphaTestOnlyInDeferredPrepass ? m_DepthStateOpaque : m_DepthStateOpaqueWithPrepass);
                        // but for opaque alpha tested object we use a depth equal and no depth write. And we rely on the shader pass GbufferWithDepthPrepass
                        RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferWithPrepassName, m_currentRendererConfigurationBakedLighting, rangeOpaqueAlphaTest, m_DepthStateOpaqueWithPrepass);
                    }
                    else
                    {
                        // No depth prepass, use regular depth test - Note that we will render opaque then opaque alpha tested (based on the RenderQueue system)
                        RenderOpaqueRenderList(cull, camera, renderContext, cmd, HDShaderPassNames.s_GBufferName, m_currentRendererConfigurationBakedLighting, RenderQueueRange.opaque, m_DepthStateOpaque);
                    }
                }
            }
        }

        void RenderDebugViewMaterial(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "DisplayDebug ViewMaterial", GetSampler(CustomSamplerId.DisplayDebugViewMaterial)))
            {
                if (m_CurrentDebugDisplaySettings.materialDebugSettings.IsDebugGBufferEnabled() && !m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly())
                {
                    using (new ProfilingSample(cmd, "DebugViewMaterialGBuffer", GetSampler(CustomSamplerId.DebugViewMaterialGBuffer)))
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
                    RenderTransparentRenderList(cull, hdCamera.camera, renderContext, cmd, m_AllForwardDebugDisplayPassNames, true, m_currentRendererConfigurationBakedLighting);
                    RenderTransparentRenderList(cull, hdCamera.camera, renderContext, cmd, m_AllForwardDebugDisplayPassNames, false, m_currentRendererConfigurationBakedLighting);
                }
            }

            // Last blit
            {
                using (new ProfilingSample(cmd, "Blit DebugView Material Debug", GetSampler(CustomSamplerId.BlitDebugViewMaterialDebug)))
                {
                    cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                }
            }
        }

        void RenderSSAO(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext, PostProcessLayer postProcessLayer)
        {
            // Apply SSAO from PostProcessLayer
            if (postProcessLayer != null && postProcessLayer.enabled)
            {
                var settings = postProcessLayer.GetSettings<AmbientOcclusion>();

                if (settings.IsEnabledAndSupported(null))
                {
                    cmd.ReleaseTemporaryRT(HDShaderIDs._AmbientOcclusionTexture);
                    cmd.GetTemporaryRT(HDShaderIDs._AmbientOcclusionTexture, new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, RenderTextureFormat.R8, 0)
                    {
                        sRGB = false,
                        enableRandomWrite = true
                    }, FilterMode.Bilinear);
                    postProcessLayer.BakeMSVOMap(cmd, camera, HDShaderIDs._AmbientOcclusionTexture, GetDepthTexture(), true);
                    cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(settings.color.value.r, settings.color.value.g, settings.color.value.b, settings.directLightingStrength.value));
                    PushFullScreenDebugTexture(cmd, HDShaderIDs._AmbientOcclusionTexture, camera, renderContext, FullScreenDebugMode.SSAO);
                    return;
                }
            }

            // No AO applied - neutral is black, see the comment in the shaders
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, RuntimeUtilities.blackTexture);
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
        }

        void RenderDeferredLighting(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly())
                return;

            m_MRTCache2[0] = m_CameraColorBufferRT;
            m_MRTCache2[1] = m_CameraSssDiffuseLightingBufferRT;
            var depthTexture = GetDepthTexture();

            var options = new LightLoop.LightingPassOptions();

            if (m_CurrentDebugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission)
            {
                // Output split lighting for materials asking for it (masked in the stencil buffer)
                options.outputSplitLighting = true;

                m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_CameraDepthStencilBufferRT, depthTexture, options);
            }

            // Output combined lighting for all the other materials.
            options.outputSplitLighting = false;

            m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_CameraDepthStencilBufferRT, depthTexture, options);
        }

        // Combines specular lighting and diffuse lighting with subsurface scattering.
        void SubsurfaceScatteringPass(HDCamera hdCamera, CommandBuffer cmd, SubsurfaceScatteringSettings sssParameters)
        {
            // Currently, forward-rendered objects do not output split lighting required for the SSS pass.
            if (!m_CurrentDebugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission || m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly())
                return;

            using (new ProfilingSample(cmd, "Subsurface Scattering", GetSampler(CustomSamplerId.SubsurfaceScattering)))
            {
                if (sssSettings.useDisneySSS)
                {
                    // TODO: Remove this once fix, see comment inside the function
                    hdCamera.SetupComputeShader(m_SubsurfaceScatteringCS, cmd);

                    unsafe
                    {
                        // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                        // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                        uint texturingModeFlags = sssParameters.texturingModeFlags;
                        cmd.SetComputeFloatParam(m_SubsurfaceScatteringCS, HDShaderIDs._TexturingModeFlags, *(float*)&texturingModeFlags);
                    }

                    cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._WorldScales,        sssParameters.worldScales);
                    cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._FilterKernels,      sssParameters.filterKernels);
                    cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._ShapeParams,        sssParameters.shapeParams);

                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._GBufferTexture0,  m_GbufferManager.GetGBuffers()[0]);
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._GBufferTexture1,  m_GbufferManager.GetGBuffers()[1]);
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._GBufferTexture2,  m_GbufferManager.GetGBuffers()[2]);
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._GBufferTexture3,  m_GbufferManager.GetGBuffers()[3]);
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._DepthTexture,     GetDepthTexture());
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._HTile,            GetHTile());
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._IrradianceSource, m_CameraSssDiffuseLightingBufferRT);

                    if (NeedTemporarySubsurfaceBuffer())
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._CameraFilteringBuffer, m_CameraFilteringBufferRT);

                        // Perform the SSS filtering pass which fills 'm_CameraFilteringBufferRT'.
                        cmd.DispatchCompute(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, ((int)hdCamera.screenSize.x + 15) / 16, ((int)hdCamera.screenSize.y + 15) / 16, 1);

                        cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraFilteringBufferRT);  // Cannot set a RT on a material

                        // Additively blend diffuse and specular lighting into 'm_CameraColorBufferRT'.
                        CoreUtils.DrawFullScreen(cmd, m_CombineLightingPass, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._CameraColorTexture, m_CameraColorBufferRT);

                        // Perform the SSS filtering pass which performs an in-place update of 'm_CameraColorBufferRT'.
                        cmd.DispatchCompute(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, ((int)hdCamera.screenSize.x + 15) / 16, ((int)hdCamera.screenSize.y + 15) / 16, 1);
                    }
                }
                else
                {
                    cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraSssDiffuseLightingBufferRT);  // Cannot set a RT on a material
                    m_SssVerticalFilterPass.SetVectorArray(HDShaderIDs._WorldScales,              sssParameters.worldScales);
                    m_SssVerticalFilterPass.SetVectorArray(HDShaderIDs._FilterKernelsBasic,       sssParameters.filterKernelsBasic);
                    m_SssVerticalFilterPass.SetVectorArray(HDShaderIDs._HalfRcpWeightedVariances, sssParameters.halfRcpWeightedVariances);
                    // Perform the vertical SSS filtering pass which fills 'm_CameraFilteringBufferRT'.
                    CoreUtils.DrawFullScreen(cmd, m_SssVerticalFilterPass, m_CameraFilteringBufferRT, m_CameraDepthStencilBufferRT);

                    cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraFilteringBufferRT);  // Cannot set a RT on a material
                    m_SssHorizontalFilterAndCombinePass.SetVectorArray(HDShaderIDs._WorldScales,              sssParameters.worldScales);
                    m_SssHorizontalFilterAndCombinePass.SetVectorArray(HDShaderIDs._FilterKernelsBasic,       sssParameters.filterKernelsBasic);
                    m_SssHorizontalFilterAndCombinePass.SetVectorArray(HDShaderIDs._HalfRcpWeightedVariances, sssParameters.halfRcpWeightedVariances);
                    // Perform the horizontal SSS filtering pass, and combine diffuse and specular lighting into 'm_CameraColorBufferRT'.
                    CoreUtils.DrawFullScreen(cmd, m_SssHorizontalFilterAndCombinePass, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);
                }
            }
        }

        void UpdateSkyEnvironment(HDCamera hdCamera, CommandBuffer cmd)
        {
            m_SkyManager.UpdateEnvironment(hdCamera,m_LightLoop.GetCurrentSunLight(), cmd);
        }

        void RenderSky(HDCamera hdCamera, CommandBuffer cmd)
        {
            m_SkyManager.RenderSky(hdCamera, m_LightLoop.GetCurrentSunLight(), m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, cmd, m_DebugDisplaySettings);
            m_SkyManager.RenderOpaqueAtmosphericScattering(cmd);
        }

        public Texture2D ExportSkyToTexture()
        {
            return m_SkyManager.ExportSkyToTexture();
        }

        // Render forward is use for both transparent and opaque objects. In case of deferred we can still render opaque object in forward.
        void RenderForward(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
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

            using (new ProfilingSample(cmd, profileName, GetSampler(CustomSamplerId.ForwardPassName)))
            {
                CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);

                m_LightLoop.RenderForward(camera, cmd, pass == ForwardPass.Opaque);

                if (pass == ForwardPass.Opaque)
                {
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

                    var passNames = m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly() ? m_ForwardAndForwardOnlyPassNames : m_ForwardOnlyPassNames;
                    // Forward opaque material always have a prepass (whether or not we use deferred, whether or not there is option like alpha test only) so we pass the right depth state here.
                    RenderOpaqueRenderList(cullResults, camera, renderContext, cmd, passNames, m_currentRendererConfigurationBakedLighting, null, m_DepthStateOpaqueWithPrepass);
                }
                else
                {
                    var passNames = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ? m_AllTransparentDebugDisplayPassNames : m_AllTransparentPassNames;
                    RenderTransparentRenderList(cullResults, camera, renderContext, cmd, passNames, pass == ForwardPass.PreRefraction, m_currentRendererConfigurationBakedLighting);
                }
            }
        }

        // This is use to Display legacy shader with an error shader
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void RenderForwardError(CullResults cullResults, Camera camera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
        {
            using (new ProfilingSample(cmd, "Render Forward Error", GetSampler(CustomSamplerId.RenderForwardError)))
            {
                CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);

                if (pass == ForwardPass.Opaque)
                {
                    RenderOpaqueRenderList(cullResults, camera, renderContext, cmd, m_ForwardErrorPassNames, 0, null, null, m_ErrorMaterial);
                }
                else
                {
                    RenderTransparentRenderList(cullResults, camera, renderContext, cmd, m_ForwardErrorPassNames, pass == ForwardPass.PreRefraction, 0, null, m_ErrorMaterial);
                }
            }
        }

        void RenderVelocity(CullResults cullResults, HDCamera hdcam, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Velocity", GetSampler(CustomSamplerId.Velocity)))
            {
                // If opaque velocity have been render during GBuffer no need to render it here
                // TODO: Currently we can't render velocity vector into GBuffer, neither during forward pass (in case of forward opaque), so it is always a separate pass
                // Note that we if we have forward only opaque with deferred rendering, it must also support the rendering of velocity vector to be correct with following test.
                if ((ShaderConfig.s_VelocityInGbuffer == 1))
                {
                    Debug.LogWarning("Velocity in Gbuffer is currently not supported");
                    return;
                }

                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdcam.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                int w = (int)hdcam.screenSize.x;
                int h = (int)hdcam.screenSize.y;

                m_CameraMotionVectorsMaterial.SetVector(HDShaderIDs._CameraPosDiff, hdcam.prevCameraPos - hdcam.cameraPos);

                cmd.ReleaseTemporaryRT(m_VelocityBuffer);
                cmd.GetTemporaryRT(m_VelocityBuffer, w, h, 0, FilterMode.Point, Builtin.GetVelocityBufferFormat(), Builtin.GetVelocityBufferReadWrite());
                CoreUtils.DrawFullScreen(cmd, m_CameraMotionVectorsMaterial, m_VelocityBufferRT, null, 0);
                cmd.SetRenderTarget(m_VelocityBufferRT, m_CameraDepthStencilBufferRT);

                RenderOpaqueRenderList(cullResults, hdcam.camera, renderContext, cmd, HDShaderPassNames.s_MotionVectorsName, RendererConfiguration.PerObjectMotionVectors);

                PushFullScreenDebugTexture(cmd, m_VelocityBuffer, hdcam.camera, renderContext, FullScreenDebugMode.MotionVectors);
            }
        }

        void RenderGaussianPyramidColor(Camera camera, CommandBuffer cmd, ScriptableRenderContext renderContext, FullScreenDebugMode debugMode)
        {
            if (!m_CurrentDebugDisplaySettings.renderingDebugSettings.enableGaussianPyramid)
                return;

            using (new ProfilingSample(cmd, "Gaussian Pyramid Color", GetSampler(CustomSamplerId.GaussianPyramidColor)))
            {
                int w = camera.pixelWidth;
                int h = camera.pixelHeight;
                int size = CalculatePyramidSize(w, h);

                // The gaussian pyramid compute works in blocks of 8x8 so make sure the last lod has a
                // minimum size of 8x8
                int lodCount = Mathf.FloorToInt(Mathf.Log(size, 2f) - 3f);
                if (lodCount > HDShaderIDs._GaussianPyramidColorMips.Length)
                {
                    Debug.LogWarningFormat("Cannot compute all mipmaps of the color pyramid, max texture size supported: {0}", (2 << HDShaderIDs._GaussianPyramidColorMips.Length).ToString());
                    lodCount = HDShaderIDs._GaussianPyramidColorMips.Length;
                }

                cmd.SetGlobalVector(HDShaderIDs._GaussianPyramidColorMipSize, new Vector4(size, size, lodCount, 0));

                cmd.Blit(m_CameraColorBufferRT, m_GaussianPyramidColorBuffer);

                var last = m_GaussianPyramidColorBuffer;

                var mipSize = size;
                for (int i = 0; i < lodCount; i++)
                {
                    mipSize >>= 1;

                    cmd.ReleaseTemporaryRT(HDShaderIDs._GaussianPyramidColorMips[i + 1]);
                    cmd.GetTemporaryRT(HDShaderIDs._GaussianPyramidColorMips[i + 1], mipSize, mipSize, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, true);
                    cmd.SetComputeTextureParam(m_GaussianPyramidCS, m_GaussianPyramidKernel, "_Source", last);
                    cmd.SetComputeTextureParam(m_GaussianPyramidCS, m_GaussianPyramidKernel, "_Result", HDShaderIDs._GaussianPyramidColorMips[i + 1]);
                    cmd.SetComputeVectorParam(m_GaussianPyramidCS, "_Size", new Vector4(mipSize, mipSize, 1f / mipSize, 1f / mipSize));
                    cmd.DispatchCompute(m_GaussianPyramidCS, m_GaussianPyramidKernel, mipSize / 8, mipSize / 8, 1);
                    cmd.CopyTexture(HDShaderIDs._GaussianPyramidColorMips[i + 1], 0, 0, m_GaussianPyramidColorBufferRT, 0, i + 1);

                    last = HDShaderIDs._GaussianPyramidColorMips[i + 1];
                }

                PushFullScreenDebugTextureMip(cmd, m_GaussianPyramidColorBufferRT, lodCount, size, size, debugMode);

                cmd.SetGlobalTexture(HDShaderIDs._GaussianPyramidColorTexture, m_GaussianPyramidColorBuffer);

                for (int i = 0; i < lodCount; i++)
                    cmd.ReleaseTemporaryRT(HDShaderIDs._GaussianPyramidColorMips[i + 1]);
            }
        }

        void RenderPyramidDepth(Camera camera, CommandBuffer cmd, ScriptableRenderContext renderContext, FullScreenDebugMode debugMode)
        {
            if (!m_CurrentDebugDisplaySettings.renderingDebugSettings.enableGaussianPyramid)
                return;

            using (new ProfilingSample(cmd, "Pyramid Depth", GetSampler(CustomSamplerId.PyramidDepth)))
            {
                int w = camera.pixelWidth;
                int h = camera.pixelHeight;
                int size = CalculatePyramidSize(w, h);

                // The gaussian pyramid compute works in blocks of 8x8 so make sure the last lod has a
                // minimum size of 8x8
                int lodCount = Mathf.FloorToInt(Mathf.Log(size, 2f) - 3f);
                if (lodCount > HDShaderIDs._DepthPyramidMips.Length)
                {
                    Debug.LogWarningFormat("Cannot compute all mipmaps of the depth pyramid, max texture size supported: {0}", (2 << HDShaderIDs._DepthPyramidMips.Length).ToString());
                    lodCount = HDShaderIDs._DepthPyramidMips.Length;
                }

                cmd.SetGlobalVector(HDShaderIDs._DepthPyramidMipSize, new Vector4(size, size, lodCount, 0));

                cmd.ReleaseTemporaryRT(HDShaderIDs._DepthPyramidMips[0]);
                cmd.GetTemporaryRT(HDShaderIDs._DepthPyramidMips[0], size, size, 0, FilterMode.Bilinear, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1, true);
                m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, GetDepthTexture(), HDShaderIDs._DepthPyramidMips[0], new Vector2(size, size));
                cmd.CopyTexture(HDShaderIDs._DepthPyramidMips[0], 0, 0, m_DepthPyramidBuffer, 0, 0);

                var mipSize = size;
                for (int i = 0; i < lodCount; i++)
                {
                    mipSize >>= 1;

                    cmd.ReleaseTemporaryRT(HDShaderIDs._DepthPyramidMips[i + 1]);
                    cmd.GetTemporaryRT(HDShaderIDs._DepthPyramidMips[i + 1], mipSize, mipSize, 0, FilterMode.Bilinear, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1, true);
                    cmd.SetComputeTextureParam(m_DepthPyramidCS, m_DepthPyramidKernel, "_Source", HDShaderIDs._DepthPyramidMips[i]);
                    cmd.SetComputeTextureParam(m_DepthPyramidCS, m_DepthPyramidKernel, "_Result", HDShaderIDs._DepthPyramidMips[i + 1]);
                    cmd.SetComputeVectorParam(m_DepthPyramidCS, "_Size", new Vector4(mipSize, mipSize, 1f / mipSize, 1f / mipSize));
                    cmd.DispatchCompute(m_DepthPyramidCS, m_DepthPyramidKernel, mipSize / 8, mipSize / 8, 1);
                    cmd.CopyTexture(HDShaderIDs._DepthPyramidMips[i + 1], 0, 0, m_DepthPyramidBufferRT, 0, i + 1);
                }

                PushFullScreenDebugDepthMip(cmd, m_DepthPyramidBufferRT, lodCount, size, size, debugMode);

                cmd.SetGlobalTexture(HDShaderIDs._DepthPyramidTexture, m_DepthPyramidBuffer);

                for (int i = 0; i < lodCount + 1; i++)
                    cmd.ReleaseTemporaryRT(HDShaderIDs._DepthPyramidMips[i]);
            }
        }

        void RenderPostProcesses(HDCamera hdcamera, CommandBuffer cmd, PostProcessLayer layer)
        {
            using (new ProfilingSample(cmd, "Post-processing", GetSampler(CustomSamplerId.PostProcessing)))
            {
                if (CoreUtils.IsPostProcessingActive(layer))
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
                else
                {
                    cmd.Blit(m_CameraColorBufferRT, BuiltinRenderTextureType.CameraTarget);
                }
            }
        }

        public void ApplyDebugDisplaySettings(CommandBuffer cmd)
        {
            m_ShadowSettings.enabled = m_CurrentDebugDisplaySettings.lightingDebugSettings.enableShadows;

            var lightingDebugSettings = m_CurrentDebugDisplaySettings.lightingDebugSettings;
            var debugAlbedo = new Vector4(lightingDebugSettings.debugLightingAlbedo.r, lightingDebugSettings.debugLightingAlbedo.g, lightingDebugSettings.debugLightingAlbedo.b, 0.0f);
            var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);

            cmd.SetGlobalInt(HDShaderIDs._DebugViewMaterial, (int)m_CurrentDebugDisplaySettings.GetDebugMaterialIndex());
            cmd.SetGlobalInt(HDShaderIDs._DebugLightingMode, (int)m_CurrentDebugDisplaySettings.GetDebugLightingMode());
            cmd.SetGlobalVector(HDShaderIDs._DebugLightingAlbedo, debugAlbedo);
            cmd.SetGlobalVector(HDShaderIDs._DebugLightingSmoothness, debugSmoothness);
        }

        public void PushFullScreenDebugTexture(CommandBuffer cb, RenderTargetIdentifier textureID, Camera camera, ScriptableRenderContext renderContext, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no fullscreen debug is pushed, when we render the result in RenderDebug the temporary RT will not exist.
                cb.ReleaseTemporaryRT(m_DebugFullScreenTempRT);
                cb.GetTemporaryRT(m_DebugFullScreenTempRT, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                cb.Blit(textureID, m_DebugFullScreenTempRT);
            }
        }

        void PushFullScreenDebugTextureMip(CommandBuffer cmd, RenderTargetIdentifier textureID, int lodCount, int width, int height, FullScreenDebugMode debugMode)
        {
            var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.fullscreenDebugMip * (lodCount));
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no fullscreen debug is pushed, when we render the result in RenderDebug the temporary RT will not exist.
                cmd.ReleaseTemporaryRT(m_DebugFullScreenTempRT);
                cmd.GetTemporaryRT(m_DebugFullScreenTempRT, width >> mipIndex, height >> mipIndex, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                cmd.CopyTexture(textureID, 0, mipIndex, m_DebugFullScreenTempRT, 0, 0);
            }
        }

        void PushFullScreenDebugDepthMip(CommandBuffer cmd, RenderTargetIdentifier textureID, int lodCount, int width, int height, FullScreenDebugMode debugMode)
        {
            var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.fullscreenDebugMip * (lodCount));
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no fullscreen debug is pushed, when we render the result in RenderDebug the temporary RT will not exist.
                cmd.ReleaseTemporaryRT(m_DebugFullScreenTempRT);
                cmd.GetTemporaryRT(m_DebugFullScreenTempRT, width >> mipIndex, height >> mipIndex, 0, FilterMode.Point, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                cmd.CopyTexture(textureID, 0, mipIndex, m_DebugFullScreenTempRT, 0, 0);
            }
        }

        public void PushFullScreenDebugTexture(CommandBuffer cb, int textureID, Camera camera, ScriptableRenderContext renderContext, FullScreenDebugMode debugMode)
        {
            PushFullScreenDebugTexture(cb, new RenderTargetIdentifier(textureID), camera, renderContext, debugMode);
        }

        void RenderDebug(HDCamera camera, CommandBuffer cmd)
        {
            // We don't want any overlay for these kind of rendering
            if (camera.camera.cameraType == CameraType.Reflection || camera.camera.cameraType == CameraType.Preview)
                return;

            using (new ProfilingSample(cmd, "Render Debug", GetSampler(CustomSamplerId.RenderDebug)))
            {
                // We make sure the depth buffer is bound because we need it to write depth at near plane for overlays otherwise the editor grid end up visible in them.
                CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, m_CameraDepthStencilBufferRT);

                // First render full screen debug texture
                if (m_CurrentDebugDisplaySettings.fullScreenDebugMode != FullScreenDebugMode.None && m_FullScreenDebugPushed)
                {
                    m_FullScreenDebugPushed = false;
                    cmd.SetGlobalTexture(HDShaderIDs._DebugFullScreenTexture, m_DebugFullScreenTempRT);
                    m_DebugFullScreen.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)m_CurrentDebugDisplaySettings.fullScreenDebugMode);
                    CoreUtils.DrawFullScreen(cmd, m_DebugFullScreen, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);
                }

                // Then overlays
                float x = 0;
                float overlayRatio = m_CurrentDebugDisplaySettings.debugOverlayRatio;
                float overlaySize = Math.Min(camera.camera.pixelHeight, camera.camera.pixelWidth) * overlayRatio;
                float y = camera.camera.pixelHeight - overlaySize;

                var lightingDebug = m_CurrentDebugDisplaySettings.lightingDebugSettings;

                if (lightingDebug.displaySkyReflection)
                {
                    var skyReflection = m_SkyManager.skyReflection;
                    m_SharedPropertyBlock.SetTexture(HDShaderIDs._InputCubemap, skyReflection);
                    m_SharedPropertyBlock.SetFloat(HDShaderIDs._Mipmap, lightingDebug.skyReflectionMipmap);
                    cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                    cmd.DrawProcedural(Matrix4x4.identity, m_DebugDisplayLatlong, 0, MeshTopology.Triangles, 3, 1, m_SharedPropertyBlock);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, camera.camera.pixelWidth);
                }

                m_LightLoop.RenderDebugOverlay(camera, cmd, m_CurrentDebugDisplaySettings, ref x, ref y, overlaySize, camera.camera.pixelWidth);
            }
        }

        void InitAndClearBuffer(HDCamera camera, bool enableBakeShadowMask, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "InitAndClearBuffer", GetSampler(CustomSamplerId.InitAndClearBuffer)))
            {
                // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.
                // Clear depth/stencil and init buffers
                using (new ProfilingSample(cmd, "InitGBuffers and clear Depth/Stencil", GetSampler(CustomSamplerId.InitGBuffersAndClearDepthStencil)))
                {
                    // Init buffer
                    // With scriptable render loop we must allocate ourself depth and color buffer (We must be independent of backbuffer for now, hope to fix that later).
                    // Also we manage ourself the HDR format, here allocating fp16 directly.
                    // With scriptable render loop we can allocate temporary RT in a command buffer, they will not be release with ExecuteCommandBuffer
                    // These temporary surface are release automatically at the end of the scriptable render pipeline if not release explicitly
                    int w = camera.camera.pixelWidth;
                    int h = camera.camera.pixelHeight;

                    cmd.ReleaseTemporaryRT(m_CameraColorBuffer);
                    cmd.ReleaseTemporaryRT(m_CameraSssDiffuseLightingBuffer);
                    cmd.GetTemporaryRT(m_CameraColorBuffer,              w, h, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf,       RenderTextureReadWrite.Linear, 1, true); // Enable UAV
                    cmd.GetTemporaryRT(m_CameraSssDiffuseLightingBuffer, w, h, 0, FilterMode.Point, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, 1, true); // Enable UAV
                    if (NeedTemporarySubsurfaceBuffer())
                    {
                        cmd.GetTemporaryRT(m_CameraFilteringBuffer,      w, h, 0, FilterMode.Point, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, 1, true); // Enable UAV
                    }

                    // Color and depth pyramids
                    int s = CalculatePyramidSize(w, h);
                    m_GaussianPyramidColorBufferDesc.width = s;
                    m_GaussianPyramidColorBufferDesc.height = s;
                    cmd.ReleaseTemporaryRT(m_GaussianPyramidColorBuffer);
                    cmd.GetTemporaryRT(m_GaussianPyramidColorBuffer, m_GaussianPyramidColorBufferDesc, FilterMode.Trilinear);

                    m_DepthPyramidBufferDesc.width = s;
                    m_DepthPyramidBufferDesc.height = s;
                    cmd.ReleaseTemporaryRT(m_DepthPyramidBuffer);
                    cmd.GetTemporaryRT(m_DepthPyramidBuffer, m_DepthPyramidBufferDesc, FilterMode.Trilinear);
                    // End

                    if (!m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly())
                        m_GbufferManager.InitGBuffers(w, h, m_DeferredMaterial, enableBakeShadowMask, cmd);

                    CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.Depth);
                }

                // Clear the diffuse SSS lighting target
                using (new ProfilingSample(cmd, "Clear SSS diffuse target", GetSampler(CustomSamplerId.ClearSSSDiffuseTarget)))
                {
                    CoreUtils.SetRenderTarget(cmd, m_CameraSssDiffuseLightingBufferRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                }

                // Old SSS Model >>>
                if (NeedTemporarySubsurfaceBuffer())
                {
                    // Clear the SSS filtering target
                    using (new ProfilingSample(cmd, "Clear SSS filtering target", GetSampler(CustomSamplerId.ClearSSSFilteringTarget)))
                    {
                        CoreUtils.SetRenderTarget(cmd, m_CameraFilteringBuffer, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }
                // <<< Old SSS Model

                if (NeedStencilBufferCopy())
                {
                    using (new ProfilingSample(cmd, "Clear stencil texture", GetSampler(CustomSamplerId.ClearStencilTexture)))
                    {
                        CoreUtils.SetRenderTarget(cmd, m_CameraStencilBufferCopyRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }

                if (NeedHTileCopy())
                {
                    using (new ProfilingSample(cmd, "Clear HTile", GetSampler(CustomSamplerId.ClearHTile)))
                    {
                        CoreUtils.SetRenderTarget(cmd, m_HTileRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }

                // TODO: As we are in development and have not all the setup pass we still clear the color in emissive buffer and gbuffer, but this will be removed later.

                // Clear the HDR target
                using (new ProfilingSample(cmd, "Clear HDR target", GetSampler(CustomSamplerId.ClearHDRTarget)))
                {
                    Color clearColor = camera.camera.backgroundColor.linear; // Need it in linear because we clear a linear fp16 texture.
                    CoreUtils.SetRenderTarget(cmd, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT, ClearFlag.Color, clearColor);
                }

                // Clear GBuffers
                if (!m_Asset.globalRenderingSettings.ShouldUseForwardRenderingOnly())
                {
                    using (new ProfilingSample(cmd, "Clear GBuffer", GetSampler(CustomSamplerId.ClearGBuffer)))
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
    }
}
