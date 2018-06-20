Shader "HDRenderPipeline/GraphicTests/2004_AnimatedCookie_AnimMat2"
{
    Properties
    {
        _Nyan("Nyan",2D) = "white"{}
        _Speed("Speed", float) = 5.0

        _rainbowValues("Rainbow: speed, frequency, amplitude, scale", vector) = (10, 5, 0.05, 0.8)

        _StarSize ("Star Size", float) = 0.2

        _MyTime("My Time", float) = 0
    }

        SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

#define STAR_ROW 7
#define STAR_COL 7
#define STAR_FRAME 4
#define STAR_ARRSIZE STAR_FRAME*STAR_COL*STAR_ROW

            float4      _Color;
            sampler2D   _Nyan;
            float4 _Nyan_ST;
            float _Speed;

            float4 _rainbowValues;

            float _StarSize;

            float _MyTime;

            static const int _StarsAnim[STAR_ARRSIZE] = {
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,

                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0,
                0, 0, 1, 0, 1, 0, 0,
                0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,

                0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                1, 1, 0, 1, 0, 1, 1,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0,

                0, 0, 0, 1, 0, 0, 0,
                0, 1, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 0,
                1, 0, 0, 0, 0, 0, 1,
                0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 0, 1, 0,
                0, 0, 0, 1, 0, 0, 0
            };

            // single nyan pixel size : 36, 21
            #define _NyanSize float2(36, 21)

            void DrawNyan(float2 uv, inout float3 c)
            {
                uv.y *= 36.0 / 21.0;

                if (uv.x > 1 || uv.x < 0 || uv.y < 0 || uv.y > 1) return;

                uv.x /= 6.0;

                uv.x += floor(-(-_MyTime * _Speed ) % 6) / 6.0;

                float4 nyan = tex2D(_Nyan, uv);

                if (nyan.a > 0) c.rgb = nyan.rgb;
            }

            void DrawRainbow(float2 uv, inout float3 c)
            {
                uv.y *= 36.0 / 21.0;

                uv.y += sin(_MyTime * _rainbowValues.x + uv.x * _rainbowValues.y) * _rainbowValues.z;

                uv.y = uv.y / _rainbowValues.w - ( 1 - _rainbowValues.w );

                if (uv.x > 0.3 || uv.y < 0 || uv.y > 1) return;

                uv.y *= 6;
                uv.y = floor(uv.y);

                switch (uv.y)
                {
                case 0: c.rgb = float3(0.5, 0, 1); break;
                case 1: c.rgb = float3(0, 0, 1); break;
                case 2: c.rgb = float3(0, 1, 0); break;
                case 3: c.rgb = float3(1, 1, 0); break;
                case 4: c.rgb = float3(1, 0.5, 0); break;
                case 5: c.rgb = float3(1, 0, 0); break;
                }
            }

            void DrawStar(float2 uv, float t, inout float3 c)
            {
                if (uv.x < 0 || uv.x>1 || uv.y < 0 || uv.y>1) return;

                uv *= 7;
                uv = floor(uv);

                float frame = floor(t % 4);

                if (_StarsAnim[frame*STAR_COL*STAR_ROW + uv.y*STAR_ROW +uv.x]) c = 1;
            }

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float4 c = float4(12.0/255.0, 65.0/255.0, 121.0/255.0, 1); // background color

                float2 uv = IN.localTexcoord.xy;

                float2 nyanUV = (uv - _Nyan_ST.zw / _NyanSize) / _Nyan_ST.x;


                float2 starUV = uv;
                for (int l = 0; l < 2; ++l)
                {
                    for (int s = 0; s < 7; ++s)
                    {
                        starUV = uv;

                        starUV /= _StarSize;

                        starUV.x += ((s*91548.92548*(l+1)) % 7.0);

                        starUV.y -= ((s*36598.214563*(l+1)) % 7.0);

                        starUV.x += _MyTime * (1+(l*0.5)) * 3;
                        starUV.x = starUV.x % 5;

                        starUV *= 1 - l * 0.1;

                        DrawStar(starUV, (_MyTime+s/7.0+l/6.0) * 5, c.rgb);
                    }
                }

                DrawRainbow(nyanUV, c.rgb);

                DrawNyan(nyanUV, c.rgb);

                return c;
            }
            ENDCG
        }
    }
}
