<<<<<<< HEAD
Shader "Generated.ExportTextureMasterNode31754f39-6eb9-4773-ae21-b2fca7f2fcde" 
{
	Properties 
	{
		
=======
Shader "Graph/Generated.MetallicMasterNode588ae349-7b2c-4dcc-a420-78da738e8509" 
{
	Properties 
	{
		Texture_a6eb1bfb_6ea7_4e0b_bfc1_c9e0ed434f76_Uniform("Albedo", 2D) = "white" {}
		Texture_df5248d2_ffaf_4f62_9322_5575ac4818d0_Uniform("Normal", 2D) = "bump" {}
		[HDR]Color_11e7c6f6_9284_4d76_bfbf_3a1eced790b6_Uniform("Color", Color) = (2,0.5379311,0,1)

>>>>>>> 82dce9042a3bb161933bb4ee1fa5fa11724776d6
	}	
	
	SubShader 
	{
		Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }

		ZWrite Off
		Blend One Zero
<<<<<<< HEAD
		
		Pass 
		{
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"
		 	#pragma vertex CustomRenderTextureVertexShader_Preview
			#pragma fragment frag
			#pragma target 4.0
			
			v2f_customrendertexture CustomRenderTextureVertexShader_Preview(appdata_base IN)
			{
				v2f_customrendertexture OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.primitiveID = 0;//TODO
				OUT.localTexcoord = IN.texcoord;
				OUT.globalTexcoord = IN.texcoord;
				OUT.direction = CustomRenderTextureComputeCubeDirection(OUT.globalTexcoord.xy);
				return OUT;
			}



			inline void unity_Gradient_float (float v, out float4 finalColor, out float finalR, out float finalG, out float finalB, out float finalA)
			{
				float3 color0=float3(0.1176471,0.08960744,0.07439446);
				float colorp0=0;
				float3 color1=float3(0,0,0);
				float colorp1=0.1147021;
				float3 color2=float3(0.2132353,0.07766853,0);
				float colorp2=0.2500038;
				float3 color3=float3(0.6364486,0.2844907,0);
				float colorp3=0.3764706;
				float3 color4=float3(1,0.9310344,0);
				float colorp4=0.5058824;
				float3 color5=float3(1,1,1);
				float colorp5=0.6529488;
				float3 color6=float3(1,0.6413793,0);
				float colorp6=0.7823606;
				float3 color7=float3(0.4779412,0.353851,0.02811421);
				float colorp7=1;
				float3 gradcolor = color0;
				float colorLerpPosition0=smoothstep(colorp0,colorp1,v);
				gradcolor = lerp(gradcolor,color1,colorLerpPosition0);
				float colorLerpPosition1=smoothstep(colorp1,colorp2,v);
				gradcolor = lerp(gradcolor,color2,colorLerpPosition1);
				float colorLerpPosition2=smoothstep(colorp2,colorp3,v);
				gradcolor = lerp(gradcolor,color3,colorLerpPosition2);
				float colorLerpPosition3=smoothstep(colorp3,colorp4,v);
				gradcolor = lerp(gradcolor,color4,colorLerpPosition3);
				float colorLerpPosition4=smoothstep(colorp4,colorp5,v);
				gradcolor = lerp(gradcolor,color5,colorLerpPosition4);
				float colorLerpPosition5=smoothstep(colorp5,colorp6,v);
				gradcolor = lerp(gradcolor,color6,colorLerpPosition5);
				float colorLerpPosition6=smoothstep(colorp6,colorp7,v);
				gradcolor = lerp(gradcolor,color7,colorLerpPosition6);
				float alpha0=1;
				float alphap0=0;
				float alpha1=1;
				float alphap1=1;
				float gradalpha = alpha0;
				float alphaLerpPosition0=smoothstep(alphap0,alphap1,v);
				gradalpha = lerp(gradalpha,alpha1,alphaLerpPosition0);
				finalColor = float4(gradcolor,gradalpha);
				finalR = finalColor.r;
				finalG = finalColor.g;
				finalB = finalColor.b;
				finalA = finalColor.a;
			}


			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				half4 uv0 = float4(IN.localTexcoord.xyz,1.0);
				float4 UV_49a5272a_c49d_4f7e_94ba_3a2e27fcbed4_UV = uv0;
				 float4 Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalColor;
				 float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalR;
				 float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalG;
				 float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalB;
				 float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalA;
				unity_Gradient_float (UV_49a5272a_c49d_4f7e_94ba_3a2e27fcbed4_UV, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalColor, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalR, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalG, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalB, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalA);
				return Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalColor;

			}
			ENDCG
		}
=======

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
		float2 Remap_c59de931_587b_4bd5_8442_b14cce0a0208_InMinMax;
		float2 Remap_c59de931_587b_4bd5_8442_b14cce0a0208_OutMinMax;
		float Combine_6e51fc05_a66b_4b70_8024_a68288e35b69_Input3;
		float Combine_6e51fc05_a66b_4b70_8024_a68288e35b69_Input4;
		float4 Multiply_83fd2441_1d6a_4f28_8ee0_65576c6ed07f_Input2;
		float Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Input2;
		float Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Input3;

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
			float Remap_c59de931_587b_4bd5_8442_b14cce0a0208_Output = unity_remap_float (Split_71e36b70_b3f9_4f61_afdb_1c781b2fcd97.g, Remap_c59de931_587b_4bd5_8442_b14cce0a0208_InMinMax, Remap_c59de931_587b_4bd5_8442_b14cce0a0208_OutMinMax);
			float4 Combine_6e51fc05_a66b_4b70_8024_a68288e35b69_Output = float4(Split_71e36b70_b3f9_4f61_afdb_1c781b2fcd97.r,Split_71e36b70_b3f9_4f61_afdb_1c781b2fcd97.b,0.0, 0.0);
			float4 Multiply_83fd2441_1d6a_4f28_8ee0_65576c6ed07f_Output = unity_multiply_float (Combine_6e51fc05_a66b_4b70_8024_a68288e35b69_Output, Multiply_83fd2441_1d6a_4f28_8ee0_65576c6ed07f_Input2);
			float Noise_56e06f3e_5014_4678_b1bf_2c92a67f4952_Output = unity_noise_float (Multiply_83fd2441_1d6a_4f28_8ee0_65576c6ed07f_Output);
			float Add_90f8adc1_246d_42c3_86fb_812d9895dd2b_Output = unity_add_float (Remap_c59de931_587b_4bd5_8442_b14cce0a0208_Output, Noise_56e06f3e_5014_4678_b1bf_2c92a67f4952_Output);
			float Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Output = clamp (Add_90f8adc1_246d_42c3_86fb_812d9895dd2b_Output, Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Input2, Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Input3);
			float4 Multiply_48befeb5_6a8f_4ad2_a0bc_86af5b21d5c4_Output = unity_multiply_float (Color_11e7c6f6_9284_4d76_bfbf_3a1eced790b6_Uniform, Clamp_15b0b059_9349_429e_b7b8_47acccc69d45_Output);
			o.Albedo = Texture_a6eb1bfb_6ea7_4e0b_bfc1_c9e0ed434f76;
			o.Normal = Texture_df5248d2_ffaf_4f62_9322_5575ac4818d0;
			o.Normal += 1e-6;
			o.Emission = Multiply_48befeb5_6a8f_4ad2_a0bc_86af5b21d5c4_Output;

>>>>>>> 82dce9042a3bb161933bb4ee1fa5fa11724776d6
	}
	Fallback Off
}