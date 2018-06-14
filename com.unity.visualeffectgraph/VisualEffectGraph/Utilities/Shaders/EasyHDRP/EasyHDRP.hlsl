//////////////////////////////////////////////////////////////
// HDRP Shader Includes                                     //
//////////////////////////////////////////////////////////////

#include "CoreRP/ShaderLibrary/common.hlsl"
#include "HDRP/ShaderVariables.hlsl"

//////////////////////////////////////////////////////////////
// BASE CHECKS                                              //
//////////////////////////////////////////////////////////////

#ifdef MESH_HAS_TANGENT
    // We assume mesh has normals if ever it has tangents
    #define MESH_HAS_NORMALS
#endif

//////////////////////////////////////////////////////////////
//  VERTEX TO PIXEL STRUCTURE                               //
//////////////////////////////////////////////////////////////

struct appdata
{
    float4 vertex : POSITION;
#ifdef MESH_HAS_UV
    float4 uv : TEXCOORD0;
#endif
#ifdef MESH_HAS_UV2
    float4 uv2 : TEXCOORD1;
#endif
#ifdef MESH_HAS_UV3
    float4 uv3 : TEXCOORD2;
#endif
#ifdef MESH_HAS_UV4
    float4 uv3 : TEXCOORD3;
#endif
#ifdef MESH_HAS_NORMALS
    float3 normal : NORMAL;
#endif
#ifdef MESH_HAS_TANGENT
    float4 tangent : TANGENT;
#endif
#ifdef MESH_HAS_COLOR
    float4 color : COLOR;
#endif
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float3 worldPosition : TEXCOORD6;

#ifdef MESH_HAS_UV
    float4 uv : TEXCOORD0;
#endif
#ifdef MESH_HAS_UV2
    float4 uv2 : TEXCOORD1;
#endif
#ifdef MESH_HAS_UV3
    float4 uv3 : TEXCOORD2;
#endif
#ifdef MESH_HAS_UV4
    float4 uv4 : TEXCOORD3;
#endif
#ifdef MESH_HAS_NORMALS
    float3 normal : NORMAL;
#endif
#ifdef MESH_HAS_TANGENT
    float3 tangent : TANGENT;
    float3 bitangent : TEXCOORD5;
#endif
#ifdef MESH_HAS_COLOR
    float4 color : TEXCOORD4;
#endif
};

#ifdef SHADER_CUSTOM_VERTEX
v2f SHADER_CUSTOM_VERTEX(v2f i);
#endif

v2f vert(appdata v)
{
    v2f o;

#ifdef MESH_HAS_UV
    o.uv = v.uv;
#endif
#ifdef MESH_HAS_UV2
    o.uv2 = v.uv2;
#endif
#ifdef MESH_HAS_UV3
    o.uv3 = v.uv3;
#endif
#ifdef MESH_HAS_UV4
    o.uv3 = v.uv3;
#endif
#ifdef MESH_HAS_NORMALS
    o.normal = TransformObjectToWorldDir(v.normal);
#endif
#ifdef MESH_HAS_TANGENT
    o.tangent = TransformObjectToWorldDir(v.tangent.xyz);
    o.bitangent = cross(o.normal, o.tangent) * v.tangent.w;
#endif
#ifdef MESH_HAS_COLOR
    o.color = v.color;
#endif

    // Transform local to world before custom vertex code
    o.vertex.xyz = TransformObjectToWorld(v.vertex.xyz);
    o.worldPosition = o.vertex.xyz;

#ifdef SHADER_CUSTOM_VERTEX
    o = SHADER_CUSTOM_VERTEX(o);
#endif

    // o.vertex is overwritten by custom vertex function so have to
    // apply position modifications only on worldPosition
    o.vertex.xyz = GetCameraRelativePositionWS(o.worldPosition);
    o.vertex = TransformWorldToHClip(o.vertex.xyz);
    return o;
}

