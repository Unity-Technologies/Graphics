using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlackboardViewSelection : ViewSelection
    {
        protected readonly BlackboardViewStateComponent m_BlackboardViewState;

        /// <inheritdoc />
        public override IEnumerable<IGraphElementModel> SelectableModels
        {
            get => m_GraphModelState.GraphModel.SectionModels.SelectMany(t=> t.ContainedModels).Where(t => t.IsSelectable());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewSelection"/> class.
        /// </summary>
        /// <param name="view">The view used to dispatch commands.</param>
        /// <param name="viewModel">The blackboard view model.</param>
        public BlackboardViewSelection(RootView view, BlackboardViewModel viewModel)
            : base(view, viewModel.GraphModelState, viewModel.SelectionState)
        {
            m_BlackboardViewState = viewModel.ViewState;
        }

        /// <inheritdoc />
        protected override CopyPasteData BuildCopyPasteData(HashSet<IGraphElementModel> elementsToCopySet)
        {
            var copyPaste = CopyPasteData.GatherCopiedElementsData(m_BlackboardViewState, elementsToCopySet.ToList());
            return copyPaste;
        }
    }
}
