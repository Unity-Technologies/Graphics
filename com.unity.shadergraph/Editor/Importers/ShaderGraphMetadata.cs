using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class MinimalCategoryData
    {
        [Serializable]
        public struct GraphInputData
        {
            public string referenceName;
            public bool isKeyword;
            public PropertyType propertyType;
            public KeywordType keywordType;
        }
        public string categoryName;
        public List<GraphInputData> propertyDatas;
    }

    class ShaderGraphMetadata : ScriptableObject
    {
        public string outputNodeTypeName;

        // these asset dependencies are stored here as a way for "Export Package..." to discover them
        // and automatically pull them in to the .unitypackage
        public List<Object> assetDependencies;

        public List<MinimalCategoryData> categoryDatas;
    }
}
