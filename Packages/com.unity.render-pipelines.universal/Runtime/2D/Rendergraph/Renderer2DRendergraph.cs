using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;
using static UnityEngine.Rendering.Universal.UniversalResourceDataBase;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal sealed partial class Renderer2D : ScriptableRenderer
    {
        private struct RenderPassInputSummary
        {
            internal bool requiresDepthTexture;
            internal bool requiresColorTexture;
        }

        private struct ImportResourceSummary
        {
            internal RenderTargetInfo importInfo;
            internal RenderTargetInfo importInfoDepth;
            internal ImportResourceParams cameraColorParams;
            internal ImportResourceParams cameraDepthParams;
            internal ImportResourceParams backBufferColorParams;
            internal ImportResourceParams backBufferDepthParams;
        }

        const int k_FinalBlitPassQueueOffset = 1;
        const int k_AfterFinalBlitPassQueueOffset = k_FinalBlitPassQueueOffset + 1;

        // TODO RENDERGRAPH: Once all cameras will run in a single RenderGraph we should remove all RTHandles and use per frame RG textures.
        // We use 2 camera color handles so we can handle the edge case when a pass might want to read and write the same target.
        // This is not allowed so we just swap the current target, this keeps camera stacking working and avoids an extra blit pass.
        static int m_CurrentColorHandle = 0;
        internal RTHandle[] m_RenderGraphCameraColorHandles = new RTHandle[]
        {
            null, null
        };

        internal RTHandle m_RenderGraphCameraDepthHandle;
        RTHandle m_RenderGraphBackbufferColorHandle;
        RTHandle m_RenderGraphBackbufferDepthHandle;
        RTHandle m_CameraSortingLayerHandle;

        Material m_BlitMaterial;
        Material m_BlitHDRMaterial;
        Material m_SamplingMaterial;

        // 2D specific render passes
        DrawNormal2DPass m_NormalPass = new DrawNormal2DPass();
        DrawLight2DPass m_LightPass = new DrawLight2DPass();
        DrawShadow2DPass m_ShadowPass = new DrawShadow2DPass();
        DrawRenderer2DPass m_RendererPass = new DrawRenderer2DPass();

        CopyDepthPass m_CopyDepthPass;
        UpscalePass m_UpscalePass;
        CopyCameraSortingLayerPass m_CopyCameraSortingLayerPass;
        FinalBlitPass m_FinalBlitPass;
        DrawScreenSpaceUIPass m_DrawOffscreenUIPass;
        DrawScreenSpaceUIPass m_DrawOverlayUIPass; // from HDRP code

        Renderer2DData m_Renderer2DData;

        internal bool m_CreateColorTexture;
        internal bool m_CreateDepthTexture;
        bool ppcUpscaleRT = false;

        PostProcess m_PostProcess;
        ColorGradingLutPass m_ColorGradingLutPassRenderGraph;

        private RTHandle currentRenderGraphCameraColorHandle
        {
            get
            {
                return m_RenderGraphCameraColorHandles[m_CurrentColorHandle];
            }
        }

        // get the next m_RenderGraphCameraColorHandles and make it the new current for future accesses
        private RTHandle nextRenderGraphCameraColorHandle
        {
            get
            {
                m_CurrentColorHandle = (m_CurrentColorHandle + 1) % 2;
                return currentRenderGraphCameraColorHandle;
            }
        }

        /// <inheritdoc/>
        public override int SupportedCameraStackingTypes()
        {
            return 1 << (int)CameraRenderType.Base | 1 << (int)CameraRenderType.Overlay;
        }

        public Renderer2D(Renderer2DData data) : base(data)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineRuntimeShaders>(out var shadersResources))
            {
                m_BlitMaterial = CoreUtils.CreateEngineMaterial(shadersResources.coreBlitPS);
                m_BlitHDRMaterial = CoreUtils.CreateEngineMaterial(shadersResources.blitHDROverlay);
                m_SamplingMaterial = CoreUtils.CreateEngineMaterial(shadersResources.samplingPS);
            }

            if (GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var renderer2DResources))
            {
                m_CopyDepthPass = new CopyDepthPass(
                    RenderPassEvent.AfterRenderingTransparents,
                    renderer2DResources.copyDepthPS,
                    shouldClear: true,
                    copyResolvedDepth: RenderingUtils.MultisampleDepthResolveSupported());
            }

            m_UpscalePass = new UpscalePass(RenderPassEvent.AfterRenderingPostProcessing, m_BlitMaterial);
            m_CopyCameraSortingLayerPass = new CopyCameraSortingLayerPass(m_BlitMaterial);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + k_FinalBlitPassQueueOffset, m_BlitMaterial, m_BlitHDRMaterial);

            m_DrawOffscreenUIPass = new DrawScreenSpaceUIPass(RenderPassEvent.BeforeRenderingPostProcessing, true);
            m_DrawOverlayUIPass = new DrawScreenSpaceUIPass(RenderPassEvent.AfterRendering + k_AfterFinalBlitPassQueueOffset, false); // after m_FinalBlitPass

            m_Renderer2DData = data;
            m_Renderer2DData.lightCullResult = new Light2DCullResult();

            supportedRenderingFeatures = new RenderingFeatures();

            LensFlareCommonSRP.mergeNeeded = 0;
            LensFlareCommonSRP.maxLensFlareWithOcclusionTemporalSample = 1;
            LensFlareCommonSRP.Initialize();

            Light2DManager.Initialize();

            if (data.postProcessData != null)
            {
                m_PostProcess = new PostProcess(data.postProcessData);
                m_ColorGradingLutPassRenderGraph = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, data.postProcessData);
            }

            PlatformAutoDetect.Initialize();

#if ENABLE_VR && ENABLE_XR_MODULE
            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineRuntimeXRResources>(out var xrResources))
            {
                XRSystem.Initialize(XRPassUniversal.Create, xrResources.xrOcclusionMeshPS, xrResources.xrMirrorViewPS);
            }
#endif

#if URP_COMPATIBILITY_MODE
            InitializeCompatibilityMode(data);
#endif
        }

        private bool IsPixelPerfectCameraEnabled(UniversalCameraData cameraData)
        {
            PixelPerfectCamera ppc = null;

            // Pixel Perfect Camera doesn't support camera stacking.
            if (cameraData.renderType == CameraRenderType.Base && cameraData.resolveFinalTarget)
                cameraData.camera.TryGetComponent(out ppc);

            return ppc != null && ppc.enabled && ppc.cropFrame != PixelPerfectCamera.CropFrame.None;
        }

        private RenderPassInputSummary GetRenderPassInputs()
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            RenderPassInputSummary inputSummary = new RenderPassInputSummary();

            for (int i = 0; i < activeRenderPassQueue.Count; ++i)
            {
                ScriptableRenderPass pass = activeRenderPassQueue[i];
                bool needsDepth = (pass.input & ScriptableRenderPassInput.Depth) != ScriptableRenderPassInput.None;
                bool needsColor = (pass.input & ScriptableRenderPassInput.Color) != ScriptableRenderPassInput.None;

                inputSummary.requiresDepthTexture |= needsDepth;
                inputSummary.requiresColorTexture |= needsColor;
            }

            inputSummary.requiresColorTexture |= cameraData.postProcessEnabled
                    || cameraData.isHdrEnabled
                    || cameraData.isSceneViewCamera
                    || !cameraData.isDefaultViewport
                    || cameraData.requireSrgbConversion
                    || !cameraData.resolveFinalTarget
                    || cameraData.cameraTargetDescriptor.msaaSamples > 1 && UniversalRenderer.PlatformRequiresExplicitMsaaResolve()
                    || m_Renderer2DData.useCameraSortingLayerTexture
                    || !Mathf.Approximately(cameraData.renderScale, 1.0f)
                    || (DebugHandler != null && DebugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget));

            return inputSummary;
        }

        ImportResourceSummary GetImportResourceSummary(RenderGraph renderGraph, UniversalCameraData cameraData)
        {
            ImportResourceSummary output = new ImportResourceSummary();

            bool clearColor = cameraData.renderType == CameraRenderType.Base;
            bool clearDepth = cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth;

            // Clear back buffer color if pixel perfect crop frame is used
            // Non-base cameras the back buffer should never be cleared
            bool ppcEnabled = IsPixelPerfectCameraEnabled(cameraData);
            bool clearColorBackbufferOnFirstUse = (cameraData.renderType == CameraRenderType.Base) && (!m_CreateColorTexture || ppcEnabled);
            bool clearDepthBackbufferOnFirstUse = (cameraData.renderType == CameraRenderType.Base) && !m_CreateColorTexture;

            // if the camera background type is "uninitialized" clear using a yellow color, so users can clearly understand the underlying behaviour
            Color cameraBackgroundColor = (cameraData.camera.clearFlags == CameraClearFlags.Nothing) ? Color.yellow : cameraData.backgroundColor;
            Color backBufferBackgroundColor = ppcEnabled ? Color.black : cameraBackgroundColor;

            if (IsSceneFilteringEnabled(cameraData.camera))
            {
                cameraBackgroundColor.a = 0;
                clearDepth = false;
            }

            // Certain debug modes (e.g. wireframe/overdraw modes) require that we override clear flags and clear everything.
            var debugHandler = cameraData.renderer.DebugHandler;
            if (debugHandler != null && debugHandler.IsActiveForCamera(cameraData.isPreviewCamera) && debugHandler.IsScreenClearNeeded)
            {
                clearColor = true;
                clearDepth = true;
                debugHandler.TryGetScreenClearColor(ref cameraBackgroundColor);
            }

            output.cameraColorParams.clearOnFirstUse = clearColor;
            output.cameraColorParams.clearColor = cameraBackgroundColor;
            output.cameraColorParams.discardOnLastUse = false;

            output.cameraDepthParams.clearOnFirstUse = clearDepth;
            output.cameraDepthParams.clearColor = cameraBackgroundColor;
            output.cameraDepthParams.discardOnLastUse = false;

            output.backBufferColorParams.clearOnFirstUse = clearColorBackbufferOnFirstUse;
            output.backBufferColorParams.clearColor = backBufferBackgroundColor;
            output.backBufferColorParams.discardOnLastUse = false;

            output.backBufferDepthParams.clearOnFirstUse = clearDepthBackbufferOnFirstUse;
            output.backBufferDepthParams.clearColor = backBufferBackgroundColor;
            output.backBufferDepthParams.discardOnLastUse = true;

            bool isBuiltInTexture = cameraData.targetTexture == null;

            bool useActualBackbufferOrienation = !cameraData.isSceneViewCamera && !cameraData.isPreviewCamera && cameraData.targetTexture == null;
            TextureUVOrigin backbufferTextureUVOrigin = useActualBackbufferOrienation ? (SystemInfo.graphicsUVStartsAtTop ? TextureUVOrigin.TopLeft : TextureUVOrigin.BottomLeft) : TextureUVOrigin.BottomLeft;

            output.backBufferColorParams.textureUVOrigin = backbufferTextureUVOrigin;
            output.backBufferDepthParams.textureUVOrigin = backbufferTextureUVOrigin;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                isBuiltInTexture = false;
            }
