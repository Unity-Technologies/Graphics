using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementations for some <see cref="IPortModel"/> methods.
    /// </summary>
    /// <remarks>
    /// These methods should only be called by <see cref="IPortModel"/> related methods.
    /// </remarks>
    public static class PortModelDefaultImplementations
    {
        /// <inheritdoc cref="IPortModel.GetConnectedPorts"/>
        /// <param name="self">The port for which we want to get the connected ports.</param>
        public static IEnumerable<IPortModel> GetConnectedPorts(IPortModel self)
        {
            if (self?.GraphModel == null)
                return Enumerable.Empty<IPortModel>();

            return self.GraphModel.GetEdgesForPort(self)
                .Select(e => self.Direction == PortDirection.Input ? e.FromPort : e.ToPort)
                .Where(p => p != null);
        }

        static readonly IReadOnlyList<IEdgeModel> k_EmptyEdgeList = new List<IEdgeModel>();

        /// <inheritdoc cref="IPortModel.GetConnectedEdges"/>
        /// <param name="self">The port for which we want to get the connected edges.</param>
        public static IReadOnlyList<IEdgeModel> GetConnectedEdges(IPortModel self)
        {
            return self?.GraphModel?.GetEdgesForPort(self) ?? k_EmptyEdgeList;
        }

        /// <inheritdoc cref="IPortModel.IsConnectedTo"/>
        /// <param name="self">The first port.</param>
#pragma warning disable 1573
        public static bool IsConnectedTo(IPortModel self, IPortModel otherPort)
#pragma warning restore 1573
        {
            if (self?.GraphModel == null)
                return false;

            var edgeModels = self.GraphModel.GetEdgesForPort(self);

            foreach (var e in edgeModels)
            {
                if (e.ToPort == otherPort || e.FromPort == otherPort)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
