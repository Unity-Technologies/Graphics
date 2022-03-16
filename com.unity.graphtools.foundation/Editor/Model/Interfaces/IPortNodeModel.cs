using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for a model of a node that has port.
    /// </summary>
    public interface IPortNodeModel : INodeModel
    {
        /// <summary>
        /// Gets all the port models this node has.
        /// </summary>
        IEnumerable<IPortModel> Ports { get; }
        // PF: Add PortsById and PortsByDisplayOrder?

        // PF: these should probably be removed.
        /// <summary>
        /// Called when any port on this node model gets connected.
        /// </summary>
        /// <param name="selfConnectedPortModel">The model of the port that got connected on this node.</param>
        /// <param name="otherConnectedPortModel">The model of the port that got connected on the other node.</param>
        void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);
        /// <summary>
        /// Called when any port on this node model gets disconnected.
        /// </summary>
        /// <param name="selfConnectedPortModel">The model of the port that got disconnected on this node.</param>
        /// <param name="otherConnectedPortModel">The model of the port that got disconnected on the other node.</param>
        void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);
        /// <summary>
        /// Gets the model of a port that would be fit to connect to another port model.
        /// </summary>
        /// <param name="portModel">The model of the port we want to connect to this node.</param>
        /// <returns>A model of a port that would be fit to connect, null if none was found.</returns>
        IPortModel GetPortFitToConnectTo(IPortModel portModel);
        /// <summary>
        /// Remove a missing port that no longer has any connection.
        /// </summary>
        void RemoveUnusedMissingPort(IPortModel portModel);
    }

    /// <summary>
    /// Interface for a node that contains both input and output ports.
    /// </summary>
    public interface IInputOutputPortsNodeModel : IPortNodeModel
    {
        /// <summary>
        /// Gets all the models of the input ports of this node, indexed by a string unique to the node.
        /// </summary>
        IReadOnlyDictionary<string, IPortModel> InputsById { get; }
        /// <summary>
        /// Gets all the models of the output ports of this node, indexed by a string unique to the node.
        /// </summary>
        IReadOnlyDictionary<string, IPortModel> OutputsById { get; }
        /// <summary>
        /// Gets all the models of the input ports of this node, in the order they should be displayed.
        /// </summary>
        IReadOnlyList<IPortModel> InputsByDisplayOrder { get; }
        /// <summary>
        /// Gets all the models of the output ports of this node, in the order they should be displayed.
        /// </summary>
        IReadOnlyList<IPortModel> OutputsByDisplayOrder { get; }

        /// <summary>
        /// Adds a new input port on the node.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="initializationCallback">An initialization method for the associated constant (if one is needed for the port) to be called right after the port is created.</param>
        /// <returns>The newly created input port.</returns>
        IPortModel AddInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<IConstant> initializationCallback = null);

        /// <summary>
        /// Adds a new output port on the node.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <returns>The newly created output port.</returns>
        IPortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default);
    }

    /// <summary>
    /// Interface for the model of a node that has a single input port.
    /// </summary>
    public interface ISingleInputPortNodeModel : IInputOutputPortsNodeModel
    {
        /// <summary>
        /// Gets the model of the input port for this node.
        /// </summary>
        IPortModel InputPort { get; }
    }

    /// <summary>
    /// Interface for the model of a node that has a single output port.
    /// </summary>
    public interface ISingleOutputPortNodeModel : IInputOutputPortsNodeModel
    {
        /// <summary>
        /// Gets the model of the output port for this node.
        /// </summary>
        IPortModel OutputPort { get; }
    }
}
