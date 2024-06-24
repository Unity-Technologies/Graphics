Shader "Hidden/Universal Render Pipeline/ObjectMotionVectorFallback"
{
    SubShader
    {
        Pass
        {
            Name "MotionVectors"

            Tags{ "LightMode" = "MotionVectors" }
            ColorMask RG

            HLSLPROGRAM
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "XRMotionVectors"
            Tags { "LightMode" = "XRMotionVectors" }
            ColorMask RGB

            // Stencil write for obj motion pixels
            Stencil
            {
                WriteMask 1
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #define APLICATION_SPACE_WARP_MOTION 1
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }
    }
}
