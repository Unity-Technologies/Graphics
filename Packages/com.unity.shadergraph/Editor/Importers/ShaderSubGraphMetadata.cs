using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class ShaderSubGraphMetadata : ScriptableObject
    {
        public List<LazyLoadReference<Object>> assetDependencies;
    }
}
