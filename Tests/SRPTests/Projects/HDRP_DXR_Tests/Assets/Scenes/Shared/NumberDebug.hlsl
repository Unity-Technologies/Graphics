// Quick try at doing a "print value" node for Unity ShaderGraph.
// Tested on Unity 2019.2.17 with ShaderGraph 6.9.2.
//
// Use with CustomFunction node, with two inputs:
// - Vector1 Value, the value to display,
// - Vector2 UV, the UVs of area to display at.
// And one output:
// - Vector4 Color, the color.
// Function name is DoDebug.

// "print in shader" based on this excellent ShaderToy by @P_Malin:
// https://www.shadertoy.com/view/4sBSWW

float DigitBin(const int x)
{
    return x==0?480599.0:x==1?139810.0:x==2?476951.0:x==3?476999.0:x==4?350020.0:x==5?464711.0:x==6?464727.0:x==7?476228.0:x==8?481111.0:x==9?481095.0:0.0;
}

float PrintValue(float2 vStringCoords, float fValue, float fMaxDigits, float fDecimalPlaces)
{
    if ((vStringCoords.y < 0.0) || (vStringCoords.y >= 1.0))
        return 0.0;

    bool bNeg = (fValue < 0.0);
    fValue = abs(fValue);

    float fLog10Value = log2(abs(fValue)) / log2(10.0);
    float fBiggestIndex = max(floor(fLog10Value), 0.0);
    float fDigitIndex = fMaxDigits - floor(vStringCoords.x);
    float fCharBin = 0.0;
    if (fDigitIndex > (-fDecimalPlaces - 1.01))
    {
        if(fDigitIndex > fBiggestIndex)
        {
            if((bNeg) && (fDigitIndex < (fBiggestIndex+1.5))) fCharBin = 1792.0;
        }
        else
        {
            if(fDigitIndex == -1.0)
            {
                if(fDecimalPlaces > 0.0) fCharBin = 2.0;
            }
            else
            {
                float fReducedRangeValue = fValue;
                if(fDigitIndex < 0.0) { fReducedRangeValue = frac( fValue ); fDigitIndex += 1.0; }
                float fDigitValue = (abs(fReducedRangeValue / (pow(10.0, fDigitIndex))));
                fCharBin = DigitBin(int(floor(fmod(fDigitValue, 10.0))));
            }
        }
    }
    return floor(fmod((fCharBin / pow(2.0, floor(frac(vStringCoords.x) * 4.0) + (floor(vStringCoords.y * 5.0) * 4.0))), 2.0));
}

float PrintValue(const in float2 fragCoord, const in float2 vPixelCoords, const in float2 vFontSize, const in float fValue, const in float fMaxDigits, const in float fDecimalPlaces)
{
    float2 vStringCharCoords = (fragCoord.xy - vPixelCoords) / vFontSize;
    return PrintValue( vStringCharCoords, fValue, fMaxDigits, fDecimalPlaces );
}

void DoDebug_float(float val, float2 uv, out float4 res)
{
    res = float4(0.3,0.2,0.1,1);
    res = PrintValue(uv*200, float2(10,100), float2(8,15), val, 10, 3);
}