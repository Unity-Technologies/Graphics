using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    static class ShaderGraphTypes
    {
        // TODO: This should eventually be more flexible, but for now serves its purpose
        static readonly Dictionary<string, TypeHandle> k_TypeHandlesByName = new()
        {
            {"GraphType", TypeHandle.Vector4},
        };

        public static TypeHandle GetTypeHandleFromKey(RegistryKey registryKey)
        {
            return k_TypeHandlesByName.TryGetValue(registryKey.Name, out var handle) ? handle : TypeHandle.Unknown;
        }
    }
}
