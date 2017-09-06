Shader "Multiply"
{
    Properties
    {

    }

SubShader
{
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Blend One Zero

        Cull Back

        ZTest LEqual

        ZWrite On


    LOD 200

    CGPROGRAM
    #pragma target 3.0
    #pragma surface surf Standard vertex:vert
    #pragma glsl
    #pragma debug


        void Unity_Multiply_float(float4 first, float4 second, out float4 result)
        {
            result = first * second;
        }



    struct Input
    {
            float4 color : COLOR;

    };

    void vert (inout appdata_full v, out Input o)
    {
        UNITY_INITIALIZE_OUTPUT(Input,o);

    }

    void surf (Input IN, inout SurfaceOutputStandard o)
    {
            float4 Color_Color_E181B4C8_Uniform = float4 (1.022059, 1.022059, 1.022059, 0);
            float4 Color_Color_8AFF9DED_Uniform = float4 (1.007353, -0.01481403, -0.01481403, 0);
            float4 Multiply_91E1EE9F_result;
            Unity_Multiply_float(Color_Color_E181B4C8_Uniform, Color_Color_8AFF9DED_Uniform, Multiply_91E1EE9F_result);
            o.Emission = Multiply_91E1EE9F_result;

    }
    ENDCG
}


    FallBack "Diffuse"
    CustomEditor "LegacyIlluminShaderGUI"
}
