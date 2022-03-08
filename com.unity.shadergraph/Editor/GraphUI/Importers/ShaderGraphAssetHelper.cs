using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ShaderGraphAssetHelper : ScriptableObject
    {
        public string GraphDeltaJSON;
        public string GTFJSON;

        public static ShaderGraphAssetHelper MakeDefault()
        {
            ShaderGraphAssetHelper assetHelper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            var defaultRegistry = Registry.Default.DefaultRegistry.CreateDefaultRegistry();
            var contextKey = Registry.Registry.ResolveKey< Registry.Default.DefaultContext>();

            GraphHandler graph = new GraphHandler();
            graph.AddContextNode(contextKey, defaultRegistry);

            assetHelper.GraphDeltaJSON = graph.ToSerializedFormat();
            return assetHelper;
        }

        public static ShaderGraphAssetHelper MakeFromModel(ShaderGraphAssetModel assetModel)
        {
            var assetHelper = ShaderGraphAssetHelper.MakeDefault();
            assetHelper.GTFJSON = EditorJsonUtility.ToJson(assetModel);
            return assetHelper;
        }

        public static ShaderGraphAssetModel GetModel(ShaderGraphAssetHelper assetHelper)
        {
            ShaderGraphAssetModel model = ScriptableObject.CreateInstance<ShaderGraphAssetModel>();
            model.CreateGraph("foo", typeof(ShaderGraphStencil));
            EditorJsonUtility.FromJsonOverwrite(assetHelper.GTFJSON, model);

            GraphHandler graph = new GraphHandler();
            EditorJsonUtility.FromJsonOverwrite(assetHelper.GraphDeltaJSON, graph);

            ((ShaderGraphModel)model.GraphModel).GraphHandler = graph;
            model.Init();

            return model;
        }

        public static string ToJson(ShaderGraphAssetHelper assetHelper)
        {
            return EditorJsonUtility.ToJson(assetHelper, true);
        }

        public static ShaderGraphAssetHelper FromJson(string jsonText)
        {
            var assetHelper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            EditorJsonUtility.FromJsonOverwrite(jsonText, assetHelper);
            return assetHelper;
        }
    }
}
