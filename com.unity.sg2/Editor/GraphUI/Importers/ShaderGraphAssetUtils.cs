using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Defs;

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
            graph.AddContextNode(propertyKey);
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
            var asset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path);
            return asset;
        }

        // TODO: TargetSettingsObject as param, so that we can initialize with a target.
        public static void HandleCreate(string path, bool isSubGraph = false)
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
                Material mat = new (shader);
                foreach (var def in defaultTextures)
                {
                    mat.SetTexture(def.Item1, def.Item2);
                }
                Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon");

                ctx.AddObjectToAsset("Shader", shader, texture);
                ctx.SetMainObject(shader);
                ctx.AddObjectToAsset("Material", mat);
                ctx.AddObjectToAsset("Data", asset);
            }
            else // is subgraph
            {
                Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon");

                ctx.AddObjectToAsset("Data", asset, texture);
                ctx.SetMainObject(asset);

                var name = Path.GetFileNameWithoutExtension(ctx.assetPath);
                var key = new RegistryKey { Name = AssetDatabase.AssetPathToGUID(ctx.assetPath), Version = 1 };

                var desc = new NodeUIDescriptor(
                    key.Version,
                    key.Name,
                    "DEFAULT_TOOLTIP",
                    new string[] { "SubGraph" },
                    new string[] { "SubGraph" },
                    name,
                    true,
                    new Dictionary<string, string> { },
                    new ParameterUIDescriptor[] { }
                );

                ShaderGraphRegistry.Instance.RefreshSubGraph(key, asset.ShaderGraphModel.GraphHandler, desc);
            }
        }
    }

    [Serializable]
    internal class SerializableGraphHandler : ISerializationCallbackReceiver
    {
        [SerializeField]
        string json = "";

        [NonSerialized]
        GraphHandler m_graph;

        public void Init(GraphHandler value)
        {
            json = value.ToSerializedFormat();
            var reg = ShaderGraphRegistry.Instance.Registry;
            m_graph = GraphHandler.FromSerializedFormat(json, reg);
            m_graph.ReconcretizeAll();
        }

        public GraphHandler Graph => m_graph;

        public void OnBeforeSerialize()
        {
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

        // TODO: Init method for providing initial state (see HandleCreate).

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
