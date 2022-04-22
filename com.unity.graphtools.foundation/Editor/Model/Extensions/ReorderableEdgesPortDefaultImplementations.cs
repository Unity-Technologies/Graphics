using UnityEngine.GraphToolsFoundation.Overdrive;

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
        /// <inheritdoc  cref="IReorderableEdgesPortModel.GetEdgeOrder"/>
        /// <param name="self">The port from which the edge is originating.</param>
        public static int GetEdgeOrder(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            return self.GetConnectedEdges().IndexOfInternal(edge);
        }
    }
}
