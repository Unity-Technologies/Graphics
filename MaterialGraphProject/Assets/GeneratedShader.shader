Shader "Hidden/PreviewShader/Sample2DTexture_1221CD9A_rgba" {
	Properties {
		[NonModifiableTextureData] Texture2D_Texture2D_D281CEC8_Uniform("Texture2D", 2D) = "white" {}

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


			UNITY_DECLARE_TEX2D(Texture2D_Texture2D_D281CEC8_Uniform);
			float2 Sample2DTexture_1221CD9A_UV;


			struct v2f 
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR;
				half4 meshUV0 : TEXCOORD5;

			};




			v2f vert (appdata_full v) 
			{
				v2f o = (v2f)0;
				o.pos = UnityObjectToClipPos(v.vertex);;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float3 viewDir = UnityWorldSpaceViewDir(worldPos);
				float4 screenPos = ComputeScreenPos(UnityObjectToClipPos(v.vertex));
				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				o.meshUV0 = v.texcoord;

				return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				half4 uv0 = IN.meshUV0;
				float4 Sample2DTexture_1221CD9A_rgba = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_D281CEC8_Uniform,uv0.xy);
				return half4(Sample2DTexture_1221CD9A_rgba.x, Sample2DTexture_1221CD9A_rgba.y, Sample2DTexture_1221CD9A_rgba.z, 1.0);

			}
			ENDCG
		}
	}
	Fallback Off
}