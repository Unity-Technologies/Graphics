using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestViewSelection : ViewSelection
    {
        GraphViewTester m_GraphViewTester;

        protected override bool UseInternalClipboard => true;
        public override IEnumerable<IGraphElementModel> SelectableModels
        {
            get => m_GraphModelState.GraphModel.GraphElementModels;
        }

        /// <inheritdoc />
        public TestViewSelection(RootView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, GraphViewTester graphViewTester)
            : base(view, graphModelState, selectionState)
        {
            m_GraphViewTester = graphViewTester;
        }

        protected override CopyPasteData BuildCopyPasteData(HashSet<IGraphElementModel> elementsToCopySet)
        {
            var copyPaste = CopyPasteData.GatherCopiedElementsData(null, elementsToCopySet.ToList());
            return copyPaste;
        }

        protected override string SerializedPasteData(CopyPasteData copyPasteData)
        {
            // A real implementation would serialize all necessary GraphElement data.
            var count = copyPasteData.edges.Count + copyPasteData.nodes.Count + copyPasteData.placemats.Count +
                copyPasteData.stickyNotes.Count + copyPasteData.variableDeclarations.Count;
            if (count > 0)
            {
                return count + " serialized elements";
            }

            return string.Empty;
        }

        protected override bool CanPasteSerializedData(string data)
        {
            if (data.StartsWith(k_SerializedDataMimeType))
            {
                data = data.Substring(k_SerializedDataMimeType.Length + 1);
            }

            // Check if the data starts with an int. That's what we need for pasting.
            int count = int.Parse(data.Split(' ')[0]);
            return count > 0;
        }

        protected override void UnserializeAndPaste(PasteOperation operation, string operationName, string data)
        {
            if (data.StartsWith(k_SerializedDataMimeType))
            {
                data = data.Substring(k_SerializedDataMimeType.Length + 1);
            }

            int count = int.Parse(data.Split(' ')[0]);
            for (int i = 0; i < count; ++i)
            {
                m_GraphViewTester.CreateNode("Pasted element " + i);
            }
        }
    }
}
