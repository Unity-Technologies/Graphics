using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [PublicAPI]
    public class ClassSearcherFilterProvider : ISearcherFilterProvider
    {
        readonly Stencil m_Stencil;

        public ClassSearcherFilterProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public virtual SearcherFilter GetGraphSearcherFilter() => null;

        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel) => null;
        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IEnumerable<IPortModel> portModel) => null;

        public virtual SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel) => null;
        public virtual SearcherFilter GetInputToGraphSearcherFilter(IEnumerable<IPortModel> portModels) => null;

        public virtual SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel) => null;

        public virtual SearcherFilter GetContextSearcherFilter(IContextNodeModel contextNodeModel) => null;
    }
}
