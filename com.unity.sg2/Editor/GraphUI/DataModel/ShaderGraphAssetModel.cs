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

    [Serializable]
    class MainPreviewData
    {
        public SerializableMesh serializedMesh = new ();
        public bool preventRotation;

        public int width = 125;
        public int height = 125;

        [NonSerialized]
        public Quaternion rotation = Quaternion.identity;

        [NonSerialized]
        public float scale = 1f;

        public void Initialize()
        {
            if (serializedMesh.IsNotInitialized)
            {
                // Initialize the sphere mesh as the default
                Mesh sphereMesh = Resources.GetBuiltinResource(typeof(Mesh), $"Sphere.fbx") as Mesh;
                serializedMesh.mesh = sphereMesh;
            }
        }
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

        #region MainPreviewData
        MainPreviewData m_MainPreviewData = new ();

        internal MainPreviewData mainPreviewData => m_MainPreviewData;

        public void SetPreviewMesh(Mesh newPreviewMesh)
        {
            m_MainPreviewData.serializedMesh.mesh = newPreviewMesh;
        }

        public void SetPreviewScale(float newPreviewScale)
        {
            m_MainPreviewData.scale = newPreviewScale;
        }

        public void SetPreviewRotation(Quaternion newRotation)
        {
            m_MainPreviewData.rotation = newRotation;
        }

        public void SetPreviewSize(Vector2 newPreviewSize)
        {
            m_MainPreviewData.width = Mathf.FloorToInt(newPreviewSize.x);
            m_MainPreviewData.height = Mathf.FloorToInt(newPreviewSize.y);
        }

        public void SetPreviewRotationLocked(bool preventRotation)
        {
            m_MainPreviewData.preventRotation = preventRotation;
        }

        #endregion

        public bool IsSubGraph { get; private set; }

        public void Init(GraphDelta.GraphHandler graph = null, bool isSubGraph = false)
        {
            GraphHandler = graph;
            IsSubGraph = isSubGraph;
            OnEnable();
            m_MainPreviewData.Initialize();
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

            // Deserialize target settings
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
