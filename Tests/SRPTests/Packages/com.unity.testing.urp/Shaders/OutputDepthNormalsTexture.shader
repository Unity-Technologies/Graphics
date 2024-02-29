Shader "Hidden/Test/OutputDepthNormalsTexture"
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutputAdjustParams;

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 normalizedScreenSpaceUV = input.texcoord;

                float2 adjustedUV = normalizedScreenSpaceUV;
                adjustedUV.x -= _OutputAdjustParams.x;
                adjustedUV.x *= _OutputAdjustParams.z;
                adjustedUV.y -= _OutputAdjustParams.y;
                adjustedUV.y *= _OutputAdjustParams.w;

                if (   normalizedScreenSpaceUV.x > (_OutputAdjustParams.x)
                    && normalizedScreenSpaceUV.y > (_OutputAdjustParams.y)
                    && normalizedScreenSpaceUV.x < (_OutputAdjustParams.x + (1.0 / _OutputAdjustParams.z))
                    && normalizedScreenSpaceUV.y < (_OutputAdjustParams.y + (1.0 / _OutputAdjustParams.w)))
                {
                    float3 normals = SampleSceneNormals(normalizedScreenSpaceUV);
                    return half4(normals,1);
                }
                else
                {
                    return half4(SampleSceneColor(normalizedScreenSpaceUV), 1.0);
                }
            }
            ENDHLSL
        }
    }
}
