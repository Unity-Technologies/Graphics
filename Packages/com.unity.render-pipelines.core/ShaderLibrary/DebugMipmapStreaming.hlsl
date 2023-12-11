#ifndef UNITY_DEBUG_MIPMAP_STREAMING_INCLUDED
#define UNITY_DEBUG_MIPMAP_STREAMING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

// Indices for Mipmap Debug Legend Strings
#define _kNotStreamingIndex 0
#define _kStreamingIndex 1
#define _kStreamingManuallyIndex 2
#define _kStatusNoTextureIndex 3
#define _kStatusWarningIndex 4
#define _kStatusUnknownIndex 5
#define _kStatusStreamerDisabledIndex 6
#define _kStatusMessageNoMipMapIndex 7
#define _kStatusMessageNotSetToStreamIndex 8
#define _kStatusMessageNoAsyncIndex 9
#define _kStatusMessageTerrainIndex 10
#define _kStatusNoTexturesIndex 11
#define _kStatusSomeTexturesHaveIssues 12
#define _kStatusAllTexturesAreStreaming 13
#define _kStatusAllTexturesAreStreamingSomeManually 14
#define _kStatusNoTexturesAreStreaming 15
#define _kStatusSomeTexturesAreStreaming 16
#define _kStatusSomeTexturesAreStreamingSomeManually 17

#define _kNoMipCountIndex 18
#define _kTooManyMipsIndex 19

#define _kHighPixelDensityIndex 20
#define _kLowPixelDensityIndex 21

#define _kLowPriorityIndex 22
#define _kHighPriorityIndex 23

#define _kBudgetSavingMips 24
#define _kBudgetSavingMipsWithCache 25
#define _kBudgetNothingSaved 26
#define _kBudgetMissingMips 27

#define _kRecentlyUpdated 28
#define _kNotRecentlyUpdated 29

