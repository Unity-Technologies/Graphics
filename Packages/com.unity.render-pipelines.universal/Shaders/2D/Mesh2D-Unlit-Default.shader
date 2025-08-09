Shader "Universal Render Pipeline/2D/Mesh2D-Unlit-Default"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        [HideInInspector] _White("Tint", Color) = (1,1,1,1)  // Added to break SRP batching. Work around for issue with SRP Batching
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Stencil
        {
            Ref 128                // Put this in the last bit of our stencil value for maximum compatibility with sprite mask
            Comp always
            Pass replace
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Back
        ZWrite On

        Pass
        {
            Stencil
            {
                Ref 128                // Put this in the last bit of our stencil value for maximum compatibility with sprite mask
                Comp always
                Pass replace
            }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
            };
                
            float4 _White;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            Varyings UnlitVertex(Attributes input)
            {
                return CommonUnlitVertex(input);
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, _White);
            }
            ENDHLSL
        }
    }
}
