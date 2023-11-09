/*
MIT License

Copyright (c) 2022 Kleber Garcia

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#ifndef __COVERAGE__
#define __COVERAGE__

//Utilities for coverage bit mask on an 8x8 grid.
namespace Coverage
{

//**************************************************************************************************************/
//                                           How to use
//**************************************************************************************************************/
/*
To utilize this library, first call the genLUT function at the beginning of your compute shader.
This function must be followed by a group sync. Example follows:

...
coverage::genLUT(groupThreadIndex);
GroupMemoryBarrierWithGroupSync();
...

Alternatively, you can dump the contents into buffer. The contents of the LUT are inside gs_quadMask, which is 64 entries.

After this use the coverage functions

*/

//**************************************************************************************************************/
//                                        Coordinate System
//**************************************************************************************************************/
/*
The functions in this library follow the same convension, input is a shape described by certain vertices,
output is a 64 bit mask with such shape's coverage.

The coordinate system is (0,0) for the top left of an 8x8 grid, and (1,1) for the bottom right.
The LSB represents coordinate (0,0), and sample points are centered on the pixel.

(0.0,0.0)                           (1.0,0.0)
    |                                   |
    |___________________________________|
    |   |   |   |   |   |   |   |   |   |
    | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
    |___|___|___|___|___|___|___|___|___|
    |   |   |   |   |   |   |   |   |   |
    | 9 | 10| 11| 12| 13| 14| 15| 16| 17|
    |___|___|___|___|___|___|___|___|___|___(1.0, 2.0/8.0)

 the center of bit 0 would be 0.5,0.5 and so on

any points outside of the range (0,1) means they are outside the grid.
*/

//**************************************************************************************************************/
//                                           Masks
//**************************************************************************************************************/
/*
Masks are stored in a packed 64 bit represented by uint2.
x component represents the first 32 bits, y component the next 32 bits.
*/

//**************************************************************************************************************/
//                                           coverage API
//**************************************************************************************************************/

/*
lut for 4x4 quad mask. See buildQuadMask function
4 states for horizontal flipping and vertical flipping
You can dump this lut to a buffer, and preload it manually,
or just regenerated in your thread group
*/
groupshared uint gs_quadMask[16 * 4];

/*
Call this function to generate the coverage 4x4 luts
groupThreadIndex - the thread index.
NOTE: must sync group threads after calling this.
*/
void GenLUT(uint groupThreadIndex);

/*
Call this function to get a 64 bit coverage mask for a triangle.
v0, v1, v2 - the triangle coordinates in right hand ruling order
return - the coverage mask for this triangle
*/
uint2 TriangleCoverageMask(float2 v0, float2 v1, float2 v2, bool showFrontFace, bool showBackface);


/*
Call this function to get a 64 bit coverage mask for a line.
v0, v1 - the line coordinates.
thickness - thickness of line in normalized space. 1.0 means the entire 8 pixels in a tile
caps - extra pixels in the caps of the line in normalized space. 1.0 means 8 pixels in a tile
return - the coverage mask of this line
*/
uint2 LineCoverageMask(float2 v0, float2 v1, float thickness, float caps);


//**************************************************************************************************************/
//                                       coverage implementation
//**************************************************************************************************************/

/*
function that builds a 4x4 compact bit quad for line coverage.
the line is assumed to have a positive slope < 1.0. That means it can only be raised 1 step at most.
"incrementMask" is a bit mask specifying how much the y component of a line increments.
"incrementMask" only describes 4 bits, the rest of the bits are ignored.
For example, given this bit mask:
1 0 1 0
would generate this 4x4 coverage mask:

0 0 0 0
0 0 0 1 <- 3rd bit tells the line to raise here
0 1 1 1 <- first bit raises the line
1 1 1 1 <- low axis is always covered
*/
uint BuildQuadMask(uint incrementMask)
{
    uint c = 0;

    uint mask = 0xF;
    for (int r = 0; r < 4; ++r)
    {
        c |= mask << (r * 8);
        if (incrementMask == 0)
            break;
        int b = firstbitlow(incrementMask);
        mask = (0xFu << (b + 1)) & 0xFu;
        incrementMask ^= 1u << b;
    }

    return c;
}

