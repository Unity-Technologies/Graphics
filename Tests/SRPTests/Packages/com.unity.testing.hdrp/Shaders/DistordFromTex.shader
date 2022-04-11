Shader "Hidden/DistordFromTex"
{
    Properties
    {
        _Tex("InputTex", 2D) = "white" {}
        _Scale("Scale", Float) = 2
        _Bias("Bias", Float) = -1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            sampler2D   _Tex;
            float _Scale, _Bias;

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                float4 c = float4(0,0,0,0);

                c.rgb = tex2D(_Tex, IN.localTexcoord.xy).rgb;
                c.rg = c.rg * _Scale + _Bias;

                return c;
            }
            ENDCG
        }
    }
}
