Shader "Custom/SnowSlope" 
{
	Properties 
	{
		Texture_fd9422b7_6255_4b8e_b616_78346d444d21_Uniform("Albedo Base", 2D) = "white" {}
		[NonModifiableTextureData] Texture_23165f20_f8eb_4f7c_86e4_7fb7aeacb0f7_Uniform("Detail Albedo", 2D) = "white" {}
		Texture_d48530d7_5921_4987_918e_46411222e797_Uniform("Snow Albedo", 2D) = "white" {}
		Texture_401188ed_6e9e_4a8a_9f5c_a7853502ba40_Uniform("Normal Base", 2D) = "bump" {}
		Vector1_15d5efb3_faea_4990_90ce_629f350a50cb_Uniform("Vector1", Range(-0.25, 0.75)) = 0.75
		Texture_6df69ae7_cb4c_435c_a029_13f370817e4c_Uniform("Texture", 2D) = "gray" {}
		[NonModifiableTextureData] Texture_19c9bbd1_c4e8_4c71_9323_a8fc6485f3e5_Uniform("Detail Normal", 2D) = "bump" {}
		Texture_c007d40f_e93f_40ea_9867_1b70cebcd3fd_Uniform("Snow Normal", 2D) = "bump" {}

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

		sampler2D Texture_fd9422b7_6255_4b8e_b616_78346d444d21_Uniform;
		sampler2D Texture_23165f20_f8eb_4f7c_86e4_7fb7aeacb0f7_Uniform;
		sampler2D Texture_d48530d7_5921_4987_918e_46411222e797_Uniform;
		sampler2D Texture_401188ed_6e9e_4a8a_9f5c_a7853502ba40_Uniform;
		float Vector1_15d5efb3_faea_4990_90ce_629f350a50cb_Uniform;
		sampler2D Texture_6df69ae7_cb4c_435c_a029_13f370817e4c_Uniform;
		sampler2D Texture_19c9bbd1_c4e8_4c71_9323_a8fc6485f3e5_Uniform;
		sampler2D Texture_c007d40f_e93f_40ea_9867_1b70cebcd3fd_Uniform;

		inline float4 unity_remap_float (float4 arg1, float2 arg2, float2 arg3)
		{
			return arg3.x + (arg1 - arg2.x) * (arg3.y - arg3.x) / (arg2.y - arg2.x);
		}
		inline float4 unity_blendmode_Overlay (float4 arg1, float4 arg2)
		{
			float4 result1 = 1.0 - 2.0 * (1.0 - arg1) * (1.0 - arg2);
			float4 result2 = 2.0 * arg1 * arg2;
			float4 zeroOrOne = step(arg2, 0.5);
			return result2 * zeroOrOne + (1 - zeroOrOne) * result1;
		}
		inline float4 unity_multiply_float (float4 arg1, float4 arg2)
		{
			return arg1 * arg2;
		}
		inline float3 unity_add_float (float3 arg1, float3 arg2)
		{
			return arg1 + arg2;
		}
		inline float unity_add_float (float arg1, float arg2)
		{
			return arg1 + arg2;
		}
		inline float4 unity_oneminus_float (float4 arg1)
		{
			return arg1 * -1 + 1;
		}
		inline float4 unity_add_float (float4 arg1, float4 arg2)
		{
			return arg1 + arg2;
		}
		inline float3 unity_blendnormal_float (float3 arg1, float3 arg2)
		{
			return normalize(float3(arg1.rg + arg2.rg, arg1.b * arg2.b));
		}
		inline float unity_remap_float (float arg1, float2 arg2, float2 arg3)
		{
			return arg3.x + (arg1 - arg2.x) * (arg3.y - arg3.x) / (arg2.y - arg2.x);
		}



	struct Input 
	{
			float4 color : COLOR;
			half4 meshUV0;
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
			float3 worldSpaceNormal = normalize(IN.worldNormal);
			float4 Texture_fd9422b7_6255_4b8e_b616_78346d444d21 = tex2D (Texture_fd9422b7_6255_4b8e_b616_78346d444d21_Uniform, uv0.xy);
			float4 Texture_23165f20_f8eb_4f7c_86e4_7fb7aeacb0f7 = tex2D (Texture_23165f20_f8eb_4f7c_86e4_7fb7aeacb0f7_Uniform, uv0.xy);
			float4 Remap_3500b6da_0980_4ebe_856e_9bb585e44b5e_Output = unity_remap_float (Texture_23165f20_f8eb_4f7c_86e4_7fb7aeacb0f7, float2 (0.4,1), float2 (0.45,0.55));
			float4 BlendMode_48660cac_6969_48be_9e23_478808ec5036_Output = unity_blendmode_Overlay (Texture_fd9422b7_6255_4b8e_b616_78346d444d21, Remap_3500b6da_0980_4ebe_856e_9bb585e44b5e_Output);
			float4 Texture_d48530d7_5921_4987_918e_46411222e797 = tex2D (Texture_d48530d7_5921_4987_918e_46411222e797_Uniform, uv0.xy);
			float4 Texture_401188ed_6e9e_4a8a_9f5c_a7853502ba40 = float4(UnpackNormal(tex2D (Texture_401188ed_6e9e_4a8a_9f5c_a7853502ba40_Uniform, uv0.xy)), 0);
			float4 Multiply_1e1dc7c9_5887_4a92_8a44_944745b74a8e_Output = unity_multiply_float (Texture_401188ed_6e9e_4a8a_9f5c_a7853502ba40, float4 (0.5,0.5,1,0));
			float3 Add_e17509ab_e45b_4970_a943_0782b282e7b5_Output = unity_add_float (worldSpaceNormal, Multiply_1e1dc7c9_5887_4a92_8a44_944745b74a8e_Output);
			float4 Split_228ad52d_91ac_41d7_ab48_8ec0711d0209 = float4(Add_e17509ab_e45b_4970_a943_0782b282e7b5_Output, 1.0);
			float Add_94ec4353_7abe_4ea1_84da_0ee3a5a015b3_Output = unity_add_float (Split_228ad52d_91ac_41d7_ab48_8ec0711d0209.g, Vector1_15d5efb3_faea_4990_90ce_629f350a50cb_Uniform);
			float Saturate_e298edf8_7a5f_4c40_b6af_5a1ace58a8ad_Output = saturate (Add_94ec4353_7abe_4ea1_84da_0ee3a5a015b3_Output);
			float4 Texture_6df69ae7_cb4c_435c_a029_13f370817e4c = tex2D (Texture_6df69ae7_cb4c_435c_a029_13f370817e4c_Uniform, uv0.xy);
			float Add_98132ed7_9749_4a03_bf6c_c26f30256062_Output = unity_add_float (Saturate_e298edf8_7a5f_4c40_b6af_5a1ace58a8ad_Output, Texture_6df69ae7_cb4c_435c_a029_13f370817e4c.r);
			float Power_3b79ec7f_f971_4fab_bd79_c87706595e6d_Output = pow (Add_98132ed7_9749_4a03_bf6c_c26f30256062_Output, 20);
			float Clamp_cba30aa4_e964_4de0_b7ba_01523fb56d02_Output = clamp (Power_3b79ec7f_f971_4fab_bd79_c87706595e6d_Output, 0, 1);
			float4 Lerp_2f51626f_1e5c_413b_bc0e_da7093151758_Output = lerp (BlendMode_48660cac_6969_48be_9e23_478808ec5036_Output, Texture_d48530d7_5921_4987_918e_46411222e797, Clamp_cba30aa4_e964_4de0_b7ba_01523fb56d02_Output);
			float4 Texture_19c9bbd1_c4e8_4c71_9323_a8fc6485f3e5 = float4(UnpackNormal(tex2D (Texture_19c9bbd1_c4e8_4c71_9323_a8fc6485f3e5_Uniform, uv0.xy)), 0);
			float4 Combine_fcc30404_8e0e_4b5a_962f_f83b2a09a36b_Output = float4(Clamp_cba30aa4_e964_4de0_b7ba_01523fb56d02_Output,Clamp_cba30aa4_e964_4de0_b7ba_01523fb56d02_Output,Clamp_cba30aa4_e964_4de0_b7ba_01523fb56d02_Output,0.0);
			float4 OneMinus_da7350ca_2b35_4252_89ef_3871745cbf41_Output = unity_oneminus_float (Combine_fcc30404_8e0e_4b5a_962f_f83b2a09a36b_Output);
			float4 Add_d02f5977_7312_4895_bf29_9f5cc25006cc_Output = unity_add_float (OneMinus_da7350ca_2b35_4252_89ef_3871745cbf41_Output, float4 (0,0,1,0));
			float4 Multiply_5a4e0a51_fcd0_4a44_adb9_2a63a9592761_Output = unity_multiply_float (Texture_19c9bbd1_c4e8_4c71_9323_a8fc6485f3e5, Add_d02f5977_7312_4895_bf29_9f5cc25006cc_Output);
			float4 Multiply_39bbcc28_14c4_4936_b465_ed217edad0a7_Output = unity_multiply_float (Multiply_5a4e0a51_fcd0_4a44_adb9_2a63a9592761_Output, float4 (0.5,0.5,1,0));
			float3 BlendNormal_14fc11c6_64aa_4b36_8f76_87f7dd3260e7_Output = unity_blendnormal_float (Texture_401188ed_6e9e_4a8a_9f5c_a7853502ba40, Multiply_39bbcc28_14c4_4936_b465_ed217edad0a7_Output);
			float4 Texture_c007d40f_e93f_40ea_9867_1b70cebcd3fd = float4(UnpackNormal(tex2D (Texture_c007d40f_e93f_40ea_9867_1b70cebcd3fd_Uniform, uv0.xy)), 0);
			float Remap_e3359a7d_ef71_4596_81db_fe49c279f942_Output = unity_remap_float (Clamp_cba30aa4_e964_4de0_b7ba_01523fb56d02_Output, float2 (0,1), float2 (0,0.5));
			float3 Lerp_84936092_34f5_4c9c_b5c0_321449b677b5_Output = lerp (BlendNormal_14fc11c6_64aa_4b36_8f76_87f7dd3260e7_Output, Texture_c007d40f_e93f_40ea_9867_1b70cebcd3fd, Remap_e3359a7d_ef71_4596_81db_fe49c279f942_Output);
			o.Albedo = Lerp_2f51626f_1e5c_413b_bc0e_da7093151758_Output;
			o.Normal = Lerp_84936092_34f5_4c9c_b5c0_321449b677b5_Output;
			o.Normal += 1e-6;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
