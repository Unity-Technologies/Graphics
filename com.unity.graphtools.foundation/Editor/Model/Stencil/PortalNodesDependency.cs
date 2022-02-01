namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Represents a dependency between two nodes linked together by portal pair.
    /// </summary>
    public class PortalNodesDependency : IDependency
    {
        /// <inheritdoc />
        public INodeModel DependentNode { get; set; }
    }
}
