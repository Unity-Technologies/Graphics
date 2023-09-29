#if UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    #define UNITY_ON_METAL
#endif

using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using static UnityEngine.Rendering.Universal.UniversalResourceDataBase;

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

    internal sealed partial class Renderer2D
    {
        TextureHandle[] m_LightTextureHandles = new TextureHandle[RendererLighting.k_ShapeLightTextureIDs.Length];
        RTHandle m_RenderGraphCameraColorHandle;
        RTHandle m_RenderGraphCameraDepthHandle;
        RTHandle m_RenderGraphBackbufferColorHandle;
        RTHandle m_RenderGraphBackbufferDepthHandle;
        RTHandle m_CameraSortingLayerHandle;

        DrawNormal2DPass m_NormalPass = new DrawNormal2DPass();
        DrawLight2DPass m_LightPass = new DrawLight2DPass();
        DrawShadow2DPass m_ShadowPass = new DrawShadow2DPass();
        DrawRenderer2DPass m_RendererPass = new DrawRenderer2DPass();

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
                if (m_CreateColorTexture && renderGraph.NativeRenderPassesEnabled && Screen.msaaSamples > 1)
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

        void CreateResources(RenderGraph renderGraph)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
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

                        resourceData.upscaleTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, upscaleDescriptor, "_UpscaleTexture", true, ppc.finalBlitFilterMode);
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

                resourceData.intermediateDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "DepthTexture", true);
            }

            // Normal and Light desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = RendererLighting.GetRenderTextureFormat();
                desc.autoGenerateMips = false;
                desc.depthBufferBits = 0;

                resourceData.normalsTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_NormalMap", true);

                for (var i = 0; i < RendererLighting.k_ShapeLightTextureIDs.Length; i++)
                    m_LightTextureHandles[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, RendererLighting.k_ShapeLightTextureIDs[i], false, FilterMode.Bilinear);

                resourceData.lightTextures = m_LightTextureHandles;
            }

            // Shadow desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                desc.autoGenerateMips = false;
                desc.depthBufferBits = 0;

                resourceData.shadowsTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ShadowTex", false, FilterMode.Bilinear);
            }

            // Shadow depth desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = GraphicsFormat.None;
                desc.autoGenerateMips = false;
                desc.depthBufferBits = k_DepthBufferBits;

                resourceData.shadowsDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ShadowDepth", false, FilterMode.Bilinear);
            }

            // Camera Sorting Layer desc
            if (m_Renderer2DData.useCameraSortingLayerTexture)
            {
                var descriptor = cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                CopyCameraSortingLayerPass.ConfigureDescriptor(m_Renderer2DData.cameraSortingLayerDownsamplingMethod, ref descriptor, out var filterMode);
                RenderingUtils.ReAllocateIfNeeded(ref m_CameraSortingLayerHandle, descriptor, filterMode, TextureWrapMode.Clamp, name: CopyCameraSortingLayerPass.k_CameraSortingLayerTexture);
                resourceData.cameraSortingLayerTexture = renderGraph.ImportTexture(m_CameraSortingLayerHandle);
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

                    RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraColorHandle, cameraTargetDescriptor, cameraTargetFilterMode, TextureWrapMode.Clamp, name: "_CameraTargetAttachment");
                    resourceData.activeColorID = ActiveID.Camera;
                }
                else
                    resourceData.activeColorID = ActiveID.BackBuffer;

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

                    RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraDepthHandle, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
                    resourceData.activeDepthID = ActiveID.Camera;
                }
                else
                    resourceData.activeDepthID = ActiveID.BackBuffer;
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
                
                resourceData.cameraColor = renderGraph.ImportTexture(m_RenderGraphCameraColorHandle, importSummary.cameraColorParams);
                resourceData.cameraDepth = renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle, importSummary.cameraDepthParams);
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

            resourceData.backBufferColor = renderGraph.ImportTexture(m_RenderGraphBackbufferColorHandle, importSummary.importInfo, importSummary.backBufferColorParams);
            resourceData.backBufferDepth = renderGraph.ImportTexture(m_RenderGraphBackbufferDepthHandle, importSummary.importInfoDepth, importSummary.backBufferDepthParams);

            var postProcessDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat, DepthBits.None);
            resourceData.afterPostProcessColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, postProcessDesc, "_AfterPostProcessTexture", true);
        }

        internal override void OnBeginRenderGraphFrame()
        {
            Universal2DResourceData resourceData = frameData.Create<Universal2DResourceData>();
            resourceData.InitFrame();
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            CreateResources(renderGraph);

            SetupRenderGraphCameraProperties(renderGraph, false);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            ProcessVFXCameraCommand(renderGraph);
#endif  

            OnBeforeRendering(renderGraph);

            OnMainRendering(renderGraph);

            OnAfterRendering(renderGraph);
        }

        internal override void OnEndRenderGraphFrame()
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
            resourceData.EndFrame();
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
        }

        private void OnMainRendering(RenderGraph renderGraph)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            RTClearFlags clearFlags = RTClearFlags.None;

            if (cameraData.renderType == CameraRenderType.Base)
                clearFlags = RTClearFlags.All;
            else if (cameraData.clearDepth)
                clearFlags = RTClearFlags.Depth;

            // Color Grading LUT
            bool requiredColorGradingLutPass = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            if (requiredColorGradingLutPass)
            {
                TextureHandle internalColorLut;
                m_PostProcessPasses.colorGradingLutPass.Render(renderGraph, frameData, out internalColorLut);
                resourceData.internalColorLut = internalColorLut;
            }

            var cameraSortingLayerBoundsIndex = Render2DLightingPass.GetCameraSortingLayerBoundsIndex(m_Renderer2DData);

            RendererLighting.lightBatch.Reset();

            // Main render passes
            var layerBatches = LayerUtility.CalculateBatches(m_Renderer2DData.lightCullResult, out var batchCount);
            for (var i = 0; i < batchCount; i++)
            {
                ref var layerBatch = ref layerBatches[i];

                // Normal Pass
                m_NormalPass.Render(renderGraph, frameData, m_Renderer2DData, ref layerBatch);

                // Shadow Pass (TODO: Optimize RT swapping between shadow and light textures)
                m_ShadowPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, frameData);

                bool clearLightTextures = !layerBatch.lightStats.useShadows;

