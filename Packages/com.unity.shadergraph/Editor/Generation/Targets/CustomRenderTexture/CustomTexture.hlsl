// Ported from CGinc version

#ifndef UNITY_CUSTOM_TEXTURE_INCLUDED
#define UNITY_CUSTOM_TEXTURE_INCLUDED

#ifndef UNITY_PI
#define UNITY_PI 3.14159265358979323846
#endif

// Keep in sync with CustomRenderTexture.h
#define kCustomTextureBatchSize 16

#define CRT_DIMENSION_2D    0.0
#define CRT_DIMENSION_3D    1.0
#define CRT_DIMENSION_CUBE  2.0

struct appdata_customrendertexture
{
    uint    vertexID    : SV_VertexID;
};

// User facing vertex to fragment shader structure
struct v2f_customrendertexture
{
    float4 vertex           : SV_POSITION;
    float3 localTexcoord    : TEXCOORD0;    // Texcoord local to the update zone (== globalTexcoord if no partial update zone is specified)
    float3 globalTexcoord   : TEXCOORD1;    // Texcoord relative to the complete custom texture
    uint primitiveID        : TEXCOORD2;    // Index of the update zone (correspond to the index in the updateZones of the Custom Texture)
    float3 direction        : TEXCOORD3;    // For cube textures, direction of the pixel being rendered in the cubemap
};

float2 CustomRenderTextureRotate2D(float2 pos, float angle)
{
    float sn = sin(angle);
    float cs = cos(angle);

    return float2( pos.x * cs - pos.y * sn, pos.x * sn + pos.y * cs);
}

// Built-in unity functions and matrices:

float4 _Time, _SinTime, _CosTime, unity_DeltaTime;

// ================================
//     PER FRAME CONSTANTS
// ================================
#if defined(USING_STEREO_MATRICES)
    float4x4 unity_StereoMatrixP[2];
    float4x4 unity_StereoMatrixV[2];
    float4x4 unity_StereoMatrixInvV[2];
    float4x4 unity_StereoMatrixVP[2];

    float4x4 unity_StereoCameraProjection[2];
    float4x4 unity_StereoCameraInvProjection[2];
    float4x4 unity_StereoWorldToCamera[2];
    float4x4 unity_StereoCameraToWorld[2];

    #define glstate_matrix_projection unity_StereoMatrixP[unity_StereoEyeIndex]
    #define unity_MatrixV unity_StereoMatrixV[unity_StereoEyeIndex]
    #define unity_MatrixInvV unity_StereoMatrixInvV[unity_StereoEyeIndex]
    #define unity_MatrixVP unity_StereoMatrixVP[unity_StereoEyeIndex]

    #define unity_CameraProjection unity_StereoCameraProjection[unity_StereoEyeIndex]
    #define unity_CameraInvProjection unity_StereoCameraInvProjection[unity_StereoEyeIndex]
    #define unity_WorldToCamera unity_StereoWorldToCamera[unity_StereoEyeIndex]
    #define unity_CameraToWorld unity_StereoCameraToWorld[unity_StereoEyeIndex]
#else
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    float4x4 unity_ObjectToWorld;
    float4 unity_StereoScaleOffset;
#endif

// Internal
float4      CustomRenderTextureCenters[kCustomTextureBatchSize];
float4      CustomRenderTextureSizesAndRotations[kCustomTextureBatchSize];
float       CustomRenderTexturePrimitiveIDs[kCustomTextureBatchSize];

float4      CustomRenderTextureParameters;
#define     CustomRenderTextureUpdateSpace  CustomRenderTextureParameters.x // Normalized(0)/PixelSpace(1)
#define     CustomRenderTexture3DTexcoordW  CustomRenderTextureParameters.y
#define     CustomRenderTextureIs3D         (CustomRenderTextureParameters.z == CRT_DIMENSION_3D)
#define     CustomRenderTextureDimension    CustomRenderTextureParameters.z

// User facing uniform variables
float4      _CustomRenderTextureInfo; // x = width, y = height, z = depth, w = face/3DSlice

// Helpers
#define _CustomRenderTextureWidth   _CustomRenderTextureInfo.x
#define _CustomRenderTextureHeight  _CustomRenderTextureInfo.y
#define _CustomRenderTextureDepth   _CustomRenderTextureInfo.z

// Those two are mutually exclusive so we can use the same slot
#define _CustomRenderTextureCubeFace    _CustomRenderTextureInfo.w
#define _CustomRenderTexture3DSlice     _CustomRenderTextureInfo.w

float _CustomRenderTextureMipLevel;

TEXTURE2D(_SelfTexture2D);
float4 _SelfTexture2D_TexelSize;
SAMPLER(sampler_SelfTexture2D);

TEXTURECUBE(_SelfTextureCube);
float4 _SelfTextureCube_TexelSize;
SAMPLER(sampler_SelfTextureCube);

TEXTURE3D(_SelfTexture3D);
float4 _SelfTexture3D_TexelSize;
SAMPLER(sampler_SelfTexture3D);

float3 ComputeCubemapDirectionFromUV(float2 uv, int cubeFace)
{
    float2 xy = uv * 2.0 - 1.0;
    float3 direction;
    if(cubeFace == 0.0)
    {
        direction = normalize(float3(1.0, -xy.y, -xy.x));
    }
    else if(cubeFace == 1.0)
    {
        direction = normalize(float3(-1.0, -xy.y, xy.x));
    }
    else if(cubeFace == 2.0)
    {
        direction = normalize(float3(xy.x, 1.0, xy.y));
    }
    else if(cubeFace == 3.0)
    {
        direction = normalize(float3(xy.x, -1.0, -xy.y));
    }
    else if(cubeFace == 4.0)
    {
        direction = normalize(float3(xy.x, -xy.y, 1.0));
    }
    else if(cubeFace == 5.0)
    {
        direction = normalize(float3(-xy.x, -xy.y, -1.0));
    }
    else
    {
        direction = float3(0, 0, 0);
    }

    return direction;
}

