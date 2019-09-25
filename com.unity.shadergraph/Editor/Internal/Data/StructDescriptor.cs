using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    public struct StructDescriptor
    {
        public string name;
        public bool interpolatorPack;
        public SubscriptDescriptor[] subscripts;
    }
}
