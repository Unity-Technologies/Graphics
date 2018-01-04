//-------------------------------------------------------------------------------------
// FragInputs
// This structure gather all possible varying/interpolator for this shader.
//-------------------------------------------------------------------------------------

#include "../Debug/MaterialDebug.cs.hlsl"

struct FragInputs
{
    // Contain value return by SV_POSITION (That is name positionCS in PackedVarying).
    // xy: unormalized screen position (offset by 0.5), z: device depth, w: depth in view space
    // Note: SV_POSITION is the result of the clip space position provide to the vertex shaders that is transform by the viewport
    float4 positionSS; // In case depth offset is use, positionWS.w is equal to depth offset
    float3 positionWS;
    float2 texCoord0;
    float2 texCoord1;
    float2 texCoord2;
    float2 texCoord3;
    float4 color; // vertex color

    // TODO: confirm with Morten following statement
    // Our TBN is orthogonal but is maybe not orthonormal in order to be compliant with external bakers (Like xnormal that use mikktspace).
    // (xnormal for example take into account the interpolation when baking the normal and normalizing the tangent basis could cause distortion).
    // When using worldToTangent with surface gradient, it doesn't normalize the tangent/bitangent vector (We instead use exact same scale as applied to interpolated vertex normal to avoid breaking compliance).
    // this mean that any usage of worldToTangent[1] or worldToTangent[2] outside of the context of normal map (like for POM) must normalize the TBN (TCHECK if this make any difference ?)
    // When not using surface gradient, each vector of worldToTangent are normalize (TODO: Maybe they should not even in case of no surface gradient ? Ask Morten)
    float3x3 worldToTangent;

    // For two sided lighting
    bool isFrontFace;
};

void GetVaryingsDataDebug(uint paramId, FragInputs input, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEWVARYING_TEXCOORD0:
        result = float3(input.texCoord0, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD1:
        result = float3(input.texCoord1, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD2:
        result = float3(input.texCoord2, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD3:
        result = float3(input.texCoord3, 0.0);
        break;
    case DEBUGVIEWVARYING_VERTEX_TANGENT_WS:
        result = input.worldToTangent[0].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_BITANGENT_WS:
        result = input.worldToTangent[1].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_NORMAL_WS:
        result = input.worldToTangent[2].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR:
        result = input.color.rgb; needLinearToSRGB = true;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR_ALPHA:
        result = input.color.aaa;
        break;
    }
}

uint GetMipCount(Texture2D tex)
{
    uint width, height, depth, mipCount;
    width = height = depth = mipCount = 0;
    tex.GetDimensions(width, height, depth, mipCount);
    return mipCount;
}

float4 GetStreamingMipColour(uint mipCount, float4 mipInfo)
{
    // mipInfo :
    // x = quality setings minStreamingMipLevel
    // y = original mip count for texture
    // z = desired on screen mip level
    // w = 0
    uint originalTextureMipCount = uint(mipInfo.y);

    // If material/shader mip info (original mip level) has not been set its not a streamed texture
    if (originalTextureMipCount == 0)
        return float4(1.0f, 1.0f, 1.0f, 0.0f);

    uint desiredMipLevel = uint(mipInfo.z);
    uint mipCountDesired = uint(originalTextureMipCount) - uint(desiredMipLevel);
    if (mipCount < mipCountDesired)
    {
        // red tones when not at the desired mip level (reduction due to budget). Brighter is further from original, alpha 0 when at desired
        float ratioToDesired = float(mipCount) / float(mipCountDesired);
        return float4(0.5f + (0.5f * ratioToDesired), 0.0f, 0.0f, 1.0f - ratioToDesired);
    }
    else if (mipCount >= originalTextureMipCount)
    {
        // original colour when at (or beyond) original mip count
        return float4(1.0f, 1.0f, 1.0f, 0.0f);
    }
    else
    {
        // green tones when not at the original mip level. Brighter is closer to original, alpha 0 when at original
        float ratioToOriginal = float(mipCount) / float(originalTextureMipCount);
        return float4(0.0f, 0.5f + (0.5f * ratioToOriginal), 0.0f, 1.0f - ratioToOriginal);
    }
}

float4 GetSimpleMipCountColour(uint mipCount)
{
    // 0-4, red tones, 4-7 green tones, 8-11 blue tones
    float4  color = float4(
        mipCount >= 0 && mipCount < 4 ? 0.5f + (float)(mipCount) / 6.0f : 0.0f,
        mipCount >= 4 && mipCount < 8 ? 0.5f + (float)(mipCount - 4) / 6.0f : 0.0f,
        mipCount >= 8 && mipCount < 12 ? 0.5f + (float)(mipCount - 8) / 6.0f : 0.0f,
        0.5f
        );

    return mipCount >= 12 ? float4(1.0f, 1.0f, 1.0f, 0.0f) : color;
}

float GetMipLevel(float2 uv, float2 textureSize)
{
    float2 dx = ddx(uv * textureSize.x);
    float2 dy = ddy(uv * textureSize.y);
    float d = max(dot(dx, dx), dot(dy, dy));
    return 0.5f * log2(d);
}

float4 GetMipLevelColour(float2 iUV, float2 iTextureSize)
{
    float mipLevel = GetMipLevel(iUV, iTextureSize);
    mipLevel = clamp(mipLevel, 0.0f, 6.0f - 0.0001f);

    float4 colours[6] = {
        float4(0.0f, 0.0f, 1.0f, 0.8f),
        float4(0.0f, 0.5f, 1.0f, 0.4f),
        float4(1.0f, 1.0f, 1.0f, 0.0f), // optimal level
        float4(1.0f, 0.7f, 0.0f, 0.2f),
        float4(1.0f, 0.3f, 0.0f, 0.6f),
        float4(1.0f, 0.0f, 0.0f, 0.8f)
    };

    int mipLevelInt = floor(mipLevel);
    float t = frac(mipLevel);
    float4 a = colours[mipLevelInt];
    float4 b = colours[mipLevelInt + 1];
    float4 color = lerp(a, b, t);

    return color;
}

float3 GetDebugMipColour(float3 originalColour, Texture2D tex, float4 texelSize, float2 uv)
{
    // https://aras-p.info/blog/2011/05/03/a-way-to-visualize-mip-levels/
    float4 mipColour = GetMipLevelColour(uv, texelSize.zw);	// /8 to push down 2 mip levels (4 * uv difference)
    return lerp(originalColour, mipColour.rgb, mipColour.a);
}

float3 GetDebugMaxMipColour(float3 originalColour, Texture2D tex)
{
    uint mipCount = GetMipCount(tex);

    float4 mipColour = GetSimpleMipCountColour(mipCount);
    return lerp(originalColour, mipColour.rgb, mipColour.a);
}

float3 GetDebugStreamingMipColour(float3 originalColour, Texture2D tex, float4 mipInfo)
{
    uint mipCount = GetMipCount(tex);

    float4 mipColour = GetStreamingMipColour(mipCount, mipInfo);
    return lerp(originalColour, mipColour.rgb, mipColour.a);
}

void GetTextureDataDebug(uint paramId, FragInputs input, Texture2D tex, float4 texelSize, float4 mipInfo, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEWTEXTURE_SHOW_MIP_RATIO:
        result = GetDebugMipColour(result, tex, texelSize, input.texCoord0.xy);
        break;
    case DEBUGVIEWTEXTURE_MAX_MIP:
        result = GetDebugMaxMipColour(result, tex);
        break;
    case DEBUGVIEWTEXTURE_STREAMING_MIPS:
        result = GetDebugStreamingMipColour(result, tex, mipInfo);
        break;
    }
}