#endif

            if (!isBuiltInTexture)
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    output.importInfo.width = cameraData.xr.renderTargetDesc.width;
                    output.importInfo.height = cameraData.xr.renderTargetDesc.height;
                    output.importInfo.volumeDepth = cameraData.xr.renderTargetDesc.volumeDepth;
                    output.importInfo.msaaSamples = cameraData.xr.renderTargetDesc.msaaSamples;
                    output.importInfo.format = cameraData.xr.renderTargetDesc.graphicsFormat;
                    if (!UniversalRenderer.PlatformRequiresExplicitMsaaResolve())
                        output.importInfo.bindMS = output.importInfo.msaaSamples > 1;

                    output.importInfoDepth = output.importInfo;
                    output.importInfoDepth.format = cameraData.xr.renderTargetDesc.depthStencilFormat;
                }
                else
#endif
                {
                    output.importInfo.width = cameraData.targetTexture.width;
                    output.importInfo.height = cameraData.targetTexture.height;
                    output.importInfo.volumeDepth = cameraData.targetTexture.volumeDepth;
                    output.importInfo.msaaSamples = cameraData.targetTexture.antiAliasing;
                    output.importInfo.format = cameraData.targetTexture.graphicsFormat;

                    output.importInfoDepth = output.importInfo;
                    output.importInfoDepth.format = cameraData.targetTexture.depthStencilFormat;

                    // We let users know that a depth format is required for correct usage, but we fallback to the old default depth format behaviour to avoid regressions
                    if (output.importInfoDepth.format == GraphicsFormat.None)
                    {
                        output.importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
                        Debug.LogWarning("In the render graph API, the output Render Texture must have a depth buffer. When you select a Render Texture in any camera's Output Texture property, the Depth Stencil Format property of the texture must be set to a value other than None.");
                    }
                }
            }
            else
            {
                // Backbuffer is the final render target, we obtain its number of MSAA samples through Screen API
                // in some cases we disable multisampling for optimization purpose
                int numSamples = AdjustAndGetScreenMSAASamples(renderGraph, m_CreateColorTexture);

                //NOTE: Careful what you use here as many of the properties bake-in the camera rect so for example
                //cameraData.cameraTargetDescriptor.width is the width of the rectangle but not the actual render target
                //same with cameraData.camera.pixelWidth
                output.importInfo.width = Screen.width;
                output.importInfo.height = Screen.height;
                output.importInfo.volumeDepth = 1;
                output.importInfo.msaaSamples = numSamples;
                output.importInfo.format = cameraData.cameraTargetDescriptor.graphicsFormat;

                output.importInfoDepth = output.importInfo;
                output.importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }

            return output;
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;

            var cullResult = m_Renderer2DData.lightCullResult as Light2DCullResult;
            cullResult.SetupCulling(ref cullingParameters, cameraData.camera);
        }

        void InitializeLayerBatches()
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
            var renderingData = frameData.Get<Universal2DRenderingData>().renderingData;
            ref var layerBatches = ref frameData.Get<Universal2DRenderingData>().layerBatches;

            layerBatches = LayerUtility.CalculateBatches(renderingData, out var batchCount);
            frameData.Get<Universal2DRenderingData>().batchCount = batchCount;

            // Initialize textures dependent on batch size
            if (resourceData.normalsTexture.Length != batchCount)
                resourceData.normalsTexture = new TextureHandle[batchCount];

            if (resourceData.shadowTextures.Length != batchCount)
                resourceData.shadowTextures = new TextureHandle[batchCount][];

            if (resourceData.lightTextures.Length != batchCount)
                resourceData.lightTextures = new TextureHandle[batchCount][];

            // Initialize light textures based on active blend styles to save on resources
            for (int i = 0; i < resourceData.lightTextures.Length; ++i)
            {
                if (resourceData.lightTextures[i] == null || resourceData.lightTextures[i].Length != layerBatches[i].activeBlendStylesIndices.Length)
                    resourceData.lightTextures[i] = new TextureHandle[layerBatches[i].activeBlendStylesIndices.Length];
            }

            for (int i = 0; i < resourceData.shadowTextures.Length; ++i)
            {
                if (resourceData.shadowTextures[i] == null || resourceData.shadowTextures[i].Length != layerBatches[i].shadowIndices.Count)
                    resourceData.shadowTextures[i] = new TextureHandle[layerBatches[i].shadowIndices.Count];
            }
        }

        void CreateResources(RenderGraph renderGraph)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;
            var cameraTargetFilterMode = FilterMode.Bilinear;
            bool lastCameraInTheStack = cameraData.resolveFinalTarget;

#if UNITY_EDITOR
            // The scene view camera cannot be uninitialized or skybox when using the 2D renderer.
            if (cameraData.cameraType == CameraType.SceneView)
            {
                cameraData.camera.clearFlags = CameraClearFlags.SolidColor;
            }
