using System;
using System.IO;
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
        public ShaderGraphAssetModel ViewModel;

        private static GraphHandler CreateBlankSubGraphHandler()
        {
            var reg = ShaderGraphRegistryBuilder.CreateDefaultRegistry();
            var graph = new GraphHandler(reg);
            return graph;
        }

        private static ShaderGraphAssetModel CreateBlankAssetGraph()
        {
            var model = CreateInstance<ShaderGraphAssetModel>();
            model.name = "View";
            model.CreateGraph("ShaderSubgraph", typeof(ShaderGraphStencil));
            return model;
        }

        public static void HandleSave(string path, ShaderGraphAssetModel model)
        {
            var asset = CreateInstance<ShaderSubGraphAsset>();
            asset.GraphJSON = model.GraphHandler.ToSerializedFormat();
            asset.ViewModelJSON = EditorJsonUtility.ToJson(model);
            asset.ViewModel = model;
            AssetDatabase.CreateAsset(asset, path);
        }

        public static void HandleCreate(string path)
        {
            GraphHandler graph = CreateBlankSubGraphHandler();
            var model = CreateBlankAssetGraph();
            model.Init(graph);

            HandleSave(path, model);
        }

        public static ShaderGraphAssetModel HandleLoad(string path)
        {
            AssetDatabase.ImportAsset(path);
            var asset = AssetDatabase.LoadAssetAtPath(path, typeof(ShaderSubGraphAsset)) as ShaderSubGraphAsset;
            var assetModel = asset.ViewModel;
            assetModel.Init(); // trust that we'll find the GraphHandler through our OnEnable...
            return assetModel;
        }


    }
}
