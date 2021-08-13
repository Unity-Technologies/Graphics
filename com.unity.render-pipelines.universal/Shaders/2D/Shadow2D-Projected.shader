Shader "Hidden/ShadowProjected2D"
{
    Properties
    {
        [HideInInspector] _ShadowColorMask("__ShadowColorMask", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }

        Cull Off
        BlendOp Add
        Blend One Zero
        ZWrite Off

        // This pass draws the projected shadow and sets the composite shadow bit.
        Pass
        {
            // Bit 0: Not Composite Shadows, Bit 1: Global Shadow
            Stencil
            {
                Ref         1     
                ReadMask    3           // If no composite shadows and no global shadows set
                WriteMask   1           // Write to bit 0
                Comp        GEqual
                Pass        Zero        // Write composite shadow value
                Fail        Keep
            }

            // Draw the shadow
            ColorMask[_ShadowColorMask]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShadowProjectVertex.hlsl"

            Varyings vert (Attributes v)
            {
                return ProjectShadow(v);
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(1,1,1,1);
            }
            ENDHLSL
        }
        // This pass converts the composite shadow to global shadow
        Pass
        {
            // Bit 0: Composite Shadows, Bit 1: Global Shadow
            Stencil 
            {
                // We know this is drawing a composite shadow. If not sprite then convert to global shadow and draw.
                Ref         3
                ReadMask    1
                WriteMask   3
                Comp        NotEqual
                Pass        Replace
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShadowProjectVertex.hlsl"

            Varyings vert (Attributes v)
            {
                return ProjectShadow(v);
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(1,1,1,1);
            }
            ENDHLSL
        }
    }
}
