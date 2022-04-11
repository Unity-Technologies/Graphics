using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(menuName = "Testing/Static Analysis Tests")]
public class EditorShaderStaticAnalysisAsset : ScriptableObject, ISerializationCallbackReceiver
{
    [Serializable]
    public struct ProcessAssetDefinition
    {
        public string assetAlias;
        public string assetCategory;
        public string testName;
        public Object asset;
        public Filter filter;
        public BuildTarget[] includeInTargets;
    }

    [Serializable]
    public enum FilterType
    {
        None,
        Definition,
        Reference,
    }

    [Serializable]
    public struct Filter
    {
        public FilterType filterType;
        public string referenceName;
        public FilterDefinition definition;

        [Pure]
        public bool Resolve(Dictionary<string, FilterDefinition> filters,
            out FilterDefinition filter,
            out string errorMessage)
        {
            switch (filterType)
            {
                case FilterType.None:
                    filter = default;
                    errorMessage = null;
                    return true;
                case FilterType.Definition:
                    filter = definition;
                    errorMessage = null;
                    return true;
                case FilterType.Reference:
                    if (filters.TryGetValue(referenceName, out filter))
                    {
                        errorMessage = null;
                        return true;
                    }
                    else
                    {
                        errorMessage = $"Missing filter {referenceName}";
                        return false;
                    }
                default: throw new ArgumentOutOfRangeException(nameof(filterType));
            }
        }
    }

    [Serializable]
    public struct FilterDefinition
    {
        public string name;
        public string category;
        public string keywordFilter;
        public string passNameFilter;
    }

    [SerializeField]
    List<FilterDefinition> m_Filters = new List<FilterDefinition>();
    [SerializeField]
    List<ProcessAssetDefinition> m_AssetDefinitions = new List<ProcessAssetDefinition>();
    [SerializeField]
    float m_StaticAnalysisTimeout = 600;

    Dictionary<string, FilterDefinition> m_FilterDefinitions = new Dictionary<string, FilterDefinition>();

    public Dictionary<string, FilterDefinition> filters => m_FilterDefinitions;
    public IList<ProcessAssetDefinition> processAssetDefinitions => m_AssetDefinitions;
    public float staticAnalysisTimeout => m_StaticAnalysisTimeout;

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {
        m_FilterDefinitions.Clear();
        foreach (var filter in m_Filters)
            m_FilterDefinitions[filter.name] = filter;
    }
}
