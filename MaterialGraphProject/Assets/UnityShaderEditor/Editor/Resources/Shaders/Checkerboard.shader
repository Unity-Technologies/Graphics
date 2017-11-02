Shader "Hidden/Checkerboard"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            static const float rows = 32;
            static const float columns = 32;

            static const float4 col1 = float4(32.0/255.0, 32.0/255.0, 32.0/255.0, 1.0);
            static const float4 col2 = float4(42.0/255.0, 42.0/255.0, 42.0/255.0, 1.0);

            float4 frag(v2f_img i) : COLOR
            {
                float total = floor(i.uv.x * rows) + floor(i.uv.y * columns);
                return lerp(col1, col2, step(fmod(total, 2.0), 0.5));
            }
            ENDCG
        }
    }
}
