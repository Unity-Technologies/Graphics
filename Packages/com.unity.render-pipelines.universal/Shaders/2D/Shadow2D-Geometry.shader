Shader "Hidden/Shadow2DGeometry"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" }

        Cull Off
        ZWrite Off
        ZTest Always

        // Shared vertex entry-point and include for all three geometry passes; only the
        // per-pass HLSLPROGRAM declares its own `frag` function body (kept per-pass even
        // though the body is identical, because empty HLSLPROGRAM blocks relying entirely
        // on HLSLINCLUDE expansion are not an established pattern in URP/HDRP).
        HLSLINCLUDE
        #pragma vertex ShadowGeometryVert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2DGeometryVertex.hlsl"
        ENDHLSL

        // Self-shadow pass: draws shadow into R channel.
        // Used by self-shadowing geometry casters (selfShadows == true).
        Pass
        {
            Name "Self"

            BlendOp Max
            Blend One One
            ColorMask R

            HLSLPROGRAM
            half4 frag(Varyings i) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }

        // Unshadow "Mark" pass: sets stencil ref 1. No color writes (ColorMask 0).
        // First of the two unshadow passes for non-self-shadowing geometry casters.
        Pass
        {
            Name "UnshadowMark"

            Stencil
            {
                Ref       1
                Comp      Always
                Pass      Replace
            }

            ColorMask 0

            HLSLPROGRAM
            half4 frag(Varyings i) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }

        // Unshadow "Unmark" pass: writes B channel and clears stencil ref 0.
        Pass
        {
            Name "UnshadowUnmark"

            Stencil
            {
                Ref       0
                Comp      Always
                Pass      Replace
            }

            Blend   One One
            BlendOp Add
            ColorMask B

            HLSLPROGRAM
            half4 frag(Varyings i) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
}
