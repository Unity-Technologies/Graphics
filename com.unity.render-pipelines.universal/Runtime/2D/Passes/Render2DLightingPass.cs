using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Render2DLightingPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");
        private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        private static readonly int k_UseSceneLightingID = Shader.PropertyToID("_UseSceneLighting");
        private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");
        private static readonly int k_CameraSortingLayerTextureID = Shader.PropertyToID("_CameraSortingLayerTexture");

        private static readonly int[] k_ShapeLightTextureIDs =
        {
            Shader.PropertyToID("_ShapeLightTexture0"),
            Shader.PropertyToID("_ShapeLightTexture1"),
            Shader.PropertyToID("_ShapeLightTexture2"),
            Shader.PropertyToID("_ShapeLightTexture3")
        };

        private static readonly ShaderTagId k_CombinedRenderingPassNameOld = new ShaderTagId("Lightweight2D");
        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        private static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_CombinedRenderingPassName, k_CombinedRenderingPassNameOld };

        private static readonly ProfilingSampler m_ProfilingDrawLights = new ProfilingSampler("Draw 2D Lights");
        private static readonly ProfilingSampler m_ProfilingDrawLightTextures = new ProfilingSampler("Draw 2D Lights Textures");
        private static readonly ProfilingSampler m_ProfilingDrawRenderers = new ProfilingSampler("Draw All Renderers");
        private static readonly ProfilingSampler m_ProfilingDrawLayerBatch = new ProfilingSampler("Draw Layer Batch");
        private static readonly ProfilingSampler m_ProfilingSamplerUnlit = new ProfilingSampler("Render Unlit");

        Material m_BlitMaterial;
        Material m_SamplingMaterial;

        private readonly Renderer2DData m_Renderer2DData;

        private bool m_HasValidDepth;

        public Render2DLightingPass(Renderer2DData rendererData, Material blitMaterial, Material samplingMaterial)
        {
            m_Renderer2DData = rendererData;
            m_BlitMaterial = blitMaterial;
            m_SamplingMaterial = samplingMaterial;
        }

        internal void Setup(bool hasValidDepth)
        {
            m_HasValidDepth = hasValidDepth;
        }

        private void GetTransparencySortingMode(Camera camera, ref SortingSettings sortingSettings)
        {
            var mode = camera.transparencySortMode;

            if (mode == TransparencySortMode.Default)
            {
                mode = m_Renderer2DData.transparencySortMode;
                if (mode == TransparencySortMode.Default)
                    mode = camera.orthographic ? TransparencySortMode.Orthographic : TransparencySortMode.Perspective;
            }

            if (mode == TransparencySortMode.Perspective)
            {
                sortingSettings.distanceMetric = DistanceMetric.Perspective;
            }
            else if (mode == TransparencySortMode.Orthographic)
            {
                sortingSettings.distanceMetric = DistanceMetric.Orthographic;
            }
            else
            {
                sortingSettings.distanceMetric = DistanceMetric.CustomAxis;
                sortingSettings.customAxis = m_Renderer2DData.transparencySortAxis;
            }
        }

        private void CopyCameraSortingLayerRenderTexture(ScriptableRenderContext context, RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.Clear();
            this.CreateCameraSortingLayerRenderTexture(renderingData, cmd, m_Renderer2DData.cameraSortingLayerDownsamplingMethod);

            Material copyMaterial = m_Renderer2DData.cameraSortingLayerDownsamplingMethod == Downsampling._4xBox ? m_SamplingMaterial : m_BlitMaterial;
            RenderingUtils.Blit(cmd, colorAttachment, m_Renderer2DData.cameraSortingLayerRenderTarget.id, copyMaterial, 0, false, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            cmd.SetRenderTarget(colorAttachment);
            cmd.SetGlobalTexture(k_CameraSortingLayerTextureID, m_Renderer2DData.cameraSortingLayerRenderTarget.id);
            context.ExecuteCommandBuffer(cmd);
        }

        private short GetCameraSortingLayerBoundsIndex()
        {
            SortingLayer[] sortingLayers = Light2DManager.GetCachedSortingLayer();
            for (short i = 0; i < sortingLayers.Length; i++)
            {
                if (sortingLayers[i].id == m_Renderer2DData.cameraSortingLayerTextureBound)
                    return (short)sortingLayers[i].value;
            }

            return short.MinValue;
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
                        var depthTarget = m_HasValidDepth ? depthAttachment : BuiltinRenderTextureType.None;
                        this.RenderNormals(context, renderingData, normalsDrawSettings, filterSettings, depthTarget, cmd, layerBatch.lightStats);
                    }

                    using (new ProfilingScope(cmd, m_ProfilingDrawLightTextures))
                    {
                        this.RenderLights(renderingData, cmd, layerBatch.startLayerID, ref layerBatch, ref desc);
                    }
                }
            }

            // Draw renderers
            var blendStylesCount = m_Renderer2DData.lightBlendStyles.Length;
            using (new ProfilingScope(cmd, m_ProfilingDrawRenderers))
            {
                cmd.SetRenderTarget(colorAttachment, depthAttachment);

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


                        short cameraSortingLayerBoundsIndex = GetCameraSortingLayerBoundsIndex();
                        // If our camera sorting layer texture bound is inside our batch we need to break up the DrawRenderers into two batches
                        if (cameraSortingLayerBoundsIndex >= layerBatch.layerRange.lowerBound && cameraSortingLayerBoundsIndex < layerBatch.layerRange.upperBound && m_Renderer2DData.useCameraSortingLayerTexture)
                        {
                            filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, cameraSortingLayerBoundsIndex);
                            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
                            CopyCameraSortingLayerRenderTexture(context, renderingData);

                            filterSettings.sortingLayerRange = new SortingLayerRange((short)(cameraSortingLayerBoundsIndex + 1), layerBatch.layerRange.upperBound);
                            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
                        }
                        else
                        {
                            filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound);
                            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
                            if (cameraSortingLayerBoundsIndex == layerBatch.layerRange.upperBound && m_Renderer2DData.useCameraSortingLayerTexture)
                                CopyCameraSortingLayerRenderTexture(context, renderingData);
                        }

                        // Draw light volumes
                        if (layerBatch.lightStats.totalVolumetricUsage > 0)
                        {
                            var sampleName = "Render 2D Light Volumes";
                            cmd.BeginSample(sampleName);

                            this.RenderLightVolumes(renderingData, cmd, layerBatch.startLayerID, layerBatch.endLayerValue, colorAttachment, depthAttachment, m_Renderer2DData.lightCullResult.visibleLights);

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

            var isSceneLit = m_Renderer2DData.lightCullResult.IsSceneLit();
            if (isSceneLit)
            {
                var combinedDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                var normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                var sortSettings = combinedDrawSettings.sortingSettings;
                GetTransparencySortingMode(camera, ref sortSettings);
                combinedDrawSettings.sortingSettings = sortSettings;
                normalsDrawSettings.sortingSettings = sortSettings;

                var cmd = CommandBufferPool.Get();
                cmd.SetGlobalFloat(k_HDREmulationScaleID, m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                cmd.SetGlobalColor(k_RendererColorID, Color.white);
                this.SetShapeLightShaderGlobals(cmd);

                var desc = this.GetBlendStyleRenderTextureDesc(renderingData);

                var layerBatches = LayerUtility.CalculateBatches(m_Renderer2DData.lightCullResult, out var batchCount);
                var batchesDrawn = 0;

                for (var i = 0; i < batchCount; i += batchesDrawn)
                    batchesDrawn = DrawLayerBatches(layerBatches, batchCount, i, cmd, context, ref renderingData, ref filterSettings, ref normalsDrawSettings, ref combinedDrawSettings, ref desc);

                this.ReleaseRenderTextures(cmd);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            else
            {
                var unlitDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);

                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSamplerUnlit))
                {
                    CoreUtils.SetRenderTarget(cmd, colorAttachment, depthAttachment, ClearFlag.None, Color.white);

                    cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                    cmd.SetGlobalColor(k_RendererColorID, Color.white);

                    for (var blendStyleIndex = 0; blendStyleIndex < k_ShapeLightTextureIDs.Length; blendStyleIndex++)
                    {
                        if (blendStyleIndex == 0)
                            cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);

                        RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleIndex == 0);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                Profiler.BeginSample("Render Sprites Unlit");
                context.DrawRenderers(renderingData.cullResults, ref unlitDrawSettings, ref filterSettings);
                Profiler.EndSample();
            }

            filterSettings.sortingLayerRange = SortingLayerRange.all;
            RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None);
        }

        Renderer2DData IRenderPass2D.rendererData
        {
            get { return m_Renderer2DData; }
        }
    }
}
