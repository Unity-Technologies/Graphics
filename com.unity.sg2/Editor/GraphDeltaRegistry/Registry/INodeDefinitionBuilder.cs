using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal interface INodeDefinitionBuilder : IRegistryEntry
    {
        void BuildNode(NodeHandler node, Registry registry);
        ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry);
    }
}
