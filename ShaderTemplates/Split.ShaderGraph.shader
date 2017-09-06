Shader "Split" 
{
	Properties 
	{

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





	struct Input 
	{
			float4 color : COLOR;

	};

	void vert (inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input,o);

	}
  
	void surf (Input IN, inout SurfaceOutputStandard o) 
	{
			float3 Vector3_Vector3_3E03E6E5_Uniform = float3 (0.25, 0.5, 0.75);
			float Split_CDE20F4C_R = Vector3_Vector3_3E03E6E5_Uniform[0];
			float Split_CDE20F4C_G = Vector3_Vector3_3E03E6E5_Uniform[1];
			float Split_CDE20F4C_B = Vector3_Vector3_3E03E6E5_Uniform[2];
			float Split_CDE20F4C_A = 1.0;
			o.Albedo = Split_CDE20F4C_A;
			o.Emission = Split_CDE20F4C_R;
			o.Metallic = Split_CDE20F4C_B;
			o.Smoothness = Split_CDE20F4C_G;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
