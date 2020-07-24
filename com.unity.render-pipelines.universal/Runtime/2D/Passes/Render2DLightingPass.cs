using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Render2DLightingPass : ScriptableRenderPass, IRenderPass2D
    {
        static SortingLayer[] s_SortingLayers;
        Renderer2DData m_Renderer2DData;
        static readonly ShaderTagId k_CombinedRenderingPassNameOld = new ShaderTagId("Lightweight2D");
        static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_CombinedRenderingPassName, k_CombinedRenderingPassNameOld };

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render 2D Lighting");
        private static readonly ProfilingSampler m_ProfilingSamplerUnlit = new ProfilingSampler("Render Unlit");

        public Render2DLightingPass(Renderer2DData rendererData)
        {
            if (s_SortingLayers == null)
                s_SortingLayers = SortingLayer.layers;

            m_Renderer2DData = rendererData;
        }

        public void GetTransparencySortingMode(Camera camera, ref SortingSettings sortingSettings)
        {
            TransparencySortMode mode = camera.transparencySortMode;

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

        bool CompareLightsInLayer(int layerIndex1, int layerIndex2)
        {
            var layerId1 = s_SortingLayers[layerIndex1].id;
            var layerId2 = s_SortingLayers[layerIndex2].id;
            foreach (var lightStyle in Light2DManager.lights)
            {
                foreach (var light in lightStyle)
                {
                    if (light.IsLitLayer(layerId1) != light.IsLitLayer(layerId2))
                        return false;
                }
            }
            return true;
        }

        int FindUpperBoundInBatch(int startLayerIndex)
        {
            // start checking at the next layer
            for (var i = startLayerIndex+1; i < s_SortingLayers.Length; i++)
            {
                if(!CompareLightsInLayer(startLayerIndex, i))
                    return i-1;
            }
            return s_SortingLayers.Length-1;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            bool isLitView = true;

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
                isLitView = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                isLitView = false;

            if (!Application.isPlaying)
                s_SortingLayers = SortingLayer.layers;
#endif
            Camera camera = renderingData.cameraData.camera;

            FilteringSettings filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;


            bool isSceneLit = Light2D.IsSceneLit(camera);
            if (isSceneLit)
            {
                m_Renderer2DData.InitializeTransient();

                CommandBuffer cmd = CommandBufferPool.Get();
                cmd.Clear();

                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    this.CreateNormalMapRenderTexture(renderingData, cmd);

                    cmd.SetGlobalFloat("_HDREmulationScale", m_Renderer2DData.hdrEmulationScale);
                    cmd.SetGlobalFloat("_InverseHDREmulationScale", 1.0f / m_Renderer2DData.hdrEmulationScale);
                    cmd.SetGlobalFloat("_UseSceneLighting", isLitView ? 1.0f : 0.0f);
                    cmd.SetGlobalColor("_RendererColor", Color.white);
                    this.SetShapeLightShaderGlobals(cmd);

                    context.ExecuteCommandBuffer(cmd);

                    DrawingSettings combinedDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                    DrawingSettings normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                    SortingSettings sortSettings = combinedDrawSettings.sortingSettings;
                    GetTransparencySortingMode(camera, ref sortSettings);
                    combinedDrawSettings.sortingSettings = sortSettings;
                    normalsDrawSettings.sortingSettings = sortSettings;

                    var blendStylesCount = m_Renderer2DData.lightBlendStyles.Length;
                    for (var i = 0; i < s_SortingLayers.Length;)
                    {
                        var layerToRender = s_SortingLayers[i].id;
                        var lightStats = Light2D.GetLightStatsByLayer(layerToRender, camera);

                        cmd.Clear();
                        for (int blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                        {
                            uint blendStyleMask = (uint) (1 << blendStyleIndex);
                            bool blendStyleUsed = (lightStats.blendStylesUsed & blendStyleMask) > 0;

                            if (blendStyleUsed && !m_Renderer2DData.lightBlendStyles[blendStyleIndex].hasRenderTarget)
                            {
                                this.CreateBlendStyleRenderTexture(renderingData, cmd, blendStyleIndex);
                            }

                            RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);
                        }

                        context.ExecuteCommandBuffer(cmd);

                        // find the highest layer that share the same set of lights as this layer
                        var upperLayerInBatch = FindUpperBoundInBatch(i);
                        // Some renderers override their sorting layer value with short.MinValue or short.MaxValue.
                        // When drawing the first sorting layer, we should include the range from short.MinValue to layerValue.
                        // Similarly, when drawing the last sorting layer, include the range from layerValue to short.MaxValue.
                        var startLayerValue = (short) s_SortingLayers[i].value;
                        var lowerBound = (i == 0) ? short.MinValue : startLayerValue;
                        var endLayerValue = (short) s_SortingLayers[upperLayerInBatch].value;
                        var upperBound = (upperLayerInBatch == s_SortingLayers.Length - 1) ? short.MaxValue : endLayerValue;
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
                DrawingSettings unlitDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData,
                    SortingCriteria.CommonTransparent);

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSamplerUnlit))
                {
                    CoreUtils.SetRenderTarget(cmd, colorAttachment, depthAttachment, ClearFlag.None, Color.white);
                    cmd.SetGlobalTexture("_ShapeLightTexture0", Texture2D.blackTexture);
                    cmd.SetGlobalTexture("_ShapeLightTexture1", Texture2D.blackTexture);
                    cmd.SetGlobalTexture("_ShapeLightTexture2", Texture2D.blackTexture);
                    cmd.SetGlobalTexture("_ShapeLightTexture3", Texture2D.blackTexture);
                    cmd.SetGlobalFloat("_UseSceneLighting", isLitView ? 1.0f : 0.0f);
                    cmd.SetGlobalColor("_RendererColor", Color.white);
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
