using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a port in a node.
    /// </summary>
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class PortModel : GraphElementModel, IReorderableEdgesPortModel, IHasTitle
    {
        string m_UniqueId;

        string m_Title;
        PortType m_PortType;
        TypeHandle m_DataTypeHandle;
        PortDirection m_Direction;

        PortCapacity? m_PortCapacity;

        Type m_PortDataTypeCache;

        string m_TooltipCache;
        string m_TooltipOverride;

        /// <inheritdoc />
        public IPortNodeModel NodeModel { get; set; }

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set
            {
                var oldUniqueName = UniqueName;
                m_Title = value;
                OnUniqueNameChanged(oldUniqueName, UniqueName);
            }
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <inheritdoc />
        public string UniqueName
        {
            get => m_UniqueId ?? Title ?? Guid.ToString();
            set
            {
                var oldUniqueName = UniqueName;
                m_UniqueId = value;
                OnUniqueNameChanged(oldUniqueName, UniqueName);
            }
        }

        /// <inheritdoc />
        public override SerializableGUID Guid
        {
            get => base.Guid;
            set
            {
                var oldUniqueName = UniqueName;
                base.Guid = value;
                OnUniqueNameChanged(oldUniqueName, UniqueName);
            }
        }

        /// <inheritdoc />
        public PortModelOptions Options { get; set; }

        /// <inheritdoc />
        public PortType PortType
        {
            get => m_PortType;
            set
            {
                m_PortType = value;
                // Invalidate cache.
                m_TooltipCache = null;
            }
        }


        /// <inheritdoc />
        public PortDirection Direction
        {
            get => m_Direction;
            set
            {
                var oldDirection = m_Direction;
                m_Direction = value;
                OnDirectionChanged(oldDirection,value);
            }
        }

        /// <inheritdoc />
        public PortOrientation Orientation { get; set; }

        /// <inheritdoc />
        public virtual PortCapacity Capacity
        {
            get
            {
                if (m_PortCapacity != null)
                    return m_PortCapacity.Value;

                // If not set, fallback to default behavior.
                return PortType == PortType.Data && Direction == PortDirection.Input ? PortCapacity.Single : PortCapacity.Multi;
            }
            set => m_PortCapacity = value;
        }

        /// <inheritdoc />
        public TypeHandle DataTypeHandle
        {
            get => m_DataTypeHandle;
            set
            {
                m_DataTypeHandle = value;
                m_PortDataTypeCache = null;
            }
        }

        /// <inheritdoc />
        public Type PortDataType
        {
            get
            {
                if (m_PortDataTypeCache == null)
                {
                    Type t = DataTypeHandle.Resolve();
                    m_PortDataTypeCache = t == typeof(void) || t.ContainsGenericParameters ? typeof(Unknown) : t;
                }
                return m_PortDataTypeCache;
            }
        }

        /// <summary>
        /// Notifies the graph model that the port unique name has changed. Derived implementations of PortModel
        /// should call this method whenever a change makes <see cref="UniqueName"/> return a different value than before.
        /// </summary>
        /// <param name="oldUniqueName">The previous name.</param>
        /// <param name="newUniqueName">The new name.</param>
        protected void OnUniqueNameChanged(string oldUniqueName, string newUniqueName)
        {
            (GraphModel as GraphModel)?.PortEdgeIndex.UpdatePortUniqueName(this, oldUniqueName, newUniqueName);
        }

        /// <summary>
        /// Notifies the graph model that the port direction has changed. Derived implementations of PortModel
        /// should call this method whenever a change makes <see cref="Direction"/> return a different value than before.
        /// </summary>
        /// <param name="oldDirection">The previous direction.</param>
        /// <param name="newDirection">The new direction.</param>

        protected void OnDirectionChanged(PortDirection oldDirection, PortDirection newDirection)
        {
            (GraphModel as GraphModel)?.PortEdgeIndex.UpdatePortDirection(this, oldDirection, newDirection);
        }

        /// <inheritdoc />
        public virtual IEnumerable<IPortModel> GetConnectedPorts()
        {
            return PortModelDefaultImplementations.GetConnectedPorts(this);
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<IEdgeModel> GetConnectedEdges()
        {
            return PortModelDefaultImplementations.GetConnectedEdges(this);
        }

        /// <inheritdoc />
        public virtual bool IsConnectedTo(IPortModel otherPort)
        {
            return PortModelDefaultImplementations.IsConnectedTo(this, otherPort);
        }

        /// <inheritdoc />
        public virtual bool HasReorderableEdges => PortType == PortType.Execution && Direction == PortDirection.Output && this.IsConnected();

        /// <inheritdoc />
        public IConstant EmbeddedValue
        {
            get
            {
                if (NodeModel is NodeModel node && node.InputConstantsById.TryGetValue(UniqueName, out var inputModel))
                {
                    return inputModel;
                }

                return null;
            }
        }

        /// <summary>
        /// The tooltip for the port.
        /// </summary>
        /// <remarks>
        /// If the tooltip is not set, or if it's set to null, the default value for the tooltip will be returned.
        /// The default tooltip is "[Input|Output] execution flow" for execution ports (e.g. "Output execution flow"
        /// and "[Input|Output] of type (friendly name of the port type)" for data ports (e.g. "Input of type Float").
        /// </remarks>
        public virtual string ToolTip
        {
            get
            {
                if (m_TooltipOverride != null)
                    return m_TooltipOverride;

                if (m_TooltipCache == null)
                {
                    var newTooltip = new StringBuilder(Direction == PortDirection.Output ? "Output" : "Input");
                    if (PortType == PortType.Execution)
                    {
                        newTooltip.Append(" execution flow");
                    }
                    else if (PortType == PortType.Data)
                    {
                        var stencil = GraphModel.Stencil;
                        newTooltip.Append($" of type {DataTypeHandle.GetMetadata(stencil).FriendlyName}");
                    }

                    m_TooltipCache = newTooltip.ToString();
                }

                return m_TooltipCache;
            }

            set => m_TooltipOverride = value;
        }

        /// <inheritdoc />
        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Title}(id: {UniqueName ?? "\"\""})";
        }

        /// <inheritdoc />
        public void ReorderEdge(IEdgeModel edgeModel, ReorderType reorderType)
        {
            (GraphModel as GraphModel)?.ReorderEdge(edgeModel, reorderType);
        }

        /// <inheritdoc />
        public int GetEdgeOrder(IEdgeModel edge)
        {
            return ReorderableEdgesPortDefaultImplementations.GetEdgeOrder(this, edge);
        }
    }
}
