#include "ShaderApiReflectionSupport.hlsl"

namespace ShaderFunctionReflectionReference {
    /// <funchints>
    ///     <sg:ProviderKey>SG.NodeReference.Static.Example</sg:ProviderKey>
    ///     <sg:DisplayName>Static Example</sg:DisplayName>
    ///     <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///     <sg:SearchName>Static Example</sg:SearchName>
    ///     <sg:SearchTerms>Reference, Hint</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "color">
    ///     <sg:Color/>
    ///     <sg:Default>.5,.5,.5,1</sg:Default>
    ///</paramhints>
    ///<paramhints name = "multiplier">
    ///     <sg:DisplayName>Multiplier</sg:DisplayName>
    ///     <sg:Default>1</sg:Default>
    ///     <sg:Static />
    ///     <sg:Range>0, 1</sg:Range>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    void StaticExample(inout float3 color, float multiplier)
    {
        color *= multiplier;
    }

    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Dynamic.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Dynamic Example</sg:DisplayName>
    ///    <sg:ReturnDisplayName>Dot Product</sg:ReturnDisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Dynamic Example</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Hint</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "a">
    ///    <Dynamic/>
    ///    <sg:Default>0, 1</sg:Default>
    ///</paramhints>
    ///<paramhints name = "b">
    ///    <Dynamic/>
    ///    <sg:Default>1, 0</sg:Default>
    ///</paramhints>
    ///<paramhints name = "sum">
    ///    <Dynamic/>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    float DynamicExample(inout float a, float b, out float sum)
    {
        a *= b;
        sum = a + b;
        return dot(a, b);
    }

    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Precision.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Precision Example</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Precision Example</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Hint</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "a">
    ///    <sg:Default>1</sg:Default>
    ///</paramhints>
    ///<paramhints name = "b">
    ///    <sg:Default>1</sg:Default>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    float PrecisionExample(float a, float b)
    {
        return dot(a, b);
    }

    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Reference.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Reference Example</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Reference Example</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Hint</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "uv">
    ///     <UV/>
    ///     <Default>UV0</Default>
    ///</paramhints>
    ///<paramhints name = "vc">
    ///     <VertexColor />
    ///</paramhints>
    ///<paramhints name = "position">
    ///     <Position />
    ///     <Default>ObjectSpace</Default>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    void ReferenceExample(inout float2 uv, inout float4 vc, inout float3 position)
    {
    
    }
    
    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Linkage.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Linkage Example</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Linkage Example</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Hint</sg:SearchTerms>
    /// </funchints>
    /// <paramhints name = "skyColor">
    ///     <CustomBinding>Ambient Sky Color</CustomBinding>
    /// </paramhints>
    /// <paramhints name = "groundColor">
    ///     <CustomBinding>Ambient Ground Color</CustomBinding>
    /// </paramhints>
    /// <paramhints name = "cameraDirectionWS">
    ///     <CustomBinding>Camera Direction WS</CustomBinding>
    /// </paramhints>
    /// <paramhints name = "overrideSkyColor">
    ///     <Linkage>skyColor</Linkage>
    /// </paramhints>
    /// <paramhints name = "overrideGroundColor">
    ///     <Linkage>groundColor</Linkage>
    /// </paramhints>
    /// <paramhints name = "overrideCameraDirectionWS">
    ///     <Linkage>cameraDirectionWS</Linkage>
    /// </paramhints>
    UNITY_EXPORT_REFLECTION
    float3 LinkageExample(
        float3 skyColor,
        float3 groundColor,
        bool overrideSkyColor,
        bool overrideGroundColor,
        float3 cameraDirectionWS,
        bool overrideCameraDirectionWS)
    {
        if (!overrideSkyColor)
            skyColor = unity_AmbientSky.xyz;
        
        if (!overrideGroundColor)
            groundColor = unity_AmbientGround.xyz;
        
        if (!overrideCameraDirectionWS)
            cameraDirectionWS = - UNITY_MATRIX_V[2].xyz;
        
        float upFacing = dot(cameraDirectionWS, float3(0, 1, 0)) * 0.5 + 0.5;
        
        return lerp(groundColor, skyColor, upFacing);
    }

    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Local.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Local Example</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Local Example</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Hint</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "vc">
    ///     <VertexColor />
    ///     <Local />
    ///</paramhints>
    ///<paramhints name = "color">
    ///     <Color />
    ///     <Default>1,1,1,1</Default>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    void LocalExample(float4 vc, inout float3 color)
    {
        color *= vc;
    }
    
    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Texture.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Texture Example</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Texture Example</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Hint</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "uv">
    ///     <UV />
    ///     <Default>UV0</Default>
    ///</paramhints>
    ///<paramhints name = "color">
    ///     <Color />
    ///     <Default>1,1,1,1</Default>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    float4 TextureExample(UnityTexture2D Tex, float2 uv, float4 color)
    {
        return Tex.Sample( Tex.samplerstate, uv) * color;
    }
}
