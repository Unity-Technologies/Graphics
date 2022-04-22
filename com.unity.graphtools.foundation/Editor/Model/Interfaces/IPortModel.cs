using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The number of connections a port can accept.
    /// </summary>
    public enum PortCapacity
    {
        /// <summary>
        /// The port cannot accept any connection.
        /// </summary>
        None,
        /// <summary>
        /// The port can only accept a single connection.
        /// </summary>
        Single,

        /// <summary>
        /// The port can accept multiple connections.
        /// </summary>
        Multi
    }

    /// <summary>
    /// Options for the port.
    /// </summary>
    [Flags]
    public enum PortModelOptions
    {
        /// <summary>
        /// No option set.
        /// </summary>
        None = 0,

        /// <summary>
        /// The port has no constant to set its value when not connected.
        /// </summary>
        NoEmbeddedConstant = 1,

        /// <summary>
        /// The port is hidden.
        /// </summary>
        Hidden = 2,

        /// <summary>
        /// Default port options.
        /// </summary>
        Default = None,
    }

    /// <summary>
    /// Interface for ports.
    /// </summary>
    public interface IPortModel : IGraphElementModel
    {
        /// <summary>
        /// The node model that owns this port.
        /// </summary>
        IPortNodeModel NodeModel { get; set; }

        /// <summary>
        /// The port direction (input, output, undetermined).
        /// </summary>
        PortDirection Direction { get; set; }

        /// <summary>
        /// The port type (data, execution, etc.).
        /// </summary>
        PortType PortType { get; set; }

        /// <summary>
        /// The orientation of the port (horizontal, vertical).
        /// </summary>
        PortOrientation Orientation { get; set; }

        /// <summary>
        /// The capacity of the port in term of connected edges.
        /// </summary>
        PortCapacity Capacity { get; set; }

        /// <summary>
        /// The port data type.
        /// </summary>
        Type PortDataType { get; }

        /// <summary>
        /// Port options.
        /// </summary>
        PortModelOptions Options { get; set; }

        /// <summary>
        /// The port data type handle.
        /// </summary>
        TypeHandle DataTypeHandle { get; set; }

        /// <summary>
        /// The tooltip for the port.
        /// </summary>
        string ToolTip { get; set; }

        /// <summary>
        /// Should the port create a default embedded constant.
        /// </summary>
        bool CreateEmbeddedValueIfNeeded { get; }

        /// <summary>
        /// Gets the ports connected to this port.
        /// </summary>
        /// <returns>The ports connected to this port.</returns>
        IEnumerable<IPortModel> GetConnectedPorts();

        /// <summary>
        /// Gets the edges connected to this port.
        /// </summary>
        /// <returns>The edges connected to this port.</returns>
        IReadOnlyList<IEdgeModel> GetConnectedEdges();

        /// <summary>
        /// Checks whether two ports are connected.
        /// </summary>
        /// <param name="otherPort">The second port.</param>
        /// <returns>True if there is at least one edge that connects the two ports.</returns>
        bool IsConnectedTo(IPortModel otherPort);

        /// <summary>
        /// A constant representing the port default value.
        /// </summary>
        IConstant EmbeddedValue { get; }

        /// <summary>
        /// The port unique name.
        /// </summary>
        /// <remarks>The name should only be unique within a node.</remarks>
        string UniqueName { get; }
    }
}
