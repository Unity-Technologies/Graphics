using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using Unity.ItemLibrary.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphSearcherFilterProvider : ILibraryFilterProvider
    {
        // FIXME: GTF Internal usage.
        static readonly ItemLibraryFilter k_NoOpFilter = new ItemLibraryFuncFilter_Internal(_ => true);

        public ItemLibraryFilter GetGraphFilter() => k_NoOpFilter;

        public ItemLibraryFilter GetOutputToGraphFilter(IEnumerable<PortModel> portModels) => k_NoOpFilter;

        public ItemLibraryFilter GetOutputToGraphFilter(PortModel portModel) => k_NoOpFilter;

        public ItemLibraryFilter GetInputToGraphFilter(IEnumerable<PortModel> portModels) => k_NoOpFilter;

        public ItemLibraryFilter GetInputToGraphFilter(PortModel portModel) => k_NoOpFilter;

        public ItemLibraryFilter GetWireFilter(WireModel edgeModel) => k_NoOpFilter;

        public ItemLibraryFilter GetContextFilter(ContextNodeModel contextNodeModel) => k_NoOpFilter;
    }
}
