using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A model to hold the result of the graph processing.
    /// </summary>
    public class GraphProcessingErrorModel : IGraphProcessingErrorModel
    {
        SerializableGUID m_Guid;
        protected List<Capabilities> m_Capabilities = new List<Capabilities>();

        /// <inheritdoc/>
        public IGraphModel GraphModel => ParentModel.GraphModel;

        /// <inheritdoc/>
        public SerializableGUID Guid
        {
            get
            {
                if (!m_Guid.Valid)
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        /// <inheritdoc/>
        public IGraphAssetModel AssetModel { get; set; }

        /// <summary>
        /// The container this node is stored into.
        /// </summary>
        public virtual IGraphElementContainer Container
        {
            get => GraphModel;
        }

        /// <inheritdoc/>
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        /// <inheritdoc/>
        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        /// <inheritdoc/>
        public Color Color { get; set; }

        /// <inheritdoc/>
        public bool HasUserColor => false;

        /// <inheritdoc/>
        public void ResetColor()
        {
        }

        /// <inheritdoc/>
        public IGraphElementModel ParentModel { get; }

        /// <inheritdoc/>
        public string ErrorMessage { get; }

        /// <inheritdoc/>
        public QuickFix Fix { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphProcessingErrorModel" /> class.
        /// </summary>
        /// <param name="error">The <see cref="GraphProcessingError"/> used to initialize the instance.</param>
        public GraphProcessingErrorModel(GraphProcessingError error)
        {
            ParentModel = error.SourceNode;
            ErrorMessage = error.Description;
            Fix = error.Fix;
        }
    }
}
