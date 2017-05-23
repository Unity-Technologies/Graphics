Shader "Graph/UnityEngine.MaterialGraph.MetallicMasterNode56fdded2-bae1-4a2c-b2db-6e9c206bf8ff" 
{
	Properties 
	{
		Vector1_681e693a_7812_42ab_a37b_8a1edc00d63e_Uniform("", Float) = 0.005
		Vector1_bc1392c9_f4d7_47a8_8add_f4f4b00fdfe5_Uniform("", Float) = 0
		Vector1_36d8f7c7_3ada_4604_9149_df2ba1745b80_Uniform("", Float) = 8

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

		inline float4 unity_add_float (float4 arg1, float4 arg2)
		{
			return arg1 + arg2;
		}
		inline float2 unity_multiply_float (float2 arg1, float2 arg2)
		{
			return arg1 * arg2;
		}
		inline float2 unity_voronoi_noise_randomVector (float2 uv, float offset)
		{
			float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
			uv = frac(sin(mul(uv, m)) * 46839.32);
			return float2(sin(uv.y*+offset)*0.5+0.5, cos(uv.x*offset)*0.5+0.5);
		}
		inline float unity_voronoinoise_float (float2 uv, float angleOffset)
		{
			float2 g = floor(uv);
			float2 f = frac(uv);
			float t = 8.0;
			float3 res = float3(8.0, 0.0, 0.0);
			for(int y=-1; y<=1; y++)
			{
				for(int x=-1; x<=1; x++)
				{
					float2 lattice = float2(x,y);
					float2 offset = unity_voronoi_noise_randomVector(lattice + g, angleOffset);
					float d = distance(lattice + offset, f);
					if(d < res.x)
					{
						res = float3(d, offset.x, offset.y);
					}
				}
			}
			return res.x;
		}
		inline float unity_oneminus_float (float arg1)
		{
			return arg1 * -1 + 1;
		}
		inline float4 unity_multiply_float (float4 arg1, float4 arg2)
		{
			return arg1 * arg2;
		}
		inline float unity_subtract_float (float arg1, float arg2)
		{
			return arg1 - arg2;
		}
		inline float unity_multiply_float (float arg1, float arg2)
		{
			return arg1 * arg2;
		}

		float Vector1_681e693a_7812_42ab_a37b_8a1edc00d63e_Uniform;
		float Vector1_bc1392c9_f4d7_47a8_8add_f4f4b00fdfe5_Uniform;
		float Vector1_36d8f7c7_3ada_4604_9149_df2ba1745b80_Uniform;


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
			float4 Color_a5c79f09_af3c_413d_a03a_0fbcda1f305f_Uniform = float4 (0.5588235, 0.5588235, 0.5588235, 0);
			float Vector1_b212d213_1dc1_491d_8dbf_0add4ab24246_Uniform = 1;
			float Vector1_c753024a_f95d_4487_bcb7_13a86e18dd68_Uniform = 0;
			float4 Combine_523239ba_f790_4fca_a9e2_babb69c74568_Output = float4(Vector1_681e693a_7812_42ab_a37b_8a1edc00d63e_Uniform,Vector1_bc1392c9_f4d7_47a8_8add_f4f4b00fdfe5_Uniform,0.0, 0.0);
			float4 UV_2eb2b635_fc17_44ea_90c4_c1188f10857b_UV = uv0;
			float4 Add_2c8c5705_3762_4ed4_b2de_b611d458bd65_Output = unity_add_float (Combine_523239ba_f790_4fca_a9e2_babb69c74568_Output, UV_2eb2b635_fc17_44ea_90c4_c1188f10857b_UV);
			float4 Split_0c1c11c9_9ee2_40d8_9cb0_b913d2f3e22a = float4(Add_2c8c5705_3762_4ed4_b2de_b611d458bd65_Output);
			float2 Multiply_f3f41634_3594_48d4_b7b6_3104b523e7dd_Output = unity_multiply_float (Split_0c1c11c9_9ee2_40d8_9cb0_b913d2f3e22a.rg, float2 (20,20));
			float VoronoiNoise_175ef02c_a94e_44e5_9668_dff18e2112fe_Output = unity_voronoinoise_float (Multiply_f3f41634_3594_48d4_b7b6_3104b523e7dd_Output, _Time.w);
			float OneMinus_0b02f4c6_d95f_4305_9d7c_5e720ad0d2c4_Output = unity_oneminus_float (VoronoiNoise_175ef02c_a94e_44e5_9668_dff18e2112fe_Output);
			float4 Multiply_766862d4_1f32_4bdc_93ba_e17532ee9f8b_Output = unity_multiply_float (UV_2eb2b635_fc17_44ea_90c4_c1188f10857b_UV, float4 (20,20,0,0));
			float VoronoiNoise_b5d8acbb_4b86_4780_8ba2_9b79cdf9b0ba_Output = unity_voronoinoise_float (Multiply_766862d4_1f32_4bdc_93ba_e17532ee9f8b_Output, _Time.w);
			float OneMinus_95c0d854_a598_4448_b1ec_19f0ae2e3207_Output = unity_oneminus_float (VoronoiNoise_b5d8acbb_4b86_4780_8ba2_9b79cdf9b0ba_Output);
			float Subtract_d442f4ab_3183_4dba_88bb_12b82b1e4b22_Output = unity_subtract_float (OneMinus_0b02f4c6_d95f_4305_9d7c_5e720ad0d2c4_Output, OneMinus_95c0d854_a598_4448_b1ec_19f0ae2e3207_Output);
			float Multiply_223da849_596f_4a92_ac44_065f9082e924_Output = unity_multiply_float (Subtract_d442f4ab_3183_4dba_88bb_12b82b1e4b22_Output, Vector1_36d8f7c7_3ada_4604_9149_df2ba1745b80_Uniform);
			float4 Combine_261925f9_60e1_402b_9b71_69912cd8c284_Output = float4(Vector1_b212d213_1dc1_491d_8dbf_0add4ab24246_Uniform,Vector1_c753024a_f95d_4487_bcb7_13a86e18dd68_Uniform,Multiply_223da849_596f_4a92_ac44_065f9082e924_Output,0.0);
			float4 Split_637f528c_802b_4b35_abbe_e94b103a0f9a = float4(Combine_261925f9_60e1_402b_9b71_69912cd8c284_Output);
			float4 Combine_1399c2cd_55f2_48bf_a413_58c773e76556_Output = float4(Vector1_bc1392c9_f4d7_47a8_8add_f4f4b00fdfe5_Uniform,Vector1_681e693a_7812_42ab_a37b_8a1edc00d63e_Uniform,0.0, 0.0);
			float4 Add_3698bc9c_3a9c_4f77_86ad_50ac01252625_Output = unity_add_float (UV_2eb2b635_fc17_44ea_90c4_c1188f10857b_UV, Combine_1399c2cd_55f2_48bf_a413_58c773e76556_Output);
			float4 Multiply_94a1c8a0_7a01_42a5_baf8_3460cc52ccce_Output = unity_multiply_float (Add_3698bc9c_3a9c_4f77_86ad_50ac01252625_Output, float4 (20,20,0,0));
			float VoronoiNoise_041c0818_8a7a_490c_ae6b_362b2e498fa6_Output = unity_voronoinoise_float (Multiply_94a1c8a0_7a01_42a5_baf8_3460cc52ccce_Output, _Time.w);
			float OneMinus_b7c5d2e2_90e6_4cb2_be5c_3aa34ea18363_Output = unity_oneminus_float (VoronoiNoise_041c0818_8a7a_490c_ae6b_362b2e498fa6_Output);
			float Subtract_c4f59a4e_29c0_4f5e_92c7_52ab56cc6a23_Output = unity_subtract_float (OneMinus_b7c5d2e2_90e6_4cb2_be5c_3aa34ea18363_Output, OneMinus_95c0d854_a598_4448_b1ec_19f0ae2e3207_Output);
			float Multiply_f3bd2e5b_f8c4_486a_83c8_df44e04bc77a_Output = unity_multiply_float (Vector1_36d8f7c7_3ada_4604_9149_df2ba1745b80_Uniform, Subtract_c4f59a4e_29c0_4f5e_92c7_52ab56cc6a23_Output);
			float4 Combine_d1411122_6465_494a_88e6_98e96eba037a_Output = float4(Vector1_c753024a_f95d_4487_bcb7_13a86e18dd68_Uniform,Vector1_b212d213_1dc1_491d_8dbf_0add4ab24246_Uniform,Multiply_f3bd2e5b_f8c4_486a_83c8_df44e04bc77a_Output,0.0);
			float4 Split_bdd63fad_c244_45a4_ae4c_ed756a69ac03 = float4(Combine_d1411122_6465_494a_88e6_98e96eba037a_Output);
			float3 CrossProduct_0955bdd9_9b65_4141_b394_1b86aa4c7d3f_Output = cross (Split_637f528c_802b_4b35_abbe_e94b103a0f9a.rgb, Split_bdd63fad_c244_45a4_ae4c_ed756a69ac03.rgb);
			o.Albedo = Color_a5c79f09_af3c_413d_a03a_0fbcda1f305f_Uniform;
			o.Normal = CrossProduct_0955bdd9_9b65_4141_b394_1b86aa4c7d3f_Output;
			o.Normal += 1e-6;

	}
	ENDCG
}


	FallBack "Diffuse"
}
