using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphMetadata : ScriptableObject
    {
        public string outputNodeTypeName;

        // these asset dependencies are stored here as a way for "Export Package..." to discover them
        // and automatically pull them in to the .unitypackage
        public List<Object> assetDependencies;
    }
}
