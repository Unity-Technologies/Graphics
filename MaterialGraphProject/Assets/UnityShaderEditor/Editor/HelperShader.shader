Shader "Graph/UnityEngine.MaterialGraph.MetallicMasterNodefb4e5d27-b55b-4c39-84a8-ae6dcd6d7631" 
{
	Properties 
	{
		[NonModifiableTextureData] Texture_5537060b_db74_4900_8f2b_178ba97b7f11_Uniform("", 2D) = "white" {}
		Cubemap_ee09e335_3be2_46ee_9d59_ce2673aeedf9_Uniform("", Cube) = "" {}

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

		inline float unity_remap_float (float arg1, float2 arg2, float2 arg3)
		{
			return arg1 * ((arg3.y - arg3.x) / (arg2.y - arg2.x)) + arg3.x;
		}

		sampler2D Texture_5537060b_db74_4900_8f2b_178ba97b7f11_Uniform;
		samplerCUBE Cubemap_ee09e335_3be2_46ee_9d59_ce2673aeedf9_Uniform;


	struct Input 
	{
			float4 color : COLOR;
			half4 meshUV0;
			float3 worldViewDir;
			float3 worldNormal;

	};

	void vert (inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input,o);
			o.meshUV0 = v.texcoord;

	}
  
	void surf (Input IN, inout SurfaceOutputStandard o) 
	{
			half4 uv0 = IN.meshUV0;
			float3 worldSpaceViewDirection = IN.worldViewDir;
			float3 worldSpaceNormal = normalize(IN.worldNormal);
			float4 Texture_5537060b_db74_4900_8f2b_178ba97b7f11 = tex2D (Texture_5537060b_db74_4900_8f2b_178ba97b7f11_Uniform, uv0.xy);
			float Remap_26b4b7a4_1c55_4c03_9830_fc5d53b3a504_Output = unity_remap_float (Texture_5537060b_db74_4900_8f2b_178ba97b7f11.r, float2 (0,1), float2 (7,-2));
			float4 Cubemap_ee09e335_3be2_46ee_9d59_ce2673aeedf9 = texCUBElod (Cubemap_ee09e335_3be2_46ee_9d59_ce2673aeedf9_Uniform, float4(reflect(-worldSpaceViewDirection, worldSpaceNormal).xyz,Remap_26b4b7a4_1c55_4c03_9830_fc5d53b3a504_Output));
			o.Emission = Cubemap_ee09e335_3be2_46ee_9d59_ce2673aeedf9;

	}
	ENDCG
}


	FallBack "Diffuse"
}
