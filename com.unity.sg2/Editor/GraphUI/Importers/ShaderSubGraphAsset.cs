using System;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ShaderSubGraphAsset : ScriptableObject
    {
        public string GraphJSON;
        public string ViewModelJSON;

        public static GraphHandler CreateBlankSubGraphHandler()
        {
            var reg = ShaderGraphRegistryBuilder.CreateDefaultRegistry();
            var graph = new GraphHandler(reg);
            return graph;
        }

        private static ShaderGraphAssetModel CreateBlankAssetGraph()
        {
            var model = CreateInstance<ShaderGraphAssetModel>();
            model.name = "View";
            model.CreateGraph(typeof(ShaderGraphStencil));
            return model;
        }

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

        public static void HandleSave(string path, ShaderGraphAssetModel model)
        {
            Debug.Log("Save subgraph");
            var asset = CreateInstance<ShaderSubGraphAsset>();
            asset.GraphJSON = model.GraphHandler.ToSerializedFormat();
            asset.ViewModelJSON = EditorJsonUtility.ToJson(model);
            var json = EditorJsonUtility.ToJson(asset, true);
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path);
        }

        public static void HandleCreate(string path)
        {
            GraphHandler graph = CreateBlankSubGraphHandler();
            var model = CreateBlankAssetGraph();
            model.Init(graph, isSubGraph: true);

            HandleSave(path, model);
        }

        public static ShaderGraphAssetModel HandleLoad(string path)
        {
            AssetDatabase.ImportAsset(path);
            var assetModel = AssetDatabase.LoadAssetAtPath(path, typeof(ShaderGraphAssetModel)) as ShaderGraphAssetModel;
            assetModel.Init(isSubGraph: true); // trust that we'll find the GraphHandler through our OnEnable...
            return assetModel;
        }

        public static void HandleImport(AssetImportContext ctx)
        {
            // Deserialize the json box
            string path = ctx.assetPath;
            string json = File.ReadAllText(path, Encoding.UTF8);
            var asset = CreateInstance<ShaderSubGraphAsset>();
            EditorJsonUtility.FromJsonOverwrite(json, asset);

            // create initialize objects and copy serialized state
            var model = CreateBlankAssetGraph();
            EditorJsonUtility.FromJsonOverwrite(asset.ViewModelJSON, model);

            // explicit reinitialize with the graphHandler here, but otherwise OnEnable should pull from the asset.
            var graph = asset.ResolveGraph();
            model.Init(graph, isSubGraph: true);

            // build shader and setup supplementary assets
            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon");

            ctx.AddObjectToAsset("AssetHelper", asset, texture); // so we can resolve GraphHandler in OnEnable
            ctx.SetMainObject(asset);
            ctx.AddObjectToAsset("View", model);
        }

    }
}
