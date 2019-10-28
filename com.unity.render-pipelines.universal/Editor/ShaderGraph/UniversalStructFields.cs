using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalStructFields
    {
        public struct Varyings
        {
            public static string name = "Varyings";
            public static SubscriptDescriptor lightmapUV = new SubscriptDescriptor(Varyings.name, "lightmapUV", "", ShaderValueType.Float2,
                preprocessor : "defined(LIGHTMAP_ON)", subscriptOptions : SubscriptOptions.Optional);
            public static SubscriptDescriptor sh = new SubscriptDescriptor(Varyings.name, "sh", "", ShaderValueType.Float3,
                preprocessor : "!defined(LIGHTMAP_ON)", subscriptOptions : SubscriptOptions.Optional);
            public static SubscriptDescriptor fogFactorAndVertexLight = new SubscriptDescriptor(Varyings.name, "fogFactorAndVertexLight", "VARYINGS_NEED_FOG_AND_VERTEX_LIGHT", ShaderValueType.Float4,
                subscriptOptions : SubscriptOptions.Optional);
            public static SubscriptDescriptor shadowCoord = new SubscriptDescriptor(Varyings.name, "shadowCoord", "VARYINGS_NEED_SHADOWCOORD", ShaderValueType.Float4,
                subscriptOptions : SubscriptOptions.Optional);
        }
    }
}