#if UNITY_ON_METAL
                // Metal doesn't support MRT clear, so we have to clear RTs individually
                if (clearLightTextures)
                {
                    ClearLightTextures(renderGraph, m_Renderer2DData, ref layerBatch);
                    clearLightTextures = false;
                }
#endif

                // Light Pass
                m_LightPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, frameData, m_LightTextureHandles, resourceData.intermediateDepth, clear: clearLightTextures);

                // Clear camera targets
                if (i == 0 && clearFlags != RTClearFlags.None)
                    ClearTargets2DPass.Render(renderGraph, resourceData.activeColorTexture, resourceData.activeDepthTexture, clearFlags, cameraData.backgroundColor);

                LayerUtility.GetFilterSettings(m_Renderer2DData, ref layerBatch, cameraSortingLayerBoundsIndex, out var filterSettings);

                // Default Render Pass
                m_RendererPass.Render(renderGraph, frameData, m_Renderer2DData, ref layerBatch, ref filterSettings, resourceData.activeColorTexture, resourceData.activeDepthTexture, m_LightTextureHandles);

                // Camera Sorting Layer Pass
                if (m_Renderer2DData.useCameraSortingLayerTexture)
                {
                    // Split Render Pass if CameraSortingLayer is in the middle of a batch
                    if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex < layerBatch.layerRange.upperBound)
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, resourceData.activeColorTexture, resourceData.cameraSortingLayerTexture);

                        filterSettings.sortingLayerRange = new SortingLayerRange((short)(cameraSortingLayerBoundsIndex + 1), layerBatch.layerRange.upperBound);
                        m_RendererPass.Render(renderGraph, frameData, m_Renderer2DData, ref layerBatch, ref filterSettings, resourceData.activeColorTexture, resourceData.activeDepthTexture, m_LightTextureHandles);
                    }
                    else if (cameraSortingLayerBoundsIndex == layerBatch.layerRange.upperBound)
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, resourceData.activeColorTexture, resourceData.cameraSortingLayerTexture);
                    }
                }

                m_ShadowPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, frameData, true);

                // Light Volume Pass
                m_LightPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, isVolumetric: true);
            }

            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && outputToHDR)
            {
                TextureHandle overlayUI;
                m_DrawOffscreenUIPass.RenderOffscreen(renderGraph, frameData, k_DepthStencilFormat, out overlayUI);
                resourceData.overlayUITexture = overlayUI;
            }
        }

        private void OnAfterRendering(RenderGraph renderGraph)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, GizmoSubset.PreImageEffects);

            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            // Allocate debug screen texture if the debug mode needs it.
            if (resolveToDebugScreen)
            {
                RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureColorDescriptorForDebugScreen(ref colorDesc, cameraData.pixelWidth, cameraData.pixelHeight);
                resourceData.debugScreenColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDesc, "_DebugScreenColor", false);

                RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, k_DepthStencilFormat, cameraData.pixelWidth, cameraData.pixelHeight);
                resourceData.debugScreenDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "_DebugScreenDepth", false);
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

            var finalColorHandle = resourceData.activeColorTexture;

            if (applyPostProcessing)
            {
                postProcessPass.RenderPostProcessingRenderGraph(
                    renderGraph,
                    frameData,
                    resourceData.activeColorTexture,
                    resourceData.internalColorLut,
                    resourceData.overlayUITexture,
                    resourceData.afterPostProcessColor,
                    requireFinalPostProcessPass,
                    resolveToDebugScreen,
                    needsColorEncoding);
                finalColorHandle = resourceData.afterPostProcessColor;
            }

            if (isPixelPerfectCameraEnabled)
            {
                // Do PixelPerfect upscaling when using the Stretch Fill option
                if (requirePixelPerfectUpscale)
                {
                    m_UpscalePass.Render(renderGraph, cameraData.camera, in finalColorHandle, resourceData.upscaleTexture);
                    finalColorHandle = resourceData.upscaleTexture;
                }

                ClearTargets2DPass.Render(renderGraph, resourceData.backBufferColor, TextureHandle.nullHandle, RTClearFlags.Color, Color.black);
            }

            // We need to switch the "final" blit target to debugScreenColor if HDR debug views are enabled.
            var finalBlitTarget = resolveToDebugScreen ? resourceData.debugScreenColor : resourceData.backBufferColor;
            var finalDepthHandle = resolveToDebugScreen ? resourceData.debugScreenDepth : resourceData.backBufferDepth;

            if (createColorTexture)
            {
                if (requireFinalPostProcessPass)
                    postProcessPass.RenderFinalPassRenderGraph(renderGraph, frameData, in finalColorHandle, resourceData.overlayUITexture, in finalBlitTarget, needsColorEncoding);
                else
                    m_FinalBlitPass.Render(renderGraph, cameraData, finalColorHandle, finalBlitTarget, resourceData.overlayUITexture);

                finalColorHandle = finalBlitTarget;
            }

            // We can explicitly render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && !outputToHDR)
                m_DrawOverlayUIPass.RenderOverlay(renderGraph, cameraData.camera, in finalColorHandle, in finalDepthHandle);

            // If HDR debug views are enabled, DebugHandler will perform the blit from debugScreenColor (== finalColorHandle) to backBufferColor.
            DebugHandler?.Setup(renderingData.commandBuffer, cameraData.isPreviewCamera);
            DebugHandler?.Render(renderGraph, renderingData.commandBuffer, cameraData, finalColorHandle, resourceData.overlayUITexture, resourceData.backBufferColor);

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, resourceData.backBufferColor, resourceData.activeDepthTexture, GizmoSubset.PostImageEffects);
        }

