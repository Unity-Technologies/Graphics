Shader "Hidden/Universal/CoreBlitColorAndDepth"
{
    HLSLINCLUDE

        #pragma target 2.0
        #pragma editor_sync_compilation
        // Core.hlsl for XR dependencies
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/Runtime/Utilities/BlitColorAndDepth.hlsl"
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        // 0: Color Only
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Name "ColorOnly"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragColorOnly
            ENDHLSL
        }

        // 1:  Color Only and Depth
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off
            Name "ColorAndDepth"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragColorAndDepth
            ENDHLSL
        }

        // 2:  Depth Only
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off ColorMask 0
            Name "DepthOnly"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragDepthOnly
            ENDHLSL
        }
    }

    Fallback Off
}
