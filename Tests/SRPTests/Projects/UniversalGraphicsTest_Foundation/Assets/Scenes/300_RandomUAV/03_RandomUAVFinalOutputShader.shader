Shader "Hidden/Test/RandomUAVFinalOutputShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "UAV-Final-Output-Pass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Fragment
                #pragma target 4.5

                #include "RandomUAVShared.hlsl"

                // This pass outputs the results from the 2 UAV buffers and the opaque texture color
                float4 Fragment(Varyings input) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    const float2 uv = input.texcoord;

                    // UAV Texture Buffer
                    if (uv.x < ONE_THIRD)
                        return ReadFromTextureBuffer(uv);

                    // UAV Buffer
                    if (uv.x < TWO_THIRDS)
                        return ReadFromBuffer(uv);

                    // Scene color
                    return float4(SampleSceneColor(uv), 1);
                }
            ENDHLSL
        }
    }
}
