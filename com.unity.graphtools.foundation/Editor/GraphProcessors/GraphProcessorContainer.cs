using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Class that contains graph processors used to process a graph.
    /// </summary>
    public class GraphProcessorContainer
    {
        List<IGraphProcessor> m_GraphProcessors;

        /// <summary>
        /// Adds a graph processor to the container.
        /// </summary>
        /// <param name="graphProcessor">The graph processor.</param>
        public void AddGraphProcessor(IGraphProcessor graphProcessor)
        {
            m_GraphProcessors ??= new List<IGraphProcessor>();
            m_GraphProcessors.Add(graphProcessor);
        }

        /// <summary>
        /// Processes a graph using the container's graph processors.
        /// </summary>
        /// <param name="graphModel">The graph to process.</param>
        /// <param name="changes">A description of what changed in the graph. If null, the method assumes everything changed.</param>
        /// <returns>A list of <see cref="GraphProcessingResult"/>, one for each <see cref="IGraphProcessor"/>.</returns>
        public IReadOnlyList<GraphProcessingResult> ProcessGraph(IGraphModel graphModel, GraphChangeDescription changes)
        {
            var results = new List<GraphProcessingResult>();
            foreach (var graphProcessor in m_GraphProcessors)
            {
                results.Add(graphProcessor.ProcessGraph(graphModel, changes));
            }
            return results;
        }
    }
}
