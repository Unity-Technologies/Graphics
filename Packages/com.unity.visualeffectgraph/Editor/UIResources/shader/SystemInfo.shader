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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            uniform float _OrdinateScale;

            v2f vert(vs_input i)
            {
                v2f o;
                float2 shrinkedPoint = float2(i.vertex.x, (i.vertex.y * _OrdinateScale - 0.5) * 0.98 + 0.5);
                o.vertex = UnityObjectToClipPos(float4(shrinkedPoint, 0, 1));
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
