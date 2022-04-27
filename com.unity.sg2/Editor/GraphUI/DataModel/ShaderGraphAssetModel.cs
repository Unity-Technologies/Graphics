using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

using Target = UnityEditor.ShaderGraph.Target;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphAssetModel : GraphAsset
    {
        protected override Type GraphModelType => typeof(ShaderGraphModel);

        public ShaderGraphModel ShaderGraphModel => GraphModel as ShaderGraphModel;
        public GraphDelta.GraphHandler GraphHandler { get; set; }

        List<Target> m_GraphTargets = new();

        internal List<Target> ActiveTargets => m_GraphTargets;

        public void Init(GraphDelta.GraphHandler graph = null)
        {
            GraphHandler = graph;
            OnEnable();
        }

        protected override void OnEnable()
        {
            if (GraphModel != null)
                GraphModel.Asset = this;

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
                        string json = File.ReadAllText(this.FilePath, Encoding.UTF8);
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
    }
}
