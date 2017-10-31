Shader "Hidden/DistordNoise"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

#define CENTER 0.15

			float4 frag (v2f i) : SV_Target
			{
                float4 col = float4(0,0,0,1);

                /*
                col.x = cos(i.uv.x * UNITY_PI * 8);
                col.y = sin(i.uv.y * UNITY_PI * 8);
                */

                col.xy = frac(i.uv.xy * 8)*2-1;

				return col;
			}
			ENDCG
		}
	}
}
