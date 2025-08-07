#ifndef DEBUG_LIGHTING_COMPLEXITY
#define DEBUG_LIGHTING_COMPLEXITY

#define PACK_BITS25(_x0,_x1,_x2,_x3,_x4,_x5,_x6,_x7,_x8,_x9,_x10,_x11,_x12,_x13,_x14,_x15,_x16,_x17,_x18,_x19,_x20,_x21,_x22,_x23,_x24) (_x0|(_x1<<1)|(_x2<<2)|(_x3<<3)|(_x4<<4)|(_x5<<5)|(_x6<<6)|(_x7<<7)|(_x8<<8)|(_x9<<9)|(_x10<<10)|(_x11<<11)|(_x12<<12)|(_x13<<13)|(_x14<<14)|(_x15<<15)|(_x16<<16)|(_x17<<17)|(_x18<<18)|(_x19<<19)|(_x20<<20)|(_x21<<21)|(_x22<<22)|(_x23<<23)|(_x24<<24))
#define _ 0
#define x 1
const static uint debugFontData[9][2] = {
    { PACK_BITS25(_,_,x,_,_,        _,_,x,_,_,      _,x,x,x,_,      x,x,x,x,x,      _,_,_,x,_), PACK_BITS25(x,x,x,x,x,      _,x,x,x,_,      x,x,x,x,x,      _,x,x,x,_,      _,x,x,x,_) },
    { PACK_BITS25(_,x,_,x,_,        _,x,x,_,_,      x,_,_,_,x,      _,_,_,_,x,      _,_,_,x,_), PACK_BITS25(x,_,_,_,_,      x,_,_,_,x,      _,_,_,_,x,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        x,_,x,_,_,      x,_,_,_,x,      _,_,_,x,_,      _,_,x,x,_), PACK_BITS25(x,_,_,_,_,      x,_,_,_,_,      _,_,_,x,_,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,_,_,_,x,      _,_,x,_,_,      _,x,_,x,_), PACK_BITS25(x,_,x,x,_,      x,_,_,_,_,      _,_,_,x,_,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,_,_,x,_,      _,x,x,x,_,      _,x,_,x,_), PACK_BITS25(x,x,_,_,x,      x,x,x,x,_,      _,_,x,_,_,      _,x,x,x,_,      _,x,x,x,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,_,x,_,_,      _,_,_,_,x,      x,_,_,x,_), PACK_BITS25(_,_,_,_,x,      x,_,_,_,x,      _,_,x,_,_,      x,_,_,_,x,      _,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,x,_,_,_,      _,_,_,_,x,      x,x,x,x,x), PACK_BITS25(_,_,_,_,x,      x,_,_,_,x,      _,x,_,_,_,      x,_,_,_,x,      _,_,_,_,x) },
    { PACK_BITS25(_,x,_,x,_,        _,_,x,_,_,      x,_,_,_,_,      x,_,_,_,x,      _,_,_,x,_), PACK_BITS25(x,_,_,_,x,      x,_,_,_,x,      _,x,_,_,_,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(_,_,x,_,_,        x,x,x,x,x,      x,x,x,x,x,      _,x,x,x,_,      _,_,_,x,_), PACK_BITS25(_,x,x,x,_,      _,x,x,x,_,      _,x,_,_,_,      _,x,x,x,_,      _,x,x,x,_) }
};
#undef _
#undef x
#undef PACK_BITS25

bool SampleDebugFontData(int2 pixCoord, uint digit)
{
    if (pixCoord.x < 0 || pixCoord.y < 0 || pixCoord.x >= 5 || pixCoord.y >= 9 || digit > 9)
        return false;

    return (debugFontData[8 - pixCoord.y][digit >= 5] >> ((digit % 5) * 5 + pixCoord.x)) & 1;
}

/*
 * Sample up to 3 digits of a number. (Excluding leading zeroes)
 *
 * Note: Digit have a size of 5x8 pixels and spaced by 1 pixel
 * See SampleDebugFontNumberAllDigits to sample all digits.
 *
 * @param pixCoord: pixel coordinate of the number sample
 * @param number: number to sample
 * @return true when the pixel is a pixel of a digit.
 */
bool SampleDebugFontDataNumber3Digits(int2 pixCoord, uint number)
{
    pixCoord.y -= 4;
    if (number <= 9)
    {
        return SampleDebugFontData(pixCoord - int2(6, 0), number);
    }
    else if (number <= 99)
    {
        return (SampleDebugFontData(pixCoord, (number / 10) % 10) | SampleDebugFontData(pixCoord - int2(6, 0), number % 10));
    }
    else
    {
        return (SampleDebugFontData(pixCoord, (number / 100)) | SampleDebugFontData(pixCoord - int2(4, 0),(number / 10) % 10) | SampleDebugFontData(pixCoord - int2(8, 0),(number / 10) % 10) );
    }
}

#define DEBUG_COLORS_COUNT 12
#define kDebugColorBlack        float4(0.0   / 255.0, 0.0   / 255.0, 0.0   / 255.0, 1.0) // #000000
#define kDebugColorLightPurple  float4(166.0 / 255.0, 70.0  / 255.0, 242.0 / 255.0, 1.0) // #A646F2
#define kDebugColorDeepBlue     float4(0.0   / 255.0, 26.0  / 255.0, 221.0 / 255.0, 1.0) // #001ADD
#define kDebugColorSkyBlue      float4(65.0  / 255.0, 152.0 / 255.0, 224.0 / 255.0, 1.0) // #4198E0
#define kDebugColorLightBlue    float4(158.0 / 255.0, 228.0 / 255.0, 251.0 / 255.0, 1.0) // #1A1D21
#define kDebugColorTeal         float4(56.0  / 255.0, 243.0 / 255.0, 176.0 / 255.0, 1.0) // #38F3B0
#define kDebugColorBrightGreen  float4(168.0 / 255.0, 238.0 / 255.0, 46.0  / 255.0, 1.0) // #A8EE2E
#define kDebugColorBrightYellow float4(255.0 / 255.0, 253.0 / 255.0, 76.0  / 255.0, 1.0) // #FFFD4C
#define kDebugColorDarkYellow   float4(255.0 / 255.0, 214.0 / 255.0, 0.0   / 255.0, 1.0) // #FFD600
#define kDebugColorOrange       float4(253.0 / 255.0, 152.0 / 255.0, 0.0   / 255.0, 1.0) // #FD9800
#define kDebugColorBrightRed    float4(255.0 / 255.0, 67.0  / 255.0, 51.0  / 255.0, 1.0) // #FF4333
#define kDebugColorDarkRed      float4(132.0 / 255.0, 10.0  / 255.0, 54.0  / 255.0, 1.0) // #840A36

// UX-verified colorblind-optimized "heat color gradient"
static const float4 kDebugColorGrad[DEBUG_COLORS_COUNT] = { kDebugColorBlack, kDebugColorLightPurple, kDebugColorDeepBlue,
    kDebugColorSkyBlue, kDebugColorLightBlue, kDebugColorTeal, kDebugColorBrightGreen, kDebugColorBrightYellow,
    kDebugColorDarkYellow, kDebugColorOrange, kDebugColorBrightRed, kDebugColorDarkRed };

float4 HeatMap(uint2 pixCoord, uint2 tileSize, uint n, uint maxN, float opacity)
{
    int colorIndex = 1 + (int)floor(10 * (log2((float)n + 0.1f) / log2(float(maxN))));
    colorIndex = clamp(colorIndex, 0, DEBUG_COLORS_COUNT-1);
    float4 col = kDebugColorGrad[colorIndex];

    int2 coord = (pixCoord & (tileSize - 1)) - int2(tileSize.x/4+1, tileSize.y/3-3);

    float4 color = float4(PositivePow(col.rgb, 2.2), opacity * col.a);
    if (n >= 0)
    {
        if (SampleDebugFontDataNumber3Digits(coord, n))        // Shadow
            color = float4(0, 0, 0, 1);
        if (SampleDebugFontDataNumber3Digits(coord + 1, n))    // Text
            color = float4(1, 1, 1, 1);
    }
    return color;
}

void LightingComplexity_float(float2 normalizedScreenSpaceUV, float3 positionWS, float3 albedo, out float3 Out)
{
#if defined(SHADERGRAPH_PREVIEW)
    Out = float3(0,0,0);
#else
#if USE_CLUSTER_LIGHT_LOOP
    int numLights = URP_FP_DIRECTIONAL_LIGHTS_COUNT;
    uint entityIndex;
    ClusterIterator it = ClusterInit(normalizedScreenSpaceUV, positionWS, 0);
    [loop] while (ClusterNext(it, entityIndex))
    {
        numLights++;
    }
    it = ClusterInit(normalizedScreenSpaceUV, positionWS, 1);
    [loop] while (ClusterNext(it, entityIndex))
    {
        numLights++;
    }
#else
    // Assume a main light and add 1 to the additional lights.
    int numLights = GetAdditionalLightsCount() + 1;
#endif

    const uint2 tileSize = uint2(32,32);
    const uint maxLights = 9;
    const float opacity = 0.8f;

    uint2 pixelCoord = uint2(normalizedScreenSpaceUV * _ScreenParams.xy);
    half3 base = albedo;
    half4 overlay = half4(HeatMap(pixelCoord, tileSize, numLights, maxLights, opacity));

    uint2 tileCoord = (float2)pixelCoord / tileSize;
    uint2 offsetInTile = pixelCoord - tileCoord * tileSize;
    bool border = any(offsetInTile == 0 || offsetInTile == tileSize.x - 1);
    if (border)
        overlay = half4(1, 1, 1, 0.4f);

    Out = half3(lerp(base.rgb, overlay.rgb, overlay.a));
#endif
}

void LightingComplexity_half(half2 normalizedScreenSpaceUV, half3 positionWS, half3 albedo, out half3 Out)
{
#if defined(SHADERGRAPH_PREVIEW)
    Out = float3(0,0,0);
#else
#if USE_CLUSTER_LIGHT_LOOP
    int numLights = URP_FP_DIRECTIONAL_LIGHTS_COUNT;
    uint entityIndex;
    ClusterIterator it = ClusterInit(normalizedScreenSpaceUV, positionWS, 0);
    [loop] while (ClusterNext(it, entityIndex))
    {
        numLights++;
    }
    it = ClusterInit(normalizedScreenSpaceUV, positionWS, 1);
    [loop] while (ClusterNext(it, entityIndex))
    {
        numLights++;
    }
#else
    // Assume a main light and add 1 to the additional lights.
    int numLights = GetAdditionalLightsCount() + 1;
#endif

    const uint2 tileSize = uint2(32,32);
    const uint maxLights = 9;
    const float opacity = 0.8f;

    uint2 pixelCoord = uint2(normalizedScreenSpaceUV * _ScreenParams.xy);
    half3 base = albedo;
    half4 overlay = half4(HeatMap(pixelCoord, tileSize, numLights, maxLights, opacity));

    uint2 tileCoord = (float2)pixelCoord / tileSize;
    uint2 offsetInTile = pixelCoord - tileCoord * tileSize;
    bool border = any(offsetInTile == 0 || offsetInTile == tileSize.x - 1);
    if (border)
        overlay = half4(1, 1, 1, 0.4f);

    Out = half3(lerp(base.rgb, overlay.rgb, overlay.a));
#endif
}

#endif