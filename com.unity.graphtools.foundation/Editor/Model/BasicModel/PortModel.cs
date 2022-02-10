using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a port in a node.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class PortModel : GraphElementModel, IReorderableEdgesPortModel, IHasTitle
    {
        string m_UniqueId;

        string m_Title;
        PortType m_PortType;
        TypeHandle m_DataTypeHandle;

        string m_DisplayTitleCache;
        Type m_PortDataTypeCache;

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
                m_DisplayTitleCache = null;
                OnUniqueNameChanged(oldUniqueName, UniqueName);
            }
        }

        /// <inheritdoc />
        public virtual string DisplayTitle
        {
            get
            {
                if (m_DisplayTitleCache == null)
                    m_DisplayTitleCache = Title.Nicify();
                return m_DisplayTitleCache;
            }
        }

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
        public PortDirection Direction { get; set; }

        /// <inheritdoc />
        public PortOrientation Orientation { get; set; }

        /// <inheritdoc />
        public virtual PortCapacity Capacity => NodeModel?.GetPortCapacity(this) ?? GetDefaultCapacity();

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

        /// <inheritdoc />
        public bool DisableEmbeddedValueEditor => this.IsConnected() && GetConnectedPorts().Any(p => p.NodeModel.State == ModelState.Enabled);

        string m_TooltipCache = null;
        /// <inheritdoc />
        public virtual string ToolTip
        {
            get
            {
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

            // We don't support setting the tooltip for base port models.
            set {}
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (AssetModel == null && NodeModel != null)
                AssetModel = NodeModel.AssetModel;
        }

        /// <inheritdoc />
        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Title}(id: {UniqueName ?? "\"\""})";
        }

        /// <inheritdoc />
        public PortCapacity GetDefaultCapacity()
        {
            return PortType == PortType.Data ? Direction == PortDirection.Input ? PortCapacity.Single :
                PortCapacity.Multi :
                PortCapacity.Multi;
        }

        /// <inheritdoc />
        public virtual void MoveEdgeFirst(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeFirst(this, edge);
        }

        /// <inheritdoc />
        public virtual void MoveEdgeUp(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeUp(this, edge);
        }

        /// <inheritdoc />
        public virtual void MoveEdgeDown(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeDown(this, edge);
        }

        /// <inheritdoc />
        public virtual void MoveEdgeLast(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeLast(this, edge);
        }

        /// <inheritdoc />
        public int GetEdgeOrder(IEdgeModel edge)
        {
            return ReorderableEdgesPortDefaultImplementations.GetEdgeOrder(this, edge);
        }
    }
}
