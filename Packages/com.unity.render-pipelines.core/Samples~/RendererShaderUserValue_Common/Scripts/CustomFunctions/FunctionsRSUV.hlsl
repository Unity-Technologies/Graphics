#include "HelpersRSUV.hlsl"
#include "ShaderApiReflectionSupport.hlsl"

///<funchints>
///     <sg:ProviderKey>Texture2DArraySize</sg:ProviderKey>
///     <sg:DisplayName>Texture2D Array Size</sg:DisplayName>
///     <sg:SearchName>Texture2D Array Size</sg:SearchName>
///     <sg:SearchCategory>Texture</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION
void GetTexture2DArraySize(UnityTexture2DArray texture2DArray, out float Width, out float Height)
{
    uint w, h, e = 0;
    texture2DArray.tex.GetDimensions(w, h, e);
    Width = w;
    Height = h;
}

///<funchints>
///     <sg:ProviderKey>RSUVSoldierHealth</sg:ProviderKey>
///     <sg:DisplayName>RSUV Soldier Health</sg:DisplayName>
///     <sg:SearchName>RSUV Soldier Health</sg:SearchName>
///     <sg:SearchCategory>RSUV/Soldier</sg:SearchCategory>
///</funchints>
///<paramhints name = "Health">
///    <sg:int/>
///    <sg:Default>7</sg:Default>
///</paramhints>
///<paramhints name = "ShowHealthBar">
///    <sg:bool/>
///    <sg:Default>1</sg:Default>
///</paramhints>
///<paramhints name = "HealthBarOpacity">
///    <sg:float/>
///    <sg:Default>1</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
void GetRendererShaderUserValueHealth(out float Health, out bool ShowHealthBar, out float HealthBarOpacity)
{
    Health = DecodeBitsToInt(0, 3) / 7;
    ShowHealthBar = GetBit(3);
    HealthBarOpacity = DecodeBitsToInt(4, 3) / 7;
}

///<funchints>
///     <sg:ProviderKey>RSUVSoldierVAT</sg:ProviderKey>
///     <sg:DisplayName>RSUV Soldier Vertex Animation Texture</sg:DisplayName>
///     <sg:SearchName>RSUV Soldier Vertex Animation Texture</sg:SearchName>
///     <sg:SearchCategory>RSUV/Soldier</sg:SearchCategory>
///</funchints>
///<paramhints name = "AnimIndexFrom">
///    <sg:int/>
///    <sg:Default>0</sg:Default>
///</paramhints>
///<paramhints name = "AnimIndexTo">
///    <sg:bool/>
///    <sg:Default>0</sg:Default>
///</paramhints>
///<paramhints name = "TransitionLerpFactor">
///    <sg:float/>
///    <sg:Default>1</sg:Default>
///</paramhints>
///<paramhints name = "FrameTime">
///    <sg:float/>
///    <sg:Default>0</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
void GetRendererShaderUserValueVAT(out float AnimIndexFrom, out float AnimIndexTo, out float TransitionLerpFactor, out float FrameTime)
{    
    AnimIndexFrom = DecodeBitsToInt(30, 1);
    AnimIndexTo = DecodeBitsToInt(31, 1);
    TransitionLerpFactor = DecodeBitsToInt(27, 3) / 7;
    float animationLength = 50;
    float maxFrameTime = 127;
    float ratio = animationLength / maxFrameTime;
    FrameTime = DecodeBitsToInt(20, 7) * ratio;
}

///<funchints>
///     <sg:ProviderKey>RSUVSoldierHeadGear</sg:ProviderKey>
///     <sg:DisplayName>RSUV Soldier Head Gear</sg:DisplayName>
///     <sg:SearchName>RSUV Soldier Head Gear</sg:SearchName>
///     <sg:SearchCategory>RSUV/Soldier</sg:SearchCategory>
///</funchints>
///<paramhints name = "HeadGear">
///    <sg:int/>
///    <sg:Default>0</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
void GetRendererShaderUserValueHeadGear(out float HeadGear)
{
    // Default is none => 0
    float rawHeadGear = DecodeBitsToInt(18, 2);
    HeadGear = rawHeadGear == 0 ? 0 : 0.95 - ((rawHeadGear - 1) * 0.1);
}

///<funchints>
///     <sg:ProviderKey>RSUVSoldierBody</sg:ProviderKey>
///     <sg:DisplayName>RSUV Soldier Body</sg:DisplayName>
///     <sg:SearchName>RSUV Soldier Body</sg:SearchName>
///     <sg:SearchCategory>RSUV/Soldier</sg:SearchCategory>
///</funchints>
///<paramhints name = "BellySize">
///    <sg:float/>
///    <sg:Default>1</sg:Default>
///</paramhints>
///<paramhints name = "SkinColor">
///    <sg:float/>
///    <sg:Default>0</sg:Default>
///</paramhints>
///<paramhints name = "ClothColor">
///    <sg:float/>
///    <sg:Default>0</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
void GetRendererShaderUserValueBody(out float BellySize, out float SkinColor, out float ClothColor)
{
    SkinColor = DecodeBitsToInt(0, 3) / 5; //There's 5 different skin color
    ClothColor = DecodeBitsToInt(3, 3) / 7; //There's 7 different cloth color
    BellySize = DecodeBitsToInt(6, 2) / 3; //Encoded in 4 steps
}

///<funchints>
///     <sg:ProviderKey>RSUVSoldierFacialHair</sg:ProviderKey>
///     <sg:DisplayName>RSUV Soldier Facial Hair</sg:DisplayName>
///     <sg:SearchName>RSUV Soldier Facial Hair</sg:SearchName>
///     <sg:SearchCategory>RSUV/Soldier</sg:SearchCategory>
///</funchints>
///<paramhints name = "HairColor">
///    <sg:float/>
///    <sg:Default>0</sg:Default>
///</paramhints>
///<paramhints name = "HairStyle">
///    <sg:float/>
///    <sg:Default>0</sg:Default>
///</paramhints>
///<paramhints name = "EyebrowsStyle">
///    <sg:float/>
///    <sg:Default>0</sg:Default>
///</paramhints>
///<paramhints name = "BeardStyle">
///    <sg:float/>
///    <sg:Default>0</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
void GetRendererShaderUserValueFacialHair(out float HairColor, out float HairStyle, out float EyebrowsStyle, out float BeardStyle)
{
    HairColor = DecodeBitsToInt(8, 3) / 8;
    
    float rawHair = DecodeBitsToInt(11, 3);
    HairStyle = rawHair == 0 ? 0 : 0.95 - ((rawHair - 1) * 0.1);
    
    float rawBrow = DecodeBitsToInt(14, 2);
    float valueBrow = rawBrow;
    EyebrowsStyle = rawBrow == 0 ? 0 : 0.95 - ((rawBrow - 1) * 0.1);
    
    float rawBeard = DecodeBitsToInt(16, 2);
    BeardStyle = rawBeard == 0 ? 0 : 0.95 - ((rawBeard - 1) * 0.1);    
}
