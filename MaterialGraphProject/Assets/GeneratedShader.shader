Shader "Hidden/PreviewShader/Sin_5540BA00_result" {
	Properties {

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




			struct v2f 
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR;

			};


			void Unity_Sin_float(float argument, out float result)
			{
			    result = sin(argument);
			}


			v2f vert (appdata_full v) 
			{
				v2f o = (v2f)0;
				o.pos = UnityObjectToClipPos(v.vertex);;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float3 viewDir = UnityWorldSpaceViewDir(worldPos);
				float4 screenPos = ComputeScreenPos(UnityObjectToClipPos(v.vertex));
				float3 worldNormal = UnityObjectToWorldNormal(v.normal);

				return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				float Sin_5540BA00_result;
				Unity_Sin_float(_Time.y, Sin_5540BA00_result);
				return half4(Sin_5540BA00_result, Sin_5540BA00_result, Sin_5540BA00_result, 1.0);

			}
			ENDCG
		}
	}
	Fallback Off
}