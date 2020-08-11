Shader "Hidden/2D/Shadow2D-Projected"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull   Off
        ZWrite Off
        ZTest  Always

        // Stencil Bits
        // 0 - Sprite Mask
        // 1 - Shadow
        Pass
        {
            Tags{ "LightMode" = "StencilShadows" }  // Render the shadows into the stencil buffer

            Stencil
            {
                Ref         2           // Shadow Value
                ReadMask    2
                Comp        NotEqual
                Pass        Replace
            }

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Projected-Shared.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                return half4(0,0,0,0);
            }

            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "DrawShadow" }  // Render the shadows into the alpha channel and clear the stencil buffer

            Stencil
            {
                Ref         1           // Sprite Value
                ReadMask    1           // Sprite Value
                Comp        NotEqual
                Pass        Zero
                Fail        Zero
            }

            BlendOp Max
            Blend One One
            ColorMask A

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Projected-Shared.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                return half4(0,0,0,_ShadowIntensity);
            }

            ENDHLSL
        }
    }
}
