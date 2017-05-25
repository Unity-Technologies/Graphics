Shader "Brandon/Cellular" 
{
	Properties 
	{
		Texture_b86ff54c_bbe9_4694_8531_326cc9640f0b_Uniform("Texture", 2D) = "white" {}

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

		sampler2D Texture_b86ff54c_bbe9_4694_8531_326cc9640f0b_Uniform;

		inline float4 unity_remap_float (float4 arg1, float2 arg2, float2 arg3)
		{
			return arg3.x + (arg1 - arg2.x) * (arg3.y - arg3.x) / (arg2.y - arg2.x);
		}
		inline float2 unity_spherize_float (float2 uv, float2 position, float2 radiusAndStrength)
		{
			float2 fromUVToPoint = position - uv;
			float dist = length(fromUVToPoint);
			float mag = ((1.0 - (dist / radiusAndStrength.x)) * radiusAndStrength.y) * step(dist, radiusAndStrength.x);
			return uv + (mag * fromUVToPoint);
		}
		inline float2 unity_voronoi_noise_randomVector (float2 uv, float offset)
		{
			float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
			uv = frac(sin(mul(uv, m)) * 46839.32);
			return float2(sin(uv.y*+offset)*0.5+0.5, cos(uv.x*offset)*0.5+0.5);
		}
		inline void unity_voronoinoise_float (float2 uv, float angleOffset, out float n1, out float n2, out float n3)
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
						n1 = res.x;
						n2 = res.y;
						n3 = 1.0 - res.x;
					}
				}
			}
		}
		inline float unity_particle_float (float2 uv, float scaleFactor)
		{
			uv = uv * 2.0 - 1.0;
			return abs(1.0/length(uv * scaleFactor));
		}
		inline float unity_oneminus_float (float arg1)
		{
			return arg1 * -1 + 1;
		}
		inline float unity_multiply_float (float arg1, float arg2)
		{
			return arg1 * arg2;
		}
		inline float3 unity_multiply_float (float3 arg1, float3 arg2)
		{
			return arg1 * arg2;
		}
		inline float2 unity_uvpanner_float (float2 UV, float HorizontalOffset, float VerticalOffset)
		{
			return float2(UV.x + HorizontalOffset, UV.y + VerticalOffset);
		}
		inline float3 unity_add_float (float3 arg1, float3 arg2)
		{
			return arg1 + arg2;
		}
		inline float3 unity_rgbtolinear_float (float3 arg1)
		{
			float3 linearRGBLo = arg1 / 12.92;
			float3 linearRGBHi = pow(max(abs((arg1 + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
			return float3(arg1 <= 0.04045) ? linearRGBLo : linearRGBHi;
		}
		inline float4 unity_oneminus_float (float4 arg1)
		{
			return arg1 * -1 + 1;
		}
		inline void unity_CustomCode_test (float d, float dp, out float c)
		{
			c = step(dp, d);
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
			float3 Vector3_ff318061_3405_4489_b0c0_004f3556b379_Uniform = float3 (3, 2.5, 5);
			float4 UV_257dbdde_fb0e_45d2_b1d5_3245a77a7446_UV = uv0;
			float4 Remap_e2de5e87_5108_4e55_91d0_202c89bfdc77_Output = unity_remap_float (UV_257dbdde_fb0e_45d2_b1d5_3245a77a7446_UV, float2 (0,1), float2 (-10,10));
			float Sin_b1ddf0e6_502c_4513_9a06_d60c0f84e7e7_Output = sin (_Time.y);
			float Absolute_fca0185b_603c_4c3f_a7d7_20003e1bfbf1_Output = abs (Sin_b1ddf0e6_502c_4513_9a06_d60c0f84e7e7_Output);
			float Lerp_6aff06be_b078_46b9_a5d9_131ef80392d8_Output = lerp (1, 20, Absolute_fca0185b_603c_4c3f_a7d7_20003e1bfbf1_Output);
			float Vector1_09cb6502_ddcd_40fa_a9c4_80a8217e6532_Uniform = 0.5;
			float4 Combine_27399d01_fcb9_4ec2_8c0e_864fa09b5aa3_Output = float4(Lerp_6aff06be_b078_46b9_a5d9_131ef80392d8_Output,Vector1_09cb6502_ddcd_40fa_a9c4_80a8217e6532_Uniform,0.0, 0.0);
			float2 Spherize_fe87ab04_d3e2_4b06_93f6_ea59773ab7eb_Output = unity_spherize_float (Remap_e2de5e87_5108_4e55_91d0_202c89bfdc77_Output, float2 (0,0), Combine_27399d01_fcb9_4ec2_8c0e_864fa09b5aa3_Output);
			 float VoronoiNoise_6363f48c_fb0a_4d4a_b0a0_e9c095b25f14_n1;
			 float VoronoiNoise_6363f48c_fb0a_4d4a_b0a0_e9c095b25f14_n2;
			 float VoronoiNoise_6363f48c_fb0a_4d4a_b0a0_e9c095b25f14_n3;
			unity_voronoinoise_float (Spherize_fe87ab04_d3e2_4b06_93f6_ea59773ab7eb_Output, _Time.y, VoronoiNoise_6363f48c_fb0a_4d4a_b0a0_e9c095b25f14_n1, VoronoiNoise_6363f48c_fb0a_4d4a_b0a0_e9c095b25f14_n2, VoronoiNoise_6363f48c_fb0a_4d4a_b0a0_e9c095b25f14_n3);
			float4 UV_b1e148d9_f64d_4409_bc93_047246acb763_UV = uv0;
			float Particle_bc661883_3410_41aa_9c40_9841d35071d1_Output = unity_particle_float (UV_b1e148d9_f64d_4409_bc93_047246acb763_UV, 2.44);
			float OneMinus_ed098cd5_7e1f_43c8_aa71_e28f93649eb3_Output = unity_oneminus_float (Particle_bc661883_3410_41aa_9c40_9841d35071d1_Output);
			float Multiply_f23a464e_ad77_4844_96be_5f17a4ee2ad5_Output = unity_multiply_float (VoronoiNoise_6363f48c_fb0a_4d4a_b0a0_e9c095b25f14_n1, OneMinus_ed098cd5_7e1f_43c8_aa71_e28f93649eb3_Output);
			float Saturate_4f751b04_b16e_426b_8661_93bba25e78ea_Output = saturate (Multiply_f23a464e_ad77_4844_96be_5f17a4ee2ad5_Output);
			float3 Multiply_51e47dbb_3a27_43fb_85f3_075820b6f397_Output = unity_multiply_float (Vector3_ff318061_3405_4489_b0c0_004f3556b379_Uniform, Saturate_4f751b04_b16e_426b_8661_93bba25e78ea_Output);
			float4 UV_f53721a3_b62f_474c_8348_cd27dce64004_UV = uv0;
			float4 Remap_fc254320_74c6_4929_9c55_12b74013aa50_Output = unity_remap_float (UV_f53721a3_b62f_474c_8348_cd27dce64004_UV, float2 (0,1), float2 (-10,10));
			 float VoronoiNoise_5d4bee83_6fb2_48e2_b332_76a7ce558de7_n1;
			 float VoronoiNoise_5d4bee83_6fb2_48e2_b332_76a7ce558de7_n2;
			 float VoronoiNoise_5d4bee83_6fb2_48e2_b332_76a7ce558de7_n3;
			unity_voronoinoise_float (Remap_fc254320_74c6_4929_9c55_12b74013aa50_Output, _Time.z, VoronoiNoise_5d4bee83_6fb2_48e2_b332_76a7ce558de7_n1, VoronoiNoise_5d4bee83_6fb2_48e2_b332_76a7ce558de7_n2, VoronoiNoise_5d4bee83_6fb2_48e2_b332_76a7ce558de7_n3);
			float2 UVPanner_a7d52543_81f8_4b8f_9f48_a83f6390465f_Output = unity_uvpanner_float (Remap_fc254320_74c6_4929_9c55_12b74013aa50_Output, 0.45, 0.45);
			float Particle_ff7ecb20_7b31_4549_a468_0da4421a2b0e_Output = unity_particle_float (UVPanner_a7d52543_81f8_4b8f_9f48_a83f6390465f_Output, -0.78);
			float SmoothStep_d93b46f2_249b_4b3a_93a9_70e542a367ea_Output = smoothstep (0.15, 0.4, Particle_ff7ecb20_7b31_4549_a468_0da4421a2b0e_Output);
			float Multiply_3002b60e_2a73_4b04_9567_798629fb7fb7_Output = unity_multiply_float (VoronoiNoise_5d4bee83_6fb2_48e2_b332_76a7ce558de7_n3, SmoothStep_d93b46f2_249b_4b3a_93a9_70e542a367ea_Output);
			float Saturate_848599e3_7853_4ad1_9dc9_d854e0c98884_Output = saturate (Multiply_3002b60e_2a73_4b04_9567_798629fb7fb7_Output);
			float3 Vector3_ed4329c2_0a82_4467_816a_99ed1694361d_Uniform = float3 (2, 4, 8);
			float3 Multiply_196a897c_4a8f_4c02_87f1_4818aeec305d_Output = unity_multiply_float (Saturate_848599e3_7853_4ad1_9dc9_d854e0c98884_Output, Vector3_ed4329c2_0a82_4467_816a_99ed1694361d_Uniform);
			float3 Add_371d52ad_c3e8_484a_8546_7079c5df37df_Output = unity_add_float (Multiply_51e47dbb_3a27_43fb_85f3_075820b6f397_Output, Multiply_196a897c_4a8f_4c02_87f1_4818aeec305d_Output);
			float3 RGBtoLinear_b45ced7c_417b_406b_9524_9699354a5af9_Output = unity_rgbtolinear_float (Add_371d52ad_c3e8_484a_8546_7079c5df37df_Output);
			float4 UV_f07867b1_de17_49e3_8e83_602340969ed4_UV = uv0;
			float4 Texture_b86ff54c_bbe9_4694_8531_326cc9640f0b = tex2D (Texture_b86ff54c_bbe9_4694_8531_326cc9640f0b_Uniform, (UV_f07867b1_de17_49e3_8e83_602340969ed4_UV.xy));
			float4 OneMinus_4ee34df0_3179_41cf_b2d7_f7a3a8a7795e_Output = unity_oneminus_float (Texture_b86ff54c_bbe9_4694_8531_326cc9640f0b);
			float3 Multiply_a8ff6c5c_5b0d_413a_b22d_7a95b12dadbc_Output = unity_multiply_float (RGBtoLinear_b45ced7c_417b_406b_9524_9699354a5af9_Output, OneMinus_4ee34df0_3179_41cf_b2d7_f7a3a8a7795e_Output);
			 float CustomCode_de9dc268_2bcc_4906_804c_bd10b50ad4f4_c;
			unity_CustomCode_test (OneMinus_4ee34df0_3179_41cf_b2d7_f7a3a8a7795e_Output, 0.57, CustomCode_de9dc268_2bcc_4906_804c_bd10b50ad4f4_c);
			float Multiply_2f548bb2_d578_4b2b_b00c_20897ee877b3_Output = unity_multiply_float (CustomCode_de9dc268_2bcc_4906_804c_bd10b50ad4f4_c, 0.75);
			float3 Add_e67d33e0_c8c9_451a_9962_408b4fa9f2e9_Output = unity_add_float (Multiply_a8ff6c5c_5b0d_413a_b22d_7a95b12dadbc_Output, Multiply_2f548bb2_d578_4b2b_b00c_20897ee877b3_Output);
			float Vector1_514d9c2d_f80f_4ffd_824e_58e3f2d8a162_Uniform = 1;
			o.Emission = Add_e67d33e0_c8c9_451a_9962_408b4fa9f2e9_Output;
			o.Alpha = Vector1_514d9c2d_f80f_4ffd_824e_58e3f2d8a162_Uniform;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
