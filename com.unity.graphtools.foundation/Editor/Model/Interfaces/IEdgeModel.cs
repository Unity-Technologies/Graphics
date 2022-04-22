using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum PortMigrationResult
    {
        None,
        PlaceholderNotNeeded,
        PlaceholderPortAdded,
        PlaceholderPortFailure,
    }

    /// <summary>
    /// Identifies a side of an edge, not taking into account its direction.
    /// </summary>
    public enum EdgeSide
    {
        /// <summary>
        /// The first defined point of the edge.
        /// </summary>
        From,
        /// <summary>
        /// The second defined point of the edge.
        /// </summary>
        To
    }

    /// <summary>
    /// Interface for a model that represents an edge in a graph.
    /// </summary>
    public interface IEdgeModel : IGraphElementModel
    {
        /// <summary>
        /// The port from which the edge originates.
        /// </summary>
        IPortModel FromPort { get; set; }

        /// <summary>
        /// The port to which the edge goes.
        /// </summary>
        IPortModel ToPort { get; set; }

        /// <summary>
        /// Sets the endpoints of the edge.
        /// </summary>
        /// <param name="toPortModel">The port where the edge goes.</param>
        /// <param name="fromPortModel">The port from which the edge originates.</param>
        void SetPorts(IPortModel toPortModel, IPortModel fromPortModel);

        /// <summary>
        /// The unique id of the originating port.
        /// </summary>
        string FromPortId { get; }

        /// <summary>
        /// The unique id of the destination port.
        /// </summary>
        string ToPortId { get; }

        /// <summary>
        /// The unique identifier of the output node of the edge.
        /// </summary>
        SerializableGUID ToNodeGuid { get; }

        /// <summary>
        /// The unique identifier of the input node of the edge.
        /// </summary>
        SerializableGUID FromNodeGuid { get; }

        /// <summary>
        /// The label of the edge.
        /// </summary>
        string EdgeLabel { get; set; }

        /// <summary>
        /// Creates placeholder ports in the case where the original ports are missing.
        /// </summary>
        /// <param name="inputNode">The node owning the placeholder for the input port.</param>
        /// <param name="outputNode">The node owning the placeholder for the output port.</param>
        /// <returns>A migration result pair for the input and output port migration.</returns>
        (PortMigrationResult, PortMigrationResult) AddPlaceHolderPorts(out INodeModel inputNode, out INodeModel outputNode);
    }
}
