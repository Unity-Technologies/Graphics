using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    internal interface INodeDefinitionBuilder : IRegistryEntry
    {
        void BuildNode(
            INodeReader userData,
            INodeWriter generatedData,
            Registry registry
        );

        ShaderFunction GetShaderFunction(
            INodeReader data,
            ShaderContainer container,
            Registry registry
        );
    }
}
