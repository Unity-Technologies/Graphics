Shader "UnityEngine.MaterialGraph.ExportTextureMasterNode71166862-083a-4792-8c3a-4111b9dd9d37" 
{
	Properties 
	{
				Texture_339414d4_3f08_4a60_b7de_60b5078454cd_Uniform("Texture", 2D) = "white" {}

	}	
	
	SubShader 
	{
		Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }

		ZWrite Off
		Blend One Zero
		
		Pass 
		{
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"
		 	#pragma vertex CustomRenderTextureVertexShader
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

			sampler2D Texture_339414d4_3f08_4a60_b7de_60b5078454cd_Uniform;


			inline float3 unity_rgbtohsv_float (float3 arg1)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 P = lerp(float4(arg1.bg, K.wz), float4(arg1.gb, K.xy), step(arg1.b, arg1.g));
				float4 Q = lerp(float4(P.xyw, arg1.r), float4(arg1.r, P.yzx), step(P.x, arg1.r));
				float D = Q.x - min(Q.w, Q.y);
				float E = 1e-10;
				return float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
			}
			inline float3 unity_hsvtorgb_float (float3 arg1)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				float3 P = abs(frac(arg1.xxx + K.xyz) * 6.0 - K.www);
				return arg1.z * lerp(K.xxx, saturate(P - K.xxx), arg1.y);
			}


			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				half4 uv0 = float4(IN.localTexcoord.xyz,1.0);
				float4 Split_81de5761_6b38_4350_8ede_9db8ac1d98d0 = float4(_SinTime);
				float4 Texture_339414d4_3f08_4a60_b7de_60b5078454cd = tex2D (Texture_339414d4_3f08_4a60_b7de_60b5078454cd_Uniform, uv0.xy);
				float3 RGBtoHSV_6b42522c_e2df_4b64_8142_be2f5cf24530_Output = unity_rgbtohsv_float (Texture_339414d4_3f08_4a60_b7de_60b5078454cd);
				float4 Split_42661a92_b299_44b2_a9ad_e7a76a22817c = float4(RGBtoHSV_6b42522c_e2df_4b64_8142_be2f5cf24530_Output, 1.0);
				float4 Combine_8b3bcdf1_2eeb_4f31_a5ca_a4796ceb5066_Output = float4(Split_81de5761_6b38_4350_8ede_9db8ac1d98d0.r,Split_42661a92_b299_44b2_a9ad_e7a76a22817c.g,Split_42661a92_b299_44b2_a9ad_e7a76a22817c.b,0.0);
				float3 HSVtoRGB_e30c457c_26fb_4e7c_847e_fb4e9cb55bb6_Output = unity_hsvtorgb_float (Combine_8b3bcdf1_2eeb_4f31_a5ca_a4796ceb5066_Output);
				float Vector1_a551547a_ba15_43fe_8319_a5f8dc3c4362_Uniform = 1;
				float4 Combine_97922ab7_d2fa_4422_aae7_10b1f82228da_Output = float4(HSVtoRGB_e30c457c_26fb_4e7c_847e_fb4e9cb55bb6_Output,Vector1_a551547a_ba15_43fe_8319_a5f8dc3c4362_Uniform);
				return Combine_97922ab7_d2fa_4422_aae7_10b1f82228da_Output;

			}
			ENDCG
		}
	}
	Fallback Off
}