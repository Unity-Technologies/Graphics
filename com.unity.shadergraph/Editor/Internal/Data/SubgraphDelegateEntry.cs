using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct SubgraphDelegateEntry
    {
        public int id;
        public PropertyType propertyType;
        public string displayName;
        public string referenceName;

        public SubgraphDelegateEntry(PropertyType propertyType, string displayName, string referenceName)
        {
            this.id = -1;
            this.propertyType = propertyType;
            this.displayName = displayName;
            this.referenceName = referenceName;
        }

        internal SubgraphDelegateEntry(int id, PropertyType propertyType, string displayName, string referenceName)
        {
            this.id = id;
            this.propertyType = propertyType;
            this.displayName = displayName;
            this.referenceName = referenceName;
        }
    }
}
