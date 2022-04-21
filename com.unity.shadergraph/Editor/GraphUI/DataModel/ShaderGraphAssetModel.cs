using System;
using System.IO;
using System.Text;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ShaderGraphModel);

        public ShaderGraphModel ShaderGraphModel => GraphModel as ShaderGraphModel;
        public GraphDelta.GraphHandler GraphHandler { get; set; }

        public bool IsSubGraph { get; private set; }

        public void Init(GraphDelta.GraphHandler graph = null, bool isSubGraph = false)
        {
            GraphHandler = graph;
            IsSubGraph = isSubGraph;
            OnEnable();
        }

        protected override void OnEnable()
        {
            if (GraphModel != null)
                GraphModel.AssetModel = this;

            // We got deserialized unexpectedly, which means we'll need to find our graphHandler...
            if(GraphHandler == null)
            {
                try // to get the AssetHelper that was imported with the asset
                {
                    var assetModel = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(AssetDatabase.GetAssetPath(this));
                    GraphHandler = assetModel.ResolveGraph();
                }
                catch // otherwise try and read directly from path; shouldn't happen.
                {
                    try
                    {
                        string json = File.ReadAllText(this.GetPath(), Encoding.UTF8);
                        var asset = CreateInstance<ShaderGraphAsset>();
                        EditorJsonUtility.FromJsonOverwrite(json, asset);
                        GraphHandler = asset.ResolveGraph();
                    }
                    catch
                    {
                        GraphHandler = ShaderGraphAsset.CreateBlankGraphHandler();
                    }
                }
            }
            base.OnEnable();
            Name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));
        }

        public override bool CanBeSubgraph() => IsSubGraph;
    }
}
