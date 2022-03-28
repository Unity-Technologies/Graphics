using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal interface ITypeDefinitionBuilder : IRegistryEntry
    {
        void BuildType(FieldHandler field, Registry registry);
        ShaderType GetShaderType(FieldHandler field, ShaderContainer container, Registry registry);
        string GetInitializerList(FieldHandler field, Registry registry);
    }
}
