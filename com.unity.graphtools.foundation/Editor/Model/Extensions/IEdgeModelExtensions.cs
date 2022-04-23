namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for edges.
    /// </summary>
    public static class IEdgeModelExtensions
    {
        /// <summary>
        /// Gets the opposite side of an <see cref="EdgeSide"/>.
        /// </summary>
        /// <param name="side">The side to get the opposite of.</param>
        /// <returns>The opposite side.</returns>
        public static EdgeSide GetOtherSide(this EdgeSide side) => side == EdgeSide.From ? EdgeSide.To : EdgeSide.From;

        /// <summary>
        /// Gets the port of an edge on a specific side.
        /// </summary>
        /// <param name="edgeModel">The edge to get the port from.</param>
        /// <param name="side">The side of the edge to get the port from.</param>
        /// <returns>The port connected to the side of the edge.</returns>
        public static IPortModel GetPort(this IEdgeModel edgeModel, EdgeSide side)
        {
            return side == EdgeSide.To ? edgeModel.ToPort : edgeModel.FromPort;
        }

        /// <summary>
        /// Gets the port of an edge on the other side.
        /// </summary>
        /// <param name="edgeModel">The edge to get the port from.</param>
        /// <param name="otherSide">The other side of the edge to get the port from.</param>
        /// <returns>The port connected to the other side of the edge.</returns>
        public static IPortModel GetOtherPort(this IEdgeModel edgeModel, EdgeSide otherSide) =>
            edgeModel.GetPort(otherSide.GetOtherSide());

        /// <summary>
        /// Gets the port of an edge on a specific side.
        /// </summary>
        /// <param name="edgeModel">The edge to set the port on.</param>
        /// <param name="side">The side of the edge on which to set the port.</param>
        /// <param name="value">The new port the edge should have.</param>
        public static void SetPort(this IEdgeModel edgeModel, EdgeSide side, IPortModel value)
        {
            if (side == EdgeSide.From)
                edgeModel.FromPort = value;
            else
                edgeModel.ToPort = value;
        }

        /// <summary>
        /// Sets the other side port of an edge.
        /// </summary>
        /// <param name="edgeModel">The edge to set the port on.</param>
        /// <param name="otherSide">The other side of the edge on which to set the port.</param>
        /// <param name="value">The new port the edge should have on the other side.</param>
        public static void SetOtherPort(this IEdgeModel edgeModel, EdgeSide otherSide, IPortModel value) =>
            edgeModel.SetPort(otherSide.GetOtherSide(), value);
    }
}
