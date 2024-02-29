using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using static UnityEngine.Rendering.Universal.UniversalResourceDataBase;

using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal enum Renderer2DResource
    {
        BackBufferColor,
        BackBufferDepth,

        // intermediate camera targets
        CameraColor,
        CameraDepth,

        // intermediate depth for usage in passes with render texture scale
        IntermediateDepth,

        LightTexture0,
        LightTexture1,
        LightTexture2,
        LightTexture3,

        NormalsTexture,
        ShadowsTexture,
        UpscaleTexture,
        CameraSortingLayerTexture,

        InternalColorLut,
        AfterPostProcessColor,
        OverlayUITexture,
        DebugScreenColor,
        DebugScreenDepth
    }

    internal sealed partial class Renderer2D : ScriptableRenderer
    {
        RTHandle m_RenderGraphCameraColorHandle;
        RTHandle m_RenderGraphCameraDepthHandle;
        RTHandle m_RenderGraphBackbufferColorHandle;
        RTHandle m_RenderGraphBackbufferDepthHandle;
        RTHandle m_CameraSortingLayerHandle;

        DrawNormal2DPass m_NormalPass = new DrawNormal2DPass();
        DrawLight2DPass m_LightPass = new DrawLight2DPass();
        DrawShadow2DPass m_ShadowPass = new DrawShadow2DPass();
        DrawRenderer2DPass m_RendererPass = new DrawRenderer2DPass();

        LayerBatch[] m_LayerBatches;
        int m_BatchCount;

        bool ppcUpscaleRT = false;

        private struct ImportResourceSummary
        {
            internal RenderTargetInfo importInfo;
            internal RenderTargetInfo importInfoDepth;
            internal ImportResourceParams cameraColorParams;
            internal ImportResourceParams cameraDepthParams;
            internal ImportResourceParams backBufferColorParams;
            internal ImportResourceParams backBufferDepthParams;
        }

        ImportResourceSummary GetImportResourceSummary(RenderGraph renderGraph, UniversalCameraData cameraData)
        {
            ImportResourceSummary output = new ImportResourceSummary();

            bool clearColor = cameraData.renderType == CameraRenderType.Base;
            bool clearDepth = cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth;
            bool clearBackbufferOnFirstUse = (cameraData.renderType == CameraRenderType.Base) && !m_CreateColorTexture;

            // if the camera background type is "uninitialized" clear using a yellow color, so users can clearly understand the underlying behaviour
            Color cameraBackgroundColor = (cameraData.camera.clearFlags == CameraClearFlags.Nothing) ? Color.yellow : cameraData.backgroundColor;

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
            }

            output.cameraColorParams.clearOnFirstUse = clearColor;
            output.cameraColorParams.clearColor = cameraBackgroundColor;
            output.cameraColorParams.discardOnLastUse = false;

            output.cameraDepthParams.clearOnFirstUse = clearDepth;
            output.cameraDepthParams.clearColor = cameraBackgroundColor;
            output.cameraDepthParams.discardOnLastUse = false;

            output.backBufferColorParams.clearOnFirstUse = clearBackbufferOnFirstUse;
            output.backBufferColorParams.clearColor = cameraBackgroundColor;
            output.backBufferColorParams.discardOnLastUse = false;

            output.backBufferDepthParams.clearOnFirstUse = clearBackbufferOnFirstUse;
            output.backBufferDepthParams.clearColor = cameraBackgroundColor;
            output.backBufferDepthParams.discardOnLastUse = true;

            if (cameraData.targetTexture != null)
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
                    Debug.LogWarning("Trying to render to a rendertexture without a depth buffer. URP+RG needs a depthbuffer to render.");
                }
            }
            else
            {
                bool msaaSamplesChangedThisFrame = false;
#if !UNITY_EDITOR
                // for safety do this only for the NRP path, even though works also on non NRP, but would need extensive testing
                if (m_CreateColorTexture && renderGraph.nativeRenderPassesEnabled && Screen.msaaSamples > 1)
                {
                    msaaSamplesChangedThisFrame = true;
                    Screen.SetMSAASamples(1);
                }
#endif
                int numSamples = Mathf.Max(Screen.msaaSamples, 1);

                // Handle edge cases regarding numSamples setup
                // On OSX player, the Screen API MSAA samples change request is only applied in the following frame,
                // as a workaround we keep the old MSAA sample count for the previous frame
                // this workaround can be removed once the Screen API issue (UUM-42825) is fixed
                // The editor always allocates the system rendertarget with a single msaa sample
                // See: ConfigureTargetTexture in PlayModeView.cs
                if (msaaSamplesChangedThisFrame && Application.platform == RuntimePlatform.OSXPlayer)
                    numSamples = cameraData.cameraTargetDescriptor.msaaSamples;
                else if (Application.isEditor)
                    numSamples = 1;

                //NOTE: Careful what you use here as many of the properties bake-in the camera rect so for example
                //cameraData.cameraTargetDescriptor.width is the width of the rectangle but not the actual render target
                //same with cameraData.camera.pixelWidth
                output.importInfo.width = Screen.width;
                output.importInfo.height = Screen.height;
                output.importInfo.volumeDepth = 1;
                output.importInfo.msaaSamples = numSamples;
                output.importInfo.format = UniversalRenderPipeline.MakeRenderTextureGraphicsFormat(cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, Graphics.preserveFramebufferAlpha);

                output.importInfoDepth = output.importInfo;
                output.importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }

            return output;
        }

        void InitializeLayerBatches()
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            m_LayerBatches = LayerUtility.CalculateBatches(m_Renderer2DData.lightCullResult, out m_BatchCount);

            // Initialize textures dependent on batch size
            if (resourceData.normalsTexture.Length != m_BatchCount)
                resourceData.normalsTexture = new TextureHandle[m_BatchCount];

            if (resourceData.lightTextures.Length != m_BatchCount)
                resourceData.lightTextures = new TextureHandle[m_BatchCount][];

            // Initialize light textures based on active blend styles to save on resources
            for (int i = 0; i < resourceData.lightTextures.Length; ++i)
            {
                if (resourceData.lightTextures[i] == null || resourceData.lightTextures[i].Length != m_LayerBatches[i].activeBlendStylesIndices.Length)
                    resourceData.lightTextures[i] = new TextureHandle[m_LayerBatches[i].activeBlendStylesIndices.Length];
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
                        upscaleDescriptor.depthBufferBits = 0;

                        universal2DResourceData.upscaleTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, upscaleDescriptor, "_UpscaleTexture", true, ppc.finalBlitFilterMode);
                    }
                }
            }

            var renderTextureScale = m_Renderer2DData.lightRenderTextureScale;
            var width = (int)(cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(cameraData.cameraTargetDescriptor.height * renderTextureScale);

            // Intermediate depth desc (size of renderTextureScale)
            {
                var depthDescriptor = new RenderTextureDescriptor(width, height);
                depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                depthDescriptor.depthBufferBits = k_DepthBufferBits;
                depthDescriptor.width = width;
                depthDescriptor.height = height;

                universal2DResourceData.intermediateDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "DepthTexture", true);
            }

            // Normal and Light desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = RendererLighting.GetRenderTextureFormat();
                desc.autoGenerateMips = false;
                desc.depthBufferBits = 0;

                for (int i = 0; i < universal2DResourceData.normalsTexture.Length; ++i)
                    universal2DResourceData.normalsTexture[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_NormalMap", true, RendererLighting.k_NormalClearColor);

                for (int i = 0; i < universal2DResourceData.lightTextures.Length; ++i)
                {
                    for (var j = 0; j < m_LayerBatches[i].activeBlendStylesIndices.Length; ++j)
                    {
                        var index = m_LayerBatches[i].activeBlendStylesIndices[j];
                        if (!Light2DManager.GetGlobalColor(m_LayerBatches[i].startLayerID, index, out var clearColor))
                            clearColor = Color.black;

                        universal2DResourceData.lightTextures[i][j] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, RendererLighting.k_ShapeLightTextureIDs[j], true, clearColor, FilterMode.Bilinear);
                    }
                }
            }

            // Shadow desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                desc.autoGenerateMips = false;
                desc.depthBufferBits = 0;

                universal2DResourceData.shadowsTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ShadowTex", false, FilterMode.Bilinear);
            }

            // Shadow depth desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = GraphicsFormat.None;
                desc.autoGenerateMips = false;
                desc.depthBufferBits = k_DepthBufferBits;

                universal2DResourceData.shadowsDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ShadowDepth", false, FilterMode.Bilinear);
            }

            // Camera Sorting Layer desc
            if (m_Renderer2DData.useCameraSortingLayerTexture)
            {
                var descriptor = cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                CopyCameraSortingLayerPass.ConfigureDescriptor(m_Renderer2DData.cameraSortingLayerDownsamplingMethod, ref descriptor, out var filterMode);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_CameraSortingLayerHandle, descriptor, filterMode, TextureWrapMode.Clamp, name: CopyCameraSortingLayerPass.k_CameraSortingLayerTexture);
                universal2DResourceData.cameraSortingLayerTexture = renderGraph.ImportTexture(m_CameraSortingLayerHandle);
            }

            // now create the attachments
            if (cameraData.renderType == CameraRenderType.Base) // require intermediate textures
            {
                RenderPassInputSummary renderPassInputs = GetRenderPassInputs(cameraData);
                m_CreateColorTexture = renderPassInputs.requiresColorTexture;
                m_CreateDepthTexture = renderPassInputs.requiresDepthTexture;

                m_CreateColorTexture |= forceCreateColorTexture;

                // RTHandles do not support combining color and depth in the same texture so we create them separately
                m_CreateDepthTexture |= createColorTexture;

                // Camera Target Color
                if (createColorTexture)
                {
                    cameraTargetDescriptor.useMipMap = false;
                    cameraTargetDescriptor.autoGenerateMips = false;
                    cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraColorHandle, cameraTargetDescriptor, cameraTargetFilterMode, TextureWrapMode.Clamp, name: "_CameraTargetAttachment");
                    commonResourceData.activeColorID = ActiveID.Camera;
                }
                else
                    commonResourceData.activeColorID = ActiveID.BackBuffer;

                // Camera Target Depth
                if (createDepthTexture)
                {
                    var depthDescriptor = cameraData.cameraTargetDescriptor;
                    depthDescriptor.useMipMap = false;
                    depthDescriptor.autoGenerateMips = false;
                    if (!lastCameraInTheStack && m_UseDepthStencilBuffer)
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);

                    depthDescriptor.graphicsFormat = GraphicsFormat.None;
                    depthDescriptor.depthStencilFormat = k_DepthStencilFormat;

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

                m_RenderGraphCameraColorHandle = baseRenderer.m_RenderGraphCameraColorHandle;
                m_RenderGraphCameraDepthHandle = baseRenderer.m_RenderGraphCameraDepthHandle;
                m_RenderGraphBackbufferColorHandle = baseRenderer.m_RenderGraphBackbufferColorHandle;
                m_RenderGraphBackbufferDepthHandle = baseRenderer.m_RenderGraphBackbufferDepthHandle;

                m_CreateColorTexture = baseRenderer.m_CreateColorTexture;
                m_CreateDepthTexture = baseRenderer.m_CreateDepthTexture;
            }

            ImportResourceSummary importSummary = GetImportResourceSummary(renderGraph, cameraData);

            if (m_CreateColorTexture)
            {
            	importSummary.cameraColorParams.discardOnLastUse = lastCameraInTheStack;
                importSummary.cameraDepthParams.discardOnLastUse = lastCameraInTheStack;

                commonResourceData.cameraColor = renderGraph.ImportTexture(m_RenderGraphCameraColorHandle, importSummary.cameraColorParams);
                commonResourceData.cameraDepth = renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle, importSummary.cameraDepthParams);
            }

            RenderTargetIdentifier targetColorId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.Depth;

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

            var postProcessDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat, DepthBits.None);
            commonResourceData.afterPostProcessColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, postProcessDesc, "_AfterPostProcessTexture", true);
        }

        public override void OnBeginRenderGraphFrame()
        {
            Universal2DResourceData universal2DResourceData = frameData.Create<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.GetOrCreate<CommonResourceData>();
            universal2DResourceData.InitFrame();
            commonResourceData.InitFrame();
        }

        internal void RecordCustomRenderGraphPasses(RenderGraph renderGraph, RenderPassEvent2D activeRPEvent)
        {
            foreach (ScriptableRenderPass pass in activeRenderPassQueue)
            {
                pass.GetInjectionPoint2D(out RenderPassEvent2D rpEvent, out int rpLayer);

                if (rpEvent == activeRPEvent)
                    pass.RecordRenderGraph(renderGraph, frameData);
            }
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            CommonResourceData commonResourceData = frameData.GetOrCreate<CommonResourceData>();

            InitializeLayerBatches();

            CreateResources(renderGraph);

            SetupRenderGraphCameraProperties(renderGraph, commonResourceData.isActiveTargetBackBuffer);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            ProcessVFXCameraCommand(renderGraph);
#endif

            OnBeforeRendering(renderGraph);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRendering);
            OnMainRendering(renderGraph);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.BeforeRenderingPostProcessing);
            OnAfterRendering(renderGraph);

        }

        public override void OnEndRenderGraphFrame()
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            universal2DResourceData.EndFrame();
            commonResourceData.EndFrame();
        }

        private void OnBeforeRendering(RenderGraph renderGraph)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            m_LightPass.Setup(renderGraph, ref m_Renderer2DData);

            // Before rendering the lights cache some values that are expensive to get/calculate
            var culledLights = m_Renderer2DData.lightCullResult.visibleLights;
            for (var i = 0; i < culledLights.Count; i++)
            {
                culledLights[i].CacheValues();
            }

            ShadowCasterGroup2DManager.CacheValues();
            ShadowRendering.CallOnBeforeRender(cameraData.camera, m_Renderer2DData.lightCullResult);

            RendererLighting.lightBatch.Reset();
        }

        private void OnMainRendering(RenderGraph renderGraph)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Color Grading LUT
            bool requiredColorGradingLutPass = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            if (requiredColorGradingLutPass)
            {
                TextureHandle internalColorLut;
                m_PostProcessPasses.colorGradingLutPass.Render(renderGraph, frameData, out internalColorLut);
                commonResourceData.internalColorLut = internalColorLut;
            }

            var cameraSortingLayerBoundsIndex = Render2DLightingPass.GetCameraSortingLayerBoundsIndex(m_Renderer2DData);

            // Main render passes

            // Normal Pass
            for (var i = 0; i < m_BatchCount; i++)
                m_NormalPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches[i], i);

            // Shadow Pass (TODO: Optimize RT swapping between shadow and light textures)
            for (var i = 0; i < m_BatchCount; i++)
                m_ShadowPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches[i], i);

            // Light Pass
            for (var i = 0; i < m_BatchCount; i++)
                m_LightPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches[i], i);

            // Default Render Pass
            for (var i = 0; i < m_BatchCount; i++)
            {
                if (!renderGraph.nativeRenderPassesEnabled && i == 0)
                {
                    RTClearFlags clearFlags = (RTClearFlags)GetCameraClearFlag(cameraData);
                    if (clearFlags != RTClearFlags.None)
                        ClearTargetsPass.Render(renderGraph, commonResourceData.activeColorTexture, commonResourceData.activeDepthTexture, clearFlags, cameraData.backgroundColor);
                }

                ref var layerBatch = ref m_LayerBatches[i];

                LayerUtility.GetFilterSettings(m_Renderer2DData, ref m_LayerBatches[i], cameraSortingLayerBoundsIndex, out var filterSettings);
                m_RendererPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches, i, ref filterSettings);

                // Shadow Volumetric Pass
                m_ShadowPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches[i], i, true);

                // Light Volumetric Pass
                m_LightPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches[i], i, true);

                // Camera Sorting Layer Pass
                if (m_Renderer2DData.useCameraSortingLayerTexture)
                {
                    // Split Render Pass if CameraSortingLayer is in the middle of a batch
                    if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex < layerBatch.layerRange.upperBound)
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, commonResourceData.activeColorTexture, universal2DResourceData.cameraSortingLayerTexture);

                        filterSettings.sortingLayerRange = new SortingLayerRange((short)(cameraSortingLayerBoundsIndex + 1), layerBatch.layerRange.upperBound);
                        m_RendererPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches, i, ref filterSettings);
                    }
                    else if (cameraSortingLayerBoundsIndex == layerBatch.layerRange.upperBound)
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, commonResourceData.activeColorTexture, universal2DResourceData.cameraSortingLayerTexture);
                    }
                }
            }

            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && outputToHDR)
            {
                TextureHandle overlayUI;
                m_DrawOffscreenUIPass.RenderOffscreen(renderGraph, frameData, k_DepthStencilFormat, out overlayUI);
                commonResourceData.overlayUITexture = overlayUI;
            }
        }

        private void OnAfterRendering(RenderGraph renderGraph)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, commonResourceData.activeColorTexture, commonResourceData.activeDepthTexture, GizmoSubset.PreImageEffects);

            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            // Allocate debug screen texture if the debug mode needs it.
            if (resolveToDebugScreen)
            {
                RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureColorDescriptorForDebugScreen(ref colorDesc, cameraData.pixelWidth, cameraData.pixelHeight);
                commonResourceData.debugScreenColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDesc, "_DebugScreenColor", false);

                RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, k_DepthStencilFormat, cameraData.pixelWidth, cameraData.pixelHeight);
                commonResourceData.debugScreenDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "_DebugScreenDepth", false);
            }

            bool applyPostProcessing = postProcessingData.isEnabled && m_PostProcessPasses.isCreated;

            cameraData.camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
            bool isPixelPerfectCameraEnabled = ppc != null && ppc.enabled && ppc.cropFrame != PixelPerfectCamera.CropFrame.None;
            bool requirePixelPerfectUpscale = isPixelPerfectCameraEnabled && ppc.requiresUpscalePass;

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool requireFinalPostProcessPass = cameraData.resolveFinalTarget && !ppcUpscaleRT && applyPostProcessing && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;
            bool needsColorEncoding = DebugHandler == null || !DebugHandler.HDRDebugViewIsActive(cameraData.resolveFinalTarget);

            var finalColorHandle = commonResourceData.activeColorTexture;

            if (applyPostProcessing)
            {
                postProcessPass.RenderPostProcessingRenderGraph(
                    renderGraph,
                    frameData,
                    commonResourceData.activeColorTexture,
                    commonResourceData.internalColorLut,
                    commonResourceData.overlayUITexture,
                    commonResourceData.afterPostProcessColor,
                    requireFinalPostProcessPass,
                    resolveToDebugScreen,
                    needsColorEncoding);
                finalColorHandle = commonResourceData.afterPostProcessColor;
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent2D.AfterRenderingPostProcessing);
            // Do PixelPerfect upscaling when using the Stretch Fill option
            if (requirePixelPerfectUpscale)
            {
                m_UpscalePass.Render(renderGraph, cameraData.camera, in finalColorHandle, universal2DResourceData.upscaleTexture);
                finalColorHandle = universal2DResourceData.upscaleTexture;
            }

            // We need to switch the "final" blit target to debugScreenColor if HDR debug views are enabled.
            var finalBlitTarget = resolveToDebugScreen ? commonResourceData.debugScreenColor : commonResourceData.backBufferColor;
            var finalDepthHandle = resolveToDebugScreen ? commonResourceData.debugScreenDepth : commonResourceData.backBufferDepth;

            if (createColorTexture)
            {
                if (requireFinalPostProcessPass)
                    postProcessPass.RenderFinalPassRenderGraph(renderGraph, frameData, in finalColorHandle, commonResourceData.overlayUITexture, in finalBlitTarget, needsColorEncoding);
                else
                    m_FinalBlitPass.Render(renderGraph, cameraData, finalColorHandle, finalBlitTarget, commonResourceData.overlayUITexture);

                finalColorHandle = finalBlitTarget;
            }

            // We can explicitly render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && !outputToHDR)
                m_DrawOverlayUIPass.RenderOverlay(renderGraph, frameData, in finalColorHandle, in finalDepthHandle);

            // If HDR debug views are enabled, DebugHandler will perform the blit from debugScreenColor (== finalColorHandle) to backBufferColor.
            DebugHandler?.Setup(renderGraph, cameraData.isPreviewCamera);
            DebugHandler?.Render(renderGraph, cameraData, finalColorHandle, commonResourceData.overlayUITexture, commonResourceData.backBufferColor);

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, commonResourceData.backBufferColor, commonResourceData.activeDepthTexture, GizmoSubset.PostImageEffects);
        }

        private void CleanupRenderGraphResources()
        {
            m_RenderGraphCameraColorHandle?.Release();
            m_RenderGraphCameraDepthHandle?.Release();
            m_RenderGraphBackbufferColorHandle?.Release();
            m_RenderGraphBackbufferDepthHandle?.Release();
            m_CameraSortingLayerHandle?.Release();
            m_LightPass.Dispose();
        }
    }
}
