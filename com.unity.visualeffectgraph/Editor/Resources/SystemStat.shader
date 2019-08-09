Shader "Hidden/VFX/SystemStat"
{
    Properties
    {
        _Color("Color", Color) = (0.5,0.2,0,1)
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
                float2 screenPos : TEXCOORD1;
                nointerpolation float3 segment : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            fixed4 _Color;

            uniform float4x4 _ClipMatrix;
            sampler2D _GUIClipTexture;

            v2f vert(vs_input i)
            {
                v2f o;
                o.uv = float2( ((i.id & 2) >> 1) ^ (i.id & 1), (~(i.id & 2) & 2) >> 1);
                float2 screenPos = UnityObjectToViewPos(float3(i.vertex.xy, 0.0)).xy;
                o.vertex = float4(2.0 * screenPos - 1.0, 0, 1);
                o.clipUV = (mul(_ClipMatrix, float4(screenPos, 0, 1)).xy - float2(0.5, 0.5)) * 0.88 + float2(0.5,0.5);
                o.screenPos = screenPos;
                float2 shrinkedLeft = float2(i.leftPoint.x, (i.leftPoint.y - 0.5) * 0.95 + 0.5);
                float2 shrinkedRight = float2(i.rightPoint.x, (i.rightPoint.y - 0.5) * 0.95 + 0.5);
                float2 leftPoint = UnityObjectToViewPos(float3(shrinkedLeft, 0.0)).xy;
                float2 rightPoint = UnityObjectToViewPos(float3(shrinkedRight, 0.0)).xy;

                if (abs(leftPoint.x - rightPoint.x) < 0.001)
                {
                    o.segment = float3(1, 0, -leftPoint.x);
                }
                else
                {
                    float2 ab = normalize(float2((rightPoint.y - leftPoint.y) / (leftPoint.x - rightPoint.x), 1.0));
                    float c = -dot(ab, leftPoint);
                    o.segment = float3(ab, c);
                }

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float clip = tex2D(_GUIClipTexture, i.clipUV).a;
                if (clip < 0.1)
                    discard;
                float t = -(dot(i.screenPos, i.segment.xy) + i.segment.z) / i.segment.y;
                float2 verticalProj = float2(i.screenPos.x, t + i.screenPos.y);
                float distance = abs(i.screenPos.y - verticalProj.y);
                //distance = abs(dot(i.segment.xy, i.screenPos) + i.segment.z);
                fixed4 color = fixed4(_Color.rgb, 1.0 - saturate(distance * 4000.0 * abs(ddy(i.uv.y))));
                return color;
            }

            ENDCG
        }
    }
}
