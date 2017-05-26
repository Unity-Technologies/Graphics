Shader "Custom/LavaRock" 
{
	Properties 
	{
		Texture_a6eb1bfb_6ea7_4e0b_bfc1_c9e0ed434f76_Uniform("Albedo", 2D) = "white" {}
		Texture_df5248d2_ffaf_4f62_9322_5575ac4818d0_Uniform("Normal", 2D) = "bump" {}
		[HDR]Color_11e7c6f6_9284_4d76_bfbf_3a1eced790b6_Uniform("Color", Color) = (2,0.5379311,0,1)

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

		sampler2D Texture_a6eb1bfb_6ea7_4e0b_bfc1_c9e0ed434f76_Uniform;
		sampler2D Texture_df5248d2_ffaf_4f62_9322_5575ac4818d0_Uniform;
		float4 Color_11e7c6f6_9284_4d76_bfbf_3a1eced790b6_Uniform;

		inline float unity_remap_float (float arg1, float2 arg2, float2 arg3)
		{
			return arg3.x + (arg1 - arg2.x) * (arg3.y - arg3.x) / (arg2.y - arg2.x);
		}
		inline float4 unity_multiply_float (float4 arg1, float4 arg2)
		{
			return arg1 * arg2;
		}
		inline float unity_noise_randomValue (float2 uv)
		{
			return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
		}
		inline float unity_noise_interpolate (float a, float b, float t)
		{
			return (1.0-t)*a + (t*b);
		}
		inline float unity_valueNoise (float2 uv)
		{
			float2 i = floor(uv);
			float2 f = frac(uv);
			f = f * f * (3.0 - 2.0 * f);
			uv = abs(frac(uv) - 0.5);
			float2 c0 = i + float2(0.0, 0.0);
			float2 c1 = i + float2(1.0, 0.0);
			float2 c2 = i + float2(0.0, 1.0);
			float2 c3 = i + float2(1.0, 1.0);
			float r0 = unity_noise_randomValue(c0);
			float r1 = unity_noise_randomValue(c1);
			float r2 = unity_noise_randomValue(c2);
			float r3 = unity_noise_randomValue(c3);
			float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
			float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
			float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
			return t;
		}
		inline float unity_noise_float (float2 uv)
		{
			float t = 0.0;
			for(int i = 0; i < 3; i++)
			{
				float freq = pow(2.0, float(i));
				float amp = pow(0.5, float(3-i));
				t += unity_valueNoise(float2(uv.x/freq, uv.y/freq))*amp;
			}
			return t;
		}
		inline float unity_add_float (float arg1, float arg2)
		{
			return arg1 + arg2;
		}



	struct Input 
	{
			float4 color : COLOR;
			half4 meshUV0;
			float3 worldPos;

	};

	void vert (inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input,o);
			o.meshUV0 = v.texcoord;

	}
  
	void surf (Input IN, inout SurfaceOutputStandard o) 
	{
			half4 uv0 = IN.meshUV0;
			float3 worldPosition = IN.worldPos;
			float4 Texture_a6eb1bfb_6ea7_4e0b_bfc1_c9e0ed434f76 = tex2D (Texture_a6eb1bfb_6ea7_4e0b_bfc1_c9e0ed434f76_Uniform, uv0.xy);
			float4 Texture_df5248d2_ffaf_4f62_9322_5575ac4818d0 = float4(UnpackNormal(tex2D (Texture_df5248d2_ffaf_4f62_9322_5575ac4818d0_Uniform, uv0.xy)), 0);
			float4 Split_71e36b70_b3f9_4f61_afdb_1c781b2fcd97 = float4(worldPosition, 1.0);
			float Remap_c59de931_587b_4bd5_8442_b14cce0a0208_Output = unity_remap_float (Split_71e36b70_b3f9_4f61_afdb_1c781b2fcd97.g, float2 (-0.28,0.07), float2 (0.97,-0.61));
			float4 Combine_6e51fc05_a66b_4b70_8024_a68288e35b69_Output = float4(Split_71e36b70_b3f9_4f61_afdb_1c781b2fcd97.r,Split_71e36b70_b3f9_4f61_afdb_1c781b2fcd97.b,0.0, 0.0);
			float4 Multiply_83fd2441_1d6a_4f28_8ee0_65576c6ed07f_Output = unity_multiply_float (Combine_6e51fc05_a66b_4b70_8024_a68288e35b69_Output, float4 (100,100,0,0));
			float Noise_56e06f3e_5014_4678_b1bf_2c92a67f4952_Output = unity_noise_float (Multiply_83fd2441_1d6a_4f28_8ee0_65576c6ed07f_Output);
			float Add_90f8adc1_246d_42c3_86fb_812d9895dd2b_Output = unity_add_float (Remap_c59de931_587b_4bd5_8442_b14cce0a0208_Output, Noise_56e06f3e_5014_4678_b1bf_2c92a67f4952_Output);
			float Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Output = clamp (Add_90f8adc1_246d_42c3_86fb_812d9895dd2b_Output, 0, 1);
			float4 Multiply_48befeb5_6a8f_4ad2_a0bc_86af5b21d5c4_Output = unity_multiply_float (Color_11e7c6f6_9284_4d76_bfbf_3a1eced790b6_Uniform, Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Output);
			o.Albedo = Texture_a6eb1bfb_6ea7_4e0b_bfc1_c9e0ed434f76;
			o.Normal = Texture_df5248d2_ffaf_4f62_9322_5575ac4818d0;
			o.Normal += 1e-6;
			o.Emission = Multiply_48befeb5_6a8f_4ad2_a0bc_86af5b21d5c4_Output;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
