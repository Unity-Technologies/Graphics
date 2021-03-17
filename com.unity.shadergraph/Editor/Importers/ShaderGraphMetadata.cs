using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class MinimalCategoryData
    {
        [Serializable]
        public struct PropertyData
        {
            public string referenceName;
            public ConcreteSlotValueType valueType;
            public bool isKeyword;
        }
        public string categoryName;
        public List<PropertyData> propertyDatas;
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
