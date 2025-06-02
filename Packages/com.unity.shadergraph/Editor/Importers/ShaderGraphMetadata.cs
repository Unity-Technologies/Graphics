using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static UnityEditor.ShaderGraph.CategoryDataCollection;
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

        public string tooltip;

        public override bool Equals(object obj)
        {
            if (obj is null || obj is not GraphInputData o)
                return false;

            if (subProperties is null != o.subProperties is null
                || referenceName is null != o.referenceName is null
                || tooltip is null != o.tooltip is null
                || referenceName is not null && !referenceName.Equals(o.referenceName)
                || !isKeyword.Equals(o.isKeyword)
                || !propertyType.Equals(o.propertyType)
                || !keywordType.Equals(o.keywordType)
                || !isCompoundProperty.Equals(o.isCompoundProperty)
                // || tooltip is not null && !tooltip.Equals(o.tooltip)
                || subProperties is not null && !subProperties.Count.Equals(o.subProperties.Count))
                return false;

            if (subProperties is not null)
            for (int i = 0; i < subProperties.Count; ++i)
                if (!subProperties[i].Equals(o.subProperties[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(referenceName);
            hash.Add(isKeyword);
            hash.Add(propertyType);
            hash.Add(keywordType);
            hash.Add(isCompoundProperty);
            // hash.Add(tooltip); This is not part of the graph inputs identity.

            if (subProperties is not null)
            foreach (var subproperty in subProperties)
            {
                hash.Add(subproperty.referenceName);
                hash.Add(subproperty.propertyType);
            }
            return hash.ToHashCode();
        }
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

        public static bool TryProcessInput(ShaderInput input, out GraphInputData data)
        {
            data = default;
            if (!input.isExposed)
                return false;

            string tooltip = "";
            if (input.promoteToFinalShader)
                tooltip = $"from {input.PromotedAssetPath}";

            switch(input)
            {
                case VirtualTextureShaderProperty vt:
                    data = ProcessVirtualTextureProperty(vt);
                    return true;
                case AbstractShaderProperty prop:
                    data = new GraphInputData() { referenceName = prop.referenceName, propertyType = prop.propertyType, isKeyword = false, tooltip = tooltip };
                    return true;
                case ShaderKeyword keyword:
                    var sanitizedReferenceName = keyword.referenceName;
                    if (keyword.keywordType == KeywordType.Boolean && keyword.referenceName.Contains("_ON"))
                        sanitizedReferenceName = sanitizedReferenceName.Replace("_ON", String.Empty);
                    data = new GraphInputData() { referenceName = sanitizedReferenceName, keywordType = keyword.keywordType, isKeyword = true, tooltip = tooltip };
                    return true;

                default:
                    return false;
            }
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

    internal class CategoryDataCollection
    {
        HashSet<(GraphInputData input, string category)> lookup = new();
        Dictionary<string, int> categoryScores = new();
        Dictionary<GraphInputData, int> propertyScores = new();

        internal void Set(string categoryName, GraphInputData data, int propertyScore = 0, int categoryScore = 0)
        {
            if (!categoryScores.TryAdd(categoryName, categoryScore) && categoryScore < categoryScores[categoryName])
                categoryScores[categoryName] = categoryScore;

            if (!propertyScores.TryAdd(data, propertyScore) && propertyScore < propertyScores[data])
                propertyScores[data] = propertyScore;

            // append the tooltip if it doesn't already exist.
            if (lookup.TryGetValue((data, categoryName), out var currentEntry) && !currentEntry.input.tooltip.Contains(data.tooltip))
            {
                lookup.Remove((data, categoryName));
                currentEntry.input.tooltip += $"\n{data.tooltip}";
                lookup.Add(currentEntry);
            }
            else lookup.Add((data, categoryName));
        }

        internal List<MinimalCategoryData> GenerateMCD()
        {
            var result = new List<MinimalCategoryData>();
            var buckets = new Dictionary<string, List<GraphInputData>>();

            var order = new List<string>(categoryScores.Keys);
            order.Sort((a, b) => {
                    var score = categoryScores[a].CompareTo(categoryScores[b]);
                    if (score == 0) score = a.CompareTo(b);
                    return score;
                });

            foreach(var data in lookup)
            {
                buckets.TryAdd(data.category, new());
                buckets[data.category].Add(data.input);
            }

            foreach(var name in order)
            {
                if (!buckets.ContainsKey(name))
                    continue;

                buckets[name].Sort((a, b) => {
                    var score = propertyScores[a].CompareTo(propertyScores[b]);
                    if (score == 0) score = a.referenceName.CompareTo(b.referenceName);
                    return score;
                });

                result.Add(new() { categoryName = name, propertyDatas = buckets[name] });
            }
            return result;
        }
    }
}
