Shader "088/Mask"
{
    Properties
    {
        _MainTex("Base RGBA", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue"="Transparent-10"}
        ZWrite Off
        ZTest Always
        Fog { Mode Off }
        Lighting Off
        Cull Off

        Pass
        {
            Stencil {
                Ref 1
                WriteMask 1
                Comp Always
                Pass Replace
            }
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
