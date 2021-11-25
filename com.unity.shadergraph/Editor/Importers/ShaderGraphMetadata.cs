using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct GraphInputData
    {
        public string referenceName;
        public bool isKeyword;
        public PropertyType propertyType;
        public KeywordType keywordType;

        public bool isCompoundProperty;
        public List<SubPropertyData> subProperties;
    }

    [Serializable]
    struct SubPropertyData
    {
        public string referenceName;
        public PropertyType propertyType;
    }

    [Serializable]
    class MinimalCategoryData
    {
        // ShaderLab doesn't understand virtual texture inputs, they need to be processed to replace the virtual texture input with the layers that compose it instead
        public static GraphInputData ProcessVirtualTextureProperty(VirtualTextureShaderProperty virtualTextureShaderProperty)
        {
            var layerReferenceNames = new List<string>();
            virtualTextureShaderProperty.GetPropertyReferenceNames(layerReferenceNames);
            var virtualTextureLayerDataList = new List<SubPropertyData>();

            // Skip the first entry in this list as that's the Virtual Texture reference name itself, which won't exist in ShaderLab
            foreach (var referenceName in layerReferenceNames.Skip(1))
            {
                var layerPropertyData = new SubPropertyData() { referenceName = referenceName, propertyType = PropertyType.Texture2D };
                virtualTextureLayerDataList.Add(layerPropertyData);
            }

            var virtualTexturePropertyData = new GraphInputData() { referenceName = virtualTextureShaderProperty.displayName, propertyType = PropertyType.VirtualTexture, isKeyword = false };
            virtualTexturePropertyData.isCompoundProperty = true;
            virtualTexturePropertyData.subProperties = virtualTextureLayerDataList;
            return virtualTexturePropertyData;
        }

        public string categoryName;
        public List<GraphInputData> propertyDatas;
        [NonSerialized]
        public bool expanded = true;
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
