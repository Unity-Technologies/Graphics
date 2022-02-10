using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for graph assets.
    /// </summary>
    public abstract class GraphAssetModel : ScriptableObject, IGraphAssetModel
    {
        [SerializeReference]
        IGraphModel m_GraphModel;

        [SerializeField]
        GraphAssetType m_GraphAssetType;

        /// <inheritdoc />
        public bool Dirty
        {
            get;
            set;
        }

        /// <inheritdoc />
        public GraphAssetType GraphAssetType
        {
            get => m_GraphAssetType;
            set => m_GraphAssetType = value;
        }

        /// <inheritdoc />
        public IGraphModel GraphModel => m_GraphModel;

        /// <inheritdoc />
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <inheritdoc />
        public string FriendlyScriptName => Name.CodifyStringInternal();

        /// <summary>
        /// The type of the graph model.
        /// </summary>
        protected abstract Type GraphModelType { get; }

        /// <inheritdoc />
        public void CreateGraph(string graphName, Type stencilType = null, bool markAssetDirty = true, GraphAssetType graphAssetType = GraphAssetType.AssetGraph)
        {
            Debug.Assert(typeof(IGraphModel).IsAssignableFrom(GraphModelType));
            var graphModel = (IGraphModel)Activator.CreateInstance(GraphModelType);
            if (graphModel == null)
                return;

            // PF FIXME: graphName is not used.

            graphModel.StencilType = stencilType ?? graphModel.DefaultStencilType;

            graphModel.AssetModel = this;
            m_GraphModel = graphModel;
            m_GraphAssetType = graphAssetType;

            graphModel.OnEnable();

            if (markAssetDirty)
            {
                EditorUtility.SetDirty(this);
            }
        }

        /// <inheritdoc />
        public void UndoRedoPerformed()
        {
            Dirty = true;
            EditorUtility.SetDirty(this);
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
    }
}
