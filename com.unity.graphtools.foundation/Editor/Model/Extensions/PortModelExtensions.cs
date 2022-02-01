using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for <see cref="IPortModel"/>.
    /// </summary>
    public static class PortModelExtensions
    {
        /// <summary>
        /// Checks whether this port has any connection.
        /// </summary>
        /// <param name="self">The port.</param>
        /// <returns>True if there is at least one edge connected on this port.</returns>
        public static bool IsConnected(this IPortModel self)
        {
            return self.GetConnectedEdges().Any();
        }

        /// <summary>
        /// Checks whether two ports are equivalent.
        /// </summary>
        /// <param name="a">The first port.</param>
        /// <param name="b">The second port.</param>
        /// <returns>True if the two ports are owned by the same node, have the same direction and have the same unique name.</returns>
        public static bool Equivalent(this IPortModel a, IPortModel b)
        {
            if (a == null || b == null)
                return a == b;

            return a.Direction == b.Direction && a.NodeModel.Guid == b.NodeModel.Guid && a.UniqueName == b.UniqueName;
        }
    }
}
