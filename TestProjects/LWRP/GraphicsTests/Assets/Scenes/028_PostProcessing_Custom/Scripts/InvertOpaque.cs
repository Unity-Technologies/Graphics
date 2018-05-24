using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(InvertOpaqueRenderer), PostProcessEvent.BeforeTransparent, "Custom/LWtest/Invert Opaque")]
public sealed class InvertOpaque : PostProcessEffectSettings
{
    public BoolParameter enable = new BoolParameter();
}

public sealed class InvertOpaqueRenderer : PostProcessEffectRenderer<InvertOpaque>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/LWtest/InvertOpaque"));
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
