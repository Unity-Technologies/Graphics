#ifndef UNITY_DEBUG_INCLUDED
#define UNITY_DEBUG_INCLUDED

// Given an enum (represented by an int here), return a color.
// Use for DebugView of enum
float3 GetIndexColor(int index)
{
    float3 outColor = float3(1.0, 0.0, 0.0);

    if (index == 0)
        outColor = float3(1.0, 0.5, 0.5);
    else if (index == 1)
        outColor = float3(0.5, 1.0, 0.5);
    else if (index == 2)
        outColor = float3(0.5, 0.5, 1.0);
    else if (index == 3)
        outColor = float3(1.0, 1.0, 0.5);
    else if (index == 4)
        outColor = float3(1.0, 0.5, 1.0);
    else if (index == 5)
        outColor = float3(0.5, 1.0, 1.0);
    else if (index == 6)
        outColor = float3(0.25, 0.75, 1.0);
    else if (index == 7)
        outColor = float3(1.0, 0.75, 0.25);
    else if (index == 8)
        outColor = float3(0.75, 1.0, 0.25);
    else if (index == 9)
        outColor = float3(0.75, 0.25, 1.0);

    return outColor;
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

bool SampleDebugFontNumber(int2 pixCoord, uint number)
{
    pixCoord.y -= 4;
    if (number <= 9)
    {
        return SampleDebugFont(pixCoord - int2(6, 0), number);

    }
    else
    {
        return (SampleDebugFont(pixCoord, number / 10) | SampleDebugFont(pixCoord - int2(6, 0), number % 10));
    }
}

#endif // UNITY_DEBUG_INCLUDED
