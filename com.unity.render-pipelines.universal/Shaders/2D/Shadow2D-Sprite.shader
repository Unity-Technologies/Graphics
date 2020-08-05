Shader "Hidden/2D/Shadow2D-Sprite"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        ZWrite Off
        ZTest Always

        // Stencil Bits
        // 0 - Sprite Mask
        // 1 - Self Shadow
        // 2 - Shadow

        // Create Sprite Mesh Mask for the Light
        Pass
        {
            Tags{ "LightMode" = "MeshMasking" }

            Stencil
            {
                Ref       1     // Sprite Mask Bit
                ReadMask  1     // Sprite Mask Bit
                WriteMask 1     // Sprite Mask Bit
                Comp      NotEqual
                Pass      Replace
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Sprite-Shared.hlsl"

            // This doesn't do anything because of ColorMask 0
            half4 frag (Varyings i) : SV_Target
            {
                return half4(0,0,0,0);
            }

            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "NoSelfShadow-SelfShadow" }  // Self shadowing can still happen when you have transparency (acts like its behind the sprite). Don't draw if it is shadowed

            Stencil
            {
                Ref       3      // Sprite Mask Bit, Self Shadow Bit
                Comp      Equal
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Sprite-Shared.hlsl"

            half4 frag (Varyings i) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return (_ShadowIntensity * _LightColor * alpha) + ((1 - _ShadowIntensity) * _LightColor);
            }

            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "NoSelfShadow-Lit" }  // Don't draw if shadowed or self shadowed

            Stencil
            {
                Ref       1     // Sprite Mask Bit
                Comp      Equal
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Sprite-Shared.hlsl"

            half4 frag (Varyings i) : SV_Target
            {
                return _LightColor;
            }

            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "SelfShadow" }  // Don't draw if shadowed or self shadowed. Only draw the valid part of the sprite

            Stencil
            {
                Ref       0
                ReadMask  6  // Shadow Bit, Self Shadow Bit
                Comp      Equal
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Sprite-Shared.hlsl"


            half4 frag (Varyings i) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return (_ShadowIntensity * _LightColor * (1-alpha)) + ((1 - _ShadowIntensity) * _LightColor);
            }

            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "DrawShadows" }  // Draw if shadowed 

            Stencil
            {
                Ref       4
                ReadMask  4  // Shadow Bit, Self Shadow Bit
                Comp      Equal
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Sprite-Shared.hlsl"


            half4 frag (Varyings i) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return (_ShadowIntensity * _LightColor * (1-alpha)) + ((1 - _ShadowIntensity) * _LightColor);
            }

            ENDHLSL
        }

    }
}
