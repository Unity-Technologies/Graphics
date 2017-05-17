#ifndef __LIGHTINGUTILS_H__
#define __LIGHTINGUTILS_H__


#include "ShaderBase.h"
#include "LightDefinitions.cs.hlsl"


uniform float4x4 g_mViewToWorld;
uniform float4x4 g_mWorldToView;        // used for reflection only
uniform float4x4 g_mScrProjection;
uniform float4x4 g_mInvScrProjection;


uniform uint g_widthRT;
uniform uint g_heightRT;


float3 GetViewPosFromLinDepth(float2 v2ScrPos, float fLinDepth)
{
    float fSx = g_mScrProjection[0].x;
    //float fCx = g_mScrProjection[2].x;
    float fCx = g_mScrProjection[0].z;
    float fSy = g_mScrProjection[1].y;
    //float fCy = g_mScrProjection[2].y;
    float fCy = g_mScrProjection[1].z;

#if USE_LEFTHAND_CAMERASPACE
    return fLinDepth*float3( ((v2ScrPos.x-fCx)/fSx), ((v2ScrPos.y-fCy)/fSy), 1.0 );
#else
    return fLinDepth*float3( -((v2ScrPos.x+fCx)/fSx), -((v2ScrPos.y+fCy)/fSy), 1.0 );
#endif
}

float GetLinearZFromSVPosW(float posW)
{
#if USE_LEFTHAND_CAMERASPACE
    float linZ = posW;
#else
    float linZ = -posW;
#endif

    return linZ;
}

float GetLinearDepth(float zDptBufSpace)    // 0 is near 1 is far
{
    // todo (simplify): m22 is zero and m23 is +1/-1 (depends on left/right hand proj)
    float m22 = g_mInvScrProjection[2].z, m23 = g_mInvScrProjection[2].w;
    float m32 = g_mInvScrProjection[3].z, m33 = g_mInvScrProjection[3].w;

    return (m22*zDptBufSpace+m23) / (m32*zDptBufSpace+m33);

    //float3 vP = float3(0.0f,0.0f,zDptBufSpace);
    //float4 v4Pres = mul(g_mInvScrProjection, float4(vP,1.0));
    //return v4Pres.z / v4Pres.w;
}

bool SampleDebugFont(int2 pixCoord, uint digit)
{
    if (pixCoord.x < 0 || pixCoord.y < 0 || pixCoord.x >= 5 || pixCoord.y >= 9 || digit > 9)
        return false;
#define PACK_BITS25(_x0,_x1,_x2,_x3,_x4,_x5,_x6,_x7,_x8,_x9,_x10,_x11,_x12,_x13,_x14,_x15,_x16,_x17,_x18,_x19,_x20,_x21,_x22,_x23,_x24) (_x0|(_x1<<1)|(_x2<<2)|(_x3<<3)|(_x4<<4)|(_x5<<5)|(_x6<<6)|(_x7<<7)|(_x8<<8)|(_x9<<9)|(_x10<<10)|(_x11<<11)|(_x12<<12)|(_x13<<13)|(_x14<<14)|(_x15<<15)|(_x16<<16)|(_x17<<17)|(_x18<<18)|(_x19<<19)|(_x20<<20)|(_x21<<21)|(_x22<<22)|(_x23<<23)|(_x24<<24))
#define _ 0
#define x 1
    uint fontData[9][2] = {
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
    return (fontData[8 - pixCoord.y][digit >= 5] >> ((digit % 5) * 5 + pixCoord.x)) & 1;
}

bool SampleDebugFontNumber(int2 coord, uint number)
{
    coord.y -= 4;
    if (number <= 9)
    {
        return SampleDebugFont(coord - int2(6, 0), number);

    }
    else
    {
        return (SampleDebugFont(coord, number / 10) | SampleDebugFont(coord - int2(6, 0), number % 10));
    }
}


float3 OverlayHeatMap(uint2 pixCoord, uint numLights, float3 c)
{
    /////////////////////////////////////////////////////////////////////
    //
    const float4 kRadarColors[12] =
    {
        float4(0.0,0.0,0.0,0.0),   // black
        float4(0.0,0.0,0.6,0.5),   // dark blue
        float4(0.0,0.0,0.9,0.5),   // blue
        float4(0.0,0.6,0.9,0.5),   // light blue
        float4(0.0,0.9,0.9,0.5),   // cyan
        float4(0.0,0.9,0.6,0.5),   // blueish green
        float4(0.0,0.9,0.0,0.5),   // green
        float4(0.6,0.9,0.0,0.5),   // yellowish green
        float4(0.9,0.9,0.0,0.5),   // yellow
        float4(0.9,0.6,0.0,0.5),   // orange
        float4(0.9,0.0,0.0,0.5),   // red
        float4(1.0,0.0,0.0,0.9)    // strong red
    };

    float maxNrLightsPerTile = 31;

    int nColorIndex = numLights == 0 ? 0 : (1 + (int)floor(10 * (log2((float)numLights) / log2(maxNrLightsPerTile))));
    nColorIndex = nColorIndex<0 ? 0 : nColorIndex;
    float4 col = nColorIndex>11 ? float4(1.0, 1.0, 1.0, 1.0) : kRadarColors[nColorIndex];

    int2 coord = pixCoord - int2(1, 1);

    float3 color = lerp(c, pow(col.xyz, 2.2), 0.3*col.w);
    if(numLights > 0)
    {
        if (SampleDebugFontNumber(coord, numLights))        // Shadow
            color = 0.0f;
        if (SampleDebugFontNumber(coord + 1, numLights))    // Text
            color = 1.0f;
    }
    return color;
}



#endif