//////////////////////////////////////////////////////////////
//
//  HDRP FUNCTIONS
//  ==============

PositionInputs GetPositions(v2f input)
{
    return  GetPositionInput(input.vertex.xy, _ScreenSize.zw, input.vertex.z, input.vertex.w, input.worldPosition);
}

float3 GetWorldSpacePosition(PositionInputs input)
{
    return input.positionWS;
}

uint2 GetScreenSpacePosition(PositionInputs input)
{
    return input.positionSS.xy;
}

float2 GetScreenNormalizedPosition(PositionInputs input)
{
    return input.positionNDC.xy;
}

float GetSampledDepth(PositionInputs input)
{
    return LinearEyeDepth(LOAD_TEXTURE2D(_MainDepthTexture,  input.positionSS.xy).r, _ZBufferParams);
}

float GetPixelDepth(PositionInputs input)
{
    return input.linearDepth;
}

float GetSoftFading(PositionInputs input, float dist)
{
    return saturate( ( GetSampledDepth(input) - GetPixelDepth(input) ) * dist);
}

float GetCameraFade(PositionInputs input, float nearDist, float farDist)
{
    return saturate( (GetPixelDepth(input) - nearDist) / (farDist - nearDist) );
}

float3 GetWorldViewVector(PositionInputs input, v2f i)
{
    return normalize(i.worldPosition - GetAbsolutePositionWS(GetPrimaryCameraPosition()));
}

#ifdef MESH_HAS_TANGENT

float3x3 GetWorldTBNMatrix(v2f i)
{
    return float3x3(i.tangent.xyz, i.bitangent.xyz, i.normal.xyz);
}

float3 GetTangentViewVector(PositionInputs input, v2f i)
{
    return mul(GetWorldTBNMatrix(i), GetWorldViewVector(input, i));
}
#endif



/////////////////////////////////////////////////////////////////////////////
//
//  COORDINATE FUNCTIONS
//  ====================


float2 Panner(float2 TexCoord, float2 ConstScrollRate, float Time) {

    return(TexCoord + frac(ConstScrollRate * Time));

}

float2 RectangularToPolar(float2 TexCoord) {
    return float2(
            sqrt(dot(TexCoord,TexCoord)),
            0.5f+(atan2(TexCoord.y,TexCoord.x)/PI)
    );
}

float2 Rotator(float2 TexCoord, float2 Center, float Angle) {

        float2 AngleCoords = float2(sin(Angle%(2*PI)),cos(Angle%(2*PI)));
        TexCoord -= Center;
        return Center +
                float2(
                    TexCoord.x*AngleCoords.y - TexCoord.y * AngleCoords.x,
                    TexCoord.x*AngleCoords.x + TexCoord.y * AngleCoords.y
                    );
}

float Mask2D(float2 TexCoord, float2 Min, float2 Max) {

    float left =        saturate(ceil(TexCoord.x - min(Min.x,Max.x)));
    float right = 1.0f - saturate(ceil(TexCoord.x - max(Min.x,Max.x)));
    float top =         saturate(ceil(TexCoord.y - min(Min.y,Max.y)));
    float down = 1.0f - saturate(ceil(TexCoord.y - max(Min.y,Max.y)));

    return left * right * top * down;
}


float2 FlipBookUV(float2 TexCoord, int NumU, int NumV, float FrameRate, float Time) {

    float2 NumUV = float2((float)NumU, (float)NumV);
    float T = (Time*FrameRate)%(float)(NumU*NumV);;
    float CF = floor(T);

    return  (TexCoord + float2(CF%NumU, NumV-floor(CF/NumU)))/NumUV;
}

struct FlipBookBlendData {
    float2 CurrentImageTexCoord;
    float2 NextImageTexCoord;
    float Ratio;
};

