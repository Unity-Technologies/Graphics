using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

using Target = UnityEditor.ShaderGraph.Target;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    class TargetSettingsObject : JsonObject
    {
        [SerializeField]
        public List<JsonData<Target>> m_GraphTargets = new();
    }

    public class ShaderGraphAssetModel : GraphAsset
    {
        protected override Type GraphModelType => typeof(ShaderGraphModel);

        public ShaderGraphModel ShaderGraphModel => GraphModel as ShaderGraphModel;
        public GraphDelta.GraphHandler GraphHandler { get; set; }

        #region TargetSettingsData

        [SerializeReference]
        TargetSettingsObject m_TargetSettingsObject = new ();
        internal TargetSettingsObject targetSettingsObject => m_TargetSettingsObject;
        internal List<JsonData<Target>> ActiveTargets => m_TargetSettingsObject.m_GraphTargets;

        #endregion

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
                GraphModel.Asset = this;

            // We got deserialized unexpectedly, which means we'll need to find our graphHandler...
            if (GraphHandler == null)
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
                        GraphHandler = IsSubGraph ? ShaderGraphAsset.CreateBlankGraphHandler() : ShaderSubGraphAsset.CreateBlankSubGraphHandler();
                    }
                }
            }

            if (this.FilePath != String.Empty)
            {
                string text = File.ReadAllText(this.FilePath, Encoding.UTF8);
                var instance = CreateInstance<ShaderGraphAsset>();
                EditorJsonUtility.FromJsonOverwrite(text, instance);
                if (instance.TargetSettingsJSON != null)
                    MultiJson.Deserialize(m_TargetSettingsObject, instance.TargetSettingsJSON);
            }

            base.OnEnable();
            Name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));
        }

        public void MarkAsDirty(bool isDirty)
        {
            this.Dirty = isDirty;
        }
    }
}
