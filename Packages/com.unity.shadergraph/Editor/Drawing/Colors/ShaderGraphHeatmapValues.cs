using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct HeatmapEntry
    {
        // Type name for C# nodes, subgraph guid for subgraphs. See HeatmapEntries.GetHeatmapKey.
        [SerializeField]
        internal string m_NodeName;

        [SerializeField]
        internal int m_Category;

        public HeatmapEntry(string nodeName, int category = 0)
        {
            m_NodeName = nodeName;
            m_Category = category;
        }
    }

    [Serializable]
    class HeatmapEntries
    {
        [SerializeField]
        List<HeatmapEntry> m_Entries = new();

        public List<HeatmapEntry> Entries => m_Entries;

        public bool TryGetCategory(string nodeTypeName, out int heatValue)
        {
            foreach (var entry in m_Entries)
            {
                if (nodeTypeName == entry.m_NodeName)
                {
                    heatValue = entry.m_Category;
                    return true;
                }
            }

            heatValue = 0;
            return false;
        }
    }

    class ShaderGraphHeatmapValues : ScriptableObject
    {
        const string k_PackageDefaultsPath = "Packages/com.unity.shadergraph/Editor/Resources/DefaultHeatmapValues.asset";

        [SerializeField]
        [Tooltip("Color scale used in the Heatmap color mode. Nodes can be assigned these values in the following sections.")]
        internal Color[] m_Colors = { Color.white };

        [SerializeField]
        HeatmapEntries m_Nodes = new();
        internal HeatmapEntries Nodes => m_Nodes;

        [SerializeField]
        HeatmapEntries m_Subgraphs = new();
        internal HeatmapEntries Subgraphs => m_Subgraphs;

        [MenuItem("Assets/Create/Shader Graph/Custom Heatmap Values/Heatmap with Default Values", priority = CoreUtils.Sections.section4)]
        static void CreateFromPackageDefaults()
        {
            var packageDefaults = GetPackageDefault();
            var asset = CreateInstance<ShaderGraphHeatmapValues>();

            asset.m_Colors = new Color[packageDefaults.m_Colors.Length];
            Array.Copy(packageDefaults.m_Colors, asset.m_Colors, packageDefaults.m_Colors.Length);

            asset.m_Nodes.Entries.Clear();
            asset.m_Nodes.Entries.AddRange(packageDefaults.m_Nodes.Entries);

            asset.m_Subgraphs.Entries.Clear();
            asset.m_Subgraphs.Entries.AddRange(packageDefaults.m_Subgraphs.Entries);

            ProjectWindowUtil.CreateAsset(asset, "New Shader Graph Heatmap Values.asset");
        }

        [MenuItem("Assets/Create/Shader Graph/Custom Heatmap Values/Empty Heatmap", priority = CoreUtils.Sections.section4 + 1)]
        static void CreateEmpty()
        {
            var asset = CreateInstance<ShaderGraphHeatmapValues>();
            asset.PopulateNodesFromProject();

            ProjectWindowUtil.CreateAsset(asset, "New Shader Graph Heatmap Values.asset");
        }

        internal static IEnumerable<AbstractMaterialNode> GetApplicableNodes()
        {
            foreach (var type in NodeClassCache.knownNodeTypes)
            {
                if (!type.IsClass
                    || type.IsAbstract
                    || type == typeof(PropertyNode)
                    || type == typeof(KeywordNode)
                    || type == typeof(DropdownNode)
                    || type == typeof(SubGraphNode))
                {
                    continue;
                }

                var instance = (AbstractMaterialNode)Activator.CreateInstance(type);
                if (!instance.ExposeToSearcher ||
                    NodeClassCache.GetAttributeOnNodeType<TitleAttribute>(type) is null)
                {
                    continue;
                }

                yield return instance;
            }
        }

        internal static string GetHeatmapKey(AbstractMaterialNode node)
        {
            return node is SubGraphNode subGraph ? subGraph.subGraphGuid : node.GetType().Name;
        }

        public bool TryGetCategoryColor(AbstractMaterialNode node, out Color color)
        {
            if (m_Colors.Length > 0)
            {
                var entries = node is SubGraphNode ? m_Subgraphs : m_Nodes;
                if (entries.TryGetCategory(GetHeatmapKey(node), out var heat))
                {
                    color = m_Colors[Math.Clamp(heat, 0, m_Colors.Length - 1)];
                    color.a = 1.0f;
                    return true;
                }
            }

            color = default;
            return false;
        }

        public void PopulateNodesFromProject()
        {
            var alreadyAdded = new HashSet<string>();
            var nodeEntries = m_Nodes.Entries;

            foreach (var entry in nodeEntries)
            {
                alreadyAdded.Add(entry.m_NodeName);
            }

            foreach (var node in GetApplicableNodes())
            {
                var nodeName = node.GetType().Name;
                if (alreadyAdded.Contains(nodeName))
                {
                    continue;
                }

                nodeEntries.Add(new HeatmapEntry(nodeName));
            }

            nodeEntries.Sort((a, b) => string.Compare(a.m_NodeName, b.m_NodeName, StringComparison.Ordinal));
        }

        public bool ContainsAllApplicableNodes()
        {
            foreach (var node in GetApplicableNodes())
            {
                if (!TryGetCategoryColor(node, out _))
                {
                    return false;
                }
            }

            return true;
        }

        void Reset()
        {
            PopulateNodesFromProject();
        }

        static ShaderGraphHeatmapValues s_PackageDefaultHeatmapValues;

        internal static ShaderGraphHeatmapValues GetPackageDefault()
        {
            if (s_PackageDefaultHeatmapValues == null)
            {
                s_PackageDefaultHeatmapValues = AssetDatabase.LoadAssetAtPath<ShaderGraphHeatmapValues>(k_PackageDefaultsPath);
            }

            return s_PackageDefaultHeatmapValues != null ? s_PackageDefaultHeatmapValues : CreateInstance<ShaderGraphHeatmapValues>();
        }
    }
}