#endif

            bool forceCreateColorTexture = false;

            // Pixel Perfect Camera doesn't support camera stacking.
            if (cameraData.renderType == CameraRenderType.Base && lastCameraInTheStack)
            {
                cameraData.camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
                if (ppc != null && ppc.enabled)
                {
                    if (ppc.offscreenRTSize != Vector2Int.zero)
                    {
                        forceCreateColorTexture = true;

                        // Pixel Perfect Camera may request a different RT size than camera VP size.
                        // In that case we need to modify cameraTargetDescriptor here so that all the passes would use the same size.
                        cameraTargetDescriptor.width = ppc.offscreenRTSize.x;
                        cameraTargetDescriptor.height = ppc.offscreenRTSize.y;
                    }

                    cameraTargetFilterMode = FilterMode.Point;
                    ppcUpscaleRT = ppc.gridSnapping == PixelPerfectCamera.GridSnapping.UpscaleRenderTexture || ppc.requiresUpscalePass;

                    if (ppc.requiresUpscalePass)
                    {
                        var upscaleDescriptor = cameraTargetDescriptor;
                        upscaleDescriptor.width = ppc.refResolutionX * ppc.pixelRatio;
                        upscaleDescriptor.height = ppc.refResolutionY * ppc.pixelRatio;
                        upscaleDescriptor.depthStencilFormat = GraphicsFormat.None;

                        universal2DResourceData.upscaleTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, upscaleDescriptor, "_UpscaleTexture", true, ppc.finalBlitFilterMode);
                    }
                }
            }

            var renderTextureScale = m_Renderer2DData.lightRenderTextureScale;
            var width = (int)Mathf.Max(1, cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)Mathf.Max(1, cameraData.cameraTargetDescriptor.height * renderTextureScale);

            // Normals and Light textures have to be of the same renderTextureScale, to prevent any sampling artifacts during lighting calculations
            CreateCameraNormalsTextures(renderGraph, cameraTargetDescriptor, width, height);

            CreateLightTextures(renderGraph, width, height);

            CreateShadowTextures(renderGraph, width, height);

            if (m_Renderer2DData.useCameraSortingLayerTexture)
                CreateCameraSortingLayerTexture(renderGraph, cameraTargetDescriptor);

            // Create the attachments
            if (cameraData.renderType == CameraRenderType.Base) // require intermediate textures
            {
                RenderPassInputSummary renderPassInputs = GetRenderPassInputs();
                m_CreateColorTexture = renderPassInputs.requiresColorTexture;
                m_CreateDepthTexture = renderPassInputs.requiresDepthTexture;

                m_CreateColorTexture |= forceCreateColorTexture;

                // RTHandles do not support combining color and depth in the same texture so we create them separately
                m_CreateDepthTexture |= m_CreateColorTexture;

                // Camera Target Color
                if (m_CreateColorTexture)
                {
                    cameraTargetDescriptor.useMipMap = false;
                    cameraTargetDescriptor.autoGenerateMips = false;
                    cameraTargetDescriptor.depthStencilFormat = GraphicsFormat.None;

                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraColorHandles[0], cameraTargetDescriptor, cameraTargetFilterMode, TextureWrapMode.Clamp, name: "_CameraTargetAttachmentA");
                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraColorHandles[1], cameraTargetDescriptor, cameraTargetFilterMode, TextureWrapMode.Clamp, name: "_CameraTargetAttachmentB");
                    commonResourceData.activeColorID = ActiveID.Camera;
                }
                else
                    commonResourceData.activeColorID = ActiveID.BackBuffer;

                // Camera Target Depth
                if (m_CreateDepthTexture)
                {
                    var depthDescriptor = cameraData.cameraTargetDescriptor;
                    depthDescriptor.useMipMap = false;
                    depthDescriptor.autoGenerateMips = false;

                    bool hasMSAA = depthDescriptor.msaaSamples > 1 && (SystemInfo.supportsMultisampledTextures != 0);
                    bool resolveDepth = RenderingUtils.MultisampleDepthResolveSupported() && renderGraph.nativeRenderPassesEnabled;

                    depthDescriptor.bindMS = !resolveDepth && hasMSAA;

                    // binding MS surfaces is not supported by the GLES backend
                    if (IsGLESDevice())
                        depthDescriptor.bindMS = false;

                    if (m_CopyDepthPass != null)
                    {
                        m_CopyDepthPass.MsaaSamples = depthDescriptor.msaaSamples;
                        m_CopyDepthPass.m_CopyResolvedDepth = !depthDescriptor.bindMS;
                    }

                    depthDescriptor.graphicsFormat = GraphicsFormat.None;
                    depthDescriptor.depthStencilFormat = CoreUtils.GetDefaultDepthStencilFormat();

                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraDepthHandle, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
                    commonResourceData.activeDepthID = ActiveID.Camera;
                }
                else
                    commonResourceData.activeDepthID = ActiveID.BackBuffer;
            }
            else // Overlay camera
            {
                cameraData.baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraData);
                var baseRenderer = (Renderer2D)baseCameraData.scriptableRenderer;

                m_RenderGraphCameraColorHandles = baseRenderer.m_RenderGraphCameraColorHandles;
                m_RenderGraphCameraDepthHandle = baseRenderer.m_RenderGraphCameraDepthHandle;
                m_RenderGraphBackbufferColorHandle = baseRenderer.m_RenderGraphBackbufferColorHandle;
                m_RenderGraphBackbufferDepthHandle = baseRenderer.m_RenderGraphBackbufferDepthHandle;

                m_CreateColorTexture = baseRenderer.m_CreateColorTexture;
                m_CreateDepthTexture = baseRenderer.m_CreateDepthTexture;

                commonResourceData.activeColorID = m_CreateColorTexture ? ActiveID.Camera : ActiveID.BackBuffer;
                commonResourceData.activeDepthID = m_CreateDepthTexture ? ActiveID.Camera : ActiveID.BackBuffer;
            }

            ImportResourceSummary importSummary = GetImportResourceSummary(renderGraph, cameraData);

            if (m_CreateColorTexture)
            {
            	importSummary.cameraColorParams.discardOnLastUse = lastCameraInTheStack;
                importSummary.cameraDepthParams.discardOnLastUse = lastCameraInTheStack;

                commonResourceData.cameraColor = renderGraph.ImportTexture(currentRenderGraphCameraColorHandle, importSummary.cameraColorParams);
                commonResourceData.cameraDepth = renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle, importSummary.cameraDepthParams);
            }

            RenderTargetIdentifier targetColorId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.Depth;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                targetColorId = cameraData.xr.renderTarget;
                targetDepthId = cameraData.xr.renderTarget;
            }
