using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal
{
    internal class Render2DLightingPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");
        private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");
        private static readonly int k_LightLookupID = Shader.PropertyToID("_LightLookup");
        private static readonly int k_FalloffLookupID = Shader.PropertyToID("_FalloffLookup");

        private static readonly int[] k_ShapeLightTextureIDs =
        {
            Shader.PropertyToID("_ShapeLightTexture0"),
            Shader.PropertyToID("_ShapeLightTexture1"),
            Shader.PropertyToID("_ShapeLightTexture2"),
            Shader.PropertyToID("_ShapeLightTexture3")
        };

        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        private static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_CombinedRenderingPassName };

        private static readonly ProfilingSampler m_ProfilingDrawLights = new ProfilingSampler("Draw 2D Lights");
        private static readonly ProfilingSampler m_ProfilingDrawLightTextures = new ProfilingSampler("Draw 2D Lights Textures");
        private static readonly ProfilingSampler m_ProfilingDrawRenderers = new ProfilingSampler("Draw All Renderers");
        private static readonly ProfilingSampler m_ProfilingDrawLayerBatch = new ProfilingSampler("Draw Layer Batch");
        private static readonly ProfilingSampler m_ProfilingSamplerUnlit = new ProfilingSampler("Render Unlit");

        Material m_BlitMaterial;
        Material m_SamplingMaterial;

        private readonly Renderer2DData m_Renderer2DData;
        private readonly Texture2D m_FallOffLookup;
        private bool m_NeedsDepth;
        private short m_CameraSortingLayerBoundsIndex;

        public Render2DLightingPass(Renderer2DData rendererData, Material blitMaterial, Material samplingMaterial, Texture2D fallOffLookup)
        {
            m_Renderer2DData = rendererData;
            m_BlitMaterial = blitMaterial;
            m_SamplingMaterial = samplingMaterial;
            m_FallOffLookup = fallOffLookup;

            m_CameraSortingLayerBoundsIndex = GetCameraSortingLayerBoundsIndex(m_Renderer2DData);
        }

        internal void Setup(bool useDepth)
        {
            m_NeedsDepth = useDepth;
        }

        private void CopyCameraSortingLayerRenderTexture(ScriptableRenderContext context, RenderingData renderingData, RenderBufferStoreAction mainTargetStoreAction)
        {
            var cmd = renderingData.commandBuffer;

            this.CreateCameraSortingLayerRenderTexture(renderingData, cmd, m_Renderer2DData.cameraSortingLayerDownsamplingMethod);

            Material copyMaterial = m_SamplingMaterial;
            int passIndex = 0;
            if (m_Renderer2DData.cameraSortingLayerDownsamplingMethod != Downsampling._4xBox)
            {
                copyMaterial = m_BlitMaterial;
                passIndex = colorAttachmentHandle.rt.filterMode == FilterMode.Bilinear ? 1 : 0;
            }

            Blitter.BlitCameraTexture(cmd, colorAttachmentHandle, m_Renderer2DData.cameraSortingLayerRenderTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, copyMaterial, passIndex);
            CoreUtils.SetRenderTarget(cmd,
                colorAttachmentHandle, RenderBufferLoadAction.Load, mainTargetStoreAction,
                depthAttachmentHandle, RenderBufferLoadAction.Load, mainTargetStoreAction,
                ClearFlag.None, Color.clear);
            cmd.SetGlobalTexture(m_Renderer2DData.cameraSortingLayerRenderTarget.name, m_Renderer2DData.cameraSortingLayerRenderTarget.nameID);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public static short GetCameraSortingLayerBoundsIndex(Renderer2DData rendererData)
        {
            SortingLayer[] sortingLayers = Light2DManager.GetCachedSortingLayer();
            for (short i = 0; i < sortingLayers.Length; i++)
            {
                if (sortingLayers[i].id == rendererData.cameraSortingLayerTextureBound)
                    return (short)sortingLayers[i].value;
            }

            return short.MinValue;
        }

        private void DetermineWhenToResolve(int startIndex, int batchesDrawn, int batchCount, LayerBatch[] layerBatches,
            out int resolveDuringBatch, out bool resolveIsAfterCopy)
        {
            bool anyLightWithVolumetricShadows = false;
            var lights = m_Renderer2DData.lightCullResult.visibleLights;
            for (int i = 0; i < lights.Count; i++)
            {
                anyLightWithVolumetricShadows = lights[i].renderVolumetricShadows;
                if (anyLightWithVolumetricShadows)
                    break;
            }

            var lastVolumetricLightBatch = -1;
            if (anyLightWithVolumetricShadows)
            {
                for (int i = startIndex + batchesDrawn - 1; i >= startIndex; i--)
                {
                    if (layerBatches[i].lightStats.totalVolumetricUsage > 0)
                    {
                        lastVolumetricLightBatch = i;
                        break;
                    }
                }
            }

            if (m_Renderer2DData.useCameraSortingLayerTexture)
            {
                var cameraSortingLayerBoundsIndex = GetCameraSortingLayerBoundsIndex(m_Renderer2DData);
                var copyBatch = -1;
                for (int i = startIndex; i < startIndex + batchesDrawn; i++)
                {
                    var layerBatch = layerBatches[i];
                    if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex <= layerBatch.layerRange.upperBound)
                    {
                        copyBatch = i;
                        break;
                    }
                }

                resolveIsAfterCopy = copyBatch > lastVolumetricLightBatch;
                resolveDuringBatch = resolveIsAfterCopy ? copyBatch : lastVolumetricLightBatch;
            }
            else
            {
                resolveDuringBatch = lastVolumetricLightBatch;
                resolveIsAfterCopy = false;
            }
        }

        private void Render(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData, ref FilteringSettings filterSettings, DrawingSettings drawSettings)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            var activeDebugHandler = GetActiveDebugHandler(cameraData);
            if (activeDebugHandler != null)
            {
                UniversalRenderingData universalRenderingData = renderingData.universalRenderingData;
                RenderStateBlock renderStateBlock = new RenderStateBlock();
                var debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(context,
                    ref universalRenderingData.cullResults, ref drawSettings, ref filterSettings, ref renderStateBlock);
                debugRendererLists.DrawWithRendererList(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer));
            }
            else
            {
                var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                var rl = context.CreateRendererList(ref param);
                cmd.DrawRendererList(rl);
            }
        }

        private int DrawLayerBatches(
            LayerBatch[] layerBatches,
            int batchCount,
            int startIndex,
            CommandBuffer cmd,
            ScriptableRenderContext context,
            ref RenderingData renderingData,
            ref DrawingSettings normalsDrawSettings,
            ref DrawingSettings drawSettings,
            ref RenderTextureDescriptor desc)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();

            var debugHandler = GetActiveDebugHandler(cameraData);
            bool drawLights = debugHandler?.IsLightingActive ?? true;
            var batchesDrawn = 0;
            var rtCount = 0U;

            // Account for sprite mask interaction with normals. Only clear normals at the start as we require stencil for sprite mask in different layer batches
            bool normalsFirstClear = true;

            // Draw lights
            using (new ProfilingScope(cmd, m_ProfilingDrawLights))
            {
                for (var i = startIndex; i < batchCount; ++i)
                {
                    ref var layerBatch = ref layerBatches[i];

                    var blendStyleMask = layerBatch.lightStats.blendStylesUsed;
                    var blendStyleCount = 0U;
                    while (blendStyleMask > 0)
                    {
                        blendStyleCount += blendStyleMask & 1;
                        blendStyleMask >>= 1;
                    }

                    rtCount += blendStyleCount;

                    if (rtCount > LayerUtility.maxTextureCount)
                        break;

                    batchesDrawn++;

                    if (layerBatch.useNormals)
                    {
                        LayerUtility.GetFilterSettings(m_Renderer2DData, ref layerBatch, out var filterSettings);
                        var depthTarget = m_NeedsDepth ? depthAttachmentHandle : null;
                        this.RenderNormals(context, renderingData, normalsDrawSettings, filterSettings, depthTarget, normalsFirstClear);
						normalsFirstClear = false;
                    }

                    using (new ProfilingScope(cmd, m_ProfilingDrawLightTextures))
                    {
                        this.RenderLights(renderingData, cmd, ref layerBatch, ref desc);
                    }
                }
            }

            // Determine when to resolve in case we use MSAA
            var msaaEnabled = renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1;
            var isFinalBatchSet = startIndex + batchesDrawn >= batchCount;
            var resolveDuringBatch = -1;
            var resolveIsAfterCopy = false;
            if (msaaEnabled && isFinalBatchSet)
                DetermineWhenToResolve(startIndex, batchesDrawn, batchCount, layerBatches, out resolveDuringBatch, out resolveIsAfterCopy);


            // Draw renderers
            var blendStylesCount = m_Renderer2DData.lightBlendStyles.Length;
            using (new ProfilingScope(cmd, m_ProfilingDrawRenderers))
            {
                RenderBufferStoreAction initialStoreAction;
                if (msaaEnabled)
                    initialStoreAction = resolveDuringBatch < startIndex ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.StoreAndResolve;
                else
                    initialStoreAction = RenderBufferStoreAction.Store;
                CoreUtils.SetRenderTarget(cmd,
                    colorAttachmentHandle, RenderBufferLoadAction.Load, initialStoreAction,
                    depthAttachmentHandle, RenderBufferLoadAction.Load, initialStoreAction,
                    ClearFlag.None, Color.clear);

                for (var i = startIndex; i < startIndex + batchesDrawn; i++)
                {
                    using (new ProfilingScope(cmd, m_ProfilingDrawLayerBatch))
                    {
                        // This is a local copy of the array element (it's a struct). Remember to add a ref here if you need to modify the real thing.
                        var layerBatch = layerBatches[i];

                        if (layerBatch.lightStats.useLights)
                        {
                            for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                            {
                                var blendStyleMask = (uint)(1 << blendStyleIndex);
                                var blendStyleUsed = (layerBatch.lightStats.blendStylesUsed & blendStyleMask) > 0;

                                if (blendStyleUsed)
                                {
                                    var identifier = layerBatch.GetRTId(cmd, desc, blendStyleIndex);
                                    cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], identifier);
                                }

                                RendererLighting.EnableBlendStyle(CommandBufferHelpers.GetRasterCommandBuffer(cmd), blendStyleIndex, blendStyleUsed);
                            }
                        }
                        else
                        {
                            for (var blendStyleIndex = 0; blendStyleIndex < k_ShapeLightTextureIDs.Length; blendStyleIndex++)
                            {
                                cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);
                                RendererLighting.EnableBlendStyle(CommandBufferHelpers.GetRasterCommandBuffer(cmd), blendStyleIndex, blendStyleIndex == 0);
                            }
                        }

                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        short cameraSortingLayerBoundsIndex = GetCameraSortingLayerBoundsIndex(m_Renderer2DData);

                        RenderBufferStoreAction copyStoreAction;
                        if (msaaEnabled)
                            copyStoreAction = resolveDuringBatch == i && resolveIsAfterCopy ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.StoreAndResolve;
                        else
                            copyStoreAction = RenderBufferStoreAction.Store;


                        LayerUtility.GetFilterSettings(m_Renderer2DData, ref layerBatch, out var filterSettings);

                        Render(context, cmd, ref renderingData, ref filterSettings, drawSettings);

                        if (m_Renderer2DData.useCameraSortingLayerTexture)
                        {
                            if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex <= layerBatch.layerRange.upperBound)
                            {
                                CopyCameraSortingLayerRenderTexture(context, renderingData, copyStoreAction);
                            }
                        }

                        RendererLighting.DisableAllKeywords(CommandBufferHelpers.GetRasterCommandBuffer(cmd));

                        // Draw light volumes
                        if (drawLights && (layerBatch.lightStats.totalVolumetricUsage > 0))
                        {
                            var sampleName = "Render 2D Light Volumes";
                            cmd.BeginSample(sampleName);

                            RenderBufferStoreAction storeAction;
                            if (msaaEnabled)
                                storeAction = resolveDuringBatch == i && !resolveIsAfterCopy ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.StoreAndResolve;
                            else
                                storeAction = RenderBufferStoreAction.Store;
                            this.RenderLightVolumes(renderingData, cmd, ref layerBatch, colorAttachmentHandle.nameID, depthAttachmentHandle.nameID,
                                RenderBufferStoreAction.Store, storeAction, false, m_Renderer2DData.lightCullResult.visibleLights);

                            cmd.EndSample(sampleName);
                        }
                    }
                }
            }

            for (var i = startIndex; i < startIndex + batchesDrawn; ++i)
            {
                ref var layerBatch = ref layerBatches[i];
                layerBatch.ReleaseRT(cmd);
            }

            return batchesDrawn;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var isLitView = true;

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera && UnityEditor.SceneView.currentDrawingSceneView != null)
                isLitView = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                isLitView = false;