FlipBookBlendData FlipBookUVBlend(float2 TexCoord, int NumU, int NumV, float FrameRate, float Time) {

    FlipBookBlendData o;

    float2 NumUV = float2((float)NumU, (float)NumV);
    float T = (Time*FrameRate)%(NumUV.x * NumUV.y);
    float CF = floor(T);
    float NF = ceil(T);
    o.Ratio = frac(T);
    o.CurrentImageTexCoord = (TexCoord + float2(CF%NumU, NumV-floor(CF/NumU)))/NumUV;
    o.NextImageTexCoord  = (TexCoord + float2(NF%NumU, NumV-floor(NF/NumU)))/NumUV;
    return o;
}

float2 UVMatchMinMaxto01(float2 TexCoord, float2 MinCoord, float2 MaxCoord) {
    return (TexCoord / (MaxCoord - MinCoord)) - MinCoord;
}

float2 UVMatch01ToMinMax(float2 TexCoord, float MinCoord, float MaxCoord) {
    return (TexCoord - MinCoord) * (MaxCoord-MinCoord);
}

float2 PlanarParallax(float2 coord, float depth, float3 tangentView)
{
    float length = ((tangentView.z + depth) / tangentView.z) - 1.0;
    coord -= tangentView.xy * length;
    return coord;
}

float3 PlanarParallaxAndMask(float2 coord, float depth, float3 tangentView)
{
    coord = PlanarParallax(coord, depth, tangentView);
    float mask = (coord.x > 0) && (coord.x < 1) && (coord.y > 0) && (coord.y < 1) ? 1 : 0;
    return float3(coord, mask);
}


/////////////////////////////////////////////////////////////////////////////
//
//  TEXTURE SAMPLING FUNCTIONS
//  ==========================


float4 FlipBook(sampler2D FlipBookTexture, float2 TexCoord, int NumU, int NumV, float FrameRate, float Time) {

    return tex2D(FlipBookTexture, FlipBookUV(TexCoord, NumU, NumV, FrameRate,Time)).rgba;

}


float4 FlipBookBlend(sampler2D FlipBookTexture, float2 TexCoord, int NumU, int NumV, float FrameRate, float Time) {

    FlipBookBlendData d = FlipBookUVBlend(TexCoord, NumU, NumV, FrameRate,Time);

    return lerp(tex2D(FlipBookTexture, d.CurrentImageTexCoord).rgba, tex2D(FlipBookTexture, d.NextImageTexCoord).rgba, d.Ratio);

}

float4 TriplanarProjection(sampler2D Texture, float3 WorldPixelRatio, float3 WorldNormal, float3 WorldPosition) {

    WorldPosition /= WorldPixelRatio;

    float4 projX = tex2D(Texture, WorldPosition.yz) * abs(WorldNormal.x);
    float4 projY = tex2D(Texture, WorldPosition.xz) * abs(WorldNormal.y);
    float4 projZ = tex2D(Texture, WorldPosition.xy) * abs(WorldNormal.z);
    return projX + projY + projZ;

}


/////////////////////////////////////////////////////////////////////////////
//
//  DSP FUNCTIONS
//  =============

float Saw01(float x) {
    return abs(2*frac(abs(x))-1);
}

float Saw(float x) {
    return 2*(abs(2*frac(abs(x))-1)-0.5);
}


float Square01(float x) {
    return ceil(frac(x)-0.5f);
}

float Square(float x) {
    return 2*(ceil(frac(x)-0.5f))-0.5;
}

float SmoothStep(float x) {
    return ((2*PI*x) - sin(2*PI*x))/(2*PI);
}


/////////////////////////////////////////////////////////////////////////////
//
//  UTILITIES
//  =========


float Treshold(float x, float Treshold) {

    return saturate(ceil(x - Treshold));
}

float Cutout(float Alpha, int NumSteps) {

    NumSteps = max(2,NumSteps);
    return floor(Alpha*NumSteps)/NumSteps;
}

float Desaturate(float3 Color) {

    // Correct luminance factors
    float3 w = Color*float3(0.2126f,0.7152f,0.0722f);
    return w.r + w.g + w.b;
}