#endif

            if (m_RenderGraphBackbufferColorHandle == null)
            {
                m_RenderGraphBackbufferColorHandle = RTHandles.Alloc(targetColorId, "Backbuffer color");
            }
            else if (m_RenderGraphBackbufferColorHandle.nameID != targetColorId)
            {
                RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref m_RenderGraphBackbufferColorHandle, targetColorId);
            }

            if (m_RenderGraphBackbufferDepthHandle == null)
            {
                m_RenderGraphBackbufferDepthHandle = RTHandles.Alloc(targetDepthId, "Backbuffer depth");
            }
            else if (m_RenderGraphBackbufferDepthHandle.nameID != targetDepthId)
            {
                RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref m_RenderGraphBackbufferDepthHandle, targetDepthId);
            }

            commonResourceData.backBufferColor = renderGraph.ImportTexture(m_RenderGraphBackbufferColorHandle, importSummary.importInfo, importSummary.backBufferColorParams);
            commonResourceData.backBufferDepth = renderGraph.ImportTexture(m_RenderGraphBackbufferDepthHandle, importSummary.importInfoDepth, importSummary.backBufferDepthParams);

            if (RequiresDepthCopyPass())
                CreateCameraDepthCopyTexture(renderGraph, cameraTargetDescriptor);
        }

        void CreateCameraNormalsTextures(RenderGraph renderGraph, RenderTextureDescriptor descriptor, int width, int height)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            var desc = new RenderTextureDescriptor(width, height);
            desc.graphicsFormat = RendererLighting.GetRenderTextureFormat();
            desc.autoGenerateMips = false;
            desc.msaaSamples = descriptor.msaaSamples;

            for (int i = 0; i < resourceData.normalsTexture.Length; ++i)
                resourceData.normalsTexture[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_NormalMap", true, RendererLighting.k_NormalClearColor);

            if (m_Renderer2DData.useDepthStencilBuffer)
            {
                // Normals pass can reuse active depth if same dimensions, if not create a new depth texture
#if !(ENABLE_VR && ENABLE_XR_MODULE)
                if (descriptor.width != width || descriptor.height != height)
#endif
                {
                    var normalsDepthDesc = new RenderTextureDescriptor(width, height);
                    normalsDepthDesc.graphicsFormat = GraphicsFormat.None;
                    normalsDepthDesc.autoGenerateMips = false;
                    normalsDepthDesc.msaaSamples = descriptor.msaaSamples;
                    normalsDepthDesc.depthStencilFormat = CoreUtils.GetDefaultDepthStencilFormat();

                    resourceData.normalsDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, normalsDepthDesc, "_NormalDepth", false, FilterMode.Bilinear);
                }
            }
        }

        void CreateLightTextures(RenderGraph renderGraph, int width, int height)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
            var layerBatches = frameData.Get<Universal2DRenderingData>().layerBatches;

            var desc = new RenderTextureDescriptor(width, height);
            desc.graphicsFormat = RendererLighting.GetRenderTextureFormat();
            desc.autoGenerateMips = false;

            for (int i = 0; i < resourceData.lightTextures.Length; ++i)
            {
                for (var j = 0; j < layerBatches[i].activeBlendStylesIndices.Length; ++j)
                {
                    var index = layerBatches[i].activeBlendStylesIndices[j];
                    if (!Light2DManager.GetGlobalColor(layerBatches[i].startLayerID, index, out var clearColor))
                        clearColor = Color.black;

                    resourceData.lightTextures[i][j] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, RendererLighting.k_ShapeLightTextureIDs[index], true, clearColor, FilterMode.Bilinear);
                }
            }
        }

        void CreateShadowTextures(RenderGraph renderGraph, int width, int height)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
            var layerBatches = frameData.Get<Universal2DRenderingData>().layerBatches;

            var shadowDesc = new RenderTextureDescriptor(width, height);
            shadowDesc.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            shadowDesc.autoGenerateMips = false;

            for (int i = 0; i < resourceData.shadowTextures.Length; ++i)
            {
                for (var j = 0; j < layerBatches[i].shadowIndices.Count; ++j)
                {
                    resourceData.shadowTextures[i][j] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, shadowDesc, "_ShadowTex", false, FilterMode.Bilinear);
                }
            }

            var shadowDepthDesc = new RenderTextureDescriptor(width, height);
            shadowDepthDesc.graphicsFormat = GraphicsFormat.None;
            shadowDepthDesc.autoGenerateMips = false;
            shadowDepthDesc.depthStencilFormat = CoreUtils.GetDefaultDepthStencilFormat();

            resourceData.shadowDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, shadowDepthDesc, "_ShadowDepth", false, FilterMode.Bilinear);
        }

        void CreateCameraSortingLayerTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            descriptor.msaaSamples = 1;
            CopyCameraSortingLayerPass.ConfigureDescriptor(m_Renderer2DData.cameraSortingLayerDownsamplingMethod, ref descriptor, out var filterMode);
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_CameraSortingLayerHandle, descriptor, filterMode, TextureWrapMode.Clamp, name: CopyCameraSortingLayerPass.k_CameraSortingLayerTexture);
            resourceData.cameraSortingLayerTexture = renderGraph.ImportTexture(m_CameraSortingLayerHandle);
        }

        bool RequiresDepthCopyPass()
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var renderPassInputs = GetRenderPassInputs();
            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture;
            bool cameraHasPostProcessingWithDepth = cameraData.postProcessEnabled && m_PostProcess != null && cameraData.postProcessingRequiresDepthTexture;
            bool requiresDepthCopyPass = (cameraHasPostProcessingWithDepth || requiresDepthTexture) && m_CreateDepthTexture;

            return requiresDepthCopyPass;
        }

        void CreateCameraDepthCopyTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            CommonResourceData resourceData = frameData.Get<CommonResourceData>();

            var depthDescriptor = descriptor;
            depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
            depthDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
            depthDescriptor.depthStencilFormat = GraphicsFormat.None;

            resourceData.cameraDepthTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "_CameraDepthTexture", true);
        }

        public override void OnBeginRenderGraphFrame()
        {
            Universal2DResourceData universal2DResourceData = frameData.Create<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
			frameData.Create<Universal2DRenderingData>().renderingData = m_Renderer2DData;

			universal2DResourceData.InitFrame();
            commonResourceData.InitFrame();
        }

        internal void RecordCustomRenderGraphPasses(RenderGraph renderGraph, RenderPassEvent2D eventStart, int batchIndex = -1)
        {
            foreach (ScriptableRenderPass pass in activeRenderPassQueue)
            {
                var batchCount = frameData.Get<Universal2DRenderingData>().batchCount;
                var isBatchValid = batchIndex >= 0 && batchIndex < batchCount;

                pass.GetInjectionPoint2D(out RenderPassEvent2D rpEvent, out int layerID);
                int range = ScriptableRenderPass2D.GetRenderPassEventRange(eventStart);

                // Account for render passes in between events.
                if (rpEvent >= eventStart && rpEvent < eventStart + range)
                {
                    // Invalid sorting layer specified for render pass
                    if (!SortingLayer.IsValid(layerID) && ScriptableRenderPass2D.IsSortingLayerEvent(rpEvent))
                    {
                        Debug.Assert(SortingLayer.IsValid(layerID), SortingLayer.IDToName(layerID) + " is not a valid Sorting Layer.");
                    }
                    // BatchIndex is valid
                    else if (isBatchValid)
                    {
                        var layerBatch = frameData.Get<Universal2DRenderingData>().layerBatches[batchIndex];

                        if (layerBatch.IsValueWithinLayerRange(SortingLayer.GetLayerValueFromID(layerID)))
                            pass.RecordRenderGraph(renderGraph, frameData);
                    }
                    else
                        pass.RecordRenderGraph(renderGraph, frameData);
                }
            }
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            InitializeLayerBatches();

            CreateResources(renderGraph);

            DebugHandler?.Setup(renderGraph, cameraData.isPreviewCamera);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRendering);

            SetupRenderGraphCameraProperties(renderGraph, commonResourceData.activeColorTexture);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            ProcessVFXCameraCommand(renderGraph);
