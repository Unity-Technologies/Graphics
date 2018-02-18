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
			
            #pragma multi_compile Compare_RGB Compare_Lab Compare_Jab

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

            // Folowing color conversion code source : https://github.com/nschloe/colorio

            // RGB to XYZ : file:///C:/Users/Remy/Downloads/srgb.pdf
            float3 RGB2XYZ ( float4 color )
            {
                return float3(
                    color.r * 0.4124564 + color.g * 0.3575761 + color.b * 0.1804375,
                    color.r * 0.2126729 + color.g * 0.7151522 + color.b * 0.0721750,
                    color.r * 0.0193339 + color.g * 0.1191920 + color.b * 0.9503041
                );
            }

            // JzAzBz color conversion : https://www.osapublishing.org/oe/fulltext.cfm?uri=oe-25-13-15131&id=368272

            static const float b = 1.15;
            static const float g = 0.66;
            static const float c1 = 0.8359375;      // 3424f / 2^12
            static const float c2 = 18.8515625;     // 2413f / 2^7
            static const float c3 = 18.6875;        // 2392f / 2^7
            static const float n = 0.15930175781;   // 2610/2^14
            static const float p = 134.034375;       // 1.7*2523/2^5
            static const float d = -0.56;
            static const float d0 = 1.6295499532821566E-11;

            // JzAzBz min/max: (0.0, -0.1, -0.2) / (0.2, 0.1, 0.1)
            static const float3 jabMin = float3(0, -0.1, -0.2);
            static const float3 jabMax = float3(0.2, 0.1, 0.1);
            static const float3 jabRange = float3(0.2, 0.2, 0.3);

            // min / max lab for srgb : (0.0, -577.4, -286.8) / (186.9, 577.4, 294.5)
            static const float3 labMin = float3(0.0, -577.4, -286.8);
            static const float3 labMax = float3(186.9, 577.4, 294.5);
            static const float3 labRange = float3(186.9, 1154.8, 581.3);

            float3 RGB2JzAzBz (float4 color)
            {
                float3 xyz = RGB2XYZ( color);

                float x2 = b * xyz.x - (b-1) * xyz.z;
                float y2 = g * xyz.y - (g-1) * xyz.x;

                float3 lms = float3(
                    0.41478372 * x2 + 0.579999 * y2 + 0.0146480 * xyz.z,
                    -0.2015100 * x2 + 1.120649 * y2 + 0.0531008 * xyz.z,
                    -0.0166008 * x2 + 0.264800 * y2 + 0.6684799 * xyz.z
                );

                float3 lmsPowN = pow(lms/10000, n);

                float3 lms2 = pow( (c1.xxx + c2 * lmsPowN) / ( float3(1,1,1) + c3 * lmsPowN ) , p ) ;

                float3 jab = float3(
                    0.5 * lms2.x + 0.5 * lms2.y,
                    3.524000 * lms2.x + -4.066708 * lms2.y + 0.542708 * lms2.z,
                    0.199076 * lms2.x + 1.096799 * lms2.y + -1.295875 * lms2.z
                );

                jab.x = (((1+d)*jab.x)/(1+d*jab.x))-d0;

                return jab;
            }

            float JzAzBzDiff( float3 v1, float3 v2)
            {
                float c1 = sqrt(v1.y*v1.y + v1.z*v1.z);
                float c2 = sqrt(v2.y*v2.y + v2.z*v2.z);

                float h1 = atan(v1.z/v1.y);
                float h2 = atan(v2.z/v2.y);

                float deltaH = 2*sqrt( c1*c2 ) * sin((h1-h2)/2);

                return sqrt( pow( v1.x-v2.x ,2) + pow(c1-c2, 2) + deltaH * deltaH );
            }

            float XYZ2LabFunc( float f )
            {
                float delta = 6./29.;

                if ( f > delta )
                    return pow(f, 0.333333333);
                else
                    return f/(3*delta*delta) + 4./29.;
            }

            float3 RGB2Lab( float4 color )
            {
                float3 xyz = RGB2XYZ( color);

                float xn = 95.047;
                float yn = 100;
                float zn = 108.883;

                return float3(
                    116. * XYZ2LabFunc( xyz.y / yn ) - 16.,
                    500. * ( XYZ2LabFunc(xyz.x / xn) - XYZ2LabFunc(xyz.y/yn) ),
                    200. * (XYZ2LabFunc(xyz.y / yn) - XYZ2LabFunc(xyz.z / zn))
                );
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

                float f = 0;

                if (_MaxMode == 1)
                    f = max(o.r, max(o.g, o.b));
                else
                {
                    #ifdef Compare_RGB
                        f = (o.r*o.r + o.g*o.g + o.b*o.b) / 3;
                    #endif

                    #ifdef Compare_Jab
                        float3 jabColor1 = ( RGB2JzAzBz(c1) - jabMin ) / jabRange;
                        float3 jabColor2 = ( RGB2JzAzBz(c2) - jabMin ) / jabRange;
                        float3 jabDiff = jabColor1 - jabColor2;
                        f = JzAzBzDiff(jabColor1, jabColor2);
                    #endif

                    #ifdef Compare_Lab
                        float3 labColor1 = ( RGB2Lab(c1) - labMin ) / labRange;
                        float3 labColor2 = ( RGB2Lab(c2) - labMin ) / labRange;

                        float3 labDiff = labColor1 - labColor2;
                        f = length(labDiff);
                    #endif
                }

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
