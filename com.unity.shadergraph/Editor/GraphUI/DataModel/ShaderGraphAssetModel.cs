using System;
using System.IO;
using System.Text;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ShaderGraphModel);

        public ShaderGraphModel ShaderGraphModel => GraphModel as ShaderGraphModel;
        public GraphDelta.GraphHandler GraphHandler { get; set; }

        public void Init(GraphDelta.GraphHandler graph = null)
        {
            GraphHandler = graph;
            OnEnable();
        }

        protected override void OnEnable()
        {
            if (GraphModel != null)
                GraphModel.AssetModel = this;

            // We got deserialized unexpectedly, which means we'll need to find our graphHandler...
            if(GraphHandler == null)
            {
                Debug.LogWarning($"OnEnable called unexpectedly {this.Name}, @{this.GetPath()}- GraphHandler was not initialized normally.");
                try // to get the AssetHelper that was imported with the asset
                {
                    var assetModel = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(AssetDatabase.GetAssetPath(this));
                    GraphHandler = assetModel.ResolveGraph();
                }
                catch // otherwise try and read directly from path; shouldn't happen.
                {
                    try
                    {
                        Debug.LogWarning($"Could not resolve a GraphHandler from the 'LoadAssetAtPath,' trying to load file from path directly...");
                        string json = File.ReadAllText(this.GetPath(), Encoding.UTF8);
                        var asset = CreateInstance<ShaderGraphAsset>();
                        EditorJsonUtility.FromJsonOverwrite(json, asset);
                        GraphHandler = asset.ResolveGraph();
                    }
                    catch
                    {
                        Debug.LogWarning("Guess it was a new graph! We're probably fine...");
                        GraphHandler = ShaderGraphAsset.CreateBlankGraphHandler();
                    }
                }
            }
            base.OnEnable();
            Name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is not ShaderGraphAssetModel) return false;

            var path = AssetDatabase.GetAssetPath(instanceId);
            var asset = AssetDatabase.LoadAssetAtPath<ShaderGraphAssetModel>(path);
            if (asset == null) return false;

            var shaderGraphEditorWindow = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
            shaderGraphEditorWindow.Show();
            shaderGraphEditorWindow.Focus();
            return shaderGraphEditorWindow != null;
        }
    }
}
