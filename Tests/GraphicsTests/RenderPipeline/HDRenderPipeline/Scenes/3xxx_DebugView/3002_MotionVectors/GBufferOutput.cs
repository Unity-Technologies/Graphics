using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(GBufferOutputRenderer), PostProcessEvent.AfterStack, "HDRP/HDTest/GBufferOutput")]
public class GBufferOutput : PostProcessEffectSettings
{
    [Range(0,7)]
    public IntParameter gBufferIndex = new IntParameter();
}

public sealed class GBufferOutputRenderer : PostProcessEffectRenderer<GBufferOutput>
{

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/HDRP/HDTest/GBufferOutput"));
        sheet.properties.SetFloat(Shader.PropertyToID("_BufferIndex"), settings.gBufferIndex);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }

}