//flip 4 bit nibble
uint FlipNibble(uint mask, int offset)
{
    mask = (mask >> offset) & 0xF;
    uint r = ((mask << 3) & 0x8)
           | ((mask << 1) & 0x4)
           | ((mask >> 1) & 0x2)
           | ((mask >> 3) & 0x1);
    return (r << offset);
}

//flip an entire 4x4 bit quad
uint FlipQuadInX(uint mask)
{
    return FlipNibble(mask, 0) | FlipNibble(mask, 8) | FlipNibble(mask, 16) | FlipNibble(mask, 24);
}

uint TransposeQuad(uint mask)
{
    uint result = 0;
    [unroll]
    for (int i = 0; i < 4; ++i)
    {
        for (int j = 0; j < 4; ++j)
        {
            if (mask & (1u << (i * 8 + j)))
                result |= 1u << (j * 8 + i);
        }
    }
    return result;
}

// Builds all the luts necessary for fast bit based coverage
void GenLUT(uint groupThreadIndex)
{
    // Neutral
    if (groupThreadIndex < 16)
        gs_quadMask[groupThreadIndex] = BuildQuadMask(groupThreadIndex);

    GroupMemoryBarrierWithGroupSync();

    // Flip in X axis, transpose
    if (groupThreadIndex < 16)
    {
        gs_quadMask[groupThreadIndex + 16] = FlipQuadInX(gs_quadMask[groupThreadIndex]);
        gs_quadMask[groupThreadIndex + 32] = TransposeQuad(gs_quadMask[groupThreadIndex]);
    }
    GroupMemoryBarrierWithGroupSync();
    if (groupThreadIndex < 16)
    {
        gs_quadMask[groupThreadIndex + 48] = (~TransposeQuad(FlipQuadInX(gs_quadMask[groupThreadIndex]))) & 0x0F0F0F0F;
    }
}

// Represents a 2D analytical line.
// stores slope (a) and offset (b)
struct AnalyticalLine
{
    float a;
    float b;

    // Builds an analytical line based on two points.
    void Build(float2 v0, float2 v1)
    {
        //line equation: f(x): a * x + b;
        // where a = (v1.y - v0.y)/(v1.x - v0.x)
        float2 l = v1 - v0;
        a = l.y/l.x;
        b = v1.y - a * v1.x;
    }

    // Builds a "Flipped" line.
    // A flipped line is defined as having a positive slope < 1.0
    // The two output booleans specify the flip operators to recover the original line.
    void BuildFlipped(float2 v0, float2 v1, out bool outFlipX, out bool outFlipAxis, out bool outIsRightHand, out bool outValid)
    {
        //build line with flip bits for lookup compression
        //This line will have a slope between 0 and 0.5, and always positive.
        //We output the flips as bools

        float2 ll = v1 - v0;
        outFlipAxis = abs(ll.y) > abs(ll.x);
        outFlipX = sign(ll.y) != sign(ll.x);
        outIsRightHand = ll.x >= 0 ? v0.y >= v1.y : v0.y > v1.y;
        if (outFlipAxis)
        {
            ll.xy = ll.yx;
            v0.xy = v0.yx;
            v1.xy = v1.yx;
        }

        a = ll.y/ll.x;
        if (outFlipX)
        {
            v0.x = 1.0 - v0.x;
            v1.x = 1.0 - v1.x;
            a *= -1;
        }
        b = v1.y - a * v1.x;
        outValid = any(v1 != v0);//ll.y != 0.0f;
    }

    // Evaluates f(x) = a * x + b for the line
    float Eval(float xval)
    {
        return xval * a + b;
    }

    // Evaluates 4 inputs of f(x) = a * x + b for the line
    float4 Eval4(float4 xvals)
    {
        return xvals * a + b;
    }

    // Evaluates a single 2d in the line given an X.
    float2 PointAt(float xv)
    {
        return float2(xv, Eval(xv));
    }
};

/*
Represents a set of bits in an 8x8 grid divided by a line.
The representation is given by 2 splits of the 8x8 grid.
offsets represents how much we offset the quadCoverage on either x or y (flipped dependant axis)
the mask represents the increment mask used to look up the quadCoverage
*/
struct LineArea
{
    int offsets[2];
    uint masks[2];
    bool isValid;
    bool flipX;
    bool flipAxis;
    bool isRightHand;
    AnalyticalLine debugLine;

