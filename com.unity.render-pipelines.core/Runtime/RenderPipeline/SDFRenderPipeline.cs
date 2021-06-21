using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
[ExecuteInEditMode]
public class SDFRenderPipeline : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "My SETUP";
        cmd.ClearRenderTarget(false, true, Color.green);
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