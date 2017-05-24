Shader "Hidden/PreviewShader/Posterize_f232fbec_5ce8_4ba5_8491_a7a66291d45f_Output" {
	Properties {
		Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform("", Float) = 0.1

	}	
	
	SubShader {
		// inside SubShader
		Tags
		{
			"Queue"="Geometry"
			"RenderType"="Opaque"
			"IgnoreProjector"="True"
		}

		// inside Pass
		ZWrite On

		Blend One Zero
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"


			float3 ReflectionProbe_4f8cb744_19c5_4a6e_9bc1_172200f2fa25_normalDir;
			float Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform;


			struct v2f 
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR;
				float3 worldNormal : TEXCOORD1;

			};


			inline float unity_posterize_float (float input, float stepsize)
			{
				return floor(input / stepsize) * stepsize;
			}


			v2f vert (appdata_full v) 
			{
				v2f o = (v2f)0;
				o.pos = UnityObjectToClipPos(v.vertex);;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float3 viewDir = UnityWorldSpaceViewDir(worldPos);
				float4 screenPos = ComputeScreenPos(UnityObjectToClipPos(v.vertex));
				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldNormal = worldNormal;

				return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				float3 worldSpaceNormal = normalize(IN.worldNormal);
				float3 ReflectionProbe_4f8cb744_19c5_4a6e_9bc1_172200f2fa25 = ShadeSH9(float4(worldSpaceNormal.xyz , 1));
				float4 Split_f630c862_c9a4_4765_b085_f9b01dd46061 = float4(ReflectionProbe_4f8cb744_19c5_4a6e_9bc1_172200f2fa25, 1.0);
				float Posterize_f232fbec_5ce8_4ba5_8491_a7a66291d45f_Output = unity_posterize_float (Split_f630c862_c9a4_4765_b085_f9b01dd46061.r, Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform);
				return half4(Posterize_f232fbec_5ce8_4ba5_8491_a7a66291d45f_Output, Posterize_f232fbec_5ce8_4ba5_8491_a7a66291d45f_Output, Posterize_f232fbec_5ce8_4ba5_8491_a7a66291d45f_Output, 1.0);

			}
			ENDCG
		}
	}
	Fallback Off
}