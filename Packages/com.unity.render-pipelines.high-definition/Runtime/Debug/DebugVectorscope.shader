Shader "Hidden/PostProcessing/Debug/Vectorscope"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma target 4.5

    StructuredBuffer<uint> _VectorscopeBuffer;
    float3                 _VectorscopeParameters; // x: width, y: height, z: exposure

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 vertex : SV_POSITION;
        float2 uv     : TEXCOORD0;
    };

    Varyings Vert(Attributes input)
    {
        Varyings o;
        o.vertex = GetFullScreenTriangleVertexPosition(input.vertexID);
        o.uv     = GetFullScreenTriangleTexCoord      (input.vertexID);
        return o;
    }

    float Tonemap(float x, float exposure)
    {
        const float a = 6.2;
        const float b = 0.5;
        const float c = 1.7;
        const float d = 0.06;

        x *= exposure;
        x  = max(0.0, x - 0.004);
        x  = (x * (a * x + b)) / (x * (a * x + c) + d);

        return x * x;
    }

    float4 Frag(Varyings i) : SV_Target
    {
        i.uv.x = 1.0 - i.uv.x;

        const float3 color = YCoCgToRGB(float3(0.5, i.uv.x, i.uv.y));

        const uint2 uvI = i.uv.xy * _VectorscopeParameters.xy;
        const uint  v   = _VectorscopeBuffer[uvI.x + uvI.y * _VectorscopeParameters.x];
        const float vt  = saturate(Tonemap(v, _VectorscopeParameters.z));

        return float4(lerp(color, (0.0).xxx, vt), 1.0);
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
