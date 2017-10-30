Shader "Hidden/DistordTest"
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

                fixed2 centeredUV = i.uv * 2 - 1;

                // x distortion (r)
                if (centeredUV.x > CENTER)
                {
                    if (abs(centeredUV.y) < CENTER)
                        col.r = 1;
                    else
                        col.r = (centeredUV.x - CENTER) / (1-CENTER);
                }
                if (centeredUV.x < -CENTER)
                {
                    if (abs(centeredUV.y) < CENTER)
                        col.r = -1;
                    else
                        col.r = (centeredUV.x + CENTER) / (1 - CENTER);
                }

                // y distortion (g)
                if (centeredUV.y > CENTER)
                {
                    if (abs(centeredUV.x) < CENTER)
                        col.g = 1;
                    else
                        col.g = (centeredUV.y - CENTER) / (1 - CENTER);
                }
                if (centeredUV.y < -CENTER)
                {
                    if (abs(centeredUV.x) < CENTER)
                        col.g = -1;
                    else
                        col.g = (centeredUV.y + CENTER) / (1 - CENTER);
                }

                // distortion blur (b)
                if (abs(centeredUV.x) < CENTER)
                    if (abs(centeredUV.y) < CENTER)
                        col.b = saturate( 1 - length(centeredUV.xy) / CENTER );

                centeredUV = abs(centeredUV);
                centeredUV -= CENTER;
                centeredUV /= 1 - CENTER;
                centeredUV = centeredUV * 2 - 1;

                col.b += 1-saturate( length(centeredUV) );

				return col;
			}
			ENDCG
		}
	}
}
