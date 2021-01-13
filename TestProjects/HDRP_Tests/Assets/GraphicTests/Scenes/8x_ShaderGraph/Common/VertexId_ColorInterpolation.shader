Shader "Unlit/VertexId_ColorInterpolation"
{
    Properties
    {
        _SurfaceColor("SurfaceColor", Color) = (1, 1, 1, 1)
        _VertexIdIntensity("Intensity", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint vertexid : SV_VERTEXID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                uint vertexid : CUSTOM_VERTEX_ID;
                float4 color : COLOR;
            };

            float4 _SurfaceColor;
            float _VertexIdIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertexid = v.vertexid; // Interpolate vertex ID.
                o.color = _SurfaceColor; // Color interpolation override.
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // combine results for compound test.
                return i.color * (_VertexIdIntensity * i.vertexid);
            }
            ENDCG
        }
    }
}
