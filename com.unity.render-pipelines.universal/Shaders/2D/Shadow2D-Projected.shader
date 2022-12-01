Shader "Hidden/ShadowProjected2D"
{
    Properties
    {
        [HideInInspector] _ShadowColorMask("__ShadowColorMask", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }

        Cull    Off
        BlendOp Add
        Blend   One One, One One
        ZWrite  Off
        ZTest   Always

        // This pass draws the projected shadows
        Pass
        {
            // Draw the shadow
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShadowProjectVertex.hlsl"

            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
            half _ShadowSoftnessFalloffIntensity;

            Varyings vert (Attributes v)
            {
                return ProjectShadow(v);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half2 mappedUV;

                float value = 1 - saturate(abs(i.shadow.x) / i.shadow.y);
                mappedUV.x = value;
                mappedUV.y = _ShadowSoftnessFalloffIntensity;
                value = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;

                half4 color = half4(value, value, value, value);
                return color;
            }
            ENDHLSL
        }
        // This pass draws the projected unshadowing
        Pass
        {
            Stencil
            {
                Ref       1
                Comp      Equal
                Pass      Keep
            }

            // Draw the shadow
            ColorMask G

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShadowProjectVertex.hlsl"

            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
            half _ShadowSoftnessFalloffIntensity;


            Varyings vert (Attributes v)
            {
                return ProjectShadow(v);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half2 mappedUV;

                float value = 1-saturate(abs(i.shadow.x) / i.shadow.y);
                mappedUV.x = value;
                mappedUV.y = _ShadowSoftnessFalloffIntensity;
                value = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;

                half4 color = half4(value, value, value, value);
                return color;
            }
            ENDHLSL
        }
    }
}
