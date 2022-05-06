using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal interface ITypeDefinitionBuilder : IRegistryEntry
    {
        void BuildType(FieldHandler field, Registry registry);
        void CopySubFieldData(FieldHandler src, FieldHandler dst);
        ShaderType GetShaderType(FieldHandler field, ShaderContainer container, Registry registry);
        string GetInitializerList(FieldHandler field, Registry registry);

        internal static void CopyTypeField(FieldHandler src, FieldHandler dst, Registry registry)
        {
            // TODO: error handling in case there is no registry key
            var regkey = src.GetMetadata<RegistryKey>(GraphDelta.kRegistryKeyName);
            dst.SetMetadata<RegistryKey>(GraphDelta.kRegistryKeyName, regkey);

            var builder = dst.Registry.GetTypeBuilder(regkey);
            builder.BuildType(dst, registry);
            builder.CopySubFieldData(src, dst);
        }
    }
}
