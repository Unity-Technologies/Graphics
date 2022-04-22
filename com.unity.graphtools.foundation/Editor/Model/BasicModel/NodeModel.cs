using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Base model that represents a node in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class NodeModel : GraphElementModel, IInputOutputPortsNodeModel, IHasTitle, IHasProgress, ICollapsible
    {
        [SerializeField, HideInInspector]
        Vector2 m_Position;

        [SerializeField, HideInInspector]
        string m_Title;

        [SerializeField, HideInInspector]
        string m_Tooltip;

        [SerializeField, HideInInspector]
        SerializedReferenceDictionary<string, IConstant> m_InputConstantsById;

        [SerializeField, HideInInspector]
        ModelState m_State;

        protected OrderedPorts m_InputsById;
        protected OrderedPorts m_OutputsById;
        protected OrderedPorts m_PreviousInputs;
        protected OrderedPorts m_PreviousOutputs;

        [SerializeField, HideInInspector]
        bool m_Collapsed;

        /// <inheritdoc />
        public virtual string IconTypeString => "node";

        /// <inheritdoc />
        public ModelState State
        {
            get => m_State;
            set => m_State = value;
        }

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title.Nicify();

        /// <inheritdoc />
        public virtual string Tooltip
        {
            get => m_Tooltip;
            set => m_Tooltip = value;
        }

        /// <inheritdoc />
        public Vector2 Position
        {
            get => m_Position;
            set
            {
                if (!this.IsMovable())
                    return;

                m_Position = value;
            }
        }

        /// <inheritdoc />
        public virtual bool AllowSelfConnect => false;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IPortModel> InputsById => m_InputsById;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IPortModel> OutputsById => m_OutputsById;

        /// <inheritdoc />
        public virtual IReadOnlyList<IPortModel> InputsByDisplayOrder => m_InputsById;

        /// <inheritdoc />
        public virtual IReadOnlyList<IPortModel> OutputsByDisplayOrder => m_OutputsById;

        /// <inheritdoc />
        public IEnumerable<IPortModel> Ports => InputsById.Values.Concat(OutputsById.Values);

        public IReadOnlyDictionary<string, IConstant> InputConstantsById => m_InputConstantsById;

        /// <inheritdoc />
        public virtual bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!this.IsCollapsible())
                    return;

                m_Collapsed = value;
            }
        }

        /// <inheritdoc />
        public virtual bool HasProgress => false;

        /// <inheritdoc />
        public bool Destroyed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeModel"/> class.
        /// </summary>
        public NodeModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Droppable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Movable,
                Overdrive.Capabilities.Collapsible,
                Overdrive.Capabilities.Colorable,
                Overdrive.Capabilities.Ascendable
            });
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
            m_InputConstantsById = new SerializedReferenceDictionary<string, IConstant>();
        }

        /// <inheritdoc />
        public void Destroy() => Destroyed = true;

        internal void ClearPorts()
        {
            m_InputsById = new OrderedPorts();
            m_OutputsById = new OrderedPorts();
            m_PreviousInputs = null;
            m_PreviousOutputs = null;
        }

        /// <inheritdoc />
        public virtual void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        /// <inheritdoc />
        public virtual void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        /// <summary>
        /// Instantiates the ports of the nodes.
        /// </summary>
        public void DefineNode()
        {
            OnPreDefineNode();

            m_PreviousInputs = m_InputsById;
            m_PreviousOutputs = m_OutputsById;
            m_InputsById = new OrderedPorts(m_InputsById?.Count ?? 0);
            m_OutputsById = new OrderedPorts(m_OutputsById?.Count ?? 0);

            OnDefineNode();

            RemoveObsoleteEdgesAndConstants();
        }

        /// <summary>
        /// Called by <see cref="DefineNode"/> before the <see cref="OrderedPorts"/> lists are modified.
        /// </summary>
        protected virtual void OnPreDefineNode()
        {
        }

        /// <summary>
        /// Called by <see cref="DefineNode"/>. Override this function to instantiate the ports of your node type.
        /// </summary>
        protected virtual void OnDefineNode()
        {
        }

        /// <inheritdoc />
        public void OnCreateNode()
        {
            DefineNode();
        }

        /// <inheritdoc />
        public virtual void OnDuplicateNode(INodeModel sourceNode)
        {
            Title = (sourceNode as IHasTitle)?.Title ?? "";
            DefineNode();
            CloneInputConstants();
        }

        void RemoveObsoleteEdgesAndConstants()
        {
            foreach (var kv in m_PreviousInputs
                     .Where<KeyValuePair<string, IPortModel>>(kv => !m_InputsById.ContainsKey(kv.Key)))
            {
                DisconnectPort(kv.Value);
            }

            foreach (var kv in m_PreviousOutputs
                     .Where<KeyValuePair<string, IPortModel>>(kv => !m_OutputsById.ContainsKey(kv.Key)))
            {
                DisconnectPort(kv.Value);
            }

            // remove input constants that aren't used
            var idsToDeletes = m_InputConstantsById
                .Select(kv => kv.Key)
                .Where(id => !m_InputsById.ContainsKey(id)).ToList();
            foreach (var id in idsToDeletes)
            {
                m_InputConstantsById.Remove(id);
            }
        }

        static IPortModel ReuseOrCreatePortModel(IPortModel model, IReadOnlyDictionary<string, IPortModel> previousPorts, OrderedPorts newPorts)
        {
            // reuse existing ports when ids match, otherwise add port
            if (previousPorts.TryGetValue(model.UniqueName, out var portModelToAdd))
            {
                if (portModelToAdd is IHasTitle toAddHasTitle && model is IHasTitle hasTitle)
                {
                    toAddHasTitle.Title = hasTitle.Title;
                }
                portModelToAdd.DataTypeHandle = model.DataTypeHandle;
                portModelToAdd.PortType = model.PortType;
            }
            else
            {
                portModelToAdd = model;
            }

            newPorts.Add(portModelToAdd);
            return portModelToAdd;
        }

        /// <summary>
        /// Creates a new port on the node.
        /// </summary>
        /// <param name="direction">The direction of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="portName">The name of the port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the new port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options of the port model to create.</param>
        /// <returns>The newly created port model.</returns>
        protected virtual IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new PortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName ?? "",
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                GraphModel = GraphModel
            };
        }

        /// <summary>
        /// Deletes all the edges connected to a given port.
        /// </summary>
        /// <param name="portModel">The port model to disconnect.</param>
        protected virtual void DisconnectPort(IPortModel portModel)
        {
            if (GraphModel != null)
            {
                var edgeModels = GraphModel.GetEdgesForPort(portModel);
                GraphModel.DeleteEdges(edgeModels.ToList());
            }
        }

        /// <inheritdoc />
        public virtual IPortModel AddInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<IConstant> initializationCallback = null)
        {
            var portModel = CreatePort(PortDirection.Input, orientation, portName, portType, dataType, portId, options);
            portModel = ReuseOrCreatePortModel(portModel, m_PreviousInputs, m_InputsById);
            UpdateConstantForInput(portModel, initializationCallback);
            return portModel;
        }

        /// <inheritdoc />
        public virtual IPortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default)
        {
            var portModel = CreatePort(PortDirection.Output, orientation, portName, portType, dataType, portId, options);
            return ReuseOrCreatePortModel(portModel, m_PreviousOutputs, m_OutputsById);
        }

        /// <summary>
        /// Updates an input port's constant.
        /// </summary>
        /// <param name="inputPort">The port to update.</param>
        /// <param name="initializationCallback">An initialization method for the constant to be called right after the constant is created.</param>
        protected void UpdateConstantForInput(IPortModel inputPort, Action<IConstant> initializationCallback = null)
        {
            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModelOptions.NoEmbeddedConstant) != 0)
            {
                m_InputConstantsById.Remove(id);
                return;
            }

            if (m_InputConstantsById.TryGetValue(id, out var constant))
            {
                // Destroy existing constant if not compatible
                var embeddedConstantType = GraphModel.Stencil.GetConstantType(inputPort.DataTypeHandle);
                Type portDefinitionType;
                if (embeddedConstantType != null)
                {
                    var instance = (IConstant)Activator.CreateInstance(embeddedConstantType);
                    portDefinitionType = instance.Type;
                }
                else
                {
                    portDefinitionType = inputPort.DataTypeHandle.Resolve();
                }

                if (!constant.Type.IsAssignableFrom(portDefinitionType))
                {
                    m_InputConstantsById.Remove(id);
                }
            }

            // Create new constant if needed
            if (!m_InputConstantsById.ContainsKey(id)
                && inputPort.CreateEmbeddedValueIfNeeded
                && inputPort.DataTypeHandle != TypeHandle.Unknown
                && GraphModel.Stencil.GetConstantType(inputPort.DataTypeHandle) != null)
            {
                var embeddedConstant = ((GraphModel)GraphModel).Stencil.CreateConstantValue(inputPort.DataTypeHandle);
                initializationCallback?.Invoke(embeddedConstant);
                m_InputConstantsById[id] = embeddedConstant;
                GraphModel.Asset.Dirty = true;
            }
        }

        public IConstantNodeModel CloneConstant(IConstantNodeModel source)
        {
            var clone = Activator.CreateInstance(source.GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(source, clone);
            return (IConstantNodeModel)clone;
        }

        public void CloneInputConstants()
        {
            foreach (var id in m_InputConstantsById.Keys.ToList())
            {
                IConstant inputConstant = m_InputConstantsById[id];
                IConstant newConstant = inputConstant.Clone();
                m_InputConstantsById[id] = newConstant;
                GraphModel.Asset.Dirty = true;
            }
        }

        /// <inheritdoc />
        public IPortModel GetPortFitToConnectTo(IPortModel portModel)
        {
            var portsToChooseFrom = portModel.Direction == PortDirection.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            return GraphModel.GetCompatiblePorts(portsToChooseFrom, portModel).FirstOrDefault();
        }

        /// <inheritdoc />
        public virtual IEnumerable<IEdgeModel> GetConnectedEdges()
        {
            return NodeModelDefaultImplementations.GetConnectedEdges(this);
        }

        /// <inheritdoc />
        public void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            Position += delta;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_PreviousInputs = null;
            m_PreviousOutputs = null;
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();

            // DefineNode() will be called by the GraphModel.
        }

        /// <inheritdoc />
        public void RemoveUnusedMissingPort(IPortModel portModel)
        {
            if (portModel.PortType != PortType.MissingPort || portModel.GetConnectedEdges().Any())
                return;

            if (portModel.Direction == PortDirection.Input)
                m_InputsById.Remove(portModel);
            else
                m_OutputsById.Remove(portModel);
        }
    }
}
