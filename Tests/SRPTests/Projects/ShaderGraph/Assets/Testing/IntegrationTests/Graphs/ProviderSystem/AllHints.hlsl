#include "ShaderApiReflectionSupport.hlsl"

namespace Namespace {

struct CustomStruct
{
    float Value;
};

///<funchints>
///     <sg:ProviderKey>AllHintsUniqueStableID</sg:ProviderKey>
///     <sg:DisplayName>All Known Hints</sg:DisplayName>
///     <sg:ReturnDisplayName>Final Result</sg:ReturnDisplayName>
///     <sg:SearchCategory>Hints/All</sg:SearchCategory>
///     <sg:SearchTerms>Features, Reflected, Every, Hint</sg:SearchTerms>
///     <sg:SearchName>All Hints Search Name</sg:SearchName>
///</funchints>
///<paramhints name = "a">
///     <sg:DisplayName>Custom Struct Value</sg:DisplayName>
///     <sg:Default>0.5</sg:Default>
///     <sg:External>Namespace</sg:External>
///</paramhints>
///<paramhints name = "c3">
///     <sg:DisplayName>Color 3</sg:DisplayName>
///     <sg:Color />
///     <sg:Default>1,1,0</sg:Default>
///</paramhints>
///<paramhints name = "c4">
///     <sg:DisplayName>Color 4</sg:DisplayName>
///     <sg:Color />
///     <sg:Default>1,1,0,1</sg:Default>
///</paramhints>
///<paramhints name = "sc3">
///     <sg:DisplayName>Static Color 3</sg:DisplayName>
///     <sg:Color />
///     <sg:Static />
///     <sg:Default>1,1,0</sg:Default>
///</paramhints>
///<paramhints name = "sc4">
///     <sg:DisplayName>Static Color 4</sg:DisplayName>
///     <sg:Color />
///     <sg:Static />
///     <sg:Default>1,1,0,1</sg:Default>
///</paramhints>
///<paramhints name = "d">
///     <sg:DisplayName>Dropdown</sg:DisplayName>
///     <sg:Dropdown>OptionA, OptionB, OptionC</sg:Dropdown>
///     <sg:Default>1</sg:Default>
///</paramhints>
///<paramhints name = "sd">
///     <sg:DisplayName>Dropdown</sg:DisplayName>
///     <sg:Dropdown>OptionA, OptionB, OptionC</sg:Dropdown>
///     <sg:Static />
///     <sg:Default>2</sg:Default>
///</paramhints>
///<paramhints name = "r">
///     <sg:DisplayName>Range</sg:DisplayName>
///     <sg:Range>0, 1</sg:Range>
///     <sg:Default>0.75</sg:Default>
///</paramhints>
///<paramhints name = "sr">
///     <sg:DisplayName>Range</sg:DisplayName>
///     <sg:Static />
///     <sg:Range>0, 1</sg:Range>
///     <sg:Default>0.25</sg:Default>
///</paramhints>
///<paramhints name = "sb">
///     <sg:DisplayName>Toggle</sg:DisplayName>
///     <sg:Static />
///     <sg:Default>1</sg:Default>
///</paramhints>
///<paramhints name = "si">
///     <sg:DisplayName>Integer</sg:DisplayName>
///     <sg:Static />
///     <sg:Default>5</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION float3 AllHints(
    inout CustomStruct a,
    float3 c3,
    inout float4 c4,
    float3 sc3,
    float4 sc4,
    inout uint d,
    uint sd,
    float r,
    float sr,
    bool sb,
    int si
)
{
    return float3(a.Value, sr, sc4.r);
}

///<funchints>
///     <sg:ProviderKey>MakeCustomStruct</sg:ProviderKey>
///</funchints>
///<paramhints name = "result">
///     <sg:External>Namespace</sg:External>
///</paramhints>
UNITY_EXPORT_REFLECTION void MakeCustomStruct(float a, out CustomStruct result)
{
    result.Value = a;
}

namespace NamespaceAsCategory
{
    UNITY_EXPORT_REFLECTION float3 NamespacedFunction()
    {
        return float3(1,1,0);
    }
}

} // Namespace


///<paramhints name = "input">
    /// <Range/>
    /// <Color/>
    /// <Dropdown/>
    /// <Static/>
    /// <Local/>
///</paramhints>
///<paramhints name = "input2">
    /// <Static/>
    /// <Local/>
///</paramhints>
UNITY_EXPORT_REFLECTION float3 Confliction(float2 input, float input2)
{
    return input.xxy;
}

///<funchints>
///     <sg:ProviderKey>GoodReferables</sg:ProviderKey>
///</funchints>
///<paramhints name = "UV">
    /// <UV/>
    /// <Default>UV2</Default>
///</paramhints>
///<paramhints name = "alsoUV">
    /// <sg:Referable>UV</sg:Referable>
    /// <Default>UV1</Default>
///</paramhints>
///<paramhints name = "Position">
    /// <Position/>
    /// <Default>AbsoluteWorld</Default>
///</paramhints>
///<paramhints name = "Normal">
    /// <Normal/>
    /// <Default>World</Default>
///</paramhints>
///<paramhints name = "Tangent">
    /// <Tangent/>
    /// <Default>Object</Default>
///</paramhints>
///<paramhints name = "Bitangent">
    /// <Bitangent/>
    /// <Default>Tangent</Default>
///</paramhints>
///<paramhints name = "ViewDirection">
    /// <ViewDirection/>
    /// <Default>Screen</Default>
///</paramhints>
///<paramhints name = "VertColor">
    /// <VertexColor/>
///</paramhints>
///<paramhints name = "ScreenPosition">
    /// <ScreenPosition/>
    /// <Default>Pixel</Default>
///</paramhints>
///<paramhints name = "defaultValue">
    /// <Default>1,0,0</Default>
///</paramhints>
UNITY_EXPORT_REFLECTION float3 ReferableGood(float2 UV,
                                            float2 alsoUV,
                                            float3 Position,
                                            float3 Normal,
                                            float3 Tangent,
                                            float3 Bitangent,
                                            float3 ViewDirection,
                                            float4 VertColor,
                                            float4 ScreenPosition,
                                            float3 defaultValue)
{
    return float3(1,1,0);
}

///<funchints>
///     <sg:ProviderKey>PrecisionTest</sg:ProviderKey>
    /// <Precision/>
///</funchints>
///<paramhints name = "a">
    /// <Dynamic/>
///</paramhints>
///<paramhints name = "b">
    /// <Dynamic/>
///</paramhints>
///<paramhints name = "c">
    /// <Dynamic/>
///</paramhints>
UNITY_EXPORT_REFLECTION float2 PrecisionTest(float a, half b, out float c)
{
    c = a + b;
    return (a,b);
}

///<funchints>
/// <sg:ProviderKey>LinkageExample</sg:ProviderKey>
///</funchints>
///<paramhints name = "test">
    /// <Linkage>other</Linkage>
///</paramhints>
UNITY_EXPORT_REFLECTION float3 LinkageExample(bool test, float other)
{
    if (test)
        return float3(other, 0, 0);
    else
        return float3(0, other, 0);
}
