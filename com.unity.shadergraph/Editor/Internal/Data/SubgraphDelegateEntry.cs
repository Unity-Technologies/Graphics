using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct SubgraphDelegateEntry
    {
        public int id;
        public PropertyType propertyType;

        public SubgraphDelegateEntry(PropertyType propertyType)
        {
            this.id = -1;
            this.propertyType = propertyType;
        }

        internal SubgraphDelegateEntry(int id, PropertyType propertyType)
        {
            this.id = id;
            this.propertyType = propertyType;
        }
    }
}
