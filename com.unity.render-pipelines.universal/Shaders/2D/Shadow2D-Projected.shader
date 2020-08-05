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
                Ref         2           // Self Shadow Bit
                ReadMask    2           // Self Shadow Bit
                WriteMask   2           // Self Shadow Bit
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
                Ref         4           // Shadow Bit
                ReadMask    6           // Self Shadow Bit, Shadow Bit
                WriteMask   6           // Self Shadow Bit, Shadow Bit
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
