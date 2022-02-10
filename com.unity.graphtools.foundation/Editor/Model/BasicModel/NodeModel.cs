using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

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
        SerializedReferenceDictionary<string, IConstant> m_InputConstantsById;

        [SerializeField]
        ModelState m_State;

        internal OrderedPorts m_InputsById;
        internal OrderedPorts m_OutputsById;
        internal OrderedPorts m_PreviousInputs;
        internal OrderedPorts m_PreviousOutputs;

        [SerializeField]
        bool m_Collapsed;

        /// <summary>
        /// Stencil for this nodemodel, helper getter for Graphmodel Stencil
        /// </summary>
        protected IStencil Stencil => GraphModel.Stencil;

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
        public virtual string Tooltip { get; set; }

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

        /// <inheritdoc />
        public virtual void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        /// <inheritdoc />
        public virtual void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        /// <inheritdoc />
        public void DefineNode()
        {
            OnPreDefineNode();

            m_PreviousInputs = m_InputsById;
            m_PreviousOutputs = m_OutputsById;
            m_InputsById = new OrderedPorts(m_InputsById?.Count ?? 0);
            m_OutputsById = new OrderedPorts(m_OutputsById?.Count ?? 0);

            OnDefineNode();

            RemoveUnusedPorts();
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

        void RemoveUnusedPorts()
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
            string id = model.UniqueName;
            IPortModel portModelToAdd = model;
            if (previousPorts.TryGetValue(id, out var existingModel))
            {
                portModelToAdd = existingModel;
                if (portModelToAdd is IHasTitle toAddHasTitle && model is IHasTitle hasTitle)
                    toAddHasTitle.Title = hasTitle.Title;
                portModelToAdd.DataTypeHandle = model.DataTypeHandle;
                portModelToAdd.PortType = model.PortType;
            }
            newPorts.Add(portModelToAdd);
            return portModelToAdd;
        }

        /// <inheritdoc />
        public virtual PortCapacity GetPortCapacity(IPortModel portModel)
        {
            PortCapacity cap = PortCapacity.Single;
            return Stencil?.GetPortCapacity(portModel, out cap) ?? false ? cap : portModel?.GetDefaultCapacity() ?? PortCapacity.Multi;
        }

        /// <inheritdoc />
        public virtual IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
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
                AssetModel = AssetModel
            };
        }

        /// <inheritdoc />
        public void DisconnectPort(IPortModel portModel)
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
                var embeddedConstantType = Stencil.GetConstantNodeValueType(inputPort.DataTypeHandle);
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
                && Stencil.GetConstantNodeValueType(inputPort.DataTypeHandle) != null)
            {
                var embeddedConstant = ((GraphModel)GraphModel).Stencil.CreateConstantValue(inputPort.DataTypeHandle);
                initializationCallback?.Invoke(embeddedConstant);
                EditorUtility.SetDirty((Object)AssetModel);
                m_InputConstantsById[id] = embeddedConstant;
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
                IConstant newConstant = inputConstant.CloneConstant();
                m_InputConstantsById[id] = newConstant;
                EditorUtility.SetDirty((Object)AssetModel);
            }
        }

        /// <inheritdoc />
        public IPortModel GetPortFitToConnectTo(IPortModel portModel)
        {
            // PF: FIXME: This should be the same as GraphView.GetCompatiblePorts (which will move to GraphModel soon).
            // It should also be coherent with the nodes presented in the searcher.

            var portsToChooseFrom = portModel.Direction == PortDirection.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            return GetFirstPortModelOfType(portModel.PortType, portModel.DataTypeHandle, portsToChooseFrom);
        }

        IPortModel GetFirstPortModelOfType(PortType portType, TypeHandle typeHandle, IReadOnlyList<IPortModel> portModels)
        {
            if (typeHandle != TypeHandle.Unknown && portModels.Any())
            {
                IStencil stencil = portModels.First().GraphModel.Stencil;
                IPortModel unknownPortModel = null;

                // Return the first matching Input portModel
                // If no match was found, return the first Unknown typed portModel
                // Else return null.
                foreach (var portModel in portModels.Where(p => p.PortType == portType))
                {
                    if (portModel.DataTypeHandle == TypeHandle.Unknown && unknownPortModel == null)
                    {
                        unknownPortModel = portModel;
                    }

                    if (typeHandle.IsAssignableFrom(portModel.DataTypeHandle, stencil))
                    {
                        return portModel;
                    }
                }

                if (unknownPortModel != null)
                    return unknownPortModel;
            }

            return null;
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
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
        }
    }
}