static const uint kMipmapDebugLegendStrings[][32] =
{
    // Status
    {13,   'N','o','t',' ','s','t','r','e','a','m','i','n','g','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    { 9,   'S','t','r','e','a','m','i','n','g','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {31,   'S','t','r','e','a','m','i','n','g',' ','(','m','a','n','u','a','l','l','y',' ','v','i','a',' ','s','c','r','i','p','t',')'},
    {18,   'N','o',' ','t','e','x','t','u','r','e',' ','i','n',' ','s','l','o','t','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    { 7,   'W','a','r','n','i','n','g','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {21,   'U','n','k','n','o','w','n',' ','(','n','o',' ','r','e','n','d','e','r','e','r',')','.','.','.','.','.','.','.','.','.','.'},
    {24,   'T','e','x','t','u','r','e','S','t','r','e','a','m','e','r',' ','d','i','s','a','b','l','e','d','.','.','.','.','.','.','.'},
    {19,   'N','o',' ','m','i','p','m','a','p',' ','g','e','n','e','r','a','t','e','d','.','.','.','.','.','.','.','.','.','.','.','.'},
    {21,   'S','t','r','e','a','m','i','n','g',' ','n','o','t',' ','e','n','a','b','l','e','d','.','.','.','.','.','.','.','.','.','.'},
    {19,   'C','a','n','n','o','t',' ','s','t','r','e','a','m',' ','a','s','y','n','c','.','.','.','.','.','.','.','.','.','.','.','.'},
    { 7,   'T','e','r','r','a','i','n','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},

    {23,   'N','o',' ','t','e','x','t','u','r','e','s',' ','o','n',' ','m','a','t','e','r','i','a','l','.','.','.','.','.','.','.','.'},
    {15,   'I','s','s','u','e','s',' ','d','e','t','e','c','t','e','d','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {13,   'A','l','l',' ','s','t','r','e','a','m','i','n','g','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {29,   'A','l','l',' ','s','t','r','e','a','m','i','n','g',' ','(','s','o','m','e',' ','m','a','n','u','a','l','l','y',')','.','.'},
    {21,   'N','o',' ','t','e','x','t','u','r','e','s',' ','s','t','r','e','a','m','i','n','g','.','.','.','.','.','.','.','.','.','.'},
    {14,   'S','o','m','e',' ','s','t','r','e','a','m','i','n','g','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {30,   'S','o','m','e',' ','s','t','r','e','a','m','i','n','g',' ','(','s','o','m','e',' ','m','a','n','u','a','l','l','y',')','.'},

    // MipCount
    {16,   'I','n','v','a','l','i','d',' ','m','i','p','C','o','u','n','t','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {26,   'M','o','r','e',' ','t','h','a','n',' ','1','4',' ','m','i','p','s',' ','u','p','l','o','a','d','e','d','.','.','.','.','.'},

    // Ratio
    {18,   'H','i','g','h',' ','p','i','x','e','l',' ','d','e','n','s','i','t','y','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {17,   'L','o','w',' ','p','i','x','e','l',' ','d','e','n','s','i','t','y','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},

    // Priorities
    {10,   'L','o','w',' ','(','-','1','2','8',')','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {10,   'H','i','g','h',' ','(','1','2','7',')','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},

    // Performance
    {10,   'M','i','p','s',' ','s','a','v','e','d','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {31,   'M','i','p','s',' ','s','a','v','e','d',' ','(','s','o','m','e',' ','c','a','c','h','e','d',' ','o','n',' ','G','P','U',')'},
    {30,   'N','o',' ','s','a','v','i','n','g','s',' ','(','a','l','l',' ','m','i','p','s',' ','r','e','q','u','i','r','e','d',')','.'},
    {30,   'N','o','t',' ','a','l','l',' ','r','e','q','u','i','r','e','d',' ','m','i','p','s',' ','u','p','l','o','a','d','e','d','.'},

    // RecentlyUpdated
    {13,   'J','u','s','t',' ','s','t','r','e','a','m','e','d','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'},
    {21,   'N','o','t',' ','r','e','c','e','n','t','l','y',' ','s','t','r','e','a','m','e','d','.','.','.','.','.','.','.','.','.','.'},
};

void DrawString(int stringIdx, uint2 unormCoord, float3 textColor, uint2 textLocation, bool alignRight, inout float3 outputColor)
{
    const uint stringSize = kMipmapDebugLegendStrings[stringIdx][0];
    const int direction = alignRight ? -1 : 1;
    uint i = alignRight ? stringSize : 1;
    
    [fastopt] for (; alignRight ? i > 0 : i <= stringSize; i += direction)
        DrawCharacter(kMipmapDebugLegendStrings[stringIdx][i], textColor, unormCoord, textLocation, outputColor.rgb, direction);
}

void DrawString(int stringIdx, uint2 unormCoord, float3 textColor, uint2 textLocation, inout float3 outputColor)
{
    DrawString(stringIdx, unormCoord, textColor, textLocation, false, outputColor);
}

// mipInfo :
// x = quality settings maxLevelReduction
// y = original mip count for texture (if it has not been set, it's not a streamed texture)
// z = desired on screen mip level
// w = 0
uint GetMaxLevelReduction(float4 mipInfo) { return max(1, uint(mipInfo.x)); } // Always has a minimum value of 1.
uint GetTextureAssetMipCount(float4 mipInfo) { return uint(mipInfo.y); }
uint GetDesiredMipLevel(float4 mipInfo) { return uint(mipInfo.z); }

// Mipmap Debug Status Codes found in StreamInfo.z (Per-Texture)
#define kMipmapDebugStatusCodeNotSet 0                  // No status code has been set by the streamer
#define kMipmapDebugStatusCodeStreamerDisabled 1        // Not streaming: streamer disabled
#define kMipmapDebugStatusCodeNoTexture 2               // Nothing there, empty slot
#define kMipmapDebugStatusCodeNoMipMap 3                // Not streaming: no mips
#define kMipmapDebugStatusCodeNotSetToStream 4          // Not streaming: streaming not enabled for this texture
#define kMipmapDebugStatusCodeNoAsync 5                 // Not streaming: cannot asyncStream
#define kMipmapDebugStatusCodeTerrain 6                 // Not streaming: terrain
#define kMipmapDebugStatusCodeInvalidStreamingIndex 7   // Not streaming: invalid streaming index (issue at Unity side?)
#define kMipmapDebugStatusCodeManuallyRequested 8       // Streaming manually through script (RequestedMipmapLevel)

// Mipmap Debug Status Code Flags found in StreamInfo.z (Per-Material)
#define kMipmapDebugStatusCodeFlagNoTexturesSet 0           // No textures have been set on the material, all slots are empty
#define kMipmapDebugStatusCodeFlagHasStreamingTextures 1    // 1 or more textures assigned to the material are streaming
#define kMipmapDebugStatusCodeFlagHasNonStreamingTextures 2 // 1 or more textures assigned to the material aren't streaming
#define kMipmapDebugStatusCodeFlagHasTexturesWithIssues 4   // 1 or more textures assigned to the material have issues preventing them from streaming
#define kMipmapDebugStatusCodeFlagHasManualRequests 8       // 1 or more textures assigned to the material are streaming manually through script (RequestedMipmapLevel)

// streamInfo :
// x = streaming priority
// y = time stamp of the latest texture upload
// z = streaming status
// w = 0
int GetStreamingPriority(float4 streamInfo) { return int(streamInfo.x);}
float GetUpdateTimestamp(float4 streamInfo) { return streamInfo.y;}
bool IsStreaming(float4 streamInfo) { return streamInfo.z < 0 || (int)streamInfo.z == kMipmapDebugStatusCodeManuallyRequested; } // if manually set, that's also streaming
int GetStatusCode(float4 streamInfo, bool perMaterial) { return perMaterial ? (int)streamInfo.z >> 4 : (int)streamInfo.z & 0xF;}  // 0-15 are reserved for per-texture codes

#define kMipmapDebugLowPixelDensity float3(0.049, 0.32, 0.751)
#define kMipmapDebugHighPixelDensity float3(0.982, 0.32, 0)

float3 GetDebugMipRatioColor(float value)
{
    if (value > 0.5)
        return lerp(float3(0.5, 0.5, 0.5), kMipmapDebugHighPixelDensity, 2 * value - 1);
    else
        return lerp(kMipmapDebugLowPixelDensity, float3(0.5, 0.5, 0.5), 2 * value);
}

float3 GetMipLevelColor(float2 uv, float4 texelSize)
{
    // Push down into colors list to "optimal level" in following table.
    // .zw is texture width,height so *2 is down one mip, *4 is down two mips
    texelSize.zw *= 4.0;

    float mipLevel = ComputeTextureLOD(uv, texelSize.zw);
    mipLevel = clamp(mipLevel, 0.0, 5.0 - 0.0001);

    return GetDebugMipRatioColor(mipLevel / 5.0);
}

float3 GetDebugMipColor(float3 originalColor, float4 texelSize, float2 uv)
{
    // https://aras-p.info/blog/2011/05/03/a-way-to-visualize-mip-levels/
    return GetMipLevelColor(uv, texelSize);
}

#define kMipmapDebugTooManyMips float3(0.194, 0.007, 0.034)
#define kMipmapDebugInvalidMipCount float3(0.716, 0.066, 0.9)

float3 GetDebugMipCountColor(uint mipCount, out bool needsHatching)
{
    needsHatching = false;

    if (mipCount == 0 || mipCount > 14)
    {
        needsHatching = true;
        return mipCount == 0 ? kMipmapDebugInvalidMipCount : kMipmapDebugTooManyMips;
    }

    const float3 colors[14] = {
        float3(0.349, 0.782, 0.965),
        float3(0.188, 0.933, 0.847),
        float3(0.034, 0.9, 0.442),
        float3(0.027, 0.878, 0.035),
        float3(0.4, 0.858, 0.023),
        float3(0.694, 0.929, 0.047),
        float3(1.0, 0.982, 0.072),
        float3(0.996, 0.843, 0.039),
        float3(0.991, 0.687, 0.004),
        float3(0.988, 0.510, 0.004),
        float3(0.982, 0.320, 0.0),
        float3(0.992, 0.184, 0.016),
        float3(1.0, 0.051, 0.029),
        float3(1.0, 0.043, 0.180)
    };

    return colors[mipCount - 1];
}

float3 GetDebugMipCountHatchingColor(uint mipCount)
{
    if (mipCount == 0 || mipCount > 14)
        return float3(0.9, 0.9, 0.9);
    else
        return float3(0.1, 0.1, 0.1);
}


#define kMipmapDebugBudgetSavingMips float3(0.036, 0.9, 0.442)
#define kMipmapDebugBudgetMissing float3(1.0, 0.053, 0.029)
#define kMipmapDebugBudgetFullResolution float3(0.5, 0.5, 0.5)
#define kMipMapDebugStatusColorNotStreaming float3(0.0, 0.007, 0.731)

float3 GetDebugStreamingMipColor(uint mipCount, float4 mipInfo, float4 streamInfo, out bool needsHatching)
{
    needsHatching = false;

    if (!IsStreaming(streamInfo))
        return kMipMapDebugStatusColorNotStreaming;

    if (mipCount == 0)
        return float3(1.0, 0.0, 1.0); // Magenta if mip count invalid

    const uint originalTextureMipCount = GetTextureAssetMipCount(mipInfo);
    const uint mipCountDesired = uint(originalTextureMipCount)-GetDesiredMipLevel(mipInfo);
    const uint maxLevelReduction = GetMaxLevelReduction(mipInfo);

    const float3 colorNeutral = float3(0.5, 0.5, 0.5);
    if (mipCount < mipCountDesired)
    {
        const int missingMips = mipCountDesired - mipCount;
        const float missingRatio = float(missingMips) / float(maxLevelReduction);

        // red tones when not at the desired mip level (reduction due to budget). Full red means no more mips were allowed to be discarded
        return lerp(colorNeutral, kMipmapDebugBudgetMissing, missingRatio);
    }
    else if (mipCountDesired < originalTextureMipCount)
    {
        const int savedMips = originalTextureMipCount - mipCountDesired;
        const float savedRatio = float(savedMips) / float(maxLevelReduction);

        // green tones when we require less mips than the original texture. Full green means we've dropped as many mips as allowed
        if (mipCount > mipCountDesired) // some mips were cached
        {
            needsHatching = true;
            return lerp(colorNeutral, kMipmapDebugBudgetSavingMips, savedRatio);
        }
        else
            return lerp(colorNeutral, kMipmapDebugBudgetSavingMips, savedRatio);
    }
    else // so, (mipCount >= originalTextureMipCount)
    {
        return kMipmapDebugBudgetFullResolution;
    }
}

#define kMipMapDebugStatusColorStreaming float3(0.036, 0.9, 0.442)
#define kMipMapDebugStatusColorUnknown float3(0.349, 0.782, 0.965)
#define kMipMapDebugStatusColorNoTexture float3(0.982, 0.320, 0)
#define kMipMapDebugStatusColorWarning float3(1.0, 0.982, 0.072)

float3 GetDebugStreamingStatusColor(float4 streamInfo, out bool needsHatching)
{
    needsHatching = false;

    if (IsStreaming(streamInfo))
    {
        if (GetStatusCode(streamInfo, false) == kMipmapDebugStatusCodeManuallyRequested)
            needsHatching = true;
        return kMipMapDebugStatusColorStreaming;
    }

    int statusCode = GetStatusCode(streamInfo, false);
    switch(statusCode)
    {
        case kMipmapDebugStatusCodeNoTexture:
            return kMipMapDebugStatusColorNoTexture; 
        case kMipmapDebugStatusCodeStreamerDisabled:
        case kMipmapDebugStatusCodeNoMipMap:
        case kMipmapDebugStatusCodeNotSetToStream:
            return kMipMapDebugStatusColorNotStreaming;
        case kMipmapDebugStatusCodeNoAsync:
        case kMipmapDebugStatusCodeTerrain:
            return kMipMapDebugStatusColorWarning; 
        case kMipmapDebugStatusCodeInvalidStreamingIndex:
            return float3(1.0, 0.0, 1.0);
        case kMipmapDebugStatusCodeNotSet:
        default:
            return kMipMapDebugStatusColorUnknown;
    }
}

#define kMipMapDebugMaterialStatusColorSomeStreaming float3(0.018, 0.454, 0.587)

float3 GetDebugPerMaterialStreamingStatusColor(float4 streamInfo, out bool needsHatching)
{
    needsHatching = false;

    int statusCode = GetStatusCode(streamInfo, true);
    if (statusCode == kMipmapDebugStatusCodeFlagNoTexturesSet)
        return kMipMapDebugStatusColorNoTexture;

    const bool hasStreamingTextures = (statusCode & kMipmapDebugStatusCodeFlagHasStreamingTextures) != 0;
    const bool hasNonStreamingTextures = (statusCode & kMipmapDebugStatusCodeFlagHasNonStreamingTextures) != 0;
    const bool hasTexturesWithIssues = (statusCode & kMipmapDebugStatusCodeFlagHasTexturesWithIssues) != 0;
    const bool hasManualRequests = (statusCode & kMipmapDebugStatusCodeFlagHasManualRequests) != 0;

    if(hasTexturesWithIssues)
    {
        return kMipMapDebugStatusColorWarning;
    }

    // at this point, there are no issues to report
    if(hasStreamingTextures && !hasNonStreamingTextures)
    {
        needsHatching = hasManualRequests;
        return kMipMapDebugStatusColorStreaming;
    }
    else if(hasNonStreamingTextures && !hasStreamingTextures)
    {
        return kMipMapDebugStatusColorNotStreaming;
    }
    else
    {
        // mix of streaming and non-streaming
        needsHatching = hasManualRequests;
        return kMipMapDebugMaterialStatusColorSomeStreaming;
    }
}

float3 GetDebugMipPriorityColor(float value)
{
    const float3 kMipMapDebugLowPriorityColor = float3(0.982, 0.32, 0);
    const float3 kMipMapDebugHighPriorityColor = float3(0.4, 0.858, 0.023);

    if(value < 0.5)
        return lerp(kMipMapDebugLowPriorityColor, float3(0.5, 0.5, 0.5), 2 * value);
    else
        return lerp(float3(0.5, 0.5, 0.5), kMipMapDebugHighPriorityColor, 2 * (value - 0.5));
}

float3 GetDebugStreamingPriorityColor(float4 streamInfo)
{
    if (!IsStreaming(streamInfo))
        return kMipMapDebugStatusColorNotStreaming;

    const int textureStreamingPriority = GetStreamingPriority(streamInfo);
    const float priorityValue = (textureStreamingPriority + 128) / 255.0;
    return GetDebugMipPriorityColor(priorityValue);
}

float3 GetDebugMipRecentlyUpdatedColor(float value)
{
    const float3 kRecentlyUpdatedColor = float3(0.194, 0.007, 0.034);
    return lerp(kRecentlyUpdatedColor, float3(0.766, 0.766, 0.766), value);
}

float3 GetDebugStreamingRecentlyUpdatedColor(float currentTime, float cooldownTime, bool perMaterial, float4 streamInfo)
{
    if (!perMaterial && !IsStreaming(streamInfo))
        return kMipMapDebugStatusColorNotStreaming;

    if (perMaterial)
    {
        // The other per-material status codes don't really matter here (users visualize these in the status view).
        // Even if there are slots with issues, as long as there is one slot with a streaming texture, we can visualize
        // recent updates here.
        int statusCode = GetStatusCode(streamInfo, true);
        const bool hasStreamingTextures = (statusCode & kMipmapDebugStatusCodeFlagHasStreamingTextures) != 0;
        if (!hasStreamingTextures)
            return kMipMapDebugStatusColorNotStreaming; // nothing there, all slots are empty
    }

    const float timeSinceUpdate = currentTime - GetUpdateTimestamp(streamInfo);
    const float ratio = clamp(timeSinceUpdate / cooldownTime, 0.0, 1.0);
    return GetDebugMipRecentlyUpdatedColor(ratio);
}

float3 GetDebugMipColorIncludingMipReduction(float3 originalColor, uint mipCount, float4 texelSize, float2 uv, float4 mipInfo)
{
    const uint originalTextureMipCount = GetTextureAssetMipCount(mipInfo);
    if (originalTextureMipCount != 0)
    {
        // Mip count has been reduced but the texelSize was not updated to take that into account
        const uint mipReductionLevel = originalTextureMipCount - mipCount;
        const uint mipReductionFactor = 1U << mipReductionLevel;
        if (mipReductionFactor)
        {
            const float oneOverMipReductionFactor = 1.0 / mipReductionFactor;
            // texelSize.xy *= mipReductionRatio;   // Unused in GetDebugMipColor so lets not re-calculate it
            texelSize.zw *= oneOverMipReductionFactor;
        }
    }
    return GetDebugMipColor(originalColor, texelSize, uv);
}

void HatchColor(uint2 unormCoord, real3 hatchingColor, inout real3 color)
{
    const uint spacing = 8;
    const uint thickness = 3;
    if((unormCoord.x + unormCoord.y) % spacing < thickness)
        color = hatchingColor;
}

void HatchColor(uint2 unormCoord, inout real3 color)
{
    HatchColor(unormCoord, real3(0.1, 0.1, 0.1), color);
}

// Legend configuration
static const float _kLegendBarPaddingHorizontal = 20.0;
static const float _kLegendBarPaddingBottom = 20.0;
static const float _kLegendBarHeight = 20.0;
static const float _kLegendBarBorderThickness = 4.0;

static const float _kLegendPaddingBottom = 5.0;
static const float _kLegendMargin = 5.0;
static const float _kLegendEntryBlockSize = 15.0;
static const float _kLegendEntryBlockBorderSize = 1.0;
static const float _kLegendBorderSize = 2.0;
static const float _kLegendPaddingTextRight = 10.0;

void DrawLegendEntry(uint2 unormCoord, uint stringIndex, int code, float3 entryColor, bool hatched, real3 hatchingColor, inout uint2 pos, inout real3 outputColor)
{
    const float3 borderColor = float3(1,1,1);
    const float3 lightTextColor = float3(0.90, 0.90, 0.90);
    const float3 darkTextColor = float3(0.10, 0.10, 0.10);

    uint2 blockBottom = pos;
    uint2 blockTop = blockBottom + uint2(_kLegendEntryBlockSize, _kLegendEntryBlockSize);

    if (all(unormCoord > blockBottom) && all(unormCoord < blockTop))
    {
        if (all(unormCoord > blockBottom + uint(_kLegendEntryBlockBorderSize)) && all(unormCoord < blockTop - uint(_kLegendEntryBlockBorderSize)))
        {
            outputColor = entryColor;

            if (hatched)
                HatchColor(unormCoord, hatchingColor, outputColor);

            if (code != -1)
            {
                bool invertColor = (code == kMipmapDebugStatusCodeNotSet || code == kMipmapDebugStatusCodeNoTexture || code == kMipmapDebugStatusCodeNoAsync || code == kMipmapDebugStatusCodeTerrain);

                // draw "bold"
                UNITY_LOOP for (uint i = 0; i < 4; ++i)
                {
                    const bool isFont = SampleDebugFontNumber2Digits(unormCoord - pos + uint2(i % 2, i / 2), code);
                    UNITY_FLATTEN if (isFont)
                    {
                        outputColor = invertColor ? darkTextColor : lightTextColor;
                        break;
                    }
                }
            }
        }
        else
            outputColor = borderColor;
    }

    uint2 textLocation = pos + uint2(_kLegendEntryBlockSize + _kLegendPaddingTextRight, 0);
    DrawString(stringIndex, unormCoord, lightTextColor, textLocation, outputColor);

    pos.y -= _kLegendEntryBlockSize + _kLegendMargin;
}

// Drawing a legend entry with an status code (no hatching)
void DrawLegendEntry(uint2 unormCoord, uint stringIndex, int code, float3 entryColor, inout uint2 pos, inout real3 outputColor)
{
    DrawLegendEntry(unormCoord, stringIndex, code, entryColor, false, real3(0.1, 0.1, 0.1), pos, outputColor);
}

// Drawing a legend entry with just a color (no status code)
void DrawLegendEntry(uint2 unormCoord, uint stringIndex, float3 entryColor, real3 hatchingColor, inout uint2 pos, inout real3 outputColor)
{
    DrawLegendEntry(unormCoord, stringIndex, -1, entryColor, true, hatchingColor, pos, outputColor);
}

// Drawing a legend entry with just a color (no status code)
void DrawLegendEntry(uint2 unormCoord, uint stringIndex, float3 entryColor, bool hatched, inout uint2 pos, inout real3 outputColor)
{
    DrawLegendEntry(unormCoord, stringIndex, -1, entryColor, hatched, real3(0.1, 0.1, 0.1), pos, outputColor);
}

// Drawing a legend entry without hatching or status code
void DrawLegendEntry(uint2 unormCoord, uint stringIndex, float3 entryColor, inout uint2 pos, inout real3 outputColor)
{
    DrawLegendEntry(unormCoord, stringIndex, -1, entryColor, false, real3(0.1, 0.1, 0.1), pos, outputColor);
}

void DrawLegendBackground(float2 texCoord, float4 screenSize, int numEntries, inout real3 color, out uint2 initialEntryPos)
{
    const int largestStringSize = 31;
    const int requiredWidth = largestStringSize * DEBUG_FONT_TEXT_SCALE_WIDTH + _kLegendPaddingTextRight + _kLegendEntryBlockSize + 2 * _kLegendMargin;
    const int requiredHeight = numEntries * (_kLegendEntryBlockSize + _kLegendMargin) + _kLegendMargin;

    // Screen space (fixed x, fixed y, rel x, rel y)
    const int heightOfLegendBar = _kLegendBarPaddingBottom + _kLegendBarHeight + _kLegendBarBorderThickness;
    const float4 legendPosition = float4(screenSize.x - requiredWidth - _kLegendBarPaddingHorizontal, heightOfLegendBar + _kLegendPaddingBottom, 0, 0);
    const float4 legendSize = float4(requiredWidth, requiredHeight, 0, 0);
    const float4 legendBorderThickness = float4(_kLegendBorderSize, _kLegendBorderSize, 0, 0);
    const float4 legendWithBorderPosition = legendPosition - legendBorderThickness;
    const float4 legendWithBorderSize = legendSize + 2 * legendBorderThickness;

    // Screen UV space
    const float2 legendPositionUV = legendPosition.xy * screenSize.zw + legendPosition.zw;
    const float2 legendSizeUV = legendSize.xy * screenSize.zw + legendSize.zw;
    const float2 legendWithBorderPositionUV = legendWithBorderPosition.xy * screenSize.zw + legendWithBorderPosition.zw;
    const float2 legendWithBorderSizeUV = legendWithBorderSize.xy * screenSize.zw + legendWithBorderSize.zw;

    // Legend (with border) space
    const float2 legendBorderCoord = (texCoord - legendWithBorderPositionUV) / legendWithBorderSizeUV;
    const float2 legendCoord =  (texCoord - legendPositionUV) / legendSizeUV;

    // Draw Legend border
    if (all(legendBorderCoord >= 0) && all(legendBorderCoord <= 1))
        color = real3(0.1, 0.1, 0.1);

    // Draw Legend background
    if (all(legendCoord >= 0) && all(legendCoord <= 1))
        color = real3(0.022, 0.022, 0.022);

    initialEntryPos = uint2(legendPosition.xy) + uint2(_kLegendMargin, requiredHeight - _kLegendMargin - _kLegendEntryBlockSize);
}

void DrawLegendBar(float2 texCoord, float4 screenSize, inout real3 color)
{
    // Screen space (fixed x, fixed y, rel x, rel y)
    const float4 legendBarPosition = float4(_kLegendBarPaddingHorizontal, _kLegendBarPaddingBottom, 0, 0);
    const float4 legendBarSize = float4(-_kLegendBarPaddingHorizontal * 2, _kLegendBarHeight, 1, 0);
    const float4 legendBarBorderThickness = float4(_kLegendBarBorderThickness, _kLegendBarBorderThickness, 0, 0);
    const float4 legendBarWithBorderPosition = legendBarPosition - legendBarBorderThickness;
    const float4 legendBarWithBorderSize = legendBarSize + 2 * legendBarBorderThickness;

    // Screen UV space
    const float2 legendBarWithBorderPositionUV = legendBarWithBorderPosition.xy * screenSize.zw + legendBarWithBorderPosition.zw;
    const float2 legendBarWithBorderSizeUV = legendBarWithBorderSize.xy * screenSize.zw + legendBarWithBorderSize.zw;

    // Legend bar (with border) space
    const float2 legendBarBorderCoord = (texCoord - legendBarWithBorderPositionUV) / legendBarWithBorderSizeUV;

    // Draw legend bar (still to be filled later)
    if (all(legendBarBorderCoord >= 0) && all(legendBarBorderCoord <= 1))
        color = real3(0.1, 0.1, 0.1);
}

bool InsideLegendBar(float2 texCoord, float4 screenSize, out float xCoord)
{
    // Screen space (fixed x, fixed y, rel x, rel y)
    const float4 legendBarPosition = float4(_kLegendBarPaddingHorizontal, _kLegendBarPaddingBottom, 0, 0);
    const float4 legendBarSize = float4(-_kLegendBarPaddingHorizontal * 2, _kLegendBarHeight, 1, 0);

    // Screen UV space
    const float2 legendBarPositionUV = legendBarPosition.xy * screenSize.zw + legendBarPosition.zw;
    const float2 legendBarSizeUV = legendBarSize.xy * screenSize.zw + legendBarSize.zw;

    // Legend bar space
    const float2 legendBarCoord = (texCoord - legendBarPositionUV) / legendBarSizeUV;

    // Check if inside the legend bar (and fill in the "legend bar space" horizontal coordinate)
    xCoord = legendBarCoord.x;
    return all(legendBarCoord >= 0) && all(legendBarCoord <= 1);
}

void DrawLabelBarBackground(float2 texCoord, float4 screenSize, inout real3 color)
{
    // Screen space (fixed x, fixed y, rel x, rel y)
    const float4 labelBarPosition = float4(_kLegendBarPaddingHorizontal, 0, 0, 0);
    const float4 labelBarSize = float4(-_kLegendBarPaddingHorizontal * 2, _kLegendBarPaddingBottom - _kLegendBarBorderThickness, 1, 0);

    // Screen UV space
    const float2 labelBarPositionUV = labelBarPosition.xy * screenSize.zw + labelBarPosition.zw;
    const float2 labelBarSizeUV = labelBarSize.xy * screenSize.zw + labelBarSize.zw;

    // Label bar space
    const float2 labelBarCoord =  (texCoord - labelBarPositionUV) / labelBarSizeUV;

    // Draw label bar background
    if (all(labelBarCoord >= 0) && all(labelBarCoord <= 1))
        color = real3(0.022, 0.022, 0.022);
}

void DrawTwoValueLabelBar(uint2 unormCoord, float4 screenSize, uint leftStringId, uint rightStringId, inout real3 color)
{
    const float3 textColor = float3(1,1,1);

    // Draw left and right labels
    const uint2 startPosition = uint2(_kLegendBarPaddingHorizontal, 0);
    const uint2 endPosition = uint2(screenSize.x - _kLegendBarPaddingHorizontal - DEBUG_FONT_TEXT_SCALE_WIDTH, 0);

    DrawString(leftStringId, unormCoord, textColor, startPosition, color);
    DrawString(rightStringId, unormCoord, textColor, endPosition, true, color);
}

void DrawUniformlySpreadValues(uint2 unormCoord, float4 screenSize, uint numValues, uint startValue, inout real3 color)
{
    const float bucketWidth = (screenSize.x - 2 * _kLegendBarPaddingHorizontal) / numValues;

    for (uint i = 0; i < numValues; ++i)
    {
        const uint bucketLabel = uint(i + startValue);
        const uint labelOffset = bucketLabel < 10 ? DEBUG_FONT_TEXT_SCALE_WIDTH / 2 : DEBUG_FONT_TEXT_SCALE_WIDTH;
        const uint2 labelStartCoord = uint2(_kLegendBarPaddingHorizontal + i * bucketWidth + bucketWidth / 2 - labelOffset, 0);

        const uint2 pixCoord = unormCoord - labelStartCoord;
        if (SampleDebugFontNumberAllDigits(pixCoord, bucketLabel))
            color = real3(1, 1, 1);
    }
}


// Legend drawing functions
// ------------------------

void DrawMipCountLegend(float2 texCoord, float4 screenSize, inout real3 color)
{
    const uint2 unormCoord = texCoord * screenSize.xy;
    const real maxMipCount = 14;

    // Draw legend
    uint2 pos;
    DrawLegendBackground(texCoord, screenSize, 2, color, pos);
    DrawLegendEntry(unormCoord, _kNoMipCountIndex, kMipmapDebugInvalidMipCount, float3(0.9, 0.9, 0.9), pos, color);
    DrawLegendEntry(unormCoord, _kTooManyMipsIndex, kMipmapDebugTooManyMips, float3(0.9, 0.9, 0.9), pos, color);

    // Draw legend bar
    DrawLegendBar(texCoord, screenSize, color);

    float xCoord;
    if (InsideLegendBar(texCoord, screenSize, xCoord))
    {
        // Compute bucket index
        const int bucket = ceil(xCoord * maxMipCount);
        bool needsHatching;
        color = GetDebugMipCountColor(bucket, needsHatching);
        if (needsHatching)
            // no need to get the hatching color (to keep in sync with rendering), it's always dark anyway inside the legend bar
            HatchColor(unormCoord, color);
    }

    DrawLabelBarBackground(texCoord, screenSize, color);
    DrawUniformlySpreadValues(unormCoord, screenSize, maxMipCount, 1, color);
}

void DrawMipRatioLegend(float2 texCoord, float4 screenSize, inout real3 color)
{
    const uint2 unormCoord = texCoord * screenSize.xy;

    // Draw legend bar
    DrawLegendBar(texCoord, screenSize, color);

    float xCoord;
    if (InsideLegendBar(texCoord, screenSize, xCoord))
        color = GetDebugMipRatioColor(xCoord);

    // Text labels
    DrawLabelBarBackground(texCoord, screenSize, color);
    DrawTwoValueLabelBar(unormCoord, screenSize, _kLowPixelDensityIndex, _kHighPixelDensityIndex, color);
}

void DrawMipPriorityLegend(float2 texCoord, float4 screenSize, inout real3 color)
{
    const uint2 unormCoord = texCoord * screenSize.xy;

    // Draw the legend
    uint2 pos;
    DrawLegendBackground(texCoord, screenSize, 1, color, pos);
    DrawLegendEntry(unormCoord, _kNotStreamingIndex, kMipMapDebugStatusColorNotStreaming, pos, color);

    // Draw legend bar
    DrawLegendBar(texCoord, screenSize, color);

    float xCoord;
    if (InsideLegendBar(texCoord, screenSize, xCoord))
        color = GetDebugMipPriorityColor(xCoord);

    // Text labels
    DrawLabelBarBackground(texCoord, screenSize, color);
    DrawTwoValueLabelBar(unormCoord, screenSize, _kLowPriorityIndex, _kHighPriorityIndex, color);
}

void DrawMipRecentlyUpdatedLegend(float2 texCoord, float4 screenSize, bool perMaterial, inout real3 color)
{
    const uint2 unormCoord = texCoord * screenSize.xy;

    // Draw the legend
    uint2 pos;
    DrawLegendBackground(texCoord, screenSize, 1, color, pos);
    if(perMaterial)
        DrawLegendEntry(unormCoord, _kStatusNoTexturesAreStreaming, kMipMapDebugStatusColorNotStreaming, pos, color);
    else
        DrawLegendEntry(unormCoord, _kNotStreamingIndex, kMipMapDebugStatusColorNotStreaming, pos, color);

    // Draw legend bar
    DrawLegendBar(texCoord, screenSize, color);

    float xCoord;
    if (InsideLegendBar(texCoord, screenSize, xCoord))
        color = GetDebugMipRecentlyUpdatedColor(xCoord);

    // Text labels
    DrawLabelBarBackground(texCoord, screenSize, color);
    DrawTwoValueLabelBar(unormCoord, screenSize, _kRecentlyUpdated, _kNotRecentlyUpdated, color);
}

void DrawMipStreamingStatusLegend(float2 texCoord, float4 screenSize, bool needsStatusCodes, inout real3 color)
{
    const uint2 unormCoord = texCoord * screenSize.xy;

    // Draw the legend
    uint2 pos;
    const int numEntries = needsStatusCodes ? 11 : 6;
    DrawLegendBackground(texCoord, screenSize, numEntries, color, pos);

    DrawLegendEntry(unormCoord, _kStatusNoTextureIndex, kMipMapDebugStatusColorNoTexture, pos, color);
    DrawLegendEntry(unormCoord, _kNotStreamingIndex, kMipMapDebugStatusColorNotStreaming, pos, color);
    if (needsStatusCodes)
    {
        DrawLegendEntry(unormCoord, _kStatusStreamerDisabledIndex, kMipmapDebugStatusCodeStreamerDisabled, kMipMapDebugStatusColorNotStreaming, pos, color);
        DrawLegendEntry(unormCoord, _kStatusMessageNoMipMapIndex, kMipmapDebugStatusCodeNoMipMap, kMipMapDebugStatusColorNotStreaming, pos, color);
        DrawLegendEntry(unormCoord, _kStatusMessageNotSetToStreamIndex, kMipmapDebugStatusCodeNotSetToStream, kMipMapDebugStatusColorNotStreaming, pos, color);
    }
    DrawLegendEntry(unormCoord, _kStreamingIndex, kMipMapDebugStatusColorStreaming, pos, color);
    DrawLegendEntry(unormCoord, _kStreamingManuallyIndex, kMipMapDebugStatusColorStreaming, true, pos, color);

    DrawLegendEntry(unormCoord, _kStatusWarningIndex, kMipMapDebugStatusColorWarning, pos, color);
    if (needsStatusCodes)
    {
        DrawLegendEntry(unormCoord, _kStatusMessageNoAsyncIndex, kMipmapDebugStatusCodeNoAsync, kMipMapDebugStatusColorWarning, pos, color);
        DrawLegendEntry(unormCoord, _kStatusMessageTerrainIndex, kMipmapDebugStatusCodeTerrain, kMipMapDebugStatusColorWarning, pos, color);
    }

    DrawLegendEntry(unormCoord, _kStatusUnknownIndex, kMipMapDebugStatusColorUnknown, pos, color);
}

void DrawMipStreamingStatusPerMaterialLegend(float2 texCoord, float4 screenSize, inout real3 color)
{
    const uint2 unormCoord = texCoord * screenSize.xy;

    // Draw the legend
    uint2 pos;
    DrawLegendBackground(texCoord, screenSize, 7, color, pos);

    DrawLegendEntry(unormCoord, _kStatusNoTexturesIndex, kMipMapDebugStatusColorNoTexture, pos, color);
    DrawLegendEntry(unormCoord, _kStatusNoTexturesAreStreaming, kMipMapDebugStatusColorNotStreaming, pos, color);
    DrawLegendEntry(unormCoord, _kStatusSomeTexturesAreStreaming, kMipMapDebugMaterialStatusColorSomeStreaming, pos, color);
    DrawLegendEntry(unormCoord, _kStatusSomeTexturesAreStreamingSomeManually, kMipMapDebugMaterialStatusColorSomeStreaming, true, pos, color);
    DrawLegendEntry(unormCoord, _kStatusAllTexturesAreStreaming, kMipMapDebugStatusColorStreaming, pos, color);
    DrawLegendEntry(unormCoord, _kStatusAllTexturesAreStreamingSomeManually, kMipMapDebugStatusColorStreaming, true, pos, color);
    DrawLegendEntry(unormCoord, _kStatusSomeTexturesHaveIssues, kMipMapDebugStatusColorWarning, pos, color);
}

void DrawTextureStreamingPerformanceLegend(float2 texCoord, float4 screenSize, inout real3 color)
{
    const uint2 unormCoord = texCoord * screenSize.xy;

    // Draw the legend
    uint2 pos;
    DrawLegendBackground(texCoord, screenSize, 5, color, pos);

    DrawLegendEntry(unormCoord, _kNotStreamingIndex, kMipMapDebugStatusColorNotStreaming, pos, color);
    DrawLegendEntry(unormCoord, _kBudgetSavingMips, kMipmapDebugBudgetSavingMips, pos, color);
    DrawLegendEntry(unormCoord, _kBudgetSavingMipsWithCache, kMipmapDebugBudgetSavingMips, true, pos, color);
    DrawLegendEntry(unormCoord, _kBudgetNothingSaved, kMipmapDebugBudgetFullResolution, pos, color);
    DrawLegendEntry(unormCoord, _kBudgetMissingMips, kMipmapDebugBudgetMissing, pos, color);
}

#endif // UNITY_DEBUG_MIPMAP_STREAMING_INCLUDED
