using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Computes the changes in connected edges on a node.
    /// </summary>
    public class NodeEdgeDiff
    {
        IPortNodeModel m_NodeModel;
        PortDirection m_Direction;
        List<IEdgeModel> m_InitialEdges;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEdgeDiff"/> class.
        /// </summary>
        /// <param name="nodeModel">The node for which we want to track changes in connected edges.</param>
        /// <param name="portDirection">Specifies whether we should track connections on inputs ports, output ports or both.</param>
        public NodeEdgeDiff(IPortNodeModel nodeModel, PortDirection portDirection)
        {
            m_NodeModel = nodeModel;
            m_Direction = portDirection;
            m_InitialEdges = GetEdges().ToList();
        }

        /// <summary>
        /// Returns the edges that were added since the <see cref="NodeEdgeDiff"/> object was created.
        /// </summary>
        /// <returns>The edges that were added.</returns>
        public IEnumerable<IEdgeModel> GetAddedEdges()
        {
            var initialEdges = new HashSet<IEdgeModel>(m_InitialEdges);

            foreach (var edge in GetEdges())
            {
                if (!initialEdges.Contains(edge))
                {
                    yield return edge;
                }
            }
        }

        /// <summary>
        /// Returns the edges that were removed since the <see cref="NodeEdgeDiff"/> object was created.
        /// </summary>
        /// <returns>The edges that were removed.</returns>
        public IEnumerable<IEdgeModel> GetDeletedEdges()
        {
            var currentEdges = new HashSet<IEdgeModel>(GetEdges());

            foreach (var edge in m_InitialEdges)
            {
                if (!currentEdges.Contains(edge))
                {
                    yield return edge;
                }
            }
        }

        IEnumerable<IEdgeModel> GetEdges()
        {
            IEnumerable<IPortModel> ports = null;
            switch (m_Direction)
            {
                case PortDirection.None:
                    ports = m_NodeModel.Ports;
                    break;

                case PortDirection.Input:
                    ports = (m_NodeModel as IInputOutputPortsNodeModel)?.InputsByDisplayOrder ?? m_NodeModel.Ports;
                    break;

                case PortDirection.Output:
                    ports = (m_NodeModel as IInputOutputPortsNodeModel)?.OutputsByDisplayOrder ?? m_NodeModel.Ports;
                    break;
            }

            return ports?.SelectMany(p => p.GetConnectedEdges()) ?? Enumerable.Empty<IEdgeModel>();
        }
    }
}
