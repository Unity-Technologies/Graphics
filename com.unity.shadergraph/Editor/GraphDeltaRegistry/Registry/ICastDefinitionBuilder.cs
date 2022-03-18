using UnityEditor.ShaderFoundry;
namespace UnityEditor.ShaderGraph.GraphDelta
{

    internal interface ICastDefinitionBuilder : IRegistryEntry
    {
        // Gross- but should suffice for now.
        (RegistryKey, RegistryKey) GetTypeConversionMapping();

        // TypeConversionMapping represents the Types we can convert, but TypeDefinitions can represent templated concepts,
        // which may mean that incompatibilities within their data could be inconvertible. Types with static fields should
        // implement an ITypeConversion with itself to ensure that static concepts can be represented.
        bool CanConvert(IFieldReader src, IFieldReader dst);

        ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderContainer container, Registry registry);
    }
}