    // Recovers a single point in the boundary
    // of the line (where the line intersects a pixel).
    // Theres a total of 8 possible points
    float2 GetBoundaryPoint(uint i)
    {
        int j = i & 0x3;
        int m = i >> 2;
        int yval = offsets[m] + (int)countbits(((1u << j) - 1) & masks[m]);
        float2 v = float2(i + 0.5, yval + 0.5) * 1.0/8.0;
        if (flipX)
            v.x = 1.0 - v.x;
        if (flipAxis)
        {
            float2 tmp = v;
            v.xy = tmp.yx;
        }
        return v;
    }

    // Creates a line area object, based on 2 points on an 8x8 quad
    // quad coordinate domain is 0.0 -> 1.0 for both axis.
    // Anything negative or greater than 1.0 is by definition outside of the 8x8 quad.
    static LineArea Create(float2 v0, float2 v1)
    {
        LineArea data;

        //line debug data
        data.debugLine.Build(v0, v1);

        AnalyticalLine l;
        l.BuildFlipped(v0, v1, data.flipX, data.flipAxis, data.isRightHand, data.isValid);

        // Xs values of 8 points
        const float4 xs0 = float4(0.5,1.5,2.5,3.5)/8.0;
        const float4 xs1 = float4(4.5,5.5,6.5,7.5)/8.0;

        // Ys values of 8 points
        float4 ys0 = l.Eval4(xs0);
        float4 ys1 = l.Eval4(xs1);

        int4 ysi0 = (int4)floor(ys0 * 8.0 - 0.5);
        int4 ysi1 = (int4)floor(ys1 * 8.0 - 0.5);

        // Incremental masks
        uint4 dysmask0 = uint4(ysi0.yzw, ysi1.x) - ysi0.xyzw;
        uint4 dysmask1 = uint4(ysi1.yzw, 0) - uint4(ysi1.xyz, 0);

        // Final output, offset and mask
        data.offsets[0] = ysi0.x;
        data.masks[0] = dysmask0.x | (dysmask0.y << 1) | (dysmask0.z << 2) | (dysmask0.w << 3);
        data.offsets[1] = countbits(data.masks[0]) + data.offsets[0];
        data.masks[1] = dysmask1.x | (dysmask1.y << 1) | (dysmask1.z << 2) | (dysmask1.w << 3);
        return data;
    }
} ;

