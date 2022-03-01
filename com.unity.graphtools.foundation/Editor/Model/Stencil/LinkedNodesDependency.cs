namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Represents a dependency between two nodes linked together by an edge.
    /// </summary>
    public class LinkedNodesDependency : IDependency
    {
        /// <summary>
        /// The dependent port.
        /// </summary>
        public IPortModel DependentPort { get; set; }

        /// <summary>
        /// The parent port.
        /// </summary>
        public IPortModel ParentPort { get; set; }

        /// <inheritdoc />
        public INodeModel DependentNode => DependentPort.NodeModel;

        /// <summary>
        /// The number of such a dependency in a graph.
        /// </summary>
        public int Count { get; set; }
    }
}
