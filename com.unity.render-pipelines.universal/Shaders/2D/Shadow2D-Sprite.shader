Shader "Hidden/2D/Shadow2D-Sprite"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull   Off
        ZWrite Off
        ZTest  Always
        Blend One One
        BlendOp Max

        // Stencil Bits
        // 0 - Sprite Mask
        // 1 - Shadow
        Pass
        {
            Tags{ "LightMode" = "NoSelfShadow" }  // Draw if shadowed

            Stencil
            {
                Ref       1     // Sprite Mask Value
                WriteMask 1     // Sprite Mask Value
                Comp      Greater
                Pass      Replace
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Sprite-Shared.hlsl"

            half4 frag (Varyings i) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return _ShadowIntensity * alpha;
            }

            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "SelfShadow" }  // Draw if not shadowed

            Stencil
            {
                Ref       1         // Sprite Mask Value
                WriteMask 1         // Sprite Mask Value
                Comp      LEqual
                Pass      Replace
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2D-Sprite-Shared.hlsl"

            half4 frag (Varyings i) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return _ShadowIntensity * (1-alpha);
            }

            ENDHLSL
        }
    }
}
