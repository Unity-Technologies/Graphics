Shader "AddSubGraph"
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
            float4 Color_Color_FD942667_Uniform = float4 (1, 0, 0, 0);
            float4 Color_Color_C568FE2A_Uniform = float4 (0.1586208, 1, 0, 0);
            // Subgraph for node SubGraph_F6FDDDC0
            float4 SubGraph_F6FDDDC0_Output1 = 0;
            {
                float4 SubGraphInputs_23A80546_Input1 = Color_Color_FD942667_Uniform;
                float4 SubGraphInputs_23A80546_Input2 = Color_Color_C568FE2A_Uniform;
                float4 Add_B9CA3569_result;
                Unity_Add_float(SubGraphInputs_23A80546_Input1, SubGraphInputs_23A80546_Input2, Add_B9CA3569_result);
                SubGraph_F6FDDDC0_Output1 = Add_B9CA3569_result;
            }
            // Subgraph ends
            o.Emission = SubGraph_F6FDDDC0_Output1;

    }
    ENDCG
}


    FallBack "Diffuse"
    CustomEditor "LegacyIlluminShaderGUI"
}
