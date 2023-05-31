Shader "Hidden/Test/OutputDepthTexture"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "OutputDepthNormals"
            ZTest Always
            ZWrite Off
            Cull Off


            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 normalizedScreenSpaceUV = input.texcoord;

                float depth = SampleSceneDepth(normalizedScreenSpaceUV);
                #if !UNITY_REVERSED_Z
                    depth = 1.0 - depth;
                #endif

                return half4(depth, depth, depth, 1);
            }
            ENDHLSL
        }
    }
}
