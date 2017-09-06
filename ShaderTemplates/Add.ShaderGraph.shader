Shader "Add"
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


        void Unity_Add_float(float4 first, float4 second, out float4 result)
        {
            result = first + second;
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
            float4 Color_Color_F01F05F2_Uniform = float4 (0.1172414, 1, 0, 0);
            float4 Color_Color_3FAAF5EA_Uniform = float4 (0.01912789, -0.005136251, 0.6985294, 0);
            float4 Add_1ABB2997_result;
            Unity_Add_float(Color_Color_F01F05F2_Uniform, Color_Color_3FAAF5EA_Uniform, Add_1ABB2997_result);
            o.Emission = Add_1ABB2997_result;

    }
    ENDCG
}


    FallBack "Diffuse"
    CustomEditor "LegacyIlluminShaderGUI"
}
