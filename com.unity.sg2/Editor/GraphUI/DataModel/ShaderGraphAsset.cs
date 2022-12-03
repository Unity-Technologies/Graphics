using System;
using System.IO;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Configuration;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    enum ShaderGraphType
    {
        AssetGraph,
        SubGraph
    }

    struct AssetContextObject
    {
        ShaderGraphType m_ShaderGraphType;
        ITargetProvider m_TargetProvider;
        GraphTemplate m_GraphTemplate;
    }

    [Serializable]
    internal class SerializableGraphHandler
    {
        [SerializeField]
        string json = "";

        [NonSerialized]
        GraphHandler m_graph;

        // Provide a previously initialized graphHandler-- round-trip it for ownership.
        public void Init(GraphHandler value)
        {
            json = value.ToSerializedFormat();
            var reg = ShaderGraphRegistry.Instance.Registry; // TODO: Singleton?
            m_graph = GraphHandler.FromSerializedFormat(json, reg);
            m_graph.ReconcretizeAll();
        }

        public void SaveGraph()
        {
            // Cloning node models (i.e. GTFs model of cloning a scriptable object,
            // triggers a serialize on the cloned node before it has a graph handler reference
            if (m_graph == null)
                return;
            json = m_graph.ToSerializedFormat();
        }

        public GraphHandler Graph => m_graph;

        public void OnEnable(bool reconcretize = true)
        {
            var reg = ShaderGraphRegistry.Instance.Registry;
            m_graph = GraphHandler.FromSerializedFormat(json, reg);
            if (reconcretize)
            {
                m_graph.ReconcretizeAll();
            }
        }
    }

    class ShaderGraphAsset : GraphAsset
    {
        // TODO: Do we want to keep this here or do we want a separate subgraph class?
        public bool IsSubgraph { get; set; }

        protected override Type GraphModelType => typeof(ShaderGraphModel);
        public ShaderGraphModel ShaderGraphModel => GraphModel as ShaderGraphModel;

        // In theory we want to initialize the ShaderGraphModel after the CLDSModel, though any dependency there should be loose at best
        public GraphHandler CLDSModel => graphHandlerBox.Graph;

        [SerializeField]
        SerializableGraphHandler graphHandlerBox;

        [SerializeField]
        LegacyTargetType m_TargetType;

        public LegacyTargetType TargetType => m_TargetType;

        ShaderGraphType m_ShaderGraphType;

        string m_AssetPath;

        public static readonly string kBlackboardContextName = Registry.ResolveKey<PropertyContext>().Name;

        ShaderGraphAsset()
        {
            graphHandlerBox = new();
            m_AssetPath = String.Empty;
        }

        protected override void OnEnable()
        {
            graphHandlerBox.OnEnable();
            Name = Path.GetFileNameWithoutExtension(FilePath);
            base.OnEnable();
        }

        public void Initialize(LegacyTargetType legacyTargetType)
        {
            m_TargetType = legacyTargetType;
            var defaultRegistry = ShaderGraphRegistry.Instance.Registry;
            GraphHandler graphHandler = new(defaultRegistry);
            graphHandler.AddContextNode(kBlackboardContextName);
            if (m_TargetType == LegacyTargetType.Blank) // blank shadergraph gets the fallback context node for output.
            {
                graphHandler.AddContextNode(Registry.ResolveKey<Defs.ShaderGraphContext>());
            }
            else // otherwise we are a URP graph.
            {
                // Conventional shadergraphs with targets will always have these context nodes.
                graphHandler.AddContextNode("VertIn");
                graphHandler.AddContextNode("VertOut");
                graphHandler.AddContextNode("FragIn");
                graphHandler.AddContextNode(ShaderGraphAssetUtils.kMainEntryContextName);

                // Though we should be more procedural and be using this: to get the corresponding names, eg:
                // CPGraphDataProvider.GatherProviderCPIO(target, out var descriptors);
            }

            graphHandler.ReconcretizeAll();

            graphHandlerBox.Init(graphHandler);
        }

        public override string CreateFile(string path, bool overwriteIfExists)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (!overwriteIfExists)
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);
            }

            m_AssetPath = path;

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            if (File.Exists(path))
                AssetDatabase.DeleteAsset(path);

            var json = EditorJsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
            return path;
        }

        public override GraphAsset Import()
        {
            return this;
        }

        public override void Save()
        {
            m_AssetPath = m_AssetPath == string.Empty ? FilePath : m_AssetPath;
            graphHandlerBox.SaveGraph();
            var json = EditorJsonUtility.ToJson(this, true);
            File.WriteAllText(m_AssetPath, json);
            AssetDatabase.ImportAsset(m_AssetPath);
            EditorUtility.ClearDirty(this);
        }
    }
}
