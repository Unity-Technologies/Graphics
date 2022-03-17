namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for graph processors.
    /// </summary>
    public interface IGraphProcessor
    {
        /// <summary>
        /// Processes the graph.
        /// </summary>
        /// <param name="graphModel">The graph to process.</param>
        /// <param name="changes">A description of what changed in the graph. If null, the method assumes everything changed.</param>
        /// <returns>The results of the processing.</returns>
        GraphProcessingResult ProcessGraph(IGraphModel graphModel, GraphChangeDescription changes);
    }
}
