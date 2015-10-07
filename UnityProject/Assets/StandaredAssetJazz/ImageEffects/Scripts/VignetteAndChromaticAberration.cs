using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent (typeof(Camera))]
[AddComponentMenu ("Image Effects/Camera/Vignette and Chromatic Aberration")]
public class VignetteAndChromaticAberration : PostEffectsBase {

	public enum AberrationMode {
		Simple = 0,
		Advanced = 1,
	}

	public AberrationMode mode = AberrationMode.Simple;
	
	public float intensity = 0.375f; // intensity == 0 disables pre pass (optimization)
	public float chromaticAberration = 0.2f;
	public float axialAberration = 0.5f;

	public float blur = 0.0f; // blur == 0 disables blur pass (optimization)
	public float blurSpread = 0.75f;

	public float luminanceDependency = 0.25f;

	public float blurDistance = 2.5f;
		
	public Shader vignetteShader;
	private Material vignetteMaterial;
	
	public Shader separableBlurShader;
	private Material separableBlurMaterial;	
	
	public Shader chromAberrationShader;
	private Material chromAberrationMaterial;


    public override bool CheckResources (){	
		 CheckSupport (false);	
	
		vignetteMaterial = CheckShaderAndCreateMaterial (vignetteShader, vignetteMaterial);
		separableBlurMaterial = CheckShaderAndCreateMaterial (separableBlurShader, separableBlurMaterial);
		chromAberrationMaterial = CheckShaderAndCreateMaterial (chromAberrationShader, chromAberrationMaterial);
		
		if (!isSupported)
			ReportAutoDisable ();
		return isSupported;		
	}
	
	void OnRenderImage ( RenderTexture source ,   RenderTexture destination  ){	
		if( CheckResources () == false) {
			Graphics.Blit (source, destination);
			return;
		}

		int rtW = source.width;
		int rtH = source.height;
				
		bool  doPrepass = (Mathf.Abs(blur)>0.0f || Mathf.Abs(intensity)>0.0f);

		float widthOverHeight = (1.0f * rtW) / (1.0f * rtH);
		float oneOverBaseSize = 1.0f / 512.0f;				
		
		RenderTexture color = null;
		RenderTexture color2a = null;
		RenderTexture color2b = null;

		if (doPrepass) {
			color = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);	
		
			// Blur corners
			if (Mathf.Abs (blur)>0.0f) {
				color2a = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);		

				Graphics.Blit (source, color2a, chromAberrationMaterial, 0);

				for(int i = 0; i < 2; i++) {	// maybe make iteration count tweakable
					separableBlurMaterial.SetVector ("offsets",new Vector4 (0.0f, blurSpread * oneOverBaseSize, 0.0f, 0.0f));	
					color2b = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);
					Graphics.Blit (color2a, color2b, separableBlurMaterial);
					RenderTexture.ReleaseTemporary (color2a);

					separableBlurMaterial.SetVector ("offsets",new Vector4 (blurSpread * oneOverBaseSize / widthOverHeight, 0.0f, 0.0f, 0.0f));	
					color2a = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);
					Graphics.Blit (color2b, color2a, separableBlurMaterial);	
					RenderTexture.ReleaseTemporary (color2b);
				}	
			}

			vignetteMaterial.SetFloat ("_Intensity", intensity); 		// intensity for vignette
			vignetteMaterial.SetFloat ("_Blur", blur); 					// blur intensity
			vignetteMaterial.SetTexture ("_VignetteTex", color2a);	// blurred texture

			Graphics.Blit (source, color, vignetteMaterial, 0); 		// prepass blit: darken & blur corners
		}		

		chromAberrationMaterial.SetFloat ("_ChromaticAberration", chromaticAberration);
		chromAberrationMaterial.SetFloat ("_AxialAberration", axialAberration);
		chromAberrationMaterial.SetVector ("_BlurDistance", new Vector2 (-blurDistance, blurDistance));
		chromAberrationMaterial.SetFloat ("_Luminance", 1.0f/Mathf.Max(Mathf.Epsilon, luminanceDependency));

		if(doPrepass) color.wrapMode = TextureWrapMode.Clamp;
		else source.wrapMode = TextureWrapMode.Clamp;		
		Graphics.Blit (doPrepass ? color : source, destination, chromAberrationMaterial, mode == AberrationMode.Advanced ? 2 : 1);	
		
		RenderTexture.ReleaseTemporary (color);
		RenderTexture.ReleaseTemporary (color2a);
	}

}
