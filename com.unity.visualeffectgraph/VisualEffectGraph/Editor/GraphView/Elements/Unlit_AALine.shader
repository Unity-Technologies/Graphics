Shader "Unlit/AALine"
{
    Properties
    {
        _ZoomFactor ("Zoom Factor", float )  = 1
        _Color ("Color", Color ) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _ZoomFactor;
            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {

                float distance = (i.uv.y * _ZoomFactor - abs(i.uv.x * _ZoomFactor));

                return fixed4(_Color.rgb, _Color.a * distance );
            }
            ENDCG
        }
    }
}
