using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphAssetUtils
    {

        public static readonly string kMainEntryContextName = Registry.ResolveKey<Defs.ShaderGraphContext>().Name;
        internal static void RebuildContextNodes(GraphHandler graph, Target target)
        {
            // This should be consistent for all legacy targets.
            graph.RebuildContextData("VertIn", target, "UniversalPipeline", "VertexDescription", true);
            graph.RebuildContextData("VertOut", target, "UniversalPipeline", "VertexDescription", false);
            graph.RebuildContextData("FragIn", target, "UniversalPipeline", "SurfaceDescription", true);
            graph.RebuildContextData(kMainEntryContextName, target, "UniversalPipeline", "SurfaceDescription", false);
        }

        // TODO: Factory for different types of initial creations- this is modified to use URP for now.
        // TODO: Context object for asset? containing path, target, other info
        internal static ShaderGraphAsset CreateNewAssetGraph(string assetPath, LegacyTargetType legacyTargetType = LegacyTargetType.Blank)
        {
            // Create template, provide parameters and info. for instantiating the asset later on
            var graphTemplate = new ShaderGraphTemplate(false, legacyTargetType);

            // Create asset based on the template
            var asset = (ShaderGraphAsset)GraphAssetCreationHelpers.CreateGraphAsset(typeof(ShaderGraphAsset), typeof(ShaderGraphStencil), String.Empty, assetPath, graphTemplate);
            return asset;
        }

        internal static ShaderGraphAsset CreateNewSubGraph(string assetPath)
        {
            throw new NotImplementedException();
        }

        public static ShaderGraphAsset HandleLoad(string path)
        {
            AssetDatabase.ImportAsset(path);
            var asset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path);
            return asset;
        }

        public static void HandleImportAssetGraph(AssetImportContext ctx)
        {
            // Deserialize the json box
            string path = ctx.assetPath;
            string json = File.ReadAllText(path, Encoding.UTF8);
            var asset = ShaderGraphTemplate.CreateInMemoryGraphFromTemplate(new ShaderGraphTemplate(true, LegacyTargetType.Blank));
            EditorJsonUtility.FromJsonOverwrite(json, asset);

            // Although name gets set during asset's OnEnable, it can get clobbered during deserialize
            asset.Name = Path.GetFileNameWithoutExtension(path);
            var sgModel = asset.ShaderGraphModel;
            sgModel.OnEnable();
            var graphHandler = asset.CLDSModel;

            // TODO: SGModel should know what it's entry point is for creating a shader.
            var node = graphHandler.GetNode(kMainEntryContextName);
            var shaderCode = Interpreter.GetShaderForNode(node, graphHandler, graphHandler.registry, out var defaultTextures, sgModel.ActiveTarget, sgModel.ShaderName);

            var shader = ShaderUtil.CreateShaderAsset(ctx, shaderCode, false);
            Material mat = new(shader) { name = "Material/" + asset.Name };
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

        public static void HandleImportSubGraph(AssetImportContext ctx)
        {
            // Deserialize the json box
            string path = ctx.assetPath;
            string json = File.ReadAllText(path, Encoding.UTF8);
            var asset = ShaderGraphTemplate.CreateInMemoryGraphFromTemplate(new ShaderGraphTemplate(false, LegacyTargetType.Blank));
            EditorJsonUtility.FromJsonOverwrite(json, asset);

            // Although name gets set during asset's OnEnable, it can get clobbered during deserialize
            asset.Name = Path.GetFileNameWithoutExtension(path);
            var sgModel = asset.ShaderGraphModel;
            sgModel.OnEnable();
            var graphHandler = asset.CLDSModel;

            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon");

            ctx.AddObjectToAsset("Asset", asset, texture);
            ctx.SetMainObject(asset);

            var assetID = AssetDatabase.GUIDFromAssetPath(ctx.assetPath).ToString();
            var fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            List<Defs.ParameterUIDescriptor> paramDesc = new();
            foreach (var dec in sgModel.VariableDeclarations)
            {
                var displayName = dec.GetVariableName();
                var identifierName = ((BaseShaderGraphConstant)dec.InitializationModel).PortName;
                paramDesc.Add(new Defs.ParameterUIDescriptor(identifierName, displayName));
            }

            Defs.NodeUIDescriptor desc = new(
                version: 1,
                name: assetID,
                tooltip: "TODO: This should come from the SubGraphModel",
                category: "SubGraphs",
                synonyms: new string[] { "SubGraph" },
                displayName: fileName,
                hasPreview: true,
                parameters: paramDesc.ToArray()
            );

            RegistryKey key = new RegistryKey { Name = assetID, Version = 1 };
            var nodeBuilder = new Defs.SubGraphNodeBuilder(key, graphHandler);
            var nodeUI = new StaticNodeUIDescriptorBuilder(desc);

            ShaderGraphRegistry.Instance.Registry.Unregister(key);
            ShaderGraphRegistry.Instance.Register(nodeBuilder, nodeUI);

        }

        public static string[] GatherDependenciesForShaderGraphAsset(string assetPath)
        {
            string json = File.ReadAllText(assetPath, Encoding.UTF8);
            var asset = ScriptableObject.CreateInstance<ShaderGraphAsset>();
            EditorJsonUtility.FromJsonOverwrite(json, asset);

            SortedSet<string> deps = new();
            var graph = asset.CLDSModel;

            foreach (var node in graph.GetNodes())
            {
                // Subgraphs use their assetID as a registryKey for now-> this is bad and should be handled gracefully in the UI for a user to set in a safe way.
                // TODO: make it so any node can be asked about its asset dependencies (Either through the builder, or through a field).
                var depPath = AssetDatabase.GUIDToAssetPath(node.GetRegistryKey().Name);
                if (!String.IsNullOrEmpty(depPath))
                    deps.Add(depPath);
            }

            return deps.ToArray();
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
}
