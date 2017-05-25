Shader "Graph/Generated.MetallicMasterNode554b91b5-7a00-4f5f-b71a-f7729fcbdee8" 
{
	Properties 
	{
		[NonModifiableTextureData] TextureAsset_8f4a8771_6c8f_4107_9c0b_83da34d57a9b_Uniform("TextureAsset", 2D) = "white" {}

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

		#ifdef UNITY_COMPILER_HLSL
		Texture2D TextureAsset_8f4a8771_6c8f_4107_9c0b_83da34d57a9b_Uniform;
		#endif
		float HeightToNormal_81cdd9a9_500d_41e2_9771_df274a2363bd_texOffset;
		float HeightToNormal_81cdd9a9_500d_41e2_9771_df274a2363bd_strength;
		#ifdef UNITY_COMPILER_HLSL
		SamplerState my_linear_repeat_sampler;
		#endif

		#ifdef UNITY_COMPILER_HLSL
		#endif
		inline void unity_HeightToNormal (Texture2D heightmap, float2 texCoord, float texOffset, float strength, out float3 normalRes)
		{
		float2 offsetU = float2(texCoord.x + texOffset, texCoord.y);
		float2 offsetV = float2(texCoord.x, texCoord.y + texOffset);
		float normalSample = 0;
		float uSample = 0;
		float vSample = 0;
		normalSample = heightmap.Sample(my_linear_repeat_sampler, texCoord).r;
		uSample = heightmap.Sample(my_linear_repeat_sampler, offsetU).r;
		vSample = heightmap.Sample(my_linear_repeat_sampler, offsetV).r;
		float uMinusNormal = uSample - normalSample;
		float vMinusNormal = vSample - normalSample;
		uMinusNormal = uMinusNormal * strength;
		vMinusNormal = vMinusNormal * strength;
		float3 va = float3(1, 0, uMinusNormal);
		float3 vb = float3(0, 1, vMinusNormal);
		normalRes = cross(va, vb);
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
			float4 UV_de8d7b89_7b50_499d_aa73_0f2a725356e6_UV = uv0;
			 float3 HeightToNormal_81cdd9a9_500d_41e2_9771_df274a2363bd_normalRes;
			#ifdef UNITY_COMPILER_HLSL 
			unity_HeightToNormal (TextureAsset_8f4a8771_6c8f_4107_9c0b_83da34d57a9b_Uniform, UV_de8d7b89_7b50_499d_aa73_0f2a725356e6_UV, HeightToNormal_81cdd9a9_500d_41e2_9771_df274a2363bd_texOffset, HeightToNormal_81cdd9a9_500d_41e2_9771_df274a2363bd_strength, HeightToNormal_81cdd9a9_500d_41e2_9771_df274a2363bd_normalRes);
			 #endif
			o.Normal = HeightToNormal_81cdd9a9_500d_41e2_9771_df274a2363bd_normalRes;
			o.Normal += 1e-6;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
