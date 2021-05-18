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
        ZWrite Off
        ZTest Always

        // This pass draws the projected shadow and sets the composite shadow bit.
        Pass
        {
            // Bit 0: Composite Shadow Bit, Bit 1: Global Shadow Bit
            Stencil
            {
                Ref         5
                ReadMask    4
                WriteMask   1
                Comp        NotEqual
                Pass        Replace
                Fail        Keep
            }

            ColorMask [_ShadowColorMask]

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
        // Sets the global shadow bit, and clears the composite shadow bit
        Pass
        {
            // Bit 0: Composite Shadow Bit, Bit 1: Global Shadow Bit
            Stencil
            {
                Ref         3
                WriteMask   2
                ReadMask    1
                Comp        Equal
                Pass        Replace
                Fail        Keep
            }

            // We only want to change the stencil value in this pass
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
