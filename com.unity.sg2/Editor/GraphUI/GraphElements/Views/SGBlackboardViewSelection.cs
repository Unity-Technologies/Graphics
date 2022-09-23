using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// This class overrides the blackboards selection and context menu operation handler
    /// We need it in order to handle cut operations with a slightly different logic
    /// </summary>
    class SGBlackboardViewSelection : BlackboardViewSelection
    {
        public SGBlackboardViewSelection(RootView view, BlackboardViewModel viewModel)
            : base(view, viewModel) { }

        ShaderGraphModel shaderGraphModel => m_GraphModelState.GraphModel as ShaderGraphModel;

        protected override void CutSelection()
        {
            shaderGraphModel.isCutOperation = true;
            base.CutSelection();
        }

        protected override void Paste()
        {
            base.Paste();
            shaderGraphModel.isCutOperation = false;
        }
    }
}
