using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph
{
    public static class ShaderGraphAssetUtils
    {
        public static ShaderGraphAsset CreateNewAssetGraph(bool isSubGraph)
        {
            var defaultRegistry = ShaderGraphRegistry.Instance.Registry;
            var contextKey = Registry.ResolveKey<Defs.ShaderGraphContext>();
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            GraphHandler graph = new(defaultRegistry);
            graph.AddContextNode(propertyKey.Name);
            graph.AddContextNode(contextKey);
            graph.ReconcretizeAll();

            var asset = ScriptableObject.CreateInstance<ShaderGraphAsset>();
            asset.CreateGraph(typeof(ShaderGraphStencil));
            asset.ShaderGraphModel.Init(graph, isSubGraph);
            return asset;
        }

        public static void HandleSave(string path, ShaderGraphAsset asset)
        {
            var json = EditorJsonUtility.ToJson(asset, true);
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path); // Is this necessary?
        }

        public static ShaderGraphAsset HandleLoad(string path)
        {
            AssetDatabase.ImportAsset(path);
            var asset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path);
            return asset;
        }

        public static void HandleCreate(string path, bool isSubGraph = false) // TODO: TargetSettingsObject as param
        {
            HandleSave(path, CreateNewAssetGraph(isSubGraph));
        }

        public static void HandleImport(AssetImportContext ctx)
        {
            // Deserialize the json box
            string path = ctx.assetPath;
            string json = File.ReadAllText(path, Encoding.UTF8);
            var asset = ScriptableObject.CreateInstance<ShaderGraphAsset>();
            EditorJsonUtility.FromJsonOverwrite(json, asset);
            asset.ShaderGraphModel.OnEnable();

            if (!asset.ShaderGraphModel.IsSubGraph)
            {
                // build shader and setup supplementary assets
                var key = Registry.ResolveKey<Defs.ShaderGraphContext>();
                var node = asset.ShaderGraphModel.GraphHandler.GetNode(key.Name);
                string shaderCode = Interpreter.GetShaderForNode(node, asset.ShaderGraphModel.GraphHandler, asset.ShaderGraphModel.GraphHandler.registry, out var defaultTextures);
                var shader = ShaderUtil.CreateShaderAsset(ctx, shaderCode, false);
                Material mat = new(shader);
                foreach (var def in defaultTextures)
                {
                    mat.SetTexture(def.Item1, def.Item2);
                }
                Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon");

                ctx.AddObjectToAsset("Shader", shader, texture);
                ctx.SetMainObject(shader);
                ctx.AddObjectToAsset("Material", mat);
                ctx.AddObjectToAsset("Asset", asset);
            }
            else // is subgraph
            {
                Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon");

                ctx.AddObjectToAsset("Asset", asset, texture);
                ctx.SetMainObject(asset);

                var assetID = AssetDatabase.GUIDFromAssetPath(ctx.assetPath).ToString();
                var fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

                List<Defs.ParameterUIDescriptor> paramDesc = new();
                foreach (var dec in asset.ShaderGraphModel.VariableDeclarations)
                {
                    var displayName = dec.GetVariableName();
                    var identifierName = ((BaseShaderGraphConstant)dec.InitializationModel).PortName;
                    paramDesc.Add(new Defs.ParameterUIDescriptor(identifierName, displayName));
                }

                Defs.NodeUIDescriptor desc = new(
                        version: 1,
                        name: assetID,
                        tooltip: "TODO: This should come from the SubGraphModel",
                        categories: new string[] { "SubGraphs" },
                        synonyms: new string[] { "SubGraph" },
                        displayName: fileName,
                        hasPreview: true,
                        parameters: paramDesc.ToArray()
                    );

                RegistryKey key = new RegistryKey { Name = assetID, Version = 1 };
                var nodeBuilder = new Defs.SubGraphNodeBuilder(key, asset.ShaderGraphModel.GraphHandler);
                var nodeUI = new StaticNodeUIDescriptorBuilder(desc);

                ShaderGraphRegistry.Instance.Registry.Unregister(key);
                ShaderGraphRegistry.Instance.Register(nodeBuilder, nodeUI);
            }
        }

        public static string[] GatherDependenciesForShaderGraphAsset(string assetPath)
        {
            string json = File.ReadAllText(assetPath, Encoding.UTF8);
            var asset = ScriptableObject.CreateInstance<ShaderGraphAsset>();
            EditorJsonUtility.FromJsonOverwrite(json, asset);
            asset.ShaderGraphModel.OnEnable();



            SortedSet<string> deps = new();
            var graph = asset.ShaderGraphModel.GraphHandler;

            foreach(var node in graph.GetNodes())
            {
                // Subgraphs use their assetID as a registryKey for now-> this is bad and should be handled gracefully in the UI for a user to set in a safe way.
                // TODO: make it so any node can be asked about its asset dependencies (Either through the builder, or through a field).
                var depPath = AssetDatabase.GUIDToAssetPath(node.GetRegistryKey().Name);
                if (!String.IsNullOrEmpty(depPath))
                    deps.Add(depPath);
            }

            return deps.ToArray();
        }
    }

    [Serializable]
    internal class SerializableGraphHandler : ISerializationCallbackReceiver
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

        public GraphHandler Graph => m_graph;

        public void OnBeforeSerialize()
        {
            // Cloning node models (i.e. GTFs model of cloning a scriptable object,
            // triggers a serialize on the cloned node before it has a graph handler reference
            if (m_graph == null)
                return;
            json = m_graph.ToSerializedFormat();
        }

        public void OnAfterDeserialize() { }

        public void OnEnable()
        {
            var reg = ShaderGraphRegistry.Instance.Registry;
            m_graph = GraphHandler.FromSerializedFormat(json, reg);
            m_graph.ReconcretizeAll();
        }
    }

    [Serializable]
    internal class SerializableTargetSettings : ISerializationCallbackReceiver
    {
        [SerializeField]
        string json = "";

        [NonSerialized]
        TargetSettingsObject m_tso = new();

        class TargetSettingsObject : JsonObject
        {
            [SerializeField]
            public List<JsonData<Target>> m_GraphTargets = new();
        }

        public List<JsonData<Target>> Targets => m_tso.m_GraphTargets;

        public void OnBeforeSerialize()
        {
            json = MultiJson.Serialize(m_tso);
        }

        public void OnAfterDeserialize() { }

        public void OnEnable()
        {
            MultiJson.Deserialize(m_tso, json);
        }
    }
}
