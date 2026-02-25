

namespace Unity.GraphCommon.LowLevel.Editor
{
    /*public*/ partial class GraphTraverser
    {
        /// <summary>
        /// Delegate called when visiting a <see cref="TaskNode"/>.
        /// </summary>
        /// <param name="taskNode">The node being visited.</param>
        /// <returns>true to continue traversal, false to stop.</returns>
        public delegate bool OnVisitTaskNode(TaskNode taskNode);

        /// <summary>
        /// Delegate called when visiting a <see cref="DataNode"/>.
        /// </summary>
        /// <param name="dataNode">The node being visited.</param>
        /// <returns>true to continue traversal, false to stop.</returns>
        public delegate bool OnVisitDataNode(DataNode dataNode);

        /// <summary>
        /// Delegate called to filter a <see cref="TaskNode"/>.
        /// </summary>
        /// <param name="taskNode">The node being filtered.</param>
        /// <returns>The traversal control used to determine whether to visit the node and/or continue graph traversal.</returns>
        public delegate TraversalControl OnFilterTaskNode(TaskNode taskNode);

        /// <summary>
        /// Delegate called to filter a <see cref="DataNode"/>.
        /// </summary>
        /// <param name="dataNode">Thhe node being filtered.</param>
        /// <returns>The traversal control used to determine whether to visit the node and/or continue graph traversal.</returns>
        public delegate TraversalControl OnFilterDataNode(DataNode dataNode);
    }
}
