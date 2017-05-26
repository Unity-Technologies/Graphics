Shader "Custom/LavaFlowMap" 
{
	Properties 
	{
		Texture_f6eb7ab6_0df6_4d1f_a833_847f9eefa1ce_Uniform("Texture", 2D) = "white" {}
		Vector1_48a064e9_29c4_4cdd_8bf7_34902bb50605_Uniform("Vector1", Float) = 1
		Texture_aa489395_d5b1_4bce_a08c_71ce4329894d_Uniform("albedo2", 2D) = "white" {}
		Texture_8304e17f_0ca3_45b8_9081_4e083e4ffba7_Uniform("albedo", 2D) = "white" {}
		Texture_73d7755c_9da7_46d2_90c6_35c2a880d380_Uniform("Normal2", 2D) = "bump" {}
		Texture_d6d1ed65_5575_444c_8f92_38740ace2353_Uniform("Normal", 2D) = "bump" {}
		[HDR]Color_0cf876a7_4590_4973_82b7_3f878b008b3c_Uniform("LavaColor1", Color) = (2,1.365517,0,0)
		[HDR]Color_d1ecd429_6e84_47b1_903b_60f454b7d326_Uniform("LavaColor2", Color) = (0,0,0,0)
		Texture_c384b973_02cf_4076_96d9_302f320a2dd5_Uniform("height1", 2D) = "white" {}
		Texture_3cc862ff_e5c3_40c2_a0f2_895da84195ea_Uniform("height", 2D) = "white" {}
		Vector1_2fa9bd83_0823_4530_9787_f0fab2f47430_Uniform("LavaAmount", Range(0, 8)) = 0.2
		Texture_ead3f7f8_56e5_41f9_8217_555fb21e0536_Uniform("roughness1", 2D) = "white" {}
		Texture_9dcd98df_78a5_4ad3_83dd_e3c00c88f898_Uniform("roughness", 2D) = "white" {}

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

		sampler2D Texture_f6eb7ab6_0df6_4d1f_a833_847f9eefa1ce_Uniform;
		float Vector1_48a064e9_29c4_4cdd_8bf7_34902bb50605_Uniform;
		sampler2D Texture_aa489395_d5b1_4bce_a08c_71ce4329894d_Uniform;
		sampler2D Texture_8304e17f_0ca3_45b8_9081_4e083e4ffba7_Uniform;
		sampler2D Texture_73d7755c_9da7_46d2_90c6_35c2a880d380_Uniform;
		sampler2D Texture_d6d1ed65_5575_444c_8f92_38740ace2353_Uniform;
		float4 Color_0cf876a7_4590_4973_82b7_3f878b008b3c_Uniform;
		float4 Color_d1ecd429_6e84_47b1_903b_60f454b7d326_Uniform;
		sampler2D Texture_c384b973_02cf_4076_96d9_302f320a2dd5_Uniform;
		sampler2D Texture_3cc862ff_e5c3_40c2_a0f2_895da84195ea_Uniform;
		float Vector1_2fa9bd83_0823_4530_9787_f0fab2f47430_Uniform;
		sampler2D Texture_ead3f7f8_56e5_41f9_8217_555fb21e0536_Uniform;
		sampler2D Texture_9dcd98df_78a5_4ad3_83dd_e3c00c88f898_Uniform;

		inline float4 unity_multiply_float (float4 arg1, float4 arg2)
		{
			return arg1 * arg2;
		}
		inline float4 unity_remap_float (float4 arg1, float2 arg2, float2 arg3)
		{
			return arg3.x + (arg1 - arg2.x) * (arg3.y - arg3.x) / (arg2.y - arg2.x);
		}
		inline float unity_multiply_float (float arg1, float arg2)
		{
			return arg1 * arg2;
		}
		inline float4 unity_add_float (float4 arg1, float4 arg2)
		{
			return arg1 + arg2;
		}
		inline float unity_add_float (float arg1, float arg2)
		{
			return arg1 + arg2;
		}
		inline float unity_subtract_float (float arg1, float arg2)
		{
			return arg1 - arg2;
		}
		inline float unity_div_float (float arg1, float arg2)
		{
			return arg1 / arg2;
		}
		inline float3 unity_multiply_float (float3 arg1, float3 arg2)
		{
			return arg1 * arg2;
		}
		inline float unity_fresnel_float (float3 arg1, float3 arg2)
		{
			return (1.0 - dot (normalize (arg1), normalize (arg2)));
		}
		inline float unity_remap_float (float arg1, float2 arg2, float2 arg3)
		{
			return arg3.x + (arg1 - arg2.x) * (arg3.y - arg3.x) / (arg2.y - arg2.x);
		}
		inline float4 unity_oneminus_float (float4 arg1)
		{
			return arg1 * -1 + 1;
		}



	struct Input 
	{
			float4 color : COLOR;
			half4 meshUV0;
			float3 worldViewDir;
			float3 worldNormal;
			INTERNAL_DATA

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
			float4 vertexColor = IN.color;
			float4 UV_f421b42f_c4ee_429e_b708_74f20b521d0c_UV = uv0;
			float4 Multiply_93b72a87_aa02_4d7f_9532_2ce43a75a0cb_Output = unity_multiply_float (UV_f421b42f_c4ee_429e_b708_74f20b521d0c_UV, float4 (5,5,0,0));
			float4 Texture_f6eb7ab6_0df6_4d1f_a833_847f9eefa1ce = tex2D (Texture_f6eb7ab6_0df6_4d1f_a833_847f9eefa1ce_Uniform, uv0.xy);
			float4 Remap_aa8e6b93_bde3_46fc_a9bf_2aa8880b8f30_Output = unity_remap_float (Texture_f6eb7ab6_0df6_4d1f_a833_847f9eefa1ce, float2 (0,1), float2 (-0.5,0.5));
			float Vector1_6c65527e_4c6d_48e7_bf72_8c3a97b082f5_Uniform = 0.5;
			float Multiply_5fe876ca_d791_427b_a60a_c365e6ae4f53_Output = unity_multiply_float (Vector1_6c65527e_4c6d_48e7_bf72_8c3a97b082f5_Uniform, -1);
			float4 Multiply_5ae7596c_dc8a_4b33_bf11_ddcabb17d929_Output = unity_multiply_float (Remap_aa8e6b93_bde3_46fc_a9bf_2aa8880b8f30_Output, Multiply_5fe876ca_d791_427b_a60a_c365e6ae4f53_Output);
			float Multiply_5c580d9b_a1d8_4816_8b3d_a5b541a5334b_Output = unity_multiply_float (_Time.x, Vector1_48a064e9_29c4_4cdd_8bf7_34902bb50605_Uniform);
			float Fraction_15f91423_9b19_4442_81b7_9ed97ebc72ea_Output = frac (Multiply_5c580d9b_a1d8_4816_8b3d_a5b541a5334b_Output);
			float4 Multiply_63fe984c_5505_4dd9_b1fd_64e3fbdac2b0_Output = unity_multiply_float (Multiply_5ae7596c_dc8a_4b33_bf11_ddcabb17d929_Output, Fraction_15f91423_9b19_4442_81b7_9ed97ebc72ea_Output);
			float4 Add_a918ad87_b512_4228_a79e_9fed664eb31f_Output = unity_add_float (Multiply_93b72a87_aa02_4d7f_9532_2ce43a75a0cb_Output, Multiply_63fe984c_5505_4dd9_b1fd_64e3fbdac2b0_Output);
			float4 Texture_aa489395_d5b1_4bce_a08c_71ce4329894d = tex2D (Texture_aa489395_d5b1_4bce_a08c_71ce4329894d_Uniform, (Add_a918ad87_b512_4228_a79e_9fed664eb31f_Output.xy));
			float Vector1_f869c62d_74bf_4d98_8baf_297a55e8befb_Uniform = 0.5;
			float Add_375a7a99_064f_4492_9b30_d700e08720e0_Output = unity_add_float (Multiply_5c580d9b_a1d8_4816_8b3d_a5b541a5334b_Output, Vector1_f869c62d_74bf_4d98_8baf_297a55e8befb_Uniform);
			float Fraction_cd46bddb_f4c6_4d60_988c_74fac46174af_Output = frac (Add_375a7a99_064f_4492_9b30_d700e08720e0_Output);
			float4 Multiply_ef182c2b_4e87_43f6_9362_549430c80ccf_Output = unity_multiply_float (Multiply_5ae7596c_dc8a_4b33_bf11_ddcabb17d929_Output, Fraction_cd46bddb_f4c6_4d60_988c_74fac46174af_Output);
			float4 Add_f68bff29_c2ff_46d2_a4ab_43258df5a4dc_Output = unity_add_float (Multiply_93b72a87_aa02_4d7f_9532_2ce43a75a0cb_Output, Multiply_ef182c2b_4e87_43f6_9362_549430c80ccf_Output);
			float4 Texture_8304e17f_0ca3_45b8_9081_4e083e4ffba7 = tex2D (Texture_8304e17f_0ca3_45b8_9081_4e083e4ffba7_Uniform, (Add_f68bff29_c2ff_46d2_a4ab_43258df5a4dc_Output.xy));
			float Subtract_4acb9ef9_e7c7_4c66_8511_4b34c15bc1a8_Output = unity_subtract_float (0.5, Fraction_15f91423_9b19_4442_81b7_9ed97ebc72ea_Output);
			float Divide_5514c159_af5d_493a_8b87_5c2cc7d20b20_Output = unity_div_float (Subtract_4acb9ef9_e7c7_4c66_8511_4b34c15bc1a8_Output, 0.5);
			float Absolute_4a921b1d_1662_4268_9fcb_85b03bac472c_Output = abs (Divide_5514c159_af5d_493a_8b87_5c2cc7d20b20_Output);
			float4 Lerp_95004cc7_3bfc_4e0a_ac12_0d8b65401649_Output = lerp (Texture_aa489395_d5b1_4bce_a08c_71ce4329894d, Texture_8304e17f_0ca3_45b8_9081_4e083e4ffba7, Absolute_4a921b1d_1662_4268_9fcb_85b03bac472c_Output);
			float4 Texture_73d7755c_9da7_46d2_90c6_35c2a880d380 = float4(UnpackNormal(tex2D (Texture_73d7755c_9da7_46d2_90c6_35c2a880d380_Uniform, (Add_a918ad87_b512_4228_a79e_9fed664eb31f_Output.xy))), 0);
			float4 Texture_d6d1ed65_5575_444c_8f92_38740ace2353 = float4(UnpackNormal(tex2D (Texture_d6d1ed65_5575_444c_8f92_38740ace2353_Uniform, (Add_f68bff29_c2ff_46d2_a4ab_43258df5a4dc_Output.xy))), 0);
			float4 Lerp_d1772cb2_840c_40e0_a6bd_22bfffc1278b_Output = lerp (Texture_73d7755c_9da7_46d2_90c6_35c2a880d380, Texture_d6d1ed65_5575_444c_8f92_38740ace2353, Absolute_4a921b1d_1662_4268_9fcb_85b03bac472c_Output);
			float3 Vector3_894929cf_cdbf_4a9f_8bfb_78efb02ec5bf_Uniform = float3 (0.5, 0.5, 1);
			float3 Multiply_a87468aa_7978_4ec7_ada1_872f58bfbf06_Output = unity_multiply_float (Lerp_d1772cb2_840c_40e0_a6bd_22bfffc1278b_Output, Vector3_894929cf_cdbf_4a9f_8bfb_78efb02ec5bf_Uniform);
			float Fresnel_ff56ea0d_187e_4d44_9aba_20a2636d6e20_Output = unity_fresnel_float (worldSpaceViewDirection, worldSpaceNormal);
			float4 Lerp_39312fa9_94de_4ace_9379_60e45e762b77_Output = lerp (Color_0cf876a7_4590_4973_82b7_3f878b008b3c_Uniform, Color_d1ecd429_6e84_47b1_903b_60f454b7d326_Uniform, Fresnel_ff56ea0d_187e_4d44_9aba_20a2636d6e20_Output);
			float4 Remap_df9ad0c1_bc7d_4218_ba2f_4b54dc89e064_Output = unity_remap_float (vertexColor, float2 (0,1), float2 (0.8,0.25));
			float4 Split_7877a754_c5e4_44e2_8b95_1faeef76c45c = float4(Remap_df9ad0c1_bc7d_4218_ba2f_4b54dc89e064_Output);
			float Add_1a3abf95_b10a_4c93_8c80_0829ccd86807_Output = unity_add_float (Split_7877a754_c5e4_44e2_8b95_1faeef76c45c.r, Split_7877a754_c5e4_44e2_8b95_1faeef76c45c.g);
			float4 Texture_c384b973_02cf_4076_96d9_302f320a2dd5 = tex2D (Texture_c384b973_02cf_4076_96d9_302f320a2dd5_Uniform, (Add_a918ad87_b512_4228_a79e_9fed664eb31f_Output.xy));
			float4 Texture_3cc862ff_e5c3_40c2_a0f2_895da84195ea = tex2D (Texture_3cc862ff_e5c3_40c2_a0f2_895da84195ea_Uniform, (Add_f68bff29_c2ff_46d2_a4ab_43258df5a4dc_Output.xy));
			float Lerp_e4ffbd80_7aeb_4ffd_959e_b42c0fddb434_Output = lerp (Texture_c384b973_02cf_4076_96d9_302f320a2dd5.r, Texture_3cc862ff_e5c3_40c2_a0f2_895da84195ea.r, Absolute_4a921b1d_1662_4268_9fcb_85b03bac472c_Output);
			float Add_4e09ff76_d920_4322_8317_8f6fa3108ddd_Output = unity_add_float (Add_1a3abf95_b10a_4c93_8c80_0829ccd86807_Output, Lerp_e4ffbd80_7aeb_4ffd_959e_b42c0fddb434_Output);
			float Vector1_db502ab1_b257_430a_8eac_4c2fb63f5cf1_Uniform = 0;
			float4 Combine_c3f75c44_cd2d_4870_9d30_77d318dabf43_Output = float4(Vector1_db502ab1_b257_430a_8eac_4c2fb63f5cf1_Uniform,Vector1_2fa9bd83_0823_4530_9787_f0fab2f47430_Uniform,0.0, 0.0);
			float Remap_278d7df3_8782_4bb9_b51e_211bc43b2f8c_Output = unity_remap_float (Add_4e09ff76_d920_4322_8317_8f6fa3108ddd_Output, Combine_c3f75c44_cd2d_4870_9d30_77d318dabf43_Output, float2 (3,0));
			float Clamp_4d130bfe_d56f_4a72_896b_f91a292682e0_Output = clamp (Remap_278d7df3_8782_4bb9_b51e_211bc43b2f8c_Output, 0, 1);
			float4 Combine_b7ee0905_6d7c_437c_bb78_9f4059c9073c_Output = float4(Clamp_4d130bfe_d56f_4a72_896b_f91a292682e0_Output,Clamp_4d130bfe_d56f_4a72_896b_f91a292682e0_Output,Clamp_4d130bfe_d56f_4a72_896b_f91a292682e0_Output,0.0);
			float4 Multiply_f1276058_6b35_46b7_bd96_3b0e94ea899e_Output = unity_multiply_float (Lerp_39312fa9_94de_4ace_9379_60e45e762b77_Output, Combine_b7ee0905_6d7c_437c_bb78_9f4059c9073c_Output);
			float4 Texture_ead3f7f8_56e5_41f9_8217_555fb21e0536 = tex2D (Texture_ead3f7f8_56e5_41f9_8217_555fb21e0536_Uniform, (Add_a918ad87_b512_4228_a79e_9fed664eb31f_Output.xy));
			float4 Texture_9dcd98df_78a5_4ad3_83dd_e3c00c88f898 = tex2D (Texture_9dcd98df_78a5_4ad3_83dd_e3c00c88f898_Uniform, (Add_f68bff29_c2ff_46d2_a4ab_43258df5a4dc_Output.xy));
			float4 Lerp_fe1290fe_9261_45ed_9be5_b48e9e797683_Output = lerp (Texture_ead3f7f8_56e5_41f9_8217_555fb21e0536, Texture_9dcd98df_78a5_4ad3_83dd_e3c00c88f898, Absolute_4a921b1d_1662_4268_9fcb_85b03bac472c_Output);
			float4 OneMinus_66f9602e_9e86_48b0_9a92_559c310b6958_Output = unity_oneminus_float (Lerp_fe1290fe_9261_45ed_9be5_b48e9e797683_Output);
			o.Albedo = Lerp_95004cc7_3bfc_4e0a_ac12_0d8b65401649_Output;
			o.Normal = Multiply_a87468aa_7978_4ec7_ada1_872f58bfbf06_Output;
			o.Normal += 1e-6;
			o.Emission = Multiply_f1276058_6b35_46b7_bd96_3b0e94ea899e_Output;
			o.Smoothness = OneMinus_66f9602e_9e86_48b0_9a92_559c310b6958_Output;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
