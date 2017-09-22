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
                float2 clipUV : TEXCOORD1;
            };
            uniform float4x4 unity_GUIClipTextureMatrix;

            v2f vert (appdata v)
            {
                v2f o;
                float3 eyePos = UnityObjectToViewPos(v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
                return o;
            }

            float _ZoomFactor;
            fixed4 _Color;
            sampler2D _GUIClipTexture;


            fixed4 frag (v2f i) : SV_Target
            {
                float distance = abs(i.uv.x * _ZoomFactor);

                distance = (i.uv.y * _ZoomFactor - distance ) + 0.5;


            float clipA = tex2D(_GUIClipTexture, i.clipUV).a;
                return fixed4(_Color.rgb, _Color.a * saturate(distance) * clipA);
            }
            ENDCG
        }
    }
}
