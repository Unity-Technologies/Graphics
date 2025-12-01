#include "HelpersRSUV.hlsl"


void GetTexture2DArraySize_float(UnityTexture2DArray texture2DArray, out float Width, out float Height)
{
    uint w, h, e = 0;
    texture2DArray.tex.GetDimensions(w, h, e);
    Width = w;
    Height = h;
}

void GetColor_float(out float4 Color)
{
    uint data = GetData();
    Color = DecodeUintToFloat4(data);
}

void GetRendererShaderUserValueHealth_float(out float Health, out bool ShowHealthBar, out float HealthBarOpacity)
{
    uint data = GetData();
    Health = DecodeBitsToInt(data, 0, 3) / 7;
    ShowHealthBar = GetBit(data, 3);
    HealthBarOpacity = DecodeBitsToInt(data, 4, 3) / 7;
}

void GetRendererShaderUserValueVAT_float(out float AnimIndexFrom, out float AnimIndexTo, out float TransitionLerpFactor, out float FrameTime)
{
    uint data = GetData();
    
    AnimIndexFrom = DecodeBitsToInt(data, 30, 1);
    AnimIndexTo = DecodeBitsToInt(data, 31, 1);
    TransitionLerpFactor = DecodeBitsToInt(data, 27, 3) / 7;
    float animationLength = 50;
    float maxFrameTime = 127;
    float ratio = animationLength / maxFrameTime;
    FrameTime = DecodeBitsToInt(data, 20, 7) * ratio;
}

void GetRendererShaderUserValueSelected_float(out bool IsSelectable, out bool IsSelected)
{
    uint data = GetData();
    IsSelectable = DecodeBitsToInt(data, 0, 1);
    IsSelected = DecodeBitsToInt(data, 1, 1);
}

void GetRendererShaderUserValueHeadGear_float(out float HeadGear)
{
    // Default is none => 0
    uint data = GetData();
    float rawHeadGear = DecodeBitsToInt(data, 18, 2);
    HeadGear = rawHeadGear == 0 ? 0 : 0.95 - ((rawHeadGear - 1) * 0.1);
}

void GetRendererShaderUserValueBody_float(out float BellySize, out float SkinColor, out float ClothColor)
{
    uint data = GetData();
    SkinColor = DecodeBitsToInt(data, 0, 3) / 5; //There's 5 different skin color
    ClothColor = DecodeBitsToInt(data, 3, 3) / 7; //There's 7 different cloth color
    BellySize = DecodeBitsToInt(data, 6, 2) / 3; //Encoded in 8 steps
}

void GetRendererShaderUserValueFacialHair_float(out float HairColor, out float HairStyle, out float EyebrowsStyle, out float BeardStyle)
{
    uint data = GetData();
    HairColor = DecodeBitsToInt(data, 8, 3) / 8;
    
    float rawHair = DecodeBitsToInt(data, 11, 3);
    HairStyle = rawHair == 0 ? 0 : 0.95 - ((rawHair - 1) * 0.1);
    
    float rawBrow = DecodeBitsToInt(data, 14, 2);
    float valueBrow = rawBrow;
    EyebrowsStyle = rawBrow == 0 ? 0 : 0.95 - ((rawBrow - 1) * 0.1);
    
    float rawBeard = DecodeBitsToInt(data, 16, 2);
    BeardStyle = rawBeard == 0 ? 0 : 0.95 - ((rawBeard - 1) * 0.1);    
}
