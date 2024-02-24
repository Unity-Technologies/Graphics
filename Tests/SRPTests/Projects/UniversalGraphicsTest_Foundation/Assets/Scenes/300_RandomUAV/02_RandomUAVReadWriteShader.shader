Shader "Hidden/Test/RandomUAVReadWriteShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "UAV-Read-Write-Pass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Fragment
                #pragma target 4.5

                #include "RandomUAVShared.hlsl"

                // This pass reads from the UAV inputs, modifies the values and writes it back to them
                float4 Fragment(Varyings input) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    const float2 uv = input.texcoord;

                    // Input colors
                    const float4 sceneColor = float4(SampleSceneColor(uv), 0.0);
                    const float4 uavTextureBufferColor = ReadFromTextureBuffer(uv);
                    const float4 uavBufferColor = ReadFromBuffer(uv);

                    // UAV Texture Buffer output
                    const float4 uavModifiedTextureBufferColor = float4(1.0, 1.0, 1.0, 1.0) - uavTextureBufferColor;

                    // UAV Buffer output
                    const float lerpVal = EaseInOutQuad((uv.x - ONE_THIRD) / ONE_THIRD);
                    const float4 uavModifiedBufferColor = lerp(uavModifiedTextureBufferColor, uavBufferColor, lerpVal);

                    WriteToTextureBuffer(uv, uavModifiedTextureBufferColor);
                    WriteToBuffer(uv, uavModifiedBufferColor);

                    // Normal output - Not used.
                    return sceneColor;
                }
            ENDHLSL
        }
    }
}
