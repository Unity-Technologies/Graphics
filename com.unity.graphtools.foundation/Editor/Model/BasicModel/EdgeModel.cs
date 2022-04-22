using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents an edge in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class EdgeModel : GraphElementModel, IEdgeModel
    {
        [SerializeField, FormerlySerializedAs("m_OutputPortReference")]
        PortReference m_FromPortReference;

        [SerializeField, FormerlySerializedAs("m_InputPortReference")]
        PortReference m_ToPortReference;

        [SerializeField]
        protected string m_EdgeLabel;

        IPortModel m_FromPortModelCache;

        IPortModel m_ToPortModelCache;

        /// <inheritdoc />
        public override IGraphModel GraphModel
        {
            get => base.GraphModel;
            set
            {
                base.GraphModel = value;
                m_FromPortReference.AssignGraphModel(value);
                m_ToPortReference.AssignGraphModel(value);
            }
        }

        /// <inheritdoc />
        public virtual IPortModel FromPort
        {
            get => m_FromPortReference.GetPortModel(PortDirection.Output, ref m_FromPortModelCache);
            set
            {
                var oldPort = FromPort;
                m_FromPortReference.Assign(value);
                m_FromPortModelCache = value;
                OnPortChanged(oldPort, value);
            }
        }

        /// <inheritdoc />
        public virtual IPortModel ToPort
        {
            get => m_ToPortReference.GetPortModel(PortDirection.Input, ref m_ToPortModelCache);
            set
            {
                var oldPort = ToPort;
                m_ToPortReference.Assign(value);
                m_ToPortModelCache = value;
                OnPortChanged(oldPort, value);
            }
        }

        /// <inheritdoc />
        public virtual string FromPortId => m_FromPortReference.UniqueId;

        /// <inheritdoc />
        public virtual string ToPortId => m_ToPortReference.UniqueId;

        /// <inheritdoc />
        public virtual SerializableGUID FromNodeGuid => m_FromPortReference.NodeModelGuid;

        /// <inheritdoc />
        public virtual SerializableGUID ToNodeGuid => m_ToPortReference.NodeModelGuid;

        /// <inheritdoc />
        public virtual string EdgeLabel
        {
            get => string.IsNullOrEmpty(m_EdgeLabel) ? GetEdgeOrderLabel() : m_EdgeLabel;
            set => m_EdgeLabel = value;
        }

        string GetEdgeOrderLabel()
        {
            var edgeOrder = (FromPort as IReorderableEdgesPortModel)?.GetEdgeOrder(this) ?? -1;
            return edgeOrder == -1 ? "" : (edgeOrder + 1).ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeModel"/> class.
        /// </summary>
        public EdgeModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Movable
            });
        }

        /// <summary>
        /// Notifies the graph model that one of the ports has changed. Derived implementations of EdgeModel
        /// should call this method whenever a change makes <see cref="FromPort"/> or <see cref="ToPort"/> return
        /// a different value than before.
        /// </summary>
        /// <param name="oldPort">The previous port.</param>
        /// <param name="newPort">The new port.</param>
        protected void OnPortChanged(IPortModel oldPort, IPortModel newPort)
        {
            (GraphModel as GraphModel)?.PortEdgeIndex.UpdateEdge(this, oldPort, newPort);
        }

        /// <inheritdoc />
        public virtual void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            Assert.IsNotNull(toPortModel);
            Assert.IsNotNull(toPortModel.NodeModel);
            Assert.IsNotNull(fromPortModel);
            Assert.IsNotNull(fromPortModel.NodeModel);

            FromPort = fromPortModel;
            ToPort = toPortModel;

            toPortModel.NodeModel.OnConnection(toPortModel, fromPortModel);
            fromPortModel.NodeModel.OnConnection(fromPortModel, toPortModel);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{m_FromPortReference} -> {m_ToPortReference}";
        }

        /// <summary>
        /// Resets the port cache of the edge.
        /// </summary>
        /// <remarks>After that call, the <see cref="FromPort"/>
        /// and the <see cref="ToPort"/> will be resolved from the port reference data.</remarks>
        public void ResetPortCache()
        {
            m_FromPortModelCache = default;
            m_ToPortModelCache = default;

            m_FromPortReference.ResetCache();
            m_ToPortReference.ResetCache();
        }

        /// <summary>
        /// Updates the port references with the cached ports.
        /// </summary>
        public void UpdatePortFromCache()
        {
            if (m_FromPortModelCache == null || m_ToPortModelCache == null)
                return;

            m_FromPortReference.Assign(m_FromPortModelCache);
            m_ToPortReference.Assign(m_ToPortModelCache);
        }

        /// <inheritdoc />
        public (PortMigrationResult, PortMigrationResult) AddPlaceHolderPorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            PortMigrationResult inputResult;
            PortMigrationResult outputResult;

            inputNode = outputNode = null;
            if (ToPort == null)
            {
                inputResult = m_ToPortReference.AddPlaceHolderPort(PortDirection.Input) ?
                    PortMigrationResult.PlaceholderPortAdded : PortMigrationResult.PlaceholderPortFailure;

                inputNode = m_ToPortReference.NodeModel;
            }
            else
            {
                inputResult = PortMigrationResult.PlaceholderNotNeeded;
            }

            if (FromPort == null)
            {
                outputResult = m_FromPortReference.AddPlaceHolderPort(PortDirection.Output) ?
                    PortMigrationResult.PlaceholderPortAdded : PortMigrationResult.PlaceholderPortFailure;

                outputNode = m_FromPortReference.NodeModel;
            }
            else
            {
                outputResult = PortMigrationResult.PlaceholderNotNeeded;
            }

            return (inputResult, outputResult);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            ResetPortCache();
        }
    }
}
