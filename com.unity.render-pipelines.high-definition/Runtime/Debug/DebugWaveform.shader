Shader "Hidden/PostProcessing/Debug/Waveform"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #pragma target 4.5

    StructuredBuffer<uint4> _WaveformBuffer;
    float3                  _WaveformParameters; // x: buffer width, y: buffer height, z: exposure

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 vertex : SV_POSITION;
    };

    Varyings Vert(Attributes input)
    {
        Varyings o;
        o.vertex = GetFullScreenTriangleVertexPosition(input.vertexID);
        return o;
    }

    float3 Tonemap(float3 x, float exposure)
    {
        const float a = 6.2;
        const float b = 0.5;
        const float c = 1.7;
        const float d = 0.06;

        x *= exposure;
        x = max(0.0 .xxx, x - 0.004 .xxx);
        x = x * (a * x + b) / (x * (a * x + c) + d);
        return x * x;
    }

    float4 Frag(Varyings i) : SV_Target
    {
        const float3 red   = float3(1.40, 0.03, 0.02);
        const float3 green = float3(0.02, 1.10, 0.05);
        const float3 blue  = float3(0.00, 0.25, 1.50);
        float3 color       = float3(0.00, 0.00, 0.00);

        float3 waveform = _WaveformBuffer[uint(i.vertex.x) * _WaveformParameters.y + uint(i.vertex.y)].xyz;

        color += red   * waveform.r;
        color += green * waveform.g;
        color += blue  * waveform.b;

        return float4(saturate(Tonemap(color, _WaveformParameters.z)), 1.0);
    }

    ENDHLSL

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