float3 CustomRenderTextureComputeCubeDirection(float2 globalTexcoord)
{
    return ComputeCubemapDirectionFromUV(globalTexcoord, _CustomRenderTextureCubeFace);
}

// standard custom texture vertex shader that should always be used
v2f_customrendertexture CustomRenderTextureVertexShader(appdata_customrendertexture IN)
{
    v2f_customrendertexture OUT;

#if UNITY_UV_STARTS_AT_TOP
    const float2 vertexPositions[6] =
    {
        { -1.0f,  1.0f },
        { -1.0f, -1.0f },
        {  1.0f, -1.0f },
        {  1.0f,  1.0f },
        { -1.0f,  1.0f },
        {  1.0f, -1.0f }
    };

    const float2 texCoords[6] =
    {
        { 0.0f, 0.0f },
        { 0.0f, 1.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f },
        { 0.0f, 0.0f },
        { 1.0f, 1.0f }
    };
#else
    const float2 vertexPositions[6] =
    {
        {  1.0f,  1.0f },
        { -1.0f, -1.0f },
        { -1.0f,  1.0f },
        { -1.0f, -1.0f },
        {  1.0f,  1.0f },
        {  1.0f, -1.0f }
    };

    const float2 texCoords[6] =
    {
        { 1.0f, 1.0f },
        { 0.0f, 0.0f },
        { 0.0f, 1.0f },
        { 0.0f, 0.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f }
    };
#endif

    uint primitiveID = IN.vertexID / 6;
    uint vertexID = IN.vertexID % 6;
    float3 updateZoneCenter = CustomRenderTextureCenters[primitiveID].xyz;
    float3 updateZoneSize = CustomRenderTextureSizesAndRotations[primitiveID].xyz;
    float rotation = CustomRenderTextureSizesAndRotations[primitiveID].w * UNITY_PI / 180.0f;

#if !UNITY_UV_STARTS_AT_TOP
    rotation = -rotation;
#endif

    // Normalize rect if needed
    if (CustomRenderTextureUpdateSpace > 0.0) // Pixel space
    {
        // Normalize xy because we need it in clip space.
        updateZoneCenter.xy /= _CustomRenderTextureInfo.xy;
        updateZoneSize.xy /= _CustomRenderTextureInfo.xy;
    }
    else // normalized space
    {
        // Un-normalize depth because we need actual slice index for culling
        updateZoneCenter.z *= _CustomRenderTextureInfo.z;
        updateZoneSize.z *= _CustomRenderTextureInfo.z;
    }

    // Compute rotation

    // Compute quad vertex position
    float2 clipSpaceCenter = updateZoneCenter.xy * 2.0 - 1.0;
    float2 pos = vertexPositions[vertexID] * updateZoneSize.xy;
    pos = CustomRenderTextureRotate2D(pos, rotation);
    pos.x += clipSpaceCenter.x;
#if UNITY_UV_STARTS_AT_TOP
    pos.y += clipSpaceCenter.y;
#else
    pos.y -= clipSpaceCenter.y;
#endif

    // For 3D texture, cull quads outside of the update zone
    // This is neeeded in additional to the preliminary minSlice/maxSlice done on the CPU because update zones can be disjointed.
    // ie: slices [1..5] and [10..15] for two differents zones so we need to cull out slices 0 and [6..9]
    if (CustomRenderTextureIs3D)
    {
        int minSlice = (int)(updateZoneCenter.z - updateZoneSize.z * 0.5);
        int maxSlice = minSlice + (int)updateZoneSize.z;
        if (_CustomRenderTexture3DSlice < minSlice || _CustomRenderTexture3DSlice >= maxSlice)
        {
            pos.xy = float2(1000.0, 1000.0); // Vertex outside of ncs
        }
    }

    OUT.vertex = float4(pos, 0.0, 1.0);
    OUT.primitiveID = asuint(CustomRenderTexturePrimitiveIDs[primitiveID]);
    OUT.localTexcoord = float3(texCoords[vertexID], CustomRenderTexture3DTexcoordW);
    OUT.globalTexcoord = float3(pos.xy * 0.5 + 0.5, CustomRenderTexture3DTexcoordW);
#if UNITY_UV_STARTS_AT_TOP
    OUT.globalTexcoord.y = 1.0 - OUT.globalTexcoord.y;
#endif
    OUT.direction = CustomRenderTextureComputeCubeDirection(OUT.globalTexcoord.xy);

    return OUT;
}

struct appdata_init_customrendertexture
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
};

// User facing vertex to fragment structure for initialization materials
struct v2f_init_customrendertexture
{
    float4 vertex : SV_POSITION;
    float3 texcoord : TEXCOORD0;
    float3 direction : TEXCOORD1;
};

// standard custom texture vertex shader that should always be used for initialization shaders
v2f_init_customrendertexture InitCustomRenderTextureVertexShader (appdata_init_customrendertexture v)
{
    v2f_init_customrendertexture o;
    o.vertex = v.vertex;
    o.texcoord = float3(v.texcoord.xy, CustomRenderTexture3DTexcoordW);
    o.direction = CustomRenderTextureComputeCubeDirection(v.texcoord.xy);
    return o;
}

#endif // UNITY_CUSTOM_TEXTURE_INCLUDED
