Shader "HDRenderPipeline/GraphicTests/2004_AnimatedCookie_AnimMat"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Tex("InputTex", 2D) = "white" {}
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

            float4      _Color;
            sampler2D   _Tex;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float f = _Time.y % 3;

                float4 c = float4(0, 0, 0, 1);

                c.r = saturate(1 - ( (f > 2) ? 3 - f : f  ));
                c.g = saturate(1- abs(1-f) );
                c.b = saturate(1-abs(2-f));

                float2 uv = IN.localTexcoord.xy;

                uv = uv - 0.5;

                float sinY = abs(sin(_Time.y * 2));

                uv.x += frac(_Time.y) - 0.5;
                uv.y -= sinY * 0.8 - 0.4;

                if (length((frac(uv + 0.5) - 0.5)) < 0.1)
                {
                    c.rgb = (frac(_Time.y/ UNITY_PI) > 0.5)?1:0;
                }

                //return _Color * tex2D(_Tex, IN.localTexcoord.xy);
                return c;
            }
            ENDCG
        }
    }
}
