Shader "ScriptableRenderPipeline/LightweightPipeline/Unlit"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MainColor("MainColor", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5

        // BlendMode
        [HideInInspector] _Mode("Mode", Float) = 0.0
        [HideInInspector] _SrcBlend("Src", Float) = 1.0
        [HideInInspector] _DstBlend("Dst", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "IgnoreProjectors" = "True" "RenderPipeline" = "LightweightPipe" "Lightmode" = "LightweightForward" }
        LOD 100

        Blend [_SrcBlend][_DstBlend]
        ZWrite [_ZWrite]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _MainColor;
            half _Cutoff;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed alpha = texColor.a * _MainColor.a;
                fixed3 color = texColor.rgb * _MainColor.rgb;

#ifdef _ALPHATEST_ON
                clip(alpha - _Cutoff);
#endif
                UNITY_APPLY_FOG(i.fogCoord, color);

#ifdef _ALPHABLEND_ON
                return fixed4(color, alpha);
#else
                return fixed4(color, 1.0);
#endif
            }
            ENDCG
        }
    }
    CustomEditor "LightweightUnlitGUI"
}
