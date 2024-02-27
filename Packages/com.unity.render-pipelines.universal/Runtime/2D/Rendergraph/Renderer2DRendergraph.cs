using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        Renderer2DResource m_ActiveColorID;
        Renderer2DResource m_ActiveDepthID;
        TextureHandle activeColorTexture => (resources.GetTexture(m_ActiveColorID));
        TextureHandle activeDepthTexture => (resources.GetTexture(m_ActiveDepthID));

        TextureHandle[] m_LightTextureHandles = new TextureHandle[RendererLighting.k_ShapeLightTextureIDs.Length];
        RTHandle m_RenderGraphCameraColorHandle;
        RTHandle m_RenderGraphCameraDepthHandle;
        RTHandle m_RenderGraphBackbufferDepthHandle;
        RTHandle m_CameraSortingLayerHandle;
        
        DrawNormal2DPass m_NormalPass = new DrawNormal2DPass();
        DrawLight2DPass m_LightPass = new DrawLight2DPass();
        DrawShadow2DPass m_ShadowPass = new DrawShadow2DPass();
        DrawRenderer2DPass m_RendererPass = new DrawRenderer2DPass();

        bool ppcUpscaleRT = false;

        void CreateResources(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;
            var cameraTargetFilterMode = FilterMode.Bilinear;

#if UNITY_EDITOR
            // The scene view camera cannot be uninitialized or skybox when using the 2D renderer.
            if (cameraData.cameraType == CameraType.SceneView)
            {
                renderingData.cameraData.camera.clearFlags = CameraClearFlags.SolidColor;
            }
#endif

            bool forceCreateColorTexture = false;

            // Pixel Perfect Camera doesn't support camera stacking.
            if (cameraData.renderType == CameraRenderType.Base && cameraData.resolveFinalTarget)
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

                        resources.SetTexture(Renderer2DResource.UpscaleTexture, UniversalRenderer.CreateRenderGraphTexture(renderGraph, upscaleDescriptor, "_UpscaleTexture", true, ppc.finalBlitFilterMode));
                    }
                }
            }

            var renderTextureScale = m_Renderer2DData.lightRenderTextureScale;
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            // Intermediate depth desc (size of renderTextureScale)
            {
                var depthDescriptor = cameraTargetDescriptor;
                depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                depthDescriptor.depthBufferBits = k_DepthBufferBits;
                depthDescriptor.width = width;
                depthDescriptor.height = height;
                if (!cameraData.resolveFinalTarget && m_UseDepthStencilBuffer)
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);

                resources.SetTexture(Renderer2DResource.IntermediateDepth,  UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "DepthTexture", true));
            }

            // Normal and Light desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = cameraTargetDescriptor.graphicsFormat;
                desc.useMipMap = false;
                desc.autoGenerateMips = false;
                desc.depthBufferBits = 0;
                desc.msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
                desc.dimension = TextureDimension.Tex2D;

                resources.SetTexture(Renderer2DResource.NormalsTexture, UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_NormalMap", true));

                for (var i = 0; i < RendererLighting.k_ShapeLightTextureIDs.Length; i++)
                {
                    m_LightTextureHandles[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, RendererLighting.k_ShapeLightTextureIDs[i], false, FilterMode.Bilinear);
                    resources.SetTexture(Renderer2DResource.LightTexture0 + i, m_LightTextureHandles[i]);
                }
            }

            // Shadow desc
            {
                var desc = new RenderTextureDescriptor(width, height);
                desc.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                desc.useMipMap = false;
                desc.autoGenerateMips = false;
                desc.depthBufferBits = 0;
                desc.msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
                desc.dimension = TextureDimension.Tex2D;

                resources.SetTexture(Renderer2DResource.ShadowsTexture, UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ShadowTex", false, FilterMode.Bilinear));
            }

            // Camera Sorting Layer desc
            if (m_Renderer2DData.useCameraSortingLayerTexture)
            {
                var descriptor = cameraTargetDescriptor;
                CopyCameraSortingLayerPass.ConfigureDescriptor(m_Renderer2DData.cameraSortingLayerDownsamplingMethod, ref descriptor, out var filterMode);
                RenderingUtils.ReAllocateIfNeeded(ref m_CameraSortingLayerHandle, descriptor, filterMode, TextureWrapMode.Clamp, name: CopyCameraSortingLayerPass.k_CameraSortingLayerTexture);
                resources.SetTexture(Renderer2DResource.CameraSortingLayerTexture, renderGraph.ImportTexture(m_CameraSortingLayerHandle));
            }

            // now create the attachments
            if (cameraData.renderType == CameraRenderType.Base) // require intermediate textures
            {
                RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData, ref cameraData);
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

                    m_ActiveColorID = Renderer2DResource.CameraColor;
                }
                else
                    m_ActiveColorID = Renderer2DResource.BackBufferColor;

                // Camera Target Depth
                if (createDepthTexture)
                {
                    var depthDescriptor = cameraData.cameraTargetDescriptor;
                    depthDescriptor.useMipMap = false;
                    depthDescriptor.autoGenerateMips = false;
                    if (!cameraData.resolveFinalTarget && m_UseDepthStencilBuffer)
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);

                    depthDescriptor.graphicsFormat = GraphicsFormat.None;
                    depthDescriptor.depthStencilFormat = k_DepthStencilFormat;

                    RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraDepthHandle, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthAttachment");

                    m_ActiveDepthID = Renderer2DResource.CameraDepth;
                }
                else
                    m_ActiveDepthID = Renderer2DResource.BackBufferDepth;
            }
            else // Overlay camera
            {
                cameraData.baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraData);
                var baseRenderer = (Renderer2D)baseCameraData.scriptableRenderer;

                m_RenderGraphCameraColorHandle = baseRenderer.m_RenderGraphCameraColorHandle;
                m_RenderGraphCameraDepthHandle = baseRenderer.m_RenderGraphCameraDepthHandle;

                m_CreateColorTexture = baseRenderer.m_CreateColorTexture;
                m_CreateDepthTexture = baseRenderer.m_CreateDepthTexture;
            }

            if (m_CreateColorTexture)
            {
                resources.SetTexture(Renderer2DResource.CameraColor, renderGraph.ImportTexture(m_RenderGraphCameraColorHandle));
                resources.SetTexture(Renderer2DResource.CameraDepth, renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle));
            }

            RenderTargetIdentifier targetId;
            RenderTargetInfo importInfo = new RenderTargetInfo();
            if (cameraData.targetTexture != null)
            {
                targetId = new RenderTargetIdentifier(cameraData.targetTexture);
                importInfo.width = cameraData.targetTexture.width;
                importInfo.height = cameraData.targetTexture.height;
                importInfo.volumeDepth = cameraData.targetTexture.volumeDepth;
                importInfo.msaaSamples = cameraData.targetTexture.antiAliasing;
                importInfo.format = cameraData.targetTexture.graphicsFormat;
            }
            else
            {
                targetId = BuiltinRenderTextureType.CameraTarget;
                //NOTE: Carefull what you use here as many of the properties bake-in the camera rect so for example
                //cameraData.cameraTargetDescriptor.width is the width of the recangle but not the actual rendertarget
                //same with cameraData.camera.pixelWidth
                importInfo.width = Screen.width;
                importInfo.height = Screen.height;
                importInfo.volumeDepth = 1;
                importInfo.msaaSamples = Screen.msaaSamples; // cameraData.cameraTargetDescriptor.msaaSamples;
                // The editor always allocates the system rendertarget with a single msaa sample
                // See: ConfigureTargetTexture in PlayModeView.cs
                if (Application.isEditor)
                    importInfo.msaaSamples = 1;

                importInfo.format = UniversalRenderPipeline.MakeRenderTextureGraphicsFormat(cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, Graphics.preserveFramebufferAlpha);
            }

            ImportResourceParams importParams = new ImportResourceParams();
            importParams.clearOnFirstUse = (renderingData.cameraData.renderType == CameraRenderType.Base) && !m_CreateColorTexture;
            importParams.clearColor = renderingData.cameraData.backgroundColor;
            importParams.discardOnLastUse = false;

            resources.SetTexture(Renderer2DResource.BackBufferColor, renderGraph.ImportBackbuffer(targetId, importInfo, importParams));

            if (renderingData.cameraData.rendersOverlayUI)
            {
                // Screenspace - Overlay UI may need to write to the depth backbuffer
                RenderTargetIdentifier targetDepthId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.Depth;
                if (m_RenderGraphBackbufferDepthHandle == null || m_RenderGraphBackbufferDepthHandle.nameID != targetDepthId)
                {
                    m_RenderGraphBackbufferDepthHandle?.Release();
                    m_RenderGraphBackbufferDepthHandle = RTHandles.Alloc(targetDepthId);
                }
                resources.SetTexture(Renderer2DResource.BackBufferDepth, renderGraph.ImportTexture(m_RenderGraphBackbufferDepthHandle));
            }
            else if (!m_CreateDepthTexture)
                resources.SetTexture(Renderer2DResource.BackBufferDepth, renderGraph.ImportBackbuffer(targetId, importInfo, importParams));

            var postProcessDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat, DepthBits.None);
            resources.SetTexture(Renderer2DResource.AfterPostProcessColor, UniversalRenderer.CreateRenderGraphTexture(renderGraph, postProcessDesc, "_AfterPostProcessTexture", true));
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context,  ref RenderingData renderingData)
        {
            CreateResources(renderGraph, ref renderingData);
            SetupRenderGraphCameraProperties(renderGraph, ref renderingData, false);

            OnBeforeRendering(renderGraph, ref renderingData);

            OnMainRendering(renderGraph, ref renderingData);

            OnAfterRendering(renderGraph, ref renderingData);
        }

        private void OnBeforeRendering(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            m_LightPass.Setup(renderGraph, ref m_Renderer2DData);

            // Before rendering the lights cache some values that are expensive to get/calculate
            var culledLights = m_Renderer2DData.lightCullResult.visibleLights;
            for (var i = 0; i < culledLights.Count; i++)
            {
                culledLights[i].CacheValues();
            }

            ShadowCasterGroup2DManager.CacheValues();

            ShadowRendering.CallOnBeforeRender(renderingData.cameraData.camera, m_Renderer2DData.lightCullResult);
        }

        private void OnMainRendering(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
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
                m_PostProcessPasses.colorGradingLutPass.Render(renderGraph, out internalColorLut, ref renderingData);
                resources.SetTexture(Renderer2DResource.InternalColorLut, internalColorLut);
            }

            var cameraSortingLayerBoundsIndex = Render2DLightingPass.GetCameraSortingLayerBoundsIndex(m_Renderer2DData);

            RendererLighting.lightBatch.Reset();

            // Main render passes
            var layerBatches = LayerUtility.CalculateBatches(m_Renderer2DData.lightCullResult, out var batchCount);
            for (var i = 0; i < batchCount; i++)
            {
                ref var layerBatch = ref layerBatches[i];

                // Normal Pass
                m_NormalPass.Render(renderGraph, ref renderingData, m_Renderer2DData, ref layerBatch, resources, i, batchCount);

                bool doClear = true;

                for (int j = 0; j < layerBatch.shadowLights.Count; ++j)
                {
                    // Shadow Pass
                    m_ShadowPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, resources, j);

                    if(doClear)
                    {
                        ClearLightTextures(renderGraph, m_Renderer2DData, ref layerBatch);
                        doClear = false;
                    }

                    // Shadow Light Pass
                    m_LightPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, resources, m_LightTextureHandles, resources.GetTexture(Renderer2DResource.IntermediateDepth), shadowlightIndex: j);
                }

                // TODO: replace with clear mrt in light pass
                // Clear Light Textures
                if (doClear)
                    ClearLightTextures(renderGraph, m_Renderer2DData, ref layerBatch);

                // Light Pass
                m_LightPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, resources, m_LightTextureHandles, resources.GetTexture(Renderer2DResource.IntermediateDepth));

                // Clear camera targets
                if (i == 0 && clearFlags != RTClearFlags.None)
                    ClearTargets2DPass.Render(renderGraph, activeColorTexture, activeDepthTexture, clearFlags, renderingData.cameraData.backgroundColor);

                LayerUtility.GetFilterSettings(m_Renderer2DData, ref layerBatch, cameraSortingLayerBoundsIndex, out var filterSettings);

                // Default Render Pass
                m_RendererPass.Render(renderGraph, ref renderingData, m_Renderer2DData, ref layerBatch, ref filterSettings, activeColorTexture, activeDepthTexture, m_LightTextureHandles);

                // Camera Sorting Layer Pass
                if (m_Renderer2DData.useCameraSortingLayerTexture)
                {
                    // Split Render Pass if CameraSortingLayer is in the middle of a batch
                    if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex < layerBatch.layerRange.upperBound)
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, ref renderingData, activeColorTexture, resources.GetTexture(Renderer2DResource.CameraSortingLayerTexture));

                        filterSettings.sortingLayerRange = new SortingLayerRange((short)(cameraSortingLayerBoundsIndex + 1), layerBatch.layerRange.upperBound);                        
                        m_RendererPass.Render(renderGraph, ref renderingData, m_Renderer2DData, ref layerBatch, ref filterSettings, activeColorTexture, activeDepthTexture, m_LightTextureHandles);
                    }
                    else if (cameraSortingLayerBoundsIndex == layerBatch.layerRange.upperBound)
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, ref renderingData, activeColorTexture, resources.GetTexture(Renderer2DResource.CameraSortingLayerTexture));
                    }
                }

                for (int j = 0; j < layerBatch.shadowLights.Count; ++j)
                {
                    if (!layerBatch.shadowLights[j].volumetricEnabled)
                        continue;

                    // Shadow Pass
                    m_ShadowPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, resources, j);

                    // Shadow Light Volume Pass
                    m_LightPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, resources, activeColorTexture, activeDepthTexture, j, true);
                }

                // Light Volume Pass
                m_LightPass.Render(renderGraph, m_Renderer2DData, ref layerBatch, resources, activeColorTexture, activeDepthTexture, isVolumetric: true);
            }

            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && outputToHDR)
            {
                TextureHandle overlayUI;
                m_DrawOffscreenUIPass.RenderOffscreen(renderGraph, k_DepthStencilFormat, out overlayUI, ref renderingData);
                resources.SetTexture(Renderer2DResource.OverlayUITexture, overlayUI);
            }
        }

        private void OnAfterRendering(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, activeColorTexture, activeDepthTexture, GizmoSubset.PreImageEffects, ref renderingData);

            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(ref renderingData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(ref renderingData.cameraData);
            // Allocate debug screen texture if the debug mode needs it.
            if (resolveToDebugScreen)
            {
                RenderTextureDescriptor colorDesc = renderingData.cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureColorDescriptorForDebugScreen(ref colorDesc, renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);
                resources.SetTexture(Renderer2DResource.DebugScreenColor, UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDesc, "_DebugScreenColor", false));
                
                RenderTextureDescriptor depthDesc = renderingData.cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, k_DepthStencilFormat, renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);
                resources.SetTexture(Renderer2DResource.DebugScreenDepth, UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "_DebugScreenDepth", false));
            }

            bool applyPostProcessing = renderingData.postProcessingEnabled && m_PostProcessPasses.isCreated;

            cameraData.camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
            bool isPixelPerfectCameraEnabled = ppc != null && ppc.enabled && ppc.cropFrame != PixelPerfectCamera.CropFrame.None;
            bool requirePixelPerfectUpscale = isPixelPerfectCameraEnabled && ppc.requiresUpscalePass;

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool requireFinalPostProcessPass = renderingData.cameraData.resolveFinalTarget && !ppcUpscaleRT && applyPostProcessing && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;
            bool needsColorEncoding = DebugHandler == null || !DebugHandler.HDRDebugViewIsActive(ref cameraData);

            var finalColorHandle = activeColorTexture;

            if (applyPostProcessing)
            {
                postProcessPass.RenderPostProcessingRenderGraph(renderGraph, activeColorTexture, resources.GetTexture(Renderer2DResource.InternalColorLut), resources.GetTexture(Renderer2DResource.OverlayUITexture), resources.GetTexture(Renderer2DResource.AfterPostProcessColor), ref renderingData, requireFinalPostProcessPass, resolveToDebugScreen, needsColorEncoding);
                finalColorHandle = resources.GetTexture(Renderer2DResource.AfterPostProcessColor);
            }

            if (isPixelPerfectCameraEnabled)
            {
                // Do PixelPerfect upscaling when using the Stretch Fill option
                if (requirePixelPerfectUpscale)
                {
                    m_UpscalePass.Render(renderGraph, ref cameraData, in finalColorHandle, resources.GetTexture(Renderer2DResource.UpscaleTexture));
                    finalColorHandle = resources.GetTexture(Renderer2DResource.UpscaleTexture);
                }

                ClearTargets2DPass.Render(renderGraph, resources.GetTexture(Renderer2DResource.BackBufferColor), TextureHandle.nullHandle, RTClearFlags.Color, Color.black);
            }

            // We need to switch the "final" blit target to debugScreenColor if HDR debug views are enabled.
            var finalBlitTarget = resolveToDebugScreen ? resources.GetTexture(Renderer2DResource.DebugScreenColor) : resources.GetTexture(Renderer2DResource.BackBufferColor);
            var finalDepthHandle = resolveToDebugScreen ? resources.GetTexture(Renderer2DResource.DebugScreenDepth) : resources.GetTexture(Renderer2DResource.BackBufferDepth);

            if (createColorTexture)
            {
                if (requireFinalPostProcessPass)
                    postProcessPass.RenderFinalPassRenderGraph(renderGraph, in finalColorHandle, resources.GetTexture(Renderer2DResource.OverlayUITexture), in finalBlitTarget, ref renderingData, needsColorEncoding);
                else
                    m_FinalBlitPass.Render(renderGraph, ref renderingData, finalColorHandle, finalBlitTarget, resources.GetTexture(Renderer2DResource.OverlayUITexture));

                finalColorHandle = finalBlitTarget;
            }

            // We can explicitly render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            bool shouldRenderUI = renderingData.cameraData.rendersOverlayUI;
            bool outputToHDR = renderingData.cameraData.isHDROutputActive;
            if (shouldRenderUI && !outputToHDR)
                m_DrawOverlayUIPass.RenderOverlay(renderGraph, in finalColorHandle, in finalDepthHandle, ref renderingData);

            // If HDR debug views are enabled, DebugHandler will perform the blit from debugScreenColor (== finalColorHandle) to backBufferColor.
            DebugHandler?.Setup(ref renderingData);
            DebugHandler?.Render(renderGraph, ref renderingData, finalColorHandle, resources.GetTexture(Renderer2DResource.OverlayUITexture), resources.GetTexture(Renderer2DResource.BackBufferColor));

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, resources.GetTexture(Renderer2DResource.BackBufferColor), activeDepthTexture, GizmoSubset.PostImageEffects, ref renderingData);
        }

        internal override void OnFinishRenderGraphRendering(ref RenderingData renderingData)
        {
        }

        private void ClearLightTextures(RenderGraph graph, Renderer2DData rendererData, ref LayerBatch layerBatch)
        {
            var blendStylesCount = rendererData.lightBlendStyles.Length;
            for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
            {
                if ((layerBatch.lightStats.blendStylesUsed & (uint)(1 << blendStyleIndex)) == 0)
                    continue;

                Light2DManager.GetGlobalColor(layerBatch.startLayerID, blendStyleIndex, out var color);
                ClearTargets2DPass.Render(graph, resources.GetTexture(Renderer2DResource.LightTexture0 + blendStyleIndex), TextureHandle.nullHandle, RTClearFlags.Color, color);
            }
        }

        private void CleanupRenderGraphResources()
        {
            m_RenderGraphCameraColorHandle?.Release();
            m_RenderGraphCameraDepthHandle?.Release();
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
                    builder.UseTextureFragmentDepth(depthHandle, IBaseRenderGraphBuilder.AccessFlags.Write);
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
