using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;


namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ShaderGraphAsset : ScriptableObject
    {
        [SerializeField] public string GraphJSON;
        [SerializeField] public string ViewModelJSON;
        //[SerializeField] public List<Edge> edges = new();

        [Serializable]
        public struct Edge { public string srcNode, srcPort, dstNode, dstPort; }


        public GraphHandler ResolveGraph()
        {
            var graph = GraphHandler.FromSerializedFormat(GraphJSON);
            var reg = Registry.Default.DefaultRegistry.CreateDefaultRegistry();

            //graph.ReconcretizeAll(reg);
            //foreach (var edge in edges)
            //    graph.TryConnect(edge.srcNode, edge.srcPort, edge.dstNode, edge.dstPort, reg);
            graph.ReconcretizeAll(reg);
            return graph;
        }
        private static ShaderGraphAssetModel CreateBlankAssetGraph()
        {
            var model = CreateInstance<ShaderGraphAssetModel>();
            model.name = "View";
            model.CreateGraph("foo", typeof(ShaderGraphStencil));
            return model;
        }
        public static GraphHandler CreateBlankGraphHandler()
        {
            var defaultRegistry = Registry.Default.DefaultRegistry.CreateDefaultRegistry();
            var contextKey = Registry.Registry.ResolveKey<Registry.Default.DefaultContext>();
            GraphHandler graph = new GraphHandler();
            graph.AddContextNode(contextKey, defaultRegistry);
            return graph;
        }


        public static void HandleSave(string path, ShaderGraphAssetModel model)
        {
            var asset = CreateInstance<ShaderGraphAsset>();
            asset.GraphJSON = model.GraphHandler.ToSerializedFormat();
            asset.ViewModelJSON = EditorJsonUtility.ToJson(model);
            //foreach (var em in model.ShaderGraphModel.EdgeModels)
            //{
            //    var edge = new Edge
            //    {
            //        srcNode = ((GraphDataNodeModel)em.FromPort.NodeModel).graphDataName,
            //        srcPort = ((GraphDataPortModel)em.FromPort).graphDataName,
            //        dstNode = ((GraphDataNodeModel)em.ToPort.NodeModel).graphDataName,
            //        dstPort = ((GraphDataPortModel)em.ToPort).graphDataName
            //    };
            //    asset.edges.Add(edge);
            //}
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
            var reg = Registry.Default.DefaultRegistry.CreateDefaultRegistry();
            var key = Registry.Registry.ResolveKey<Registry.Default.DefaultContext>();
            var node = model.ShaderGraphModel.GraphHandler.GetNodeReader(key.Name);
            string shaderCode = Interpreter.GetShaderForNode(node, graph, reg);
            var shader = ShaderUtil.CreateShaderAsset(ctx, shaderCode, false);
            Material mat = new Material(shader);
            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon");

            ctx.AddObjectToAsset("MainAsset", shader, texture);
            ctx.SetMainObject(shader);
            ctx.AddObjectToAsset("View", model);
            ctx.AddObjectToAsset("Material", mat);
            ctx.AddObjectToAsset("AssetHelper", asset); // so we can resolve GraphHandler in OnEnable
        }
        public static ShaderGraphAssetModel HandleLoad(string path)
        {
            var assetModel = AssetDatabase.LoadAssetAtPath(path, typeof(ShaderGraphAssetModel)) as ShaderGraphAssetModel;
            assetModel.Init(); // trust that we'll find the GraphHandler through our OnEnable...
            return assetModel;
        }
    }
}
