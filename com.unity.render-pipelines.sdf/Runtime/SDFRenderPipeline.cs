using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
[ExecuteInEditMode]
public class SDFRenderPipeline : RenderPipeline
{
    internal static SDFRenderPipelineAsset currentAsset
            => GraphicsSettings.currentRenderPipeline is SDFRenderPipelineAsset sdfAsset ? sdfAsset : null;

//    internal static HDRenderPipeline currentPipeline
//            => RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp ? hdrp : null;

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "My SETUP";
        cmd.ClearRenderTarget(false, true, currentAsset.clearColor);
        cmd.SetViewport(cameras[0].pixelRect);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        ScriptableCullingParameters scp;
        cameras[0].TryGetCullingParameters(out scp);
        CullingResults cullResults = context.Cull(ref scp);
        // DrawRendererSettings drawRenderSettings = new DrawRendererSettings();
        // context.DrawRenderers(cullResults.visibleRenderers);
        // context.DrawSkybox(cameras[0]);
        context.Submit();
    }
}
