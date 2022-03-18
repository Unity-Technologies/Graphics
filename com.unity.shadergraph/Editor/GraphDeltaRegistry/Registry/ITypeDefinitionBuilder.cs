using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    internal interface ITypeDefinitionBuilder : IRegistryEntry
    {
        void BuildType(IFieldReader userData, IFieldWriter generatedData, Registry registry);

        ShaderType GetShaderType(IFieldReader data, ShaderContainer container, Registry registry);

        string GetInitializerList(IFieldReader data, Registry registry);
    }
}
