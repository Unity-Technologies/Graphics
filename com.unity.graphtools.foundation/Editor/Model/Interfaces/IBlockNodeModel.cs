namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The model for a block: a node that is owned by a context.
    /// </summary>
    public interface IBlockNodeModel : IInputOutputPortsNodeModel
    {
        /// <summary>
        /// The context the node belongs to
        /// </summary>
        IContextNodeModel ContextNodeModel { get; set; }

        /// <summary>
        /// The index of the position in the context.
        /// </summary>
        int GetIndex();

        /// <summary>
        /// Check whether this block node is compatible with the given context.
        /// </summary>
        /// <param name="context">The context node to test compatibility with.</param>
        /// <returns>Whether this block node is compatible with the given context.</returns>
        bool IsCompatibleWith(IContextNodeModel context);
    }
}