#endif
            var camera = renderingData.cameraData.camera;
            var filterSettings = FilteringSettings.defaultValue;
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;

            LayerUtility.InitializeBudget(m_Renderer2DData.lightRenderTextureMemoryBudget);
            ShadowRendering.InitializeBudget(m_Renderer2DData.shadowRenderTextureMemoryBudget);
            RendererLighting.lightBatch.Reset();

            // Set screenParams when pixel perfect camera is used with the reference resolution
            camera.TryGetComponent(out PixelPerfectCamera pixelPerfectCamera);
            if (pixelPerfectCamera != null && pixelPerfectCamera.enabled && pixelPerfectCamera.offscreenRTSize != Vector2Int.zero)
            {
                var cameraWidth = pixelPerfectCamera.offscreenRTSize.x;
                var cameraHeight = pixelPerfectCamera.offscreenRTSize.y;
                renderingData.commandBuffer.SetGlobalVector(ShaderPropertyId.screenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
            }

            var isSceneLit = m_Renderer2DData.lightCullResult.IsSceneLit();
            if (isSceneLit && isLitView)
            {
                var combinedDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                var normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                var sortSettings = combinedDrawSettings.sortingSettings;
                RendererLighting.GetTransparencySortingMode(m_Renderer2DData, camera, ref sortSettings);
                combinedDrawSettings.sortingSettings = sortSettings;
                normalsDrawSettings.sortingSettings = sortSettings;

                var cmd = renderingData.commandBuffer;
                cmd.SetGlobalFloat(k_HDREmulationScaleID, m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalColor(k_RendererColorID, Color.white);
                cmd.SetGlobalTexture(k_FalloffLookupID, m_FallOffLookup);
                cmd.SetGlobalTexture(k_LightLookupID, Light2DLookupTexture.GetLightLookupTexture());
                RendererLighting.SetLightShaderGlobals(m_Renderer2DData, CommandBufferHelpers.GetRasterCommandBuffer(cmd));

                var desc = this.GetBlendStyleRenderTextureDesc(renderingData);

                ShadowRendering.CallOnBeforeRender(renderingData.cameraData.camera, m_Renderer2DData.lightCullResult);

                var layerBatches = LayerUtility.CalculateBatches(m_Renderer2DData, out var batchCount);
                var batchesDrawn = 0;

                for (var i = 0; i < batchCount; i += batchesDrawn)
                    batchesDrawn = DrawLayerBatches(layerBatches, batchCount, i, cmd, context, ref renderingData, ref normalsDrawSettings, ref combinedDrawSettings, ref desc);

                RendererLighting.DisableAllKeywords(CommandBufferHelpers.GetRasterCommandBuffer(cmd));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
            else
            {
                var unlitDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                var msaaEnabled = renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1;
                var storeAction = msaaEnabled ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;

                var sortSettings = unlitDrawSettings.sortingSettings;
                RendererLighting.GetTransparencySortingMode(m_Renderer2DData, camera, ref sortSettings);
                unlitDrawSettings.sortingSettings = sortSettings;

                var cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, m_ProfilingSamplerUnlit))
                {
                    CoreUtils.SetRenderTarget(cmd,
                        colorAttachmentHandle, RenderBufferLoadAction.Load, storeAction,
                        depthAttachmentHandle, RenderBufferLoadAction.Load, storeAction,
                        ClearFlag.None, Color.clear);

                    cmd.SetGlobalColor(k_RendererColorID, Color.white);

                    for (var blendStyleIndex = 0; blendStyleIndex < k_ShapeLightTextureIDs.Length; blendStyleIndex++)
                    {
                        if (blendStyleIndex == 0)
                            cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);

                        RendererLighting.EnableBlendStyle(CommandBufferHelpers.GetRasterCommandBuffer(cmd), blendStyleIndex, blendStyleIndex == 0);
                    }
                }

                RendererLighting.DisableAllKeywords(CommandBufferHelpers.GetRasterCommandBuffer(cmd));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Profiler.BeginSample("Render Sprites Unlit");
                if (m_Renderer2DData.useCameraSortingLayerTexture)
                {
                    filterSettings.sortingLayerRange = new SortingLayerRange(short.MinValue, m_CameraSortingLayerBoundsIndex);
                    Render(context, cmd, ref renderingData, ref filterSettings, unlitDrawSettings);

                    CopyCameraSortingLayerRenderTexture(context, renderingData, storeAction);

                    filterSettings.sortingLayerRange = new SortingLayerRange((short)(m_CameraSortingLayerBoundsIndex + 1), short.MaxValue);
                    Render(context, cmd, ref renderingData, ref filterSettings, unlitDrawSettings);
                }
                else
                {
                    Render(context, cmd, ref renderingData, ref filterSettings, unlitDrawSettings);
                }
                Profiler.EndSample();
            }

            filterSettings.sortingLayerRange = SortingLayerRange.all;

            RendererList objectsWithErrorRendererList = RendererList.nullRendererList;
            RenderingUtils.CreateRendererListObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None, ref objectsWithErrorRendererList);
            RenderingUtils.DrawRendererListObjectsWithError(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), ref objectsWithErrorRendererList);
        }

        Renderer2DData IRenderPass2D.rendererData
        {
            get { return m_Renderer2DData; }
        }

        public void Dispose()
        {
            m_Renderer2DData.normalsRenderTarget?.Release();
            m_Renderer2DData.normalsRenderTarget = null;
            m_Renderer2DData.cameraSortingLayerRenderTarget?.Release();
            m_Renderer2DData.cameraSortingLayerRenderTarget = null;
        }
    }
}
