using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using static UnityEngine.Rendering.Universal.UniversalResourceDataBase;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal sealed partial class Renderer2D : ScriptableRenderer
    {
        // TODO RENDERGRAPH: Once all cameras will run in a single RenderGraph we should remove all RTHandles and use per frame RG textures.
        // We use 2 camera color handles so we can handle the edge case when a pass might want to read and write the same target.
        // This is not allowed so we just swap the current target, this keeps camera stacking working and avoids an extra blit pass.
        static int m_CurrentColorHandle = 0;
        RTHandle[] m_RenderGraphCameraColorHandles = new RTHandle[]
        {
            null, null
        };

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

        private bool IsPixelPerfectCameraEnabled(UniversalCameraData cameraData)
        {
            PixelPerfectCamera ppc = null;

            // Pixel Perfect Camera doesn't support camera stacking.
            if (cameraData.renderType == CameraRenderType.Base && cameraData.resolveFinalTarget)
                cameraData.camera.TryGetComponent(out ppc);

            return ppc != null && ppc.enabled && ppc.cropFrame != PixelPerfectCamera.CropFrame.None;
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
                    Debug.LogWarning("In the render graph API, the output Render Texture must have a depth buffer. When you select a Render Texture in any camera's Output Texture property, the Depth Stencil Format property of the texture must be set to a value other than None.");
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

        void InitializeLayerBatches()
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            m_LayerBatches = LayerUtility.CalculateBatches(m_Renderer2DData, out m_BatchCount);

            // Initialize textures dependent on batch size
            if (resourceData.normalsTexture.Length != m_BatchCount)
                resourceData.normalsTexture = new TextureHandle[m_BatchCount];

            if (resourceData.shadowTextures.Length != m_BatchCount)
                resourceData.shadowTextures = new TextureHandle[m_BatchCount][];

            if (resourceData.lightTextures.Length != m_BatchCount)
                resourceData.lightTextures = new TextureHandle[m_BatchCount][];

            // Initialize light textures based on active blend styles to save on resources
            for (int i = 0; i < resourceData.lightTextures.Length; ++i)
            {
                if (resourceData.lightTextures[i] == null || resourceData.lightTextures[i].Length != m_LayerBatches[i].activeBlendStylesIndices.Length)
                    resourceData.lightTextures[i] = new TextureHandle[m_LayerBatches[i].activeBlendStylesIndices.Length];
            }

            for (int i = 0; i < resourceData.shadowTextures.Length; ++i)
            {
                if (resourceData.shadowTextures[i] == null || resourceData.shadowTextures[i].Length != m_LayerBatches[i].shadowIndices.Count)
                    resourceData.shadowTextures[i] = new TextureHandle[m_LayerBatches[i].shadowIndices.Count];
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
                    cameraTargetDescriptor.depthStencilFormat = GraphicsFormat.None;

                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraColorHandles[0], cameraTargetDescriptor, cameraTargetFilterMode, TextureWrapMode.Clamp, name: "_CameraTargetAttachmentA");
                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraColorHandles[1], cameraTargetDescriptor, cameraTargetFilterMode, TextureWrapMode.Clamp, name: "_CameraTargetAttachmentB");
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

                    bool hasMSAA = depthDescriptor.msaaSamples > 1 && (SystemInfo.supportsMultisampledTextures != 0);
                    bool resolveDepth = RenderingUtils.MultisampleDepthResolveSupported() && renderGraph.nativeRenderPassesEnabled;

                    depthDescriptor.bindMS = !resolveDepth && hasMSAA;

                    // binding MS surfaces is not supported by the GLES backend
                    if (IsGLESDevice())
                        depthDescriptor.bindMS = false;

                    if (m_CopyDepthPass != null)
                        m_CopyDepthPass.m_CopyResolvedDepth = !depthDescriptor.bindMS;

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

            var postProcessDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat);
            commonResourceData.afterPostProcessColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, postProcessDesc, "_AfterPostProcessTexture", true);

            if (RequiresDepthCopyPass(cameraData))
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

        }

        void CreateLightTextures(RenderGraph renderGraph, int width, int height)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            var desc = new RenderTextureDescriptor(width, height);
            desc.graphicsFormat = RendererLighting.GetRenderTextureFormat();
            desc.autoGenerateMips = false;

            for (int i = 0; i < resourceData.lightTextures.Length; ++i)
            {
                for (var j = 0; j < m_LayerBatches[i].activeBlendStylesIndices.Length; ++j)
                {
                    var index = m_LayerBatches[i].activeBlendStylesIndices[j];
                    if (!Light2DManager.GetGlobalColor(m_LayerBatches[i].startLayerID, index, out var clearColor))
                        clearColor = Color.black;

                    resourceData.lightTextures[i][j] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, RendererLighting.k_ShapeLightTextureIDs[index], true, clearColor, FilterMode.Bilinear);
                }
            }
        }

        void CreateShadowTextures(RenderGraph renderGraph, int width, int height)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            var shadowDesc = new RenderTextureDescriptor(width, height);
            shadowDesc.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            shadowDesc.autoGenerateMips = false;

            for (int i = 0; i < resourceData.shadowTextures.Length; ++i)
            {
                for (var j = 0; j < m_LayerBatches[i].shadowIndices.Count; ++j)
                {
                    resourceData.shadowTextures[i][j] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, shadowDesc, "_ShadowTex", false, FilterMode.Bilinear);
                }
            }

            var shadowDepthDesc = new RenderTextureDescriptor(width, height);
            shadowDepthDesc.graphicsFormat = GraphicsFormat.None;
            shadowDepthDesc.autoGenerateMips = false;
            shadowDepthDesc.depthStencilFormat = k_DepthStencilFormat;

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

        bool RequiresDepthCopyPass(UniversalCameraData cameraData)
        {
            var renderPassInputs = GetRenderPassInputs(cameraData);
            bool cameraHasPostProcessingWithDepth = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated && cameraData.postProcessingRequiresDepthTexture;
            bool requiresDepthCopyPass = (cameraHasPostProcessingWithDepth || renderPassInputs.requiresDepthTexture) && m_CreateDepthTexture;

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

        internal override void OnFinishRenderGraphRendering(CommandBuffer cmd)
        {
            m_CopyDepthPass?.OnCameraCleanup(cmd);
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

            // Set Global Properties and Textures
            GlobalPropertiesPass.Setup(renderGraph, frameData, m_Renderer2DData, cameraData);

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

                LayerUtility.GetFilterSettings(m_Renderer2DData, ref m_LayerBatches[i], out var filterSettings);
                m_RendererPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches, i, ref filterSettings);

                // Shadow Volumetric Pass
                m_ShadowPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches[i], i, true);

                // Light Volumetric Pass
                m_LightPass.Render(renderGraph, frameData, m_Renderer2DData, ref m_LayerBatches[i], i, true);

                // Camera Sorting Layer Pass
                if (m_Renderer2DData.useCameraSortingLayerTexture)
                {
                    if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex <= layerBatch.layerRange.upperBound)
                    {
                        m_CopyCameraSortingLayerPass.Render(renderGraph, frameData);
                    }
                }
            }

            if (RequiresDepthCopyPass(cameraData))
                m_CopyDepthPass?.Render(renderGraph, frameData, commonResourceData.cameraDepthTexture, commonResourceData.activeDepthTexture, true);

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

            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool anyPostProcessing = postProcessingData.isEnabled && m_PostProcessPasses.isCreated;

            cameraData.camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
            bool requirePixelPerfectUpscale = IsPixelPerfectCameraEnabled(cameraData) && ppc.requiresUpscalePass;

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool applyFinalPostProcessing = cameraData.resolveFinalTarget && !ppcUpscaleRT && anyPostProcessing && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;
            bool needsColorEncoding = DebugHandler == null || !DebugHandler.HDRDebugViewIsActive(cameraData.resolveFinalTarget);

            // Don't resolve during post processing if there are passes after or pixel perfect camera is used
            bool pixelPerfectCameraEnabled = ppc != null && ppc.enabled;
            bool hasCaptureActions = cameraData.captureActions != null && cameraData.resolveFinalTarget;
            bool resolvePostProcessingToCameraTarget = cameraData.resolveFinalTarget && !hasCaptureActions && !hasPassesAfterPostProcessing && !applyFinalPostProcessing && !pixelPerfectCameraEnabled;
            bool doSRGBEncoding = resolvePostProcessingToCameraTarget && needsColorEncoding;

            if (applyPostProcessing)
            {
                TextureHandle activeColor = commonResourceData.activeColorTexture;

                bool isTargetBackbuffer = resolvePostProcessingToCameraTarget;

                // if the postprocessing pass is trying to read and write to the same CameraColor target, we need to swap so it writes to a different target,
                // since reading a pass attachment is not possible. Normally this would be possible using temporary RenderGraph managed textures.
                // The reason why in this case we need to use "external" RTHandles is to preserve the results for camera stacking.
                // TODO RENDERGRAPH: Once all cameras will run in a single RenderGraph we can just use temporary RenderGraph textures as intermediate buffer.
                if (!isTargetBackbuffer)
                {
                    ImportResourceParams importColorParams = new ImportResourceParams();
                    importColorParams.clearOnFirstUse = true;
                    importColorParams.clearColor = Color.black;
                    importColorParams.discardOnLastUse = cameraData.resolveFinalTarget;  // check if last camera in the stack

                    commonResourceData.cameraColor = renderGraph.ImportTexture(nextRenderGraphCameraColorHandle, importColorParams);
                }

                // Desired target for post-processing pass.
                var target = isTargetBackbuffer ? commonResourceData.backBufferColor : commonResourceData.cameraColor;

                if (resolveToDebugScreen && isTargetBackbuffer)
                    target = commonResourceData.debugScreenColor;

                postProcessPass.RenderPostProcessingRenderGraph(
                    renderGraph,
                    frameData,
                    activeColor,
                    commonResourceData.internalColorLut,
                    commonResourceData.overlayUITexture,
                    target,
                    applyFinalPostProcessing,
                    resolveToDebugScreen,
                    doSRGBEncoding);

                if (isTargetBackbuffer)
                {
                    commonResourceData.activeColorID = UniversalResourceData.ActiveID.BackBuffer;
                    commonResourceData.activeDepthID = UniversalResourceData.ActiveID.BackBuffer;
                }
            }

            var finalColorHandle = commonResourceData.activeColorTexture;

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

            if (applyFinalPostProcessing)
            {
                postProcessPass.RenderFinalPassRenderGraph(renderGraph, frameData, in finalColorHandle, commonResourceData.overlayUITexture, in finalBlitTarget, needsColorEncoding);

                commonResourceData.activeColorID = UniversalResourceData.ActiveID.BackBuffer;
                commonResourceData.activeDepthID = UniversalResourceData.ActiveID.BackBuffer;
            }

            // If post-processing then we already resolved to camera target while doing post.
            // Also only do final blit if camera is not rendering to RT.
            bool cameraTargetResolved =
                   // final PP always blit to camera target
                   applyFinalPostProcessing ||
                   // no final PP but we have PP stack. In that case it blit unless there are render pass after PP or pixel perfect camera is used
                   (applyPostProcessing && !hasPassesAfterPostProcessing && !hasCaptureActions && !pixelPerfectCameraEnabled);

            if (!commonResourceData.isActiveTargetBackBuffer && cameraData.resolveFinalTarget && !cameraTargetResolved)
            {
                m_FinalBlitPass.Render(renderGraph, frameData, cameraData, finalColorHandle, finalBlitTarget, commonResourceData.overlayUITexture);

                finalColorHandle = finalBlitTarget;

                commonResourceData.activeColorID = ActiveID.BackBuffer;
                commonResourceData.activeDepthID = ActiveID.BackBuffer;
            }

            // We can explicitly render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            bool shouldRenderUI = cameraData.rendersOverlayUI && cameraData.isLastBaseCamera;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && !outputToHDR)
                m_DrawOverlayUIPass.RenderOverlay(renderGraph, frameData, in finalColorHandle, in finalDepthHandle);

            // If HDR debug views are enabled, DebugHandler will perform the blit from debugScreenColor (== finalColorHandle) to backBufferColor.
            DebugHandler?.Setup(renderGraph, cameraData.isPreviewCamera);
            DebugHandler?.Render(renderGraph, cameraData, finalColorHandle, commonResourceData.overlayUITexture, commonResourceData.backBufferColor);

            if (cameraData.isSceneViewCamera)
                DrawRenderGraphWireOverlay(renderGraph, frameData, commonResourceData.backBufferColor);

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, commonResourceData.activeColorTexture, commonResourceData.activeDepthTexture, GizmoSubset.PostImageEffects);
        }

        private void CleanupRenderGraphResources()
        {
            m_RenderGraphCameraColorHandles[0]?.Release();
            m_RenderGraphCameraColorHandles[1]?.Release();
            m_RenderGraphCameraDepthHandle?.Release();
            m_RenderGraphBackbufferColorHandle?.Release();
            m_RenderGraphBackbufferDepthHandle?.Release();
            m_CameraSortingLayerHandle?.Release();
            Light2DLookupTexture.Release();
        }
    }
}
