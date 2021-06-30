using System.Collections.Generic;
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
        private static readonly int k_ShapeLightTexture0ID = Shader.PropertyToID("_ShapeLightTexture0");
        private static readonly int k_ShapeLightTexture1ID = Shader.PropertyToID("_ShapeLightTexture1");
        private static readonly int k_ShapeLightTexture2ID = Shader.PropertyToID("_ShapeLightTexture2");
        private static readonly int k_ShapeLightTexture3ID = Shader.PropertyToID("_ShapeLightTexture3");

        private static readonly ShaderTagId k_CombinedRenderingPassNameOld = new ShaderTagId("Lightweight2D");
        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        private static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_CombinedRenderingPassName, k_CombinedRenderingPassNameOld };

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render 2D Lighting");
        private static readonly ProfilingSampler m_ProfilingSamplerUnlit = new ProfilingSampler("Render Unlit");

        private readonly Renderer2DData m_Renderer2DData;

        public Render2DLightingPass(Renderer2DData rendererData)
        {
            m_Renderer2DData = rendererData;
        }

        private void GetTransparencySortingMode(Camera camera, ref SortingSettings sortingSettings)
        {
            var mode = m_Renderer2DData.transparencySortMode;

            if (mode == TransparencySortMode.Default)
            {
                mode = camera.orthographic ? TransparencySortMode.Orthographic : TransparencySortMode.Perspective;
            }

            switch (mode)
            {
                case TransparencySortMode.Perspective:
                    sortingSettings.distanceMetric = DistanceMetric.Perspective;
                    break;
                case TransparencySortMode.Orthographic:
                    sortingSettings.distanceMetric = DistanceMetric.Orthographic;
                    break;
                default:
                    sortingSettings.distanceMetric = DistanceMetric.CustomAxis;
                    sortingSettings.customAxis = m_Renderer2DData.transparencySortAxis;
                    break;
            }
        }

        private bool CompareLightsInLayer(int layerIndex1, int layerIndex2, SortingLayer[] sortingLayers)
        {
            var layerId1 = sortingLayers[layerIndex1].id;
            var layerId2 = sortingLayers[layerIndex2].id;
            foreach (var light in m_Renderer2DData.lightCullResult.visibleLights)
            {
                if (light.IsLitLayer(layerId1) != light.IsLitLayer(layerId2))
                    return false;
            }
            return true;
        }

        private int FindUpperBoundInBatch(int startLayerIndex, SortingLayer[] sortingLayers)
        {
            // start checking at the next layer
            for (var i = startLayerIndex+1; i < sortingLayers.Length; i++)
            {
                if(!CompareLightsInLayer(startLayerIndex, i, sortingLayers))
                    return i-1;
            }
            return sortingLayers.Length-1;
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
            var cachedSortingLayers = Light2DManager.GetCachedSortingLayer();
            var camera = renderingData.cameraData.camera;

            var filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;

            var isSceneLit = m_Renderer2DData.lightCullResult.IsSceneLit();
            if (isSceneLit)
            {
                var cmd = CommandBufferPool.Get();
                cmd.Clear();

                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    this.CreateNormalMapRenderTexture(renderingData, cmd);

                    cmd.SetGlobalFloat(k_HDREmulationScaleID, m_Renderer2DData.hdrEmulationScale);
                    cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / m_Renderer2DData.hdrEmulationScale);
                    cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                    cmd.SetGlobalColor(k_RendererColorID, Color.white);
                    this.SetShapeLightShaderGlobals(cmd);

                    context.ExecuteCommandBuffer(cmd);

                    var combinedDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                    var normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                    var sortSettings = combinedDrawSettings.sortingSettings;
                    GetTransparencySortingMode(camera, ref sortSettings);
                    combinedDrawSettings.sortingSettings = sortSettings;
                    normalsDrawSettings.sortingSettings = sortSettings;

                    var blendStylesCount = m_Renderer2DData.lightBlendStyles.Length;
                    for (var i = 0; i < cachedSortingLayers.Length;)
                    {
                        var layerToRender = cachedSortingLayers[i].id;
                        var lightStats = m_Renderer2DData.lightCullResult.GetLightStatsByLayer(layerToRender);

                        cmd.Clear();
                        for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                        {
                            var blendStyleMask = (uint) (1 << blendStyleIndex);
                            var blendStyleUsed = (lightStats.blendStylesUsed & blendStyleMask) > 0;

                            if (blendStyleUsed && !m_Renderer2DData.lightBlendStyles[blendStyleIndex].hasRenderTarget)
                            {
                                this.CreateBlendStyleRenderTexture(renderingData, cmd, blendStyleIndex);
                            }

                            RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);
                        }

                        context.ExecuteCommandBuffer(cmd);

                        // find the highest layer that share the same set of lights as this layer
                        var upperLayerInBatch = FindUpperBoundInBatch(i, cachedSortingLayers);
                        // Some renderers override their sorting layer value with short.MinValue or short.MaxValue.
                        // When drawing the first sorting layer, we should include the range from short.MinValue to layerValue.
                        // Similarly, when drawing the last sorting layer, include the range from layerValue to short.MaxValue.
                        var startLayerValue = (short) cachedSortingLayers[i].value;
                        var lowerBound = (i == 0) ? short.MinValue : startLayerValue;
                        var endLayerValue = (short) cachedSortingLayers[upperLayerInBatch].value;
                        var upperBound = (upperLayerInBatch == cachedSortingLayers.Length - 1) ? short.MaxValue : endLayerValue;
                        // renderer within this range share the same set of lights so they should be rendered together
                        filterSettings.sortingLayerRange = new SortingLayerRange(lowerBound, upperBound);

                        // Start Rendering
                        if (lightStats.totalNormalMapUsage > 0)
                            this.RenderNormals(context, renderingData.cullResults, normalsDrawSettings, filterSettings, depthAttachment);

                        cmd.Clear();
                        if (lightStats.totalLights > 0)
                        {
                            this.RenderLights(renderingData, cmd, layerToRender, lightStats.blendStylesUsed);
                        }
                        else
                        {
                            this.ClearDirtyLighting(cmd, lightStats.blendStylesUsed);
                        }

                        CoreUtils.SetRenderTarget(cmd, colorAttachment, depthAttachment, ClearFlag.None, Color.white);
                        context.ExecuteCommandBuffer(cmd);

                        Profiler.BeginSample("RenderSpritesWithLighting - Draw Transparent Renderers");
                        context.DrawRenderers(renderingData.cullResults, ref combinedDrawSettings, ref filterSettings);
                        Profiler.EndSample();

                        if (lightStats.totalVolumetricUsage > 0)
                        {
                            cmd.Clear();
                            this.RenderLightVolumes(renderingData, cmd, layerToRender, colorAttachment, depthAttachment, lightStats.blendStylesUsed);
                            context.ExecuteCommandBuffer(cmd);
                            cmd.Clear();
                        }

                        // move on to the next one
                        i = upperLayerInBatch + 1;
                    }

                    cmd.Clear();
                    Profiler.BeginSample("RenderSpritesWithLighting - Release RenderTextures");
                    this.ReleaseRenderTextures(cmd);
                    Profiler.EndSample();
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                filterSettings.sortingLayerRange = SortingLayerRange.all;
                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None);
            }
            else
            {
                var unlitDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                var sortSettings = unlitDrawSettings.sortingSettings;
                GetTransparencySortingMode(camera, ref sortSettings);
                unlitDrawSettings.sortingSettings = sortSettings;

                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSamplerUnlit))
                {
                    CoreUtils.SetRenderTarget(cmd, colorAttachment, depthAttachment, ClearFlag.None, Color.white);
                    cmd.SetGlobalTexture(k_ShapeLightTexture0ID, Texture2D.blackTexture);
                    cmd.SetGlobalTexture(k_ShapeLightTexture1ID, Texture2D.blackTexture);
                    cmd.SetGlobalTexture(k_ShapeLightTexture2ID, Texture2D.blackTexture);
                    cmd.SetGlobalTexture(k_ShapeLightTexture3ID, Texture2D.blackTexture);
                    cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                    cmd.SetGlobalColor(k_RendererColorID, Color.white);
                    cmd.EnableShaderKeyword("USE_SHAPE_LIGHT_TYPE_0");
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                Profiler.BeginSample("Render Sprites Unlit");
                    context.DrawRenderers(renderingData.cullResults, ref unlitDrawSettings, ref filterSettings);
                Profiler.EndSample();

                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None);
            }
        }

        Renderer2DData IRenderPass2D.rendererData
        {
            get { return m_Renderer2DData; }
        }
    }
}