#if UNITY_ON_METAL
        private void ClearLightTextures(RenderGraph graph, Renderer2DData rendererData, ref LayerBatch layerBatch)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            var blendStylesCount = rendererData.lightBlendStyles.Length;
            TextureHandle[] lightTextureHandles = resourceData.lightTextures;
            for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
            {
                if ((layerBatch.lightStats.blendStylesUsed & (uint)(1 << blendStyleIndex)) == 0)
                    continue;

                Light2DManager.GetGlobalColor(layerBatch.startLayerID, blendStyleIndex, out var color);
                ClearTargets2DPass.Render(graph, lightTextureHandles[blendStyleIndex], TextureHandle.nullHandle, RTClearFlags.Color, color);
            }
        }
#endif

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

    class ClearTargets2DPass
    {
        static private ProfilingSampler s_ClearProfilingSampler = new ProfilingSampler("Clear Targets");
        private class PassData
        {
            internal RTClearFlags clearFlags;
            internal Color clearColor;
        }

        internal static void Render(RenderGraph graph, in TextureHandle colorHandle, in TextureHandle depthHandle, RTClearFlags clearFlags, Color clearColor)
        {
            Debug.Assert(colorHandle.IsValid(), "Trying to clear an invalid render color target");

            if (clearFlags != RTClearFlags.Color)
                Debug.Assert(depthHandle.IsValid(), "Trying to clear an invalid depth target");

            using (var builder = graph.AddRasterRenderPass<PassData>("Clear Target", out var passData, s_ClearProfilingSampler))
            {
                builder.UseTextureFragment(colorHandle, 0);
                if (depthHandle.IsValid())
                    builder.UseTextureFragmentDepth(depthHandle, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.clearFlags = clearFlags;
                passData.clearColor = clearColor;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(data.clearFlags, data.clearColor, 1, 0);
                });
            }
        }
    }
}
