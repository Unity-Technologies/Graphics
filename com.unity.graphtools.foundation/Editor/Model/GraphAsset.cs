using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for graph assets. Uses Unity serialization by default.
    /// </summary>
    public abstract class GraphAsset : ScriptableObject, ISerializedGraphAsset, ISerializationCallbackReceiver
    {
        [SerializeReference]
        IGraphModel m_GraphModel;

        /// <inheritdoc />
        public bool Dirty
        {
            get => EditorUtility.IsDirty(this);
            set
            {
                if (value)
                {
                    EditorUtility.SetDirty(this);
                }
                else
                {
                    EditorUtility.ClearDirty(this);
                }
            }
        }

        /// <inheritdoc />
        public IGraphModel GraphModel => m_GraphModel;

        /// <inheritdoc />
        public string Name
        {
            get => name;
            set => name = value;
        }

        public uint Version { get; protected set; }

        /// <summary>
        /// The type of the graph model.
        /// </summary>
        protected abstract Type GraphModelType { get; }

        /// <summary>
        /// The path on disk of the graph asset.
        /// </summary>
        public string FilePath => AssetDatabase.GetAssetPath(this);

        /// <inheritdoc />
        public void CreateGraph(Type stencilType = null)
        {
            Debug.Assert(typeof(IGraphModel).IsAssignableFrom(GraphModelType));
            var graphModel = (IGraphModel)Activator.CreateInstance(GraphModelType);
            if (graphModel == null)
                return;

            graphModel.StencilType = stencilType ?? graphModel.DefaultStencilType;
            graphModel.Asset = this;
            m_GraphModel = graphModel;

            graphModel.OnEnable();

            Dirty = true;
        }

        /// <inheritdoc />
        public virtual void CreateFile(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
                if (File.Exists(path))
                    AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(this, path);
            }
        }

        public virtual void Save()
        {
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public virtual ISerializedGraphAsset Import()
        {
            return this;
        }

        /// <summary>
        /// Implementation of OnEnable event function.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_GraphModel?.OnEnable();
        }

        /// <summary>
        /// Implementation of OnDisable event function.
        /// </summary>
        protected virtual void OnDisable()
        {
            m_GraphModel?.OnDisable();
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            if (m_GraphModel != null)
                m_GraphModel.Asset = this;

            unchecked
            {
                Version++;
            }
        }
    }
}
