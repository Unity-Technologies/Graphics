using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface to provide different <see cref="SearcherFilter"/> depending on context.
    /// </summary>
    public interface ISearcherFilterProvider
    {
        /// <summary>
        /// Gets a filter to apply for general search in a graph.
        /// </summary>
        /// <returns>The search filter.</returns>
        SearcherFilter GetGraphSearcherFilter();

        /// <summary>
        /// Gets a filter to apply during a search for graph element connecting to outputs.
        /// </summary>
        /// <param name="portModels">Ports to connect the search result to.</param>
        /// <returns>The search filter.</returns>
        SearcherFilter GetOutputToGraphSearcherFilter(IEnumerable<IPortModel> portModels);

        /// <summary>
        /// Gets a filter to apply during a search for graph element connecting to an output.
        /// </summary>
        /// <param name="portModel">Port to connect the search result to.</param>
        /// <returns>The search filter.</returns>
        SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel);

        /// <summary>
        /// Gets a filter to apply during a search for graph element connecting to inputs.
        /// </summary>
        /// <param name="portModels">Ports to connect the search result to.</param>
        /// <returns>The search filter.</returns>
        SearcherFilter GetInputToGraphSearcherFilter(IEnumerable<IPortModel> portModels);

        /// <summary>
        /// Gets a filter to apply during a search for graph element connecting to an input.
        /// </summary>
        /// <param name="portModel">Port to connect the search result to.</param>
        /// <returns>The search filter.</returns>
        SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel);

        /// <summary>
        /// Gets a filter to apply during a search for graph element connecting to an input.
        /// </summary>
        /// <param name="edgeModel">Edge to connect the search result to.</param>
        /// <returns>The search filter.</returns>
        SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel);

        /// <summary>
        /// Returns the searcher filter for a given context.
        /// </summary>
        /// <param name="contextNodeModel">The context the filter references.</param>
        /// <returns>The searcher filter for a given context.</returns>
        SearcherFilter GetContextSearcherFilter(IContextNodeModel contextNodeModel);
    }
}
