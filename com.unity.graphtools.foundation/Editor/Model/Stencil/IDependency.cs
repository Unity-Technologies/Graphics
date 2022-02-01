namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface that represents a dependency.
    /// </summary>
    public interface IDependency
    {
        /// <summary>
        /// The dependant node in the dependency.
        /// </summary>
        INodeModel DependentNode { get; }
    }
}
