Shader "Graph/UnityEngine.MaterialGraph.MetallicMasterNode1cbd7aed-56c9-4a01-866e-a6b04a03c1b6" 
{
	Properties 
	{
		[NonModifiableTextureData] Texture2D_Texture2D_D281CEC8_Uniform("Texture2D", 2D) = "white" {}

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

		UNITY_DECLARE_TEX2D(Texture2D_Texture2D_D281CEC8_Uniform);




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
			float4 Sample2DTexture_1221CD9A_rgba = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_D281CEC8_Uniform,uv0.xy);
			o.Albedo = Sample2DTexture_1221CD9A_rgba;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
