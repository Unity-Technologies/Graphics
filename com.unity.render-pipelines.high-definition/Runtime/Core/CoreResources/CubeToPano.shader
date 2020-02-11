Shader "Hidden/CubeToPano" {
Properties {
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
}

HLSLINCLUDE
#pragma editor_sync_compilation
#pragma target 4.5
#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

#include "UnityCG.cginc"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"

UNITY_DECLARE_TEXCUBE(_srcCubeTexture);
UNITY_DECLARE_TEXCUBEARRAY(_srcCubeTextureArray);

uniform int     _cubeMipLvl;
uniform int     _cubeArrayIndex;
uniform bool    _buildPDF;

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(vertex);
    o.texcoord = texcoord.xy;
    return o;
}

half2 DirectionToSphericalTexCoordinate(half3 dir_in)      // use this for the lookup
{
    half3 dir = normalize(dir_in);
    // coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
    float recipPi = 1.0/3.1415926535897932384626433832795;
    return half2( 1.0-0.5*recipPi*atan2(dir.x, -dir.z), asin(dir.y)*recipPi + 0.5 );
}

half3 SphericalTexCoordinateToDirection(half2 sphTexCoord)
{
    float pi = 3.1415926535897932384626433832795;
    float theta = (1-sphTexCoord.x) * (pi*2);
    float phi = (sphTexCoord.y-0.5) * pi;

    float csTh, siTh, csPh, siPh;
    sincos(theta, siTh, csTh);
    sincos(phi, siPh, csPh);

    // theta is 0 at negative Z (backwards). Coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
    return float3(siTh*csPh, siPh, -csTh*csPh);
}

half3 GetDir(float2 texCoord)
{
    return SphericalTexCoordinateToDirection(texCoord.xy);
    //float theta = texCoord.y * UNITY_PI;
    //float phi = (texCoord.x * 2.f * UNITY_PI - UNITY_PI * 0.5f);
    //
    //float cosTheta = cos(theta);
    //float sinTheta = sqrt(1.0f - min(1.0f, cosTheta * cosTheta));
    //float cosPhi = cos(phi);
    //float sinPhi = sin(phi);
    //
    //float3 direction = float3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
    //direction.xy *= -1.0;
    //
    //return direction;
}

float SampleToPDFMeasure(float3 value)
{
    return (value.r + value.g + value.b)*(1.0f/3.0f);
    //return max(value.r, max(value.g, value.b));
    //return Luminance(value);
    //return AcesLuminance(value);
}

float SampleToPDFMeasure(float4 value)
{
    return SampleToPDFMeasure(value.rgb);
}

half4 frag(v2f i) : SV_Target
{
    uint2 pixCoord = ((uint2) i.vertex.xy);

    //half3 dir = SphericalTexCoordinateToDirection(i.texcoord.xy);

    half3 dir = GetDir(i.texcoord.xy);

    if (_buildPDF == 1)
        return (half4)float4(SampleToPDFMeasure(UNITY_SAMPLE_TEXCUBE_LOD(_srcCubeTexture, dir, (float)_cubeMipLvl).rgb).xxx, 1);
    else
        return (half4)UNITY_SAMPLE_TEXCUBE_LOD(_srcCubeTexture, dir, (float) _cubeMipLvl);
}

half4 fragArray(v2f i) : SV_Target
{
    uint2 pixCoord = ((uint2) i.vertex.xy);

    //half3 dir = SphericalTexCoordinateToDirection(i.texcoord.xy);

    half3 dir = GetDir(i.texcoord.xy);

    if (_buildPDF == 1)
        return (half4)float4(SampleToPDFMeasure(UNITY_SAMPLE_TEXCUBEARRAY_LOD(_srcCubeTextureArray, float4(dir, _cubeArrayIndex), (float)_cubeMipLvl).rgb).xxx, 1);
    else
        return (half4)UNITY_SAMPLE_TEXCUBEARRAY_LOD(_srcCubeTextureArray, float4(dir, _cubeArrayIndex), (float)_cubeMipLvl);
}

ENDHLSL

SubShader {
    Pass
    {
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
        ENDHLSL
    }

    Pass
    {
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragArray
        ENDHLSL
    }
}
Fallback Off
}
