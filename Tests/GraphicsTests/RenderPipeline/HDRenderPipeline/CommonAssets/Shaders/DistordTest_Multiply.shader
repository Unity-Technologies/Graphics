Shader "Hidden/DistordTest_Multiply"
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

			float4 frag (v2f i) : SV_Target
			{
                float4 col = float4(0,0,0,1);

                fixed2 uv = i.uv * 2 - 1;

                col.rg = sin(saturate( length(uv) ) * UNITY_PI * 4);

                uv = i.uv * 2;
                uv = frac(uv);

                col.b = (uv.x > 0.5) && (uv.y > 0.5) || (uv.x < 0.5) && (uv.y < 0.5);

				return col;
			}
			ENDCG
		}
	}
}