uint2 CreateCoverageMask(in LineArea lineArea)
{
    const uint leftSideMask = 0x0F0F0F0F;
    const uint2 horizontalMask = uint2(leftSideMask, ~leftSideMask);

    //prepare samples, flip samples if there is mirroring in x
    int2 ii = lineArea.flipX ? int2(1,0) : int2(0,1);
    int lutOperation = ((uint)lineArea.flipX << 4) | ((uint)lineArea.flipAxis << 5);
    int2 offsets = int2(lineArea.offsets[ii.x],lineArea.offsets[ii.y]);
    uint2 halfSamples = uint2(gs_quadMask[lineArea.masks[ii.x] + lutOperation], gs_quadMask[lineArea.masks[ii.y] + lutOperation]);

    uint2 result = 0;
    if (lineArea.flipAxis)
    {
        //Case were we have flipped axis / transpose. We generate top and bottom part
        int2 tOffsets = clamp(offsets, -31, 31);
        uint2 workMask = leftSideMask << clamp(offsets, 0, 4);
        uint2 topDownMasks = uint2( tOffsets.x > 0 ?
                                    ((halfSamples.x << min(4,tOffsets.x)) & leftSideMask) | ((halfSamples.x << min(8,tOffsets.x)) & ~leftSideMask)
                                    : ((halfSamples.x << 4) >> min(4,-tOffsets.x) & ~leftSideMask) >> 4,
                                    tOffsets.y > 0 ?
                                    ((halfSamples.y << min(4, tOffsets.y)) & leftSideMask) | ((halfSamples.y << min(8, tOffsets.y)) & ~leftSideMask)
                                    : ((halfSamples.y << 4) >> min(4, -tOffsets.y) & ~leftSideMask) >> 4);
            ;
        int2 backMaskShift = lineArea.flipX ? clamp(tOffsets + 4, -31, 31) : tOffsets;
        uint2 backMaskOp = int2((backMaskShift.x > 0 ? 1u << backMaskShift.x : 1u >> -backMaskShift.x) - 1u, (backMaskShift.y > 0 ? 1u << backMaskShift.y : 1u >> -backMaskShift.y) - 1u);
        uint2 backBite = uint2( backMaskShift.x <= 0 ? (lineArea.flipX ? ~0x0 : 0x0) : (lineArea.flipX ? (0xFF & ~backMaskOp.x) : (0xFFFF & backMaskOp.x)),
                                backMaskShift.y <= 0 ? (lineArea.flipX ? ~0x0 : 0x0) : (lineArea.flipX ? (0xFF & ~backMaskOp.y) : (0xFFFF & backMaskOp.y)));
        result = backBite | (backBite << 8) | (backBite << 16) | (backBite << 24) | (topDownMasks & workMask);
    }
    else
    {
        //Case were the masks are positioned horizontally. We generate 4 quads
        uint2 sideMasks = uint2(halfSamples.x, (halfSamples.y << 4));
        int4 tOffsets = clamp((offsets.xyxy - int4(0,0,4,4)) << 3, -31, 31);
        uint4 halfMasks = uint4( tOffsets.x > 0 ? (~sideMasks.x & horizontalMask.x) << tOffsets.x : ~(sideMasks.x >> -tOffsets.x),
                                 tOffsets.y > 0 ? (~sideMasks.y & horizontalMask.y) << tOffsets.y : ~(sideMasks.y >> -tOffsets.y),
                                 tOffsets.z > 0 ? (~sideMasks.x & horizontalMask.x) << tOffsets.z : ~(sideMasks.x >> -tOffsets.z),
                                 tOffsets.w > 0 ? (~sideMasks.y & horizontalMask.y) << tOffsets.w : ~(sideMasks.y >> -tOffsets.w)) & horizontalMask.xyxy;
        result = uint2(halfMasks.x | halfMasks.y, halfMasks.z | halfMasks.w);
    }

    result = lineArea.flipX ? ~result : result;
    result = lineArea.isRightHand ? result : ~result;
    result = lineArea.isValid ? result : 0;
    return result;

}

uint2 TriangleCoverageMask(float2 v0, float2 v1, float2 v2, bool showFrontFace, bool showBackface)
{
    uint2 mask0 = Coverage::CreateCoverageMask(Coverage::LineArea::Create(v0, v1));
    uint2 mask1 = Coverage::CreateCoverageMask(Coverage::LineArea::Create(v1, v2));
    uint2 mask2 = Coverage::CreateCoverageMask(Coverage::LineArea::Create(v2, v0));
    uint2 frontMask = (mask0 & mask1 & mask2);
    bool frontMaskValid = any(mask0 != 0) || any(mask1 != 0) || any(mask2 != 0);
    return (showFrontFace * (mask0 & mask1 & mask2)) | ((frontMaskValid && showBackface) * (~mask0 & ~mask1 & ~mask2));
}

uint2 LineCoverageMask(float2 v0, float2 v1, float thickness, float caps)
{
    float2 lineVector = normalize(v1 - v0);
    float2 D = cross(float3(lineVector, 0.0),float3(0,0,1)).xy * thickness;
    v0 -= caps * lineVector;
    v1 += caps * lineVector;

    uint2 mask0 = Coverage::CreateCoverageMask(Coverage::LineArea::Create(v0 - D, v1 - D));
    uint2 mask1 = Coverage::CreateCoverageMask(Coverage::LineArea::Create(v1 + D, v0 + D));
    uint2 mask2 = Coverage::CreateCoverageMask(Coverage::LineArea::Create(v0 + D, v0 - D));
    uint2 mask3 = Coverage::CreateCoverageMask(Coverage::LineArea::Create(v1 - D, v1 + D));
    return mask0 & mask1 & mask3 & mask2;
}

}

#endif
