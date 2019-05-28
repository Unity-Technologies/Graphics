Shader "Hiddent/HDRP/Tests/TexCubeToTex2D"
{
	Properties
	{
		_MainTex ("Texture", Cube) = "white" {}
        _CorrectGamma ("Correct Gamma", float ) = 0
        _BoxLayout ("Box Layout", float) = 0
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

            samplerCUBE _MainTex;
            bool _CorrectGamma;
            float _BoxLayout;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                float3 coords = float3(0,0,0);

                coords.x = cos( i.uv.x * UNITY_PI * 2 );
                coords.z = sin( i.uv.x * UNITY_PI * 2 );

                coords.y = sin((i.uv.y * 2 - 1) * UNITY_PI * 0.5);

                coords.xz *= 1 - abs(coords.y);

                fixed4 col = fixed4(1,1,1,1);

                if (_BoxLayout)
                {
                    if (i.uv.x < 0.25)
                    {
                        if (i.uv.y < 0.3333333)
                            col.a *= 0;
                        else if (i.uv.y < 0.6666666)
                        {
                            coords.x = saturate(i.uv.x * 4) * 2 - 1;
                            coords.y = clamp((i.uv.y * 2 - 1) * 3, -1, 1);
                            coords.z = 1;
                        }
                        else
                            col.a *= 0;
                    }
                    else if (i.uv.x < 0.5)
                    {
                        if (i.uv.y < 0.3333333)
                        {
                            coords.x = ((i.uv.y * 3) * 2 - 1);
                            coords.y = -1;
                        }
                        else if (i.uv.y < 0.6666666)
                        {
                            coords.x = 1;
                            coords.y = (i.uv.y * 2 - 1) * 3;
                        }
                        else
                        {
                            coords.x = -(((i.uv.y-.6666666) * 3) * 2 - 1);
                            coords.y = 1;
                        }
                        coords.z = -(saturate((i.uv.x - 0.25) * 4) * 2 - 1);

                    }
                    else if (i.uv.x < 0.75)
                    {
                        if (i.uv.y < 0.3333333)
                            col.a *= 0;
                        else if (i.uv.y < 0.6666666)
                        {
                            coords.x = -(saturate((i.uv.x-0.5) * 4) * 2 - 1);
                            coords.y = (i.uv.y * 2 - 1) * 3;
                            coords.z = -1;
                        }
                        else
                            col.a *= 0;
                    }
                    else
                    {
                        if (i.uv.y < 0.3333333)
                            col.a *= 0;
                        else if (i.uv.y < 0.6666666)
                        {
                            coords.x = -1;
                            coords.y = (i.uv.y * 2 - 1) * 3;
                            coords.z = saturate((i.uv.x - 0.75) * 4) * 2 - 1;
                        }
                            else
                                col.a *= 0;
                    }

                    coords = normalize(coords);
                }

				col.rgb *= texCUBElod(_MainTex, float4(coords, 0)).rgb;

                if (_CorrectGamma == 1)
                    col.rgb = pow(col.rgb, 0.4545454545); // Gamma Correction

                return col;
			}
			ENDCG
		}
	}
}
