Shader "Unlit/AALine"
{
    Properties
    {
        _ZoomFactor ("Zoom Factor", float )  = 1
        _ZoomCorrection("Zoom correction", float) = 1
        _InputColor("Input Color", Color) = (1,1,1,1)
        _OutputColor("Output Color", Color) = (1,1,1,1)
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
                float3 normal : NORMAL;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
                float2 clipUV : TEXCOORD1;
                fixed4 color : COLOR;
            };
            uniform float4x4 unity_GUIClipTextureMatrix;
            float _ZoomCorrection;
            fixed4 _InputColor;
            fixed4 _OutputColor;

            v2f vert (appdata v)
            {
                float3 vertex = v.vertex + float3(v.normal.xy,0) * _ZoomCorrection;
                v2f o;
                float3 eyePos = UnityObjectToViewPos(vertex);
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = v.uv;
                o.color = lerp(_OutputColor, _InputColor, v.normal.z);
                o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
                return o;
            }

            float _ZoomFactor;
            sampler2D _GUIClipTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                float distance = abs(i.uv.x * _ZoomFactor);

                distance = (i.uv.y * _ZoomFactor - distance ) + 0.5;


            float clipA = tex2D(_GUIClipTexture, i.clipUV).a;
                return fixed4(i.color.rgb, i.color.a * saturate(distance) * clipA);
            }
            ENDCG
        }
    }
}
