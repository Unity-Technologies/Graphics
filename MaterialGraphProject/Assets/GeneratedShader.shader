Shader "Hidden/PreviewShader/Color_Color_3FAAF5EA_Uniform" {
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


			float4 Color_Color_3FAAF5EA_Uniform;


			struct v2f 
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR;

			};




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
				return half4(Color_Color_3FAAF5EA_Uniform.x, Color_Color_3FAAF5EA_Uniform.y, Color_Color_3FAAF5EA_Uniform.z, 1.0);

			}
			ENDCG
		}
	}
	Fallback Off
}