Shader "Graph/UnityEngine.MaterialGraph.MetallicMasterNode8d0a8307-a3eb-4434-be6c-c952f4588d6c" 
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
			float4 Sample2DTexture_1EF145E2_rgba = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_ED131035_Uniform,uv0.xy);
			o.Emission = Sample2DTexture_1EF145E2_rgba;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
