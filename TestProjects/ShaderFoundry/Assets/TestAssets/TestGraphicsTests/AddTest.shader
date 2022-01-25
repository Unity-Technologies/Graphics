Shader "Hidden/Test/AddTest"
{
    Properties
    {
        _A("A", Vector) = (7.0, 7.0, 7.0, 7.0)
        _B("B", Vector) = (9.0, 9.0, 9.0, 9.0)
        _Expected("Expected", Vector) = (4.0, 4.0, 4.0, 4.0)
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _MODE_DIFF _MODE_EXPECTED _MODE_ACTUAL

            #include "UnityCG.cginc"
            #include "Assets\CommonAssets\Shaders\DiffTest.hlsl"

            float4 _A;
            float4 _B;
            float4 _Expected;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 expected = _Expected;
                float4 actual = _A + _B;
                return DiffFloat4(expected, actual);
            }
            ENDCG
        }
    }
}
