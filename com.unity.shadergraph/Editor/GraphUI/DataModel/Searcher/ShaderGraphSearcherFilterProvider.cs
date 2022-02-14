using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphSearcherFilterProvider : ISearcherFilterProvider
    {
        static readonly SearcherFilter k_NoOpFilter = new SearcherFuncFilter(_ => true);

        public SearcherFilter GetGraphSearcherFilter() => k_NoOpFilter;

        public SearcherFilter GetOutputToGraphSearcherFilter(IEnumerable<IPortModel> portModels) => k_NoOpFilter;

        public SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel) => k_NoOpFilter;

        public SearcherFilter GetInputToGraphSearcherFilter(IEnumerable<IPortModel> portModels) => k_NoOpFilter;

        public SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel) => k_NoOpFilter;

        public SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel) => k_NoOpFilter;

        public SearcherFilter GetContextSearcherFilter(IContextNodeModel contextNodeModel) => k_NoOpFilter;
    }
}
