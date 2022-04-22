using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Implements an index to quickly retrieve the list of edges that are connected to a port.
    /// </summary>
    /// <remarks>
    /// The index needs to be kept up-to-date. In addition to adding and removing edges to it,
    /// it needs to be notified when any of the ports of an edge changes,
    /// by calling <see cref="UpdateEdge"/>, or when <see cref="IPortModel.UniqueName"/> changes, by calling
    /// <see cref="UpdatePortUniqueName"/>.
    /// </remarks>
    class PortEdgeIndex
    {
        static readonly IReadOnlyList<IEdgeModel> k_EmptyEdgeModelList = new List<IEdgeModel>();

        /// <summary>
        /// Used to send 1 edge to the list reordering method.
        /// </summary>
        static readonly List<IEdgeModel> k_OneEdgeList = new List<IEdgeModel>(1) { null };

        IGraphModel m_GraphModel;
        bool m_IsDirty;
        Dictionary<(SerializableGUID nodeGUID, string portUniqueName, PortDirection direction), List<IEdgeModel>> m_EdgesByPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortEdgeIndex"/> class.
        /// </summary>
        public PortEdgeIndex(IGraphModel graphModel)
        {
            m_GraphModel = graphModel;
            m_EdgesByPort = new Dictionary<(SerializableGUID nodeGUID, string portUniqueName, PortDirection direction), List<IEdgeModel>>();
            m_IsDirty = true;
        }

        /// <summary>
        /// Gets the list of edges that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected edges.</param>
        /// <returns>The list of edges connected to the port.</returns>
        public IReadOnlyList<IEdgeModel> GetEdgesForPort(IPortModel portModel)
        {
            if (portModel?.NodeModel == null)
                return k_EmptyEdgeModelList;

            return TryGetEdgesForPort(portModel, out var list) ? list : k_EmptyEdgeModelList;
        }

        /// <summary>
        /// Gets the list of edges that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected edges.</param>
        /// <param name="edgeList">The list of edges connected to the port.</param>
        /// <returns><c>true</c> if the list was found, <c>false</c> otherwise.</returns>
        bool TryGetEdgesForPort(IPortModel portModel, out List<IEdgeModel> edgeList)
        {
            if (m_IsDirty)
                Reindex();

            var key = (portModel.NodeModel.Guid, portModel.UniqueName, portModel.Direction);
            return m_EdgesByPort.TryGetValue(key, out edgeList);
        }

        /// <summary>
        /// Marks the index as needing to be completely rebuilt.
        /// </summary>
        public void MarkDirty()
        {
            m_IsDirty = true;
        }

        /// <summary>
        /// Adds an edge to the index, using its current from and to ports.
        /// </summary>
        /// <param name="edgeModel">The edge to add.</param>
        public void AddEdge(IEdgeModel edgeModel)
        {
            if (m_IsDirty)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            if (edgeModel.FromPort != null)
            {
                var key = (edgeModel.FromPort.NodeModel.Guid, edgeModel.FromPort.UniqueName, edgeModel.FromPort.Direction);
                AddKeyEdge(key, edgeModel);
            }

            if (edgeModel.ToPort != null)
            {
                var key = (edgeModel.ToPort.NodeModel.Guid, edgeModel.ToPort.UniqueName, edgeModel.ToPort.Direction);
                AddKeyEdge(key, edgeModel);
            }

            void AddKeyEdge((SerializableGUID, string, PortDirection) key, IEdgeModel edge)
            {
                if (!m_EdgesByPort.TryGetValue(key, out var edgeList))
                {
                    edgeList = new List<IEdgeModel>();
                    m_EdgesByPort[key] = edgeList;
                }

                if (!edgeList.Contains(edge))
                    edgeList.Add(edge);
            }
        }

        /// <summary>
        /// Updates an edge in the index, when one of its port changes.
        /// </summary>
        /// <param name="edgeModel">The edge to update.</param>
        /// <param name="oldPort">The previous port value.</param>
        /// <param name="newPort">The new port value.</param>
        public void UpdateEdge(IEdgeModel edgeModel, IPortModel oldPort, IPortModel newPort)
        {
            if (m_IsDirty || oldPort == newPort)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            if (oldPort != null)
            {
                var key = (oldPort.NodeModel.Guid, oldPort.UniqueName, oldPort.Direction);
                if (m_EdgesByPort.TryGetValue(key, out var edgeList))
                {
                    edgeList.Remove(edgeModel);
                }
            }
            if (newPort != null)
            {
                var key = (newPort.NodeModel.Guid, newPort.UniqueName, newPort.Direction);
                if (!m_EdgesByPort.TryGetValue(key, out var edgeList))
                {
                    edgeList = new List<IEdgeModel>();
                    m_EdgesByPort[key] = edgeList;
                }

                if (!edgeList.Contains(edgeModel))
                    edgeList.Add(edgeModel);
            }
        }

        /// <summary>
        /// Updates the index when the port unique name changes.
        /// </summary>
        /// <param name="portModel">The port model to update.</param>
        /// <param name="oldName">The old unique name of the port.</param>
        /// <param name="newName">The new unique name of the port.</param>
        public void UpdatePortUniqueName(IPortModel portModel, string oldName, string newName)
        {
            if (m_IsDirty || oldName == newName || oldName == null || newName == null)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            var key = (portModel.NodeModel.Guid, oldName, portModel.Direction);
            if (m_EdgesByPort.TryGetValue(key, out var edgeList))
            {
                m_EdgesByPort.Remove(key);
            }

            var newKey = (portModel.NodeModel.Guid, newName, portModel.Direction);
            m_EdgesByPort[newKey] = edgeList;
        }

        /// <summary>
        /// Updates the index when the port direction changes.
        /// </summary>
        /// <param name="portModel">The port model to update.</param>
        /// <param name="oldDirection">The old direction of the port.</param>
        /// <param name="newDirection">The new direction of the port.</param>
        public void UpdatePortDirection(IPortModel portModel, PortDirection oldDirection, PortDirection newDirection)
        {
            if (m_IsDirty || oldDirection == newDirection)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            var key = (portModel.NodeModel.Guid, portModel.UniqueName, oldDirection);
            if (m_EdgesByPort.TryGetValue(key, out var edgeList))
            {
                m_EdgesByPort.Remove(key);
            }

            var newKey = (portModel.NodeModel.Guid, portModel.UniqueName, newDirection);
            m_EdgesByPort[newKey] = edgeList;
        }

        /// <summary>
        /// Removes an edge from the index.
        /// </summary>
        /// <param name="edgeModel">The edge to remove.</param>
        public void RemoveEdge(IEdgeModel edgeModel)
        {
            if (m_IsDirty || edgeModel == null)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            if (edgeModel.FromPort != null)
            {
                var key = (edgeModel.FromPort.NodeModel.Guid, edgeModel.FromPort.UniqueName, edgeModel.FromPort.Direction);
                RemoveKeyEdge(key, edgeModel);
            }

            if (edgeModel.ToPort != null)
            {
                var key = (edgeModel.ToPort.NodeModel.Guid, edgeModel.ToPort.UniqueName, edgeModel.ToPort.Direction);
                RemoveKeyEdge(key, edgeModel);
            }

            void RemoveKeyEdge((SerializableGUID, string, PortDirection) key, IEdgeModel edge)
            {
                if (m_EdgesByPort.TryGetValue(key, out var edgeList))
                {
                    edgeList.Remove(edge);
                }

                if (edgeList != null && edgeList.Count == 0)
                {
                    m_EdgesByPort.Remove(key);
                }
            }
        }

        void Reindex()
        {
            m_IsDirty = false;

            foreach (var pair in m_EdgesByPort)
            {
                pair.Value.Clear();
            }

            foreach (var edgeModel in m_GraphModel.EdgeModels)
            {
                AddEdge(edgeModel);
            }

            List<(SerializableGUID nodeGUID, string portUniqueName, PortDirection direction)> toRemove = null;
            foreach (var pair in m_EdgesByPort)
            {
                if (pair.Value.Count == 0)
                {
                    toRemove ??= new List<(SerializableGUID nodeGUID, string portUniqueName, PortDirection direction)>();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (var key in toRemove)
                {
                    m_EdgesByPort.Remove(key);
                }
            }
        }

        /// <summary>
        /// Changes the order of an edge among its siblings in the index.
        /// </summary>
        /// <param name="edgeModel">The edge to move.</param>
        /// <param name="reorderType">The type of move to do.</param>
        public void ReorderEdge(IEdgeModel edgeModel, ReorderType reorderType)
        {
            if (TryGetEdgesForPort(edgeModel.FromPort, out var list))
            {
                k_OneEdgeList[0] = edgeModel;
                list.ReorderElements(k_OneEdgeList, reorderType);
            }
            else
            {
                throw new IndexOutOfRangeException($"{edgeModel} not part of the {nameof(PortEdgeIndex)}.");
            }
        }
    }
}
