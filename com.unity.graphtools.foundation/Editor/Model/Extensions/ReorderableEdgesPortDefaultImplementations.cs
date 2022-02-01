using System.Linq;
#pragma warning disable 1573

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementations for <see cref="IReorderableEdgesPortModel"/>.
    /// </summary>
    /// <remarks>
    /// These methods should only be called by <see cref="IReorderableEdgesPortModel"/> related methods.
    /// </remarks>
    public static class ReorderableEdgesPortDefaultImplementations
    {
        /// <inheritdoc  cref="IReorderableEdgesPortModel.MoveEdgeFirst"/>
        /// <param name="self">The port.</param>
        public static void MoveEdgeFirst(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveAfter(new[] { edge }, null);
        }

        /// <inheritdoc  cref="IReorderableEdgesPortModel.MoveEdgeUp"/>
        /// <param name="self">The port.</param>
        public static void MoveEdgeUp(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx >= 1)
                self.GraphModel.MoveBefore(new[] { edge }, edges[idx - 1]);
        }

        /// <inheritdoc  cref="IReorderableEdgesPortModel.MoveEdgeDown"/>
        /// <param name="self">The port.</param>
        public static void MoveEdgeDown(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx < edges.Count - 1)
                self.GraphModel.MoveAfter(new[] { edge }, edges[idx + 1]);
        }

        /// <inheritdoc  cref="IReorderableEdgesPortModel.MoveEdgeLast"/>
        /// <param name="self">The port.</param>
        public static void MoveEdgeLast(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveBefore(new[] { edge }, null);
        }

        /// <inheritdoc  cref="IReorderableEdgesPortModel.GetEdgeOrder"/>
        /// <param name="self">The port from which the edge ir originating.</param>
        public static int GetEdgeOrder(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            var edges = self.GetConnectedEdges().ToList();
            return edges.IndexOf(edge);
        }
    }
}
