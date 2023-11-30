Shader "Hidden/Test/RandomUAVFillShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "UAV-Fill-Pass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Fragment
                #pragma target 4.5

                #include "RandomUAVShared.hlsl"

                // This pass gets the opaque texture color and writes it to the UAV targets
                float4 Fragment(Varyings input) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    const float2 uv = input.texcoord;

                    // Scene color
                    const float4 sceneColor = float4(SampleSceneColor(uv), 0.0);

                    // UAV Texture Buffer output
                    WriteToTextureBuffer(uv, sceneColor);

                    // UAV Buffer output
                    WriteToBuffer(uv, sceneColor);

                    // Normal output - Not used.
                    return sceneColor;
                }
            ENDHLSL
        }
    }
}
