using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Render2DLightingPass : ScriptableRenderPass
    {
        static SortingLayer[] s_SortingLayers;
        Renderer2DData m_Renderer2DData;
        static readonly ShaderTagId k_CombinedRenderingPassNameOld = new ShaderTagId("Lightweight2D");
        static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_CombinedRenderingPassName, k_CombinedRenderingPassNameOld };

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
                RendererLighting.Setup(renderingData, m_Renderer2DData);

                CommandBuffer cmd = CommandBufferPool.Get("Render 2D Lighting");
                cmd.Clear();

                RendererLighting.CreateNormalMapRenderTexture(cmd);

                cmd.SetGlobalFloat("_HDREmulationScale", m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalFloat("_InverseHDREmulationScale", 1.0f / m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalFloat("_UseSceneLighting", isLitView ? 1.0f : 0.0f);
                RendererLighting.SetShapeLightShaderGlobals(cmd);

                context.ExecuteCommandBuffer(cmd);

                DrawingSettings combinedDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                DrawingSettings normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                SortingSettings sortSettings = combinedDrawSettings.sortingSettings;
                GetTransparencySortingMode(camera, ref sortSettings);
                combinedDrawSettings.sortingSettings = sortSettings;
                combinedDrawSettings.sortingSettings = sortSettings;

                const int blendStylesCount = 4;
                bool[] hasBeenInitialized = new bool[blendStylesCount];
                for (int i = 0; i < s_SortingLayers.Length; i++)
                {

                    // Some renderers override their sorting layer value with short.MinValue or short.MaxValue.
                    // When drawing the first sorting layer, we should include the range from short.MinValue to layerValue.
                    // Similarly, when drawing the last sorting layer, include the range from layerValue to short.MaxValue.
                    short layerValue = (short)s_SortingLayers[i].value;
                    var lowerBound = (i == 0) ? short.MinValue : layerValue;
                    var upperBound = (i == s_SortingLayers.Length - 1) ? short.MaxValue : layerValue;
                    filterSettings.sortingLayerRange = new SortingLayerRange(lowerBound, upperBound);

                    int layerToRender = s_SortingLayers[i].id;

                    Light2D.LightStats lightStats;
                    lightStats = Light2D.GetLightStatsByLayer(layerToRender);

                    // Allocate our blend style textures
                    cmd.Clear();
                    for (int blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                    {
                        uint blendStyleMask = (uint)(1 << blendStyleIndex);
                        if ((lightStats.blendStylesUsed & blendStyleMask) > 0 && !hasBeenInitialized[blendStyleIndex])
                        {
                            RendererLighting.CreateBlendStyleRenderTexture(cmd, blendStyleIndex);
                            hasBeenInitialized[blendStyleIndex] = true;
                        }
                    }
                    context.ExecuteCommandBuffer(cmd);


                    // Start Rendering
                    if (lightStats.totalNormalMapUsage > 0)
                        RendererLighting.RenderNormals(context, renderingData.cullResults, normalsDrawSettings, filterSettings);

                    cmd.Clear();
                    if (lightStats.totalLights > 0)
                    {
                        RendererLighting.RenderLights(camera, cmd, layerToRender, lightStats.blendStylesUsed);
                    }
                    else
                    {
                        RendererLighting.ClearDirtyLighting(cmd, lightStats.blendStylesUsed);
                    }

                    CoreUtils.SetRenderTarget(cmd, colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.white);
                    context.ExecuteCommandBuffer(cmd);

                    Profiler.BeginSample("RenderSpritesWithLighting - Draw Transparent Renderers");
                    context.DrawRenderers(renderingData.cullResults, ref combinedDrawSettings, ref filterSettings);
                    Profiler.EndSample();

                    if (lightStats.totalVolumetricUsage > 0)
                    {

                        cmd.Clear();
                        RendererLighting.RenderLightVolumes(camera, cmd, layerToRender, colorAttachment, lightStats.blendStylesUsed);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                    }
                }

                cmd.Clear();
                Profiler.BeginSample("RenderSpritesWithLighting - Release RenderTextures");
                RendererLighting.ReleaseRenderTextures(cmd);
                Profiler.EndSample();

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                filterSettings.sortingLayerRange = SortingLayerRange.all;
                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None);
            }
            else
            {
                CommandBuffer cmd = CommandBufferPool.Get("Render Unlit");
                DrawingSettings unlitDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);

                CoreUtils.SetRenderTarget(cmd, colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.white);
                cmd.SetGlobalTexture("_ShapeLightTexture0", Texture2D.blackTexture);
                cmd.SetGlobalTexture("_ShapeLightTexture1", Texture2D.blackTexture);
                cmd.SetGlobalTexture("_ShapeLightTexture2", Texture2D.blackTexture);
                cmd.SetGlobalTexture("_ShapeLightTexture3", Texture2D.blackTexture);
                cmd.SetGlobalFloat("_UseSceneLighting", isLitView ? 1.0f : 0.0f);
                cmd.EnableShaderKeyword("USE_SHAPE_LIGHT_TYPE_0");
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                Profiler.BeginSample("Render Sprites Unlit");
                context.DrawRenderers(renderingData.cullResults, ref unlitDrawSettings, ref filterSettings);
                Profiler.EndSample();

                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None);
            }
        }
    }
}
