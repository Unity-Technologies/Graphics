using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.InternalModels
{
    /// <summary>
    /// A model that represents an edge in a graph.
    /// </summary>
    /// <remarks>
    /// A ghost edge is usually used as an edge that shows where an edge would connect to during edge
    /// connection manipulations.
    /// </remarks>
    public class GhostEdgeModel : GraphElementModel, IGhostEdge
    {
        /// <inheritdoc />
        public IPortModel FromPort { get; set; }

        /// <inheritdoc />
        public string FromPortId => FromPort?.UniqueName;

        /// <inheritdoc />
        public string ToPortId => ToPort?.UniqueName;

        /// <inheritdoc />
        public SerializableGUID FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        /// <inheritdoc />
        public SerializableGUID ToNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        /// <inheritdoc />
        public IPortModel ToPort { get; set; }

        /// <inheritdoc />
        public string EdgeLabel { get; set; }

        /// <inheritdoc />
        public Vector2 EndPoint { get; set; } = Vector2.zero;

        /// <inheritdoc />
        public void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        /// <inheritdoc />
        public (PortMigrationResult, PortMigrationResult) AddPlaceHolderPorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            inputNode = null;
            outputNode = null;
            return (PortMigrationResult.None, PortMigrationResult.None);
        }
    }
}
