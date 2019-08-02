Shader "Hidden/VFX/SystemStat"
{
    Properties
    {
        _Color("Color", Color) = (1,0,0,1)
    }

    SubShader
    {
        Tags {"RenderType" = "Opaque"}
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
            };

            fixed4 _Color;
            float2 _WinTopLeft;
            float _WinWidth;
            float _WinHeight;

            uniform float4x4 unity_GUIClipTextureMatrix;

            v2f vert(vs_input v)
            {
                v2f o;
                o.vertex = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }

            ENDCG
        }
    }
}
