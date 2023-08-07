using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Rendering;
using UnityEngine.U2D;

namespace UnityEngine.Rendering.Universal
{
    internal class Render2DLightingPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");
        private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        private static readonly int k_UseSceneLightingID = Shader.PropertyToID("_UseSceneLighting");
        private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");

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
        private bool m_NeedsDepth;
        private short m_CameraSortingLayerBoundsIndex;

        public Render2DLightingPass(Renderer2DData rendererData, Material blitMaterial, Material samplingMaterial)
        {
            m_Renderer2DData = rendererData;
            m_BlitMaterial = blitMaterial;
            m_SamplingMaterial = samplingMaterial;

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
            var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
            if (activeDebugHandler != null)
            {
                RenderStateBlock renderStateBlock = new RenderStateBlock();
                var debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(context, ref renderingData, ref drawSettings, ref filterSettings, ref renderStateBlock);
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
            ref FilteringSettings filterSettings,
            ref DrawingSettings normalsDrawSettings,
            ref DrawingSettings drawSettings,
            ref RenderTextureDescriptor desc)
        {
            var debugHandler = GetActiveDebugHandler(ref renderingData);
            bool drawLights = debugHandler?.IsLightingActive ?? true;
            var batchesDrawn = 0;
            var rtCount = 0U;

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

                    if (layerBatch.lightStats.totalNormalMapUsage > 0)
                    {
                        filterSettings.sortingLayerRange = layerBatch.layerRange;
                        var depthTarget = m_NeedsDepth ? depthAttachmentHandle : null;
                        this.RenderNormals(context, renderingData, normalsDrawSettings, filterSettings, depthTarget, layerBatch.lightStats);
                    }

                    using (new ProfilingScope(cmd, m_ProfilingDrawLightTextures))
                    {
                        this.RenderLights(renderingData, cmd, layerBatch.startLayerID, ref layerBatch, ref desc);
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

                        if (layerBatch.lightStats.totalLights > 0)
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

                                RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);
                            }
                        }
                        else
                        {
                            for (var blendStyleIndex = 0; blendStyleIndex < k_ShapeLightTextureIDs.Length; blendStyleIndex++)
                            {
                                cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);
                                RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleIndex == 0);
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
                        // If our camera sorting layer texture bound is inside our batch we need to break up the DrawRenderers into two batches
                        if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex < layerBatch.layerRange.upperBound && m_Renderer2DData.useCameraSortingLayerTexture)
                        {
                            filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, cameraSortingLayerBoundsIndex);
                            Render(context, cmd, ref renderingData, ref filterSettings, drawSettings);
                            CopyCameraSortingLayerRenderTexture(context, renderingData, copyStoreAction);

                            filterSettings.sortingLayerRange = new SortingLayerRange((short)(cameraSortingLayerBoundsIndex + 1), layerBatch.layerRange.upperBound);
                            Render(context, cmd, ref renderingData, ref filterSettings, drawSettings);
                        }
                        else
                        {
                            filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound);
                            Render(context, cmd, ref renderingData, ref filterSettings, drawSettings);

                            if (cameraSortingLayerBoundsIndex == layerBatch.layerRange.upperBound && m_Renderer2DData.useCameraSortingLayerTexture)
                                CopyCameraSortingLayerRenderTexture(context, renderingData, copyStoreAction);
                        }

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
                            this.RenderLightVolumes(renderingData, cmd, layerBatch.startLayerID, layerBatch.endLayerValue, colorAttachmentHandle.nameID, depthAttachmentHandle.nameID,
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

        void CalculateFrustumCornersPerspective(Camera camera, float distance, NativeArray<Vector3> corners)
        {
            float verticalFieldOfView = camera.fieldOfView;  // This will need to be converted if user direction is allowed

            float halfHeight = Mathf.Tan(0.5f * verticalFieldOfView * Mathf.Deg2Rad) * distance;
            float halfWidth = halfHeight * camera.aspect;

            corners[0] = new Vector3(halfWidth,  halfHeight, distance);
            corners[1] = new Vector3(halfWidth, -halfHeight, distance);
            corners[2] = new Vector3(-halfWidth,  halfHeight, distance);
            corners[3] = new Vector3(-halfWidth, -halfHeight, distance);
        }

        void CalculateFrustumCornersOrthographic(Camera camera, float distance, NativeArray<Vector3> corners)
        {
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;

            corners[0] = new Vector3(halfWidth, halfHeight, distance);
            corners[1] = new Vector3(halfWidth, -halfHeight, distance);
            corners[2] = new Vector3(-halfWidth, halfHeight, distance);
            corners[3] = new Vector3(-halfWidth, -halfHeight, distance);
        }

        private Bounds CalculateWorldSpaceBounds(Camera camera, ILight2DCullResult cullResult)
        {
            // TODO: This will need to take into account on screen lights as shadows can be cast from offscreen.

            const int k_Corners = 4;
            NativeArray<Vector3> nearCorners = new NativeArray<Vector3>(k_Corners, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<Vector3> farCorners = new NativeArray<Vector3>(k_Corners, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (camera.orthographic)
            {
                CalculateFrustumCornersOrthographic(camera, camera.nearClipPlane, nearCorners);
                CalculateFrustumCornersOrthographic(camera, camera.farClipPlane, farCorners);
            }
            else
            {
                CalculateFrustumCornersPerspective(camera, camera.nearClipPlane, nearCorners);
                CalculateFrustumCornersPerspective(camera, camera.farClipPlane, farCorners);
            }

            Vector3 minCorner = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxCorner = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < k_Corners; i++)
            {
                maxCorner = Vector3.Max(maxCorner, nearCorners[i]);
                maxCorner = Vector3.Max(maxCorner, farCorners[i]);
                minCorner = Vector3.Min(minCorner, nearCorners[i]);
                minCorner = Vector3.Min(minCorner, farCorners[i]);
            }

            nearCorners.Dispose();
            farCorners.Dispose();

            // Transform the point from camera space to world space
            maxCorner = camera.transform.TransformPoint(maxCorner);
            minCorner = camera.transform.TransformPoint(minCorner);

            // TODO: Iterate through the lights
            for (int i = 0; i < cullResult.visibleLights.Count; i++)
            {
                Vector3 lightPos = cullResult.visibleLights[i].transform.position;
                maxCorner = Vector3.Max(maxCorner, lightPos);
                minCorner = Vector3.Min(minCorner, lightPos);
            }

            Vector3 center = 0.5f * (minCorner + maxCorner);
            Vector3 size = maxCorner - minCorner;

            return new Bounds(center, size);;
        }

        private void CallOnBeforeRender(Camera camera, ILight2DCullResult cullResult)
        {
            if (ShadowCasterGroup2DManager.shadowCasterGroups != null)
            {
                Bounds bounds = CalculateWorldSpaceBounds(camera, cullResult);

                List<ShadowCasterGroup2D> groups = ShadowCasterGroup2DManager.shadowCasterGroups;
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    ShadowCasterGroup2D group = groups[groupIndex];

                    List<ShadowCaster2D> shadowCasters = group.GetShadowCasters();
                    if (shadowCasters != null)
                    {
                        for (int shadowCasterIndex = 0; shadowCasterIndex < shadowCasters.Count; shadowCasterIndex++)
                        {
                            ShadowCaster2D shadowCaster = shadowCasters[shadowCasterIndex];
                            if (shadowCaster != null && shadowCaster.shadowCastingSource == ShadowCaster2D.ShadowCastingSources.ShapeProvider)
                            {
                                ShapeProviderUtility.CallOnBeforeRender(shadowCaster.shadowShape2DProvider, shadowCaster.shadowShape2DComponent, shadowCaster.m_ShadowMesh, bounds);
                            }
                        }
                    }
                }
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var isLitView = true;

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
                isLitView = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                isLitView = false;
#endif
            var camera = renderingData.cameraData.camera;
            var filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;

            LayerUtility.InitializeBudget(m_Renderer2DData.lightRenderTextureMemoryBudget);
            ShadowRendering.InitializeBudget(m_Renderer2DData.shadowRenderTextureMemoryBudget);
            RendererLighting.lightBatch.Reset();

            var isSceneLit = m_Renderer2DData.lightCullResult.IsSceneLit();
            if (isSceneLit)
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
                cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                cmd.SetGlobalColor(k_RendererColorID, Color.white);
                RendererLighting.SetLightShaderGlobals(m_Renderer2DData, cmd);

                var desc = this.GetBlendStyleRenderTextureDesc(renderingData);

                CallOnBeforeRender(renderingData.cameraData.camera, m_Renderer2DData.lightCullResult);

                var layerBatches = LayerUtility.CalculateBatches(m_Renderer2DData.lightCullResult, out var batchCount);
                var batchesDrawn = 0;

                for (var i = 0; i < batchCount; i += batchesDrawn)
                    batchesDrawn = DrawLayerBatches(layerBatches, batchCount, i, cmd, context, ref renderingData, ref filterSettings, ref normalsDrawSettings, ref combinedDrawSettings, ref desc);

                RendererLighting.DisableAllKeywords(cmd);
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

                    cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                    cmd.SetGlobalColor(k_RendererColorID, Color.white);

                    for (var blendStyleIndex = 0; blendStyleIndex < k_ShapeLightTextureIDs.Length; blendStyleIndex++)
                    {
                        if (blendStyleIndex == 0)
                            cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);

                        RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleIndex == 0);
                    }
                }

                RendererLighting.DisableAllKeywords(cmd);
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
