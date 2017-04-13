Shader "Hidden/HDRenderPipeline/Sky/SkyHDRI"
{
    HLSLINCLUDE

    #pragma vertex Vert
    #pragma fragment Frag

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev

    #include "../../../../ShaderLibrary/Color.hlsl"
    #include "../../../../ShaderLibrary/Common.hlsl"
    #include "../../../../ShaderLibrary/CommonLighting.hlsl"

    TEXTURECUBE(_Cubemap);
    SAMPLERCUBE(sampler_Cubemap);

    float4   _SkyParam; // x exposure, y multiplier, z rotation

    struct Attributes
    {
        float3 positionCS : POSITION;
        float3 eyeVector : NORMAL;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 eyeVector : TEXCOORD0;
    };

    Varyings Vert(Attributes input)
    {
        // TODO: implement SV_vertexID full screen quad
        Varyings output;
        output.positionCS = float4(input.positionCS.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0);
        output.eyeVector = input.eyeVector;

        return output;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        float3 dir = normalize(input.eyeVector);

        // Rotate direction
        float phi = DegToRad(_SkyParam.z);
        float cosPhi, sinPhi;
        sincos(phi, sinPhi, cosPhi);
        float3 rotDirX = float3(cosPhi, 0, -sinPhi);
        float3 rotDirY = float3(sinPhi, 0, cosPhi);
        dir = float3(dot(rotDirX, dir), dir.y, dot(rotDirY, dir));

        float3 skyColor = ClampToFloat16Max(SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0).rgb * exp2(_SkyParam.x) * _SkyParam.y);
        return float4(skyColor, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend One Zero
            Cull Off

            HLSLPROGRAM
            ENDHLSL

        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend One Zero
            Cull Off

            HLSLPROGRAM
            ENDHLSL
        }

    }
    Fallback Off
}
