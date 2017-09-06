Shader "SplitCombine" 
{
	Properties 
	{
		[NonModifiableTextureData] Texture2D_Texture2D_FCC444C7_Uniform("Texture2D", 2D) = "white" {}

	}	
	
SubShader 
{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}

		Blend One Zero

		Cull Back

		ZTest LEqual

		ZWrite On


	LOD 200
	
	CGPROGRAM
	#pragma target 3.0
	#pragma surface surf Standard vertex:vert
	#pragma glsl
	#pragma debug

		UNITY_DECLARE_TEX2D(Texture2D_Texture2D_FCC444C7_Uniform);

		void Unity_Combine_float(float first, float second, float third, float fourth, out float4 result)
		{
		    result = float4(first, second, third, fourth);
		}



	struct Input 
	{
			float4 color : COLOR;
			half4 meshUV0;

	};

	void vert (inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input,o);
			o.meshUV0 = v.texcoord;

	}
  
	void surf (Input IN, inout SurfaceOutputStandard o) 
	{
			half4 uv0 = IN.meshUV0;
			float4 Sample2DTexture_E81B6D9_RGBA = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_FCC444C7_Uniform,uv0.xy);
			float Sample2DTexture_E81B6D9_R = Sample2DTexture_E81B6D9_RGBA.r;
			float Sample2DTexture_E81B6D9_G = Sample2DTexture_E81B6D9_RGBA.g;
			float Sample2DTexture_E81B6D9_B = Sample2DTexture_E81B6D9_RGBA.b;
			float Sample2DTexture_E81B6D9_A = Sample2DTexture_E81B6D9_RGBA.a;
			float Split_89C93FBB_R = Sample2DTexture_E81B6D9_RGBA[0];
			float Split_89C93FBB_G = Sample2DTexture_E81B6D9_RGBA[1];
			float Split_89C93FBB_B = Sample2DTexture_E81B6D9_RGBA[2];
			float Split_89C93FBB_A = Sample2DTexture_E81B6D9_RGBA[3];
			float4 Combine_4EFD61AA_result;
			Unity_Combine_float(Split_89C93FBB_R, Split_89C93FBB_G, Split_89C93FBB_B, Split_89C93FBB_A, Combine_4EFD61AA_result);
			o.Albedo = Combine_4EFD61AA_result;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
