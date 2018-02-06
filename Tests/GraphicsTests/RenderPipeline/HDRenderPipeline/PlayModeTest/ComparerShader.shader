Shader "GraphicTests/ComparerShader"
{
	Properties
	{
        _MainTex("Texture", 2D) = "white" {}
        _CompareTex("Texture", 2D) = "white" {}
        [Enum(Red, 0, Green, 1, Blue, 2, Color, 3, Greyscale, 4, Heatmap, 5)]
        _Mode("Mode", int) = 5
        [Toggle] _MaxMode("Max Mode", int)=0
        _Split("Split", Range(0,1)) = 0.5
        _ResultSplit ("Result Split", Range(0,1)) = 0.1
        _LineWidth("Line Width", float) = 0.001
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

            sampler2D _MainTex;
            sampler2D _CompareTex;
            int _Mode, _MaxMode;

            float _FlipV2 = 1;
            float _CorrectGamma = 0;

            float _Split, _ResultSplit, _LineWidth;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                fixed4 c1 = tex2D(_MainTex, i.uv);
                float2 uv2 = i.uv;
                if (_FlipV2 > 0) uv2.y = 1 - uv2.y;
                fixed4 c2 = tex2D(_CompareTex, uv2);

                fixed4 o = c1 - c2;
                o.a = 1;
                o = abs(o);

                float f = (o.r + o.g + o.b) / 3;

                if (_MaxMode == 1)
                    f = max(o.r, max(o.g, o.b));

                if (_Mode == 0)
                    o.gb = 0;
                if (_Mode == 1)
                    o.rb = 0;
                if (_Mode == 2)
                    o.rg = 0;
                if (_Mode == 4) // Greyscale view
                    o.rgb = f;
                if (_Mode == 5) // Heat view
                {
                    f = f * 3;

                    o.b = 1-abs( clamp(f, 0, 2)-1) ;
                    o.g = 1-abs( clamp(f, 1, 3)-2 );
                    o.r = clamp(f, 2, 3) - 2;
                }

                _Split = lerp(_Split, 0.5, _ResultSplit);

                float a1 = saturate(_Split - _ResultSplit * 0.5 - _LineWidth);
                float a2 = saturate(_Split - _ResultSplit * 0.5);
                float b1 = saturate(_Split + _ResultSplit * 0.5);
                float b2 = saturate(_Split + _ResultSplit * 0.5 + _LineWidth);

                if (i.uv.x < a1)
                {
                    o.rgb = c1.rgb;
                }
                else if (i.uv.x < a2)
                {
                    o.rgb = 0;
                }
                else if (i.uv.x > b2)
                {
                    o.rgb = c2.rgb;
                }
                else if (i.uv.x > b1)
                {
                    o.rgb = 0;
                }

                if (_CorrectGamma > 0)
                    o.rgb = pow(o.rgb, 0.4545454545);

                return o;
			}
			ENDCG
		}
	}
}
