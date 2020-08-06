using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphMetadata : ScriptableObject
    {
        public string outputNodeTypeName;
        public List<Object> assetDependencies;
    }
}