#endif

            OnBeforeRendering(renderGraph);

            BeginRenderGraphXRRendering(renderGraph);

            OnMainRendering(renderGraph);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRenderingPostProcessing);

            OnAfterRendering(renderGraph);

            EndRenderGraphXRRendering(renderGraph);
        }

        public override void OnEndRenderGraphFrame()
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            universal2DResourceData.EndFrame();
            commonResourceData.EndFrame();
        }

        internal override void OnFinishRenderGraphRendering(CommandBuffer cmd)
        {
            m_CopyDepthPass?.OnCameraCleanup(cmd);
        }

        private void OnBeforeRendering(RenderGraph renderGraph)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<Universal2DRenderingData>().renderingData;

            m_LightPass.Setup(renderGraph, ref renderingData);

            // Before rendering the lights cache some values that are expensive to get/calculate
            var culledLights = renderingData.lightCullResult.visibleLights;
            for (var i = 0; i < culledLights.Count; i++)
            {
                culledLights[i].CacheValues();
            }

            ShadowCasterGroup2DManager.CacheValues();
            ShadowRendering.CallOnBeforeRender(cameraData.camera, renderingData.lightCullResult);

            RendererLighting.lightBatch.Reset();
        }

        private void OnMainRendering(RenderGraph renderGraph)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            var layerBatches = frameData.Get<Universal2DRenderingData>().layerBatches;
            var batchCount = frameData.Get<Universal2DRenderingData>().batchCount;

            // Color Grading LUT
            bool requiredColorGradingLutPass = cameraData.postProcessEnabled && m_PostProcess != null;

            if (requiredColorGradingLutPass)
            {
                TextureHandle internalColorLut = TextureHandle.nullHandle;
                m_ColorGradingLutPassRenderGraph.Render(renderGraph, frameData, out internalColorLut);
                commonResourceData.internalColorLut = internalColorLut;
            }

            var cameraSortingLayerBoundsIndex = m_Renderer2DData.GetCameraSortingLayerBoundsIndex();

            bool useLights = false;
            for (int i = 0; i < batchCount; ++i)
                useLights |= layerBatches[i].lightStats.useLights;

            // Set Global Properties and Textures
            GlobalPropertiesPass.Setup(renderGraph, frameData, useLights);

            // Main render passes

            // Normal Pass
            for (var i = 0; i < batchCount; i++)
            {
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRenderingNormals, i);

                m_NormalPass.Render(renderGraph, frameData, i);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.AfterRenderingNormals, i);
            }

            // Shadow Pass (TODO: Optimize RT swapping between shadow and light textures)
            for (var i = 0; i < batchCount; i++)
            {
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRenderingShadows, i);

                m_ShadowPass.Render(renderGraph, frameData, i);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.AfterRenderingShadows, i);
            }

            // Light Pass
            for (var i = 0; i < batchCount; i++)
            {
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRenderingLights, i);

                m_LightPass.Render(renderGraph, frameData, i);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.AfterRenderingLights, i);
            }

            // Default Render Pass
            for (var i = 0; i < batchCount; i++)
            {
                if (!renderGraph.nativeRenderPassesEnabled && i == 0)
                {
                    RTClearFlags clearFlags = (RTClearFlags)GetCameraClearFlag(cameraData);
                    if (clearFlags != RTClearFlags.None)
                        ClearTargetsPass.Render(renderGraph, commonResourceData.activeColorTexture, commonResourceData.activeDepthTexture, clearFlags, cameraData.backgroundColor);
                }

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRenderingSprites, i);


                LayerUtility.GetFilterSettings(m_Renderer2DData, layerBatches[i], out var filterSettings);
                m_RendererPass.Render(renderGraph, frameData, i, ref filterSettings);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.AfterRenderingSprites, i);

                // Shadow Volumetric Pass
                m_ShadowPass.Render(renderGraph, frameData, i, true);

                // Light Volumetric Pass
                m_LightPass.Render(renderGraph, frameData, i, true);

                // Camera Sorting Layer Pass
                if (m_Renderer2DData.useCameraSortingLayerTexture)
                {
                    ref var layerBatch = ref layerBatches[i];

                    if (layerBatch.IsValueWithinLayerRange(cameraSortingLayerBoundsIndex))
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, frameData);
                    }
                }
            }

            if (RequiresDepthCopyPass())
                m_CopyDepthPass?.Render(renderGraph, frameData, commonResourceData.cameraDepthTexture, commonResourceData.activeDepthTexture, true);

            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && outputToHDR)
            {
                TextureHandle overlayUI;
                m_DrawOffscreenUIPass.RenderOffscreen(renderGraph, frameData, CoreUtils.GetDefaultDepthStencilFormat(), out overlayUI);
                commonResourceData.overlayUITexture = overlayUI;
            }
        }

        private void OnAfterRendering(RenderGraph renderGraph)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, commonResourceData.activeColorTexture, commonResourceData.activeDepthTexture, GizmoSubset.PreImageEffects);

            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);

            //When the debughandler displays HDR debug views, it needs to redirect (final) post-process output to an intermediate color target (debugScreenTexture).
            //Therefore, we swap the backbuffer textures for the debug screen textures such that the next post processing passes don't need to be aware of the debug handler at all.
            //At the end, when the handler is active, we swap them back. This isolates the debug handler code from the sequence of post process passes and is a common pattern to
            //use the resourceData.
            var debugRealBackBufferColor = TextureHandle.nullHandle;
            var debugRealBackBufferDepth = TextureHandle.nullHandle;

            // Allocate debug screen texture if the debug mode needs it.
            if (resolveToDebugScreen)
            {
                debugRealBackBufferColor = commonResourceData.backBufferColor;
                debugRealBackBufferDepth = commonResourceData.backBufferDepth;

                RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureColorDescriptorForDebugScreen(ref colorDesc, cameraData.pixelWidth, cameraData.pixelHeight);
                commonResourceData.backBufferColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDesc, "_DebugScreenColor", false);

                RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, CoreUtils.GetDefaultDepthStencilFormat(), cameraData.pixelWidth, cameraData.pixelHeight);
                commonResourceData.backBufferDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "_DebugScreenDepth", false);
            }

            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcess != null;
            bool anyPostProcessing = postProcessingData.isEnabled && m_PostProcess != null;

            cameraData.camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
            bool requirePixelPerfectUpscale = IsPixelPerfectCameraEnabled(cameraData) && ppc.requiresUpscalePass;

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool applyFinalPostProcessing = cameraData.resolveFinalTarget && !ppcUpscaleRT && anyPostProcessing && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing || (x as ScriptableRenderPass2D)?.renderPassEvent2D == RenderPassEvent2D.AfterRenderingPostProcessing) != null;
            bool needsColorEncoding = !resolveToDebugScreen;

            // Don't resolve during post processing if there are passes after or pixel perfect camera is used
            bool pixelPerfectCameraEnabled = ppc != null && ppc.enabled;
            bool hasCaptureActions = cameraData.captureActions != null && cameraData.resolveFinalTarget;
            bool resolvePostProcessingToCameraTarget = cameraData.resolveFinalTarget && !hasCaptureActions && !hasPassesAfterPostProcessing && !applyFinalPostProcessing && !pixelPerfectCameraEnabled;
            bool doSRGBEncoding = resolvePostProcessingToCameraTarget && needsColorEncoding;

            if (applyPostProcessing)
            {
                bool isTargetBackbuffer = resolvePostProcessingToCameraTarget;
                TextureHandle target;
               
                if(isTargetBackbuffer)
                {
                    target = commonResourceData.backBufferColor;
                }              
                else
                {
                    // if the postprocessing pass is trying to read and write to the same CameraColor target, we need to swap so it writes to a different target,
                    // since reading a pass attachment is not possible. Normally this would be possible using temporary RenderGraph managed textures.
                    // The reason why in this case we need to use "external" RTHandles is to preserve the results for camera stacking.
                    // TODO RENDERGRAPH: Once all cameras will run in a single RenderGraph we can just use temporary RenderGraph textures as intermediate buffer.
                    ImportResourceParams importColorParams = new ImportResourceParams();
                    importColorParams.clearOnFirstUse = true;
                    importColorParams.clearColor = Color.black;
                    importColorParams.discardOnLastUse = cameraData.resolveFinalTarget;  // check if last camera in the stack

                    target = renderGraph.ImportTexture(nextRenderGraphCameraColorHandle, importColorParams);
                }

                //We always pass a valid target because it's alway persistent. However, we still set target to the output to be correct when above code would change. So output handle is equal to input target now. See OnAfterRendering in UnversalRenderer for more context.
                target = m_PostProcess.RenderPostProcessing(
                    renderGraph,
                    frameData,
                    commonResourceData.cameraColor,
                    commonResourceData.internalColorLut,
                    commonResourceData.overlayUITexture,
                    in target,
                    applyFinalPostProcessing,
                    doSRGBEncoding);

                //Always make the switch after the pass has recorded the blit to the backbuffer, not before.
                if (isTargetBackbuffer)
                {
                    commonResourceData.SwitchActiveTexturesToBackbuffer();
                }
                else
                {
                    commonResourceData.cameraColor = target;
                }
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.AfterRenderingPostProcessing);                      

            // Do PixelPerfect upscaling when using the Stretch Fill option
            if (requirePixelPerfectUpscale)
            {
                m_UpscalePass.Render(renderGraph, cameraData.camera, commonResourceData.cameraColor, universal2DResourceData.upscaleTexture);
                commonResourceData.cameraColor = universal2DResourceData.upscaleTexture;
            } 

            if (applyFinalPostProcessing)
            {
                m_PostProcess.RenderFinalPostProcessing(renderGraph, frameData, commonResourceData.cameraColor, commonResourceData.overlayUITexture, commonResourceData.backBufferColor, needsColorEncoding);

                commonResourceData.SwitchActiveTexturesToBackbuffer();
            }

            if (!commonResourceData.isActiveTargetBackBuffer && cameraData.resolveFinalTarget)
            {
                m_FinalBlitPass.Render(renderGraph, frameData, cameraData, commonResourceData.cameraColor, commonResourceData.backBufferColor, commonResourceData.overlayUITexture);

                commonResourceData.SwitchActiveTexturesToBackbuffer();
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.AfterRendering);

            // We can explicitly render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            bool shouldRenderUI = cameraData.rendersOverlayUI && cameraData.isLastBaseCamera;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && !outputToHDR)
            {
                m_DrawOverlayUIPass.RenderOverlay(renderGraph, frameData, commonResourceData.backBufferColor, commonResourceData.backBufferDepth);
            }               

            // If HDR debug views are enabled, debugHandler will perform the blit from debugScreenColor (== finalColorHandle) to backBufferColor.
            if (resolveToDebugScreen)
            {
                debugHandler.Render(renderGraph, cameraData, commonResourceData.backBufferColor, commonResourceData.overlayUITexture, debugRealBackBufferColor);

                //Swapping the backbuffer textures back
                commonResourceData.backBufferColor = debugRealBackBufferColor;
                commonResourceData.backBufferDepth = debugRealBackBufferDepth;
            }           

            if (cameraData.resolveFinalTarget)
            {
                if (cameraData.isSceneViewCamera)
                    DrawRenderGraphWireOverlay(renderGraph, frameData, commonResourceData.activeColorTexture);

                if (drawGizmos)
                    DrawRenderGraphGizmos(renderGraph, frameData, commonResourceData.activeColorTexture, commonResourceData.activeDepthTexture, GizmoSubset.PostImageEffects);
            }
        }

        public Renderer2DData GetRenderer2DData()
        {
            return m_Renderer2DData;
        }

        protected override void Dispose(bool disposing)
        {
            CleanupRenderGraphResources();

#if URP_COMPATIBILITY_MODE
            CleanupCompatibilityModeResources();
#endif

            base.Dispose(disposing);
        }

        private void CleanupRenderGraphResources()
        {
            m_Renderer2DData.Dispose();
            m_UpscalePass.Dispose();
            m_CopyDepthPass?.Dispose();
            m_FinalBlitPass?.Dispose();
            m_DrawOffscreenUIPass?.Dispose();
            m_DrawOverlayUIPass?.Dispose();
            m_PostProcess?.Dispose();
            m_ColorGradingLutPassRenderGraph?.Cleanup();

            m_RenderGraphCameraColorHandles[0]?.Release();
            m_RenderGraphCameraColorHandles[1]?.Release();
            m_RenderGraphCameraDepthHandle?.Release();
            m_RenderGraphBackbufferColorHandle?.Release();
            m_RenderGraphBackbufferDepthHandle?.Release();
            m_CameraSortingLayerHandle?.Release();

            Light2DManager.Dispose();
            Light2DLookupTexture.Release();

            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_BlitHDRMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);

#if ENABLE_VR && ENABLE_XR_MODULE
            XRSystem.Dispose();
#endif
        }

        internal static bool IsGLESDevice()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
        }

        internal static bool supportsMRT
        {
            get => !IsGLESDevice();
        }

        internal override bool supportsNativeRenderPassRendergraphCompiler => true;
    }
}
