using System;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;


namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ShaderGraphAsset : ScriptableObject
    {
        public string GraphJSON;
        public string ViewModelJSON;

        [Serializable]
        public struct Edge { public string srcNode, srcPort, dstNode, dstPort; }


        public GraphHandler ResolveGraph()
        {
            var reg = ShaderGraphRegistryBuilder.CreateDefaultRegistry();
            var graph = GraphHandler.FromSerializedFormat(GraphJSON, reg);

            //graph.ReconcretizeAll(reg);
            //foreach (var edge in edges)
            //    graph.TryConnect(edge.srcNode, edge.srcPort, edge.dstNode, edge.dstPort, reg);
            graph.ReconcretizeAll();
            return graph;
        }
        private static ShaderGraphAssetModel CreateBlankAssetGraph()
        {
            var model = CreateInstance<ShaderGraphAssetModel>();
            model.name = "View";
            model.CreateGraph(typeof(ShaderGraphStencil));
            return model;
        }
        public static GraphHandler CreateBlankGraphHandler()
        {
            var defaultRegistry = ShaderGraphRegistryBuilder.CreateDefaultRegistry();
            var contextKey = Registry.ResolveKey<ShaderGraphContext>();
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            GraphHandler graph = new ();
            graph.AddContextNode(propertyKey, defaultRegistry);
            graph.AddContextNode(contextKey, defaultRegistry);
            return graph;
        }


        public static void HandleSave(string path, ShaderGraphAssetModel model)
        {
            var asset = CreateInstance<ShaderGraphAsset>();
            asset.GraphJSON = model.GraphHandler.ToSerializedFormat();
            asset.ViewModelJSON = EditorJsonUtility.ToJson(model);
            var json = EditorJsonUtility.ToJson(asset, true);
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path);
        }
        public static void HandleCreate(string path)
        {
            GraphHandler graph = CreateBlankGraphHandler();
            var model = CreateBlankAssetGraph();
            model.Init(graph);

            HandleSave(path, model);
        }
        public static void HandleImport(AssetImportContext ctx)
        {
            // Deserialize the json box
            string path = ctx.assetPath;
            string json = File.ReadAllText(path, Encoding.UTF8);
            var asset = CreateInstance<ShaderGraphAsset>();
            EditorJsonUtility.FromJsonOverwrite(json, asset);

            // create initialize objects and copy serialized state
            var model = CreateBlankAssetGraph();
            EditorJsonUtility.FromJsonOverwrite(asset.ViewModelJSON, model);

            // explicit reinitialize with the graphHandler here, but otherwise OnEnable should pull from the asset.
            var graph = asset.ResolveGraph();
            model.Init(graph);

            // build shader and setup supplementary assets
            var reg = ShaderGraphRegistryBuilder.CreateDefaultRegistry();
            var key = Registry.ResolveKey<ShaderGraphContext>();
            var node = model.ShaderGraphModel.GraphHandler.GetNodeReader(key.Name);
            string shaderCode = Interpreter.GetShaderForNode(node, graph, reg, out var defaultTextures);
            var shader = ShaderUtil.CreateShaderAsset(ctx, shaderCode, false);
            Material mat = new (shader);
            foreach (var def in defaultTextures)
            {
                mat.SetTexture(def.Item1, def.Item2);
            }
            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon");

            ctx.AddObjectToAsset("MainAsset", shader, texture);
            ctx.SetMainObject(shader);
            ctx.AddObjectToAsset("View", model);
            ctx.AddObjectToAsset("Material", mat);
            ctx.AddObjectToAsset("AssetHelper", asset); // so we can resolve GraphHandler in OnEnable
        }
        public static ShaderGraphAssetModel HandleLoad(string path)
        {
            AssetDatabase.ImportAsset(path);
            var assetModel = AssetDatabase.LoadAssetAtPath(path, typeof(ShaderGraphAssetModel)) as ShaderGraphAssetModel;
            assetModel.Init(); // trust that we'll find the GraphHandler through our OnEnable...
            return assetModel;
        }
    }
}
