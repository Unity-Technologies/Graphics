Shader "Hidden/2D/Shadow2D-Projected"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        ZWrite Off
        ZTest Always
        ColorMask 0


        // Stencil Bits
        // 0 - Sprite Mask
        // 1 - Self Shadow
        // 2 - Shadow
        Pass
        {
            Tags{ "LightMode" = "DrawShadow" }  // Don't draw if shadowed or self shadowed. Only draw the valid part of the sprite

            Stencil
            {
                Ref         2
                ReadMask    2
                WriteMask   2
                Comp        NotEqual
                Pass        Replace
                Fail        Keep
            }

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Projected-Shared.hlsl"

            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "ConvertShadows" }  // Convert self shadow to shadow.

            Stencil
            {
                Ref         4
                ReadMask    6
                WriteMask   6
                Comp        NotEqual
                Pass        Replace
                Fail        Keep
            }

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Projected-Shared.hlsl"

            ENDHLSL
        }
    }
}
