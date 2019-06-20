using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(InvertOpaqueRenderer), PostProcessEvent.BeforeTransparent, "Custom/LWtest/Invert Opaque")]
public sealed class InvertOpaque : PostProcessEffectSettings
{}

public sealed class InvertOpaqueRenderer : PostProcessEffectRenderer<InvertOpaque> 
{
	internal static readonly int opaqueTemp = Shader.PropertyToID("_MainTex");
	
	public override void Render(PostProcessRenderContext context){
		var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/LWtest/InvertOpaque"));

		bool srcEqualsDest = context.destination == context.source;
		
		if(srcEqualsDest)
			context.GetScreenSpaceTemporaryRT(context.command, opaqueTemp);
				
		context.command.BlitFullscreenTriangle(context.source, srcEqualsDest ? opaqueTemp : context.destination, sheet, 0);

		if (srcEqualsDest)
		{
			context.command.BlitFullscreenTriangle(opaqueTemp, context.destination);
			context.command.ReleaseTemporaryRT(opaqueTemp);
		}

	}

}
