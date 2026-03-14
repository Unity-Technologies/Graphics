#include "ShaderApiReflectionSupport.hlsl"

namespace ShaderFunctionReflectionStructExample
{
    
    struct VertexData
    {
        float3 position;
        float3 normal;
        float3 tangent;
    };

    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Struct.SetVertex.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Set Vertex</sg:DisplayName>
    ///    <sg:ReturnDisplayName>Vertex</sg:ReturnDisplayName>
    ///    <sg:SearchCategory>Node Reference/Struct</sg:SearchCategory>
    ///    <sg:SearchName>Set Vertex</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Struct</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "position">
    ///    <Position />
    ///    <Default>ObjectSpace</Default>
    ///</paramhints>
    ///<paramhints name = "normal">
    ///    <Normal />
    ///    <Default>ObjectSpace</Default>
    ///</paramhints>
    ///<paramhints name = "tangent">
    ///    <Tangent />
    ///    <Default>ObjectSpace</Default>
    ///</paramhints>
    ///<paramhints name = "vertex">
    ///     <sg:DisplayName>Vertex Data</sg:DisplayName>
    ///     <sg:External>ShaderFunctionReflectionStructExample</sg:External>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    void SetVertex(float3 position, float3 normal, float3 tangent, out VertexData vertex)
    {
        vertex.position = position;
        vertex.normal = normal;
        vertex.tangent = tangent;
    }

    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Struct.GetVertex.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Get Vertex</sg:DisplayName>
    ///    <sg:SearchCategory>Node Reference/Struct</sg:SearchCategory>
    ///    <sg:SearchName>Get Vertex</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Struct</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "vertex">
    ///     <sg:DisplayName>Vertex Data</sg:DisplayName>
    ///     <sg:External>ShaderFunctionReflectionStructExample</sg:External>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    void GetVertex(VertexData vertex, out float3 position, out float3 normal, out float3 tangent)
    {
        position = vertex.position;
        normal = vertex.normal;
        tangent = vertex.tangent;
    }

    /// <funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Struct.PushVertex.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Push Vertex</sg:DisplayName>
    ///    <sg:SearchCategory>Node Reference/Struct</sg:SearchCategory>
    ///    <sg:SearchName>Push Vertex</sg:SearchName>
    ///    <sg:SearchTerms>Reference, Struct</sg:SearchTerms>
    /// </funchints>
    ///<paramhints name = "vertex">
    ///     <sg:DisplayName>Vertex Data</sg:DisplayName>
    ///     <sg:External>ShaderFunctionReflectionStructExample</sg:External>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    void Push(inout VertexData vertex, float amount)
    {
        vertex.position += vertex.normal * amount;
    }
}
