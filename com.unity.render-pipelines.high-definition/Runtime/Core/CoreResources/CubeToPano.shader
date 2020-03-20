Shader "Hidden/CubeToPano" {
Properties {
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
}

HLSLINCLUDE
#pragma editor_sync_compilation
#pragma target 4.5
#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

//#include "UnityCG.cginc"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"
//"C:\Code\pkgs\ScriptableRenderPipeline\com.unity.render - pipelines.universal\Shaders\PostProcessing\Common.hlsl"
//UNITY_DECLARE_TEXCUBE(_srcCubeTexture);
//UNITY_DECLARE_TEXCUBEARRAY(_srcCubeTextureArray);

TEXTURECUBE(_srcCubeTexture);
SAMPLER(sampler_srcCubeTexture);
TEXTURECUBE_ARRAY(_srcCubeTextureArray);
SAMPLER(sampler_srcCubeTextureArray);

uniform int     _cubeMipLvl;
uniform int     _cubeArrayIndex;
uniform bool    _buildPDF;
uniform int     _preMultiplyByCosTheta;
uniform int     _preMultiplyBySolidAngle;
uniform int     _preMultiplyByJacobian; // Premultiply by the Det of Jacobian, to be "Integration Ready"
float4          _Sizes; // float4( outSize.xy, 1/outSize.xy )

//struct v2f
//{
//    float4 vertex : SV_POSITION;
//    float2 texcoord : TEXCOORD0;
//};

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

//v2f vert(float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
Varyings Vert(Attributes input)
{
    //v2f o;
    //o.vertex = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(vertex.xyz, 1.0f)));
    //    //UnityObjectToClipPos(vertex);
    //o.texcoord = texcoord.xy;
    //return o;

    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);

    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);

    return output;
}

float2 DirectionToSphericalTexCoordinate(float3 dir_in) // use this for the lookup
{
    float3 dir = normalize(dir_in);
    // coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
    float recipPi = 1.0/3.1415926535897932384626433832795;
    return float2( 1.0-0.5*recipPi*atan2(dir.x, -dir.z), asin(dir.y)*recipPi + 0.5 );
}

float3 SphericalTexCoordinateToDirection(float2 sphTexCoord)
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

float3 GetDir(float2 texCoord)
{
    return SphericalTexCoordinateToDirection(texCoord.xy);
}

//float SampleToPDFMeasure(float3 value)
//{
//    return (value.r + value.g + value.b)*(1.0f/3.0f);
//}
//
//float SampleToPDFMeasure(float4 value)
//{
//    return SampleToPDFMeasure(value.rgb);
//}

float GetScale(float angle)
{
    float scale = 1.0f;
    float pi = 3.1415926535897932384626433832795f;

    if (_preMultiplyByJacobian == 1)
    {
        scale *= sin(angle); // Spherical Jacobian
    }
    if (_preMultiplyByCosTheta == 1)
    {
        scale *= max(-cos(angle), 0.0f);
    }
    if (_preMultiplyBySolidAngle == 1)
    {
        scale *= _Sizes.z*_Sizes.w;
        scale *= pi*pi*0.5f;
    }

    return scale;
}

float4 Frag(Varyings input) : SV_Target
{
    float2 texCoord = input.positionCS.xy*_Sizes.zw + 0.5f*_Sizes.zw;
    float3 dir      = GetDir(texCoord);

    float3 output = SAMPLE_TEXTURECUBE_LOD(_srcCubeTexture, sampler_srcCubeTexture, dir, 0).rgb;

    if (_buildPDF == 1)
        output = SampleToPDFMeasure(output).xxx;

    float scale = 1.0f;
    float pi    = 3.1415926535897932384626433832795f;
    float angle = texCoord.y*pi;

    output *= GetScale(angle);

    //output.x = 0;
    //output.xy = texCoord;

    //return float4(output.rgb, 1);
    //return float4(abs(dir), 1);
    return float4(output.rgb, max(output.r, max(output.g, output.b)));
}

//float4 fragArray(v2f i) : SV_Target
float4 FragArray(Varyings input) : SV_Target
{
    float2 texCoord = input.positionCS.xy*_Sizes.zw + 0.5f*_Sizes.zw;
    float3 dir      = GetDir(texCoord.xy);

    float3 output = SAMPLE_TEXTURECUBE_ARRAY_LOD(_srcCubeTextureArray, sampler_srcCubeTextureArray, dir, _cubeArrayIndex, (float)_cubeMipLvl).rgb;
    if (_buildPDF == 1)
        output = SampleToPDFMeasure(output).xxx;

    float scale = 1.0f;
    float pi    = 3.1415926535897932384626433832795f;
    float angle = (1.0f - texCoord.y)*pi;

    output *= GetScale(angle);

    return float4(output.rgb, max(output.r, max(output.g, output.b)));
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
            #pragma vertex Vert
            #pragma fragment Frag
        ENDHLSL
    }

    Pass
    {
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragArray
        ENDHLSL
    }
}
Fallback Off
}
