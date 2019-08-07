Shader "Hidden/VFX/SystemStat"
{
    Properties
    {
        _Color("Color", Color) = (0.5,0.2,0,1)
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque"}
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vs_input
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 clipUV : TEXCOORD1;
            };

            fixed4 _Color;
            /*float2 _WinBotLeft;
            float _WinWidth;
            float _WinHeight;*/

            uniform float4x4 _ClipMatrix;
            sampler2D _GUIClipTexture;

            v2f vert(vs_input i)
            {
                v2f o;
                float2 screenPos = UnityObjectToViewPos(i.vertex).xy;
                o.vertex = float4(2.0 * screenPos - 1.0, 0, 1);
                o.clipUV = (mul(_ClipMatrix, float4(screenPos, 0, 1)).xy - float2(0.5, 0.5)) * 0.88 + float2(0.5,0.5);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float clip = tex2D(_GUIClipTexture, i.clipUV).a;
                if (clip < 0.1)
                    discard;
                return _Color;
            }

            ENDCG
        }
    }
}
