using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;


public class RendererLighting : MonoBehaviour
{
    //static CommandBuffer m_CommandBuffer;
    //static SortingLayer[] m_SortingLayers;
    //static SortingLayerRange m_SortingLayerRange;

    //static ShaderTagId m_CombinedShapeLightPassName = new ShaderTagId("CombinedShapeLight");

    //public static void Initialize(RenderPipeline2DAsset asset)
    //{
    //    m_CommandBuffer = new CommandBuffer();
    //    m_CommandBuffer.name = "Lights and Shadows Command Buffer";
    //    m_SortingLayers = SortingLayer.layers;

    //    RendererPointLights.Initialize(asset);
    //    RendererShapeLights.Initialize(asset);
    //}

    //public static void RenderLights(ScriptableRenderContext renderContext, Camera camera, CullingResults cullResults, RenderPipeline2DAsset rpAsset)
    //{
    //    #if UNITY_EDITOR
    //    //    if(!Application.isPlaying)
    //    //        m_SortingLayers = SortingLayer.layers;
    //    #endif

    //    Profiler.BeginSample("RenderSpritesWithLighting - Prepare");
    //    DrawingSettings drawSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), new SortingSettings(camera));
    //    FilteringSettings filterSettings = new FilteringSettings();
    //    filterSettings.renderQueueRange = RenderQueueRange.all;
    //    filterSettings.layerMask = -1;
    //    filterSettings.renderingLayerMask = 0xFFFFFFFF;
    //    bool renderBuffersDirty = true;
    //    Profiler.EndSample();

    //    Profiler.BeginSample("RenderSpritesWithLighting - Create Render Textures");
    //    RendererPointLights.CreateRenderTextures(rpAsset);
    //    RendererShapeLights.CreateRenderTextures(rpAsset);
    //    Profiler.EndSample();

    //    m_CommandBuffer.Clear();
    //    RendererPointLights.SetShaderGlobals(m_CommandBuffer);
    //    RendererShapeLights.SetShaderGlobals(m_CommandBuffer);
    //    renderContext.ExecuteCommandBuffer(m_CommandBuffer);

    //    for (int i = 0; i < m_SortingLayers.Length; i++)
    //    {
    //        m_CommandBuffer.Clear();
    //        bool isLitLayer = true;
    //        int layerToRender = m_SortingLayers[i].id;
    //        short layerValue = (short)m_SortingLayers[i].value;
            
    //        m_SortingLayerRange = new SortingLayerRange(layerValue, layerValue);
    //        filterSettings.sortingLayerRange = m_SortingLayerRange;
            
    //        if (isLitLayer)
    //        {
    //            RendererPointLights.RenderLights(m_CommandBuffer, renderContext, cullResults, drawSettings, filterSettings, layerToRender, camera);
    //            RendererShapeLights.RenderLights(m_CommandBuffer, layerToRender);
    //            renderBuffersDirty = true;
    //        }
    //        else if (renderBuffersDirty)
    //        {
    //            RendererShapeLights.Clear(m_CommandBuffer);
    //            renderBuffersDirty = false;
    //        }

    //        m_CommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
    //        renderContext.ExecuteCommandBuffer(m_CommandBuffer);

    //        // This should have an optimization where I can determine if this needs to be called
    //        Profiler.BeginSample("RenderSpritesWithLighting - Draw Renderers");
    //        drawSettings.SetShaderPassName(0, m_CombinedShapeLightPassName);
    //        renderContext.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
    //        Profiler.EndSample();
    //    }

    //    Profiler.BeginSample("RenderSpritesWithLighting - Release RenderTextures");
    //    RendererPointLights.ReleaseRenderTextures();
    //    RendererShapeLights.ReleaseRenderTextures();
    //    Profiler.EndSample();
    //}
}
