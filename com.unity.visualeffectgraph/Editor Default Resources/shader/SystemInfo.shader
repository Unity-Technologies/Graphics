Shader "Hidden/VFX/SystemInfo"
{
    Properties
    {
        _Color("Color", Color) = (0.5,0.2,0,1)
        _OrdinateScale("OrdinateScale", float) = 1 
    } 

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vs_input
            {
                uint id : SV_VertexID;
                float4 vertex : POSITION;
                float2 leftPoint : TEXCOORD0;
                float2 rightPoint : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 clipUV : TEXCOORD0;
            };

            fixed4 _Color;
            uniform float _OrdinateScale; 

            uniform float4x4 _ClipMatrix;
            sampler2D _GUIClipTexture;

            v2f vert(vs_input i)
            {
                v2f o;
                float2 shrinkedPoint = float2(i.vertex.x, (i.vertex.y * _OrdinateScale - 0.5) * 0.98 + 0.5); 
                float2 screenPos = UnityObjectToViewPos(float3(shrinkedPoint, 0.0)).xy; 
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
