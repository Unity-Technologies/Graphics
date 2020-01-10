using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Deferred2DShadingPass : ScriptableRenderPass
{
    static RenderTargetHandle s_BaseColorTarget;
    static SortingLayer[] s_SortingLayers;
    static readonly ShaderTagId k_GBufferPassName = new ShaderTagId("Universal2DGBuffer");
    static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_GBufferPassName };

    Renderer2DData m_RendererData;
    Material m_LightMaterial;

    public Deferred2DShadingPass(Renderer2DData rendererData)
    {
        if (s_SortingLayers == null)
            s_SortingLayers = SortingLayer.layers;

        m_RendererData = rendererData;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("2D Deferred Shading");

        // Create g-buffer RTs.
        if (s_BaseColorTarget.id == 0)
            s_BaseColorTarget.Init("_BaseColor");

        ref var targetDescriptor = ref renderingData.cameraData.cameraTargetDescriptor;
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(targetDescriptor.width, targetDescriptor.height);
        descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;
        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;
        descriptor.dimension = TextureDimension.Tex2D;

        cmd.GetTemporaryRT(s_BaseColorTarget.id, descriptor);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            s_SortingLayers = SortingLayer.layers;
#endif

        FilteringSettings filterSettings = new FilteringSettings();
        filterSettings.renderQueueRange = RenderQueueRange.all;
        filterSettings.layerMask = -1;
        filterSettings.renderingLayerMask = 0xFFFFFFFF;
        filterSettings.sortingLayerRange = SortingLayerRange.all;

        for (int i = 0; i < s_SortingLayers.Length; ++i)
        {
            string cmdSample = "Sorting Layer - " + s_SortingLayers[i].name;
            cmd.BeginSample(cmdSample);

            // Some renderers override their sorting layer value with short.MinValue or short.MaxValue.
            // When drawing the first sorting layer, we should include the range from short.MinValue to layerValue.
            // Similarly, when drawing the last sorting layer, include the range from layerValue to short.MaxValue.
            short layerValue = (short)s_SortingLayers[i].value;
            var lowerBound = (i == 0) ? short.MinValue : layerValue;
            var upperBound = (i == s_SortingLayers.Length - 1) ? short.MaxValue : layerValue;
            filterSettings.sortingLayerRange = new SortingLayerRange(lowerBound, upperBound);

            Color clearColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            CoreUtils.SetRenderTarget(cmd, s_BaseColorTarget.Identifier(), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Color, clearColor);

            cmd.EndSample(cmdSample);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Draw the g-buffer.
            DrawingSettings drawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);

            // Render the lights.
            CoreUtils.SetRenderTarget(cmd, colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.white);

            bool anyGlobalLightDrawn = false;
            var blendStyles = m_RendererData.lightBlendStyles;
            for (int j = 0; j < blendStyles.Length; ++j)
            {
                if (!blendStyles[j].enabled)
                    continue;

                string sampleName = "Blend Style - " + blendStyles[j].name;
                cmd.BeginSample(sampleName);

                var lights = Light2D.GetLightsByBlendStyle(j);
                foreach (var light in lights)
                {
                    if (light == null
                        || !light.IsLitLayer(s_SortingLayers[i].id)
                        || light.lightType != Light2D.LightType.Global && !light.IsLightVisible(renderingData.cameraData.camera))
                    {
                        continue;
                    }

                    // HACK: Remove later.
                    if (light.lightType != Light2D.LightType.Global)
                        continue;

                    if (m_LightMaterial == null)
                        m_LightMaterial = new Material(m_RendererData.globalLightShader);

                    cmd.SetGlobalColor("_LightColor", light.color);

                    Mesh lightMesh = RenderingUtils.fullscreenMesh;
                    if (lightMesh == null)
                        continue;

                    cmd.DrawMesh(lightMesh, Matrix4x4.identity, m_LightMaterial);
                    anyGlobalLightDrawn = true;
                }

                cmd.EndSample(sampleName);
            }

            if (!anyGlobalLightDrawn)
            {
                cmd.SetGlobalColor("_LightColor", Color.black);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_LightMaterial);
            }
        }

        cmd.ReleaseTemporaryRT(s_BaseColorTarget.id);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
