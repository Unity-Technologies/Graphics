using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(HalfToneOpaqueRenderer), PostProcessEvent.BeforeStack, "Custom/LWtest/Haltone Opaque")]
public sealed class HalfToneOpaque : PostProcessEffectSettings {
	[Range(-2f, 2f), Tooltip("Pattern Midpoint")]
	public FloatParameter midpoint = new FloatParameter{ value = 0f };
	[Range(0.001f, 0.1f), Tooltip("Pattern Scale")]
	public FloatParameter scale = new FloatParameter{ value = 0.025f };
	[Range(1, 10), Tooltip("Steps")]
	public IntParameter steps = new IntParameter{ value = 4};
	public TextureParameter pattern = new TextureParameter{ value = null};
}

public sealed class HalfToneOpaqueRenderer : PostProcessEffectRenderer<HalfToneOpaque> {

	public override void Render(PostProcessRenderContext context){
		var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/LWtest/HalftoneOpaque"));
		sheet.properties.SetTexture ("_Pattern", settings.pattern);
		sheet.properties.SetFloat("_Blend", settings.midpoint);
		sheet.properties.SetFloat("_Scale", settings.scale);
		sheet.properties.SetFloat("_Steps", settings.steps);
		context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
	}

}
