namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface to be implemented by a <see cref="INodeModel"/> when the local inspector should display some
    /// other object instead of the node.
    /// </summary>
    public interface IHasInspectorSurrogate
    {
        object Surrogate { get; }
    }
}
