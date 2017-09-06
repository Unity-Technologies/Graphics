Shader "TextureSample" 
{
	Properties 
	{
		[NonModifiableTextureData] Texture2D_Texture2D_ED131035_Uniform("Texture2D", 2D) = "white" {}

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

		UNITY_DECLARE_TEX2D(Texture2D_Texture2D_ED131035_Uniform);




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
			float4 Sample2DTexture_1EF145E2_RGBA = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_ED131035_Uniform,uv0.xy);
			float Sample2DTexture_1EF145E2_R = Sample2DTexture_1EF145E2_RGBA.r;
			float Sample2DTexture_1EF145E2_G = Sample2DTexture_1EF145E2_RGBA.g;
			float Sample2DTexture_1EF145E2_B = Sample2DTexture_1EF145E2_RGBA.b;
			float Sample2DTexture_1EF145E2_A = Sample2DTexture_1EF145E2_RGBA.a;
			o.Emission = Sample2DTexture_1EF145E2_RGBA;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
