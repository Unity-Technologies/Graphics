using System.Collections.Generic;
using UnityEditor.ShaderGraph.Registry.Experimental;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public static class ShaderGraphTypes
    {
        // TODO: This should eventually be more flexible, but for now serves its purpose
        static readonly Dictionary<string, TypeHandle> k_TypeHandlesByName = new()
        {
            {"NumericLiteral", TypeHandle.Float},
            {"StringLiteral", TypeHandle.String},
        };

        public static TypeHandle GetTypeHandleFromKey(RegistryKey registryKey)
        {
            return k_TypeHandlesByName.TryGetValue(registryKey.Name, out var handle) ? handle : TypeHandle.Unknown;
        }
    }
}
