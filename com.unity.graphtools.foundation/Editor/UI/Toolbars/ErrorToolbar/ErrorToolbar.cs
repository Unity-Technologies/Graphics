using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A <see cref="Toolbar"/> to display and navigate errors.
    /// </summary>
    public class ErrorToolbar : Toolbar
    {
        VisualElement m_ErrorIconLabel;
        ToolbarButton m_PreviousErrorButton;
        ToolbarButton m_NextErrorButton;
        Label m_ErrorCounterLabel;
        int m_CurrentErrorIndex;

        GraphProcessingStateComponent GraphProcessingState => GraphTool?.GraphProcessingState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorToolbar"/> class.
        /// </summary>
        /// <param name="graphTool">The <see cref="BaseGraphTool"/> of the toolbar.</param>
        /// <param name="graphView">The graph view to which to attach the toolbar.</param>
        public ErrorToolbar(BaseGraphTool graphTool, GraphView graphView) : base(graphTool, graphView)
        {
            name = "errorToolbar";
            this.AddStylesheet("ErrorToolbar.uss");
            var tpl = GraphElementHelper.LoadUxml("ErrorToolbar.uxml");
            tpl.CloneTree(this);

            m_ErrorIconLabel = this.MandatoryQ("errorIconLabel");

            m_PreviousErrorButton = this.MandatoryQ<ToolbarButton>("previousErrorButton");
            m_PreviousErrorButton.tooltip = "Go To Previous Error";
            m_PreviousErrorButton.ChangeClickEvent(OnPreviousErrorButton);

            m_NextErrorButton = this.MandatoryQ<ToolbarButton>("nextErrorButton");
            m_NextErrorButton.tooltip = "Go To Next Error";
            m_NextErrorButton.ChangeClickEvent(OnNextErrorButton);

            m_ErrorCounterLabel = this.MandatoryQ<Label>("errorCounterLabel");

            m_CurrentErrorIndex = 0;
        }

        void OnPreviousErrorButton()
        {
            var errors = GraphProcessingState.RawErrors;
            var errorCount = errors?.Count ?? 0;
            if (errors != null && errorCount > 0)
            {
                m_CurrentErrorIndex--;
                if (m_CurrentErrorIndex < 0)
                    m_CurrentErrorIndex = errorCount - 1;

                FrameAndSelectElement(errors[m_CurrentErrorIndex].SourceNodeGuid);
            }
        }

        void OnNextErrorButton()
        {
            var errors = GraphProcessingState.RawErrors;
            var errorCount = errors?.Count ?? 0;
            if (errors != null && errorCount > 0)
            {
                m_CurrentErrorIndex++;
                if (m_CurrentErrorIndex >= errorCount)
                    m_CurrentErrorIndex = 0;

                FrameAndSelectElement(errors[m_CurrentErrorIndex].SourceNodeGuid);
            }
        }

        void FrameAndSelectElement(SerializableGUID errorModelGuid)
        {
            if (GraphTool.ToolState.GraphModel.TryGetModelFromGuid(errorModelGuid, out var errorModel))
            {
                var ui = errorModel.GetView<GraphElement>(GraphView);
                if (ui != null)
                {
                    GraphView.DispatchFrameAndSelectElementsCommand(true, ui);
                }
            }
        }

        public void UpdateUI()
        {
            IGraphModel graphModel = GraphTool?.ToolState.GraphModel;
            int errorCount = GraphProcessingState.RawErrors?.Count ?? 0;
            bool enabled = (graphModel != null) && (errorCount > 0);

            m_ErrorIconLabel.SetEnabled(enabled);
            m_PreviousErrorButton.SetEnabled(enabled);
            m_NextErrorButton.SetEnabled(enabled);

            m_ErrorCounterLabel.SetEnabled(enabled);
            m_ErrorCounterLabel.text = errorCount + (errorCount == 1 ? " error" : " errors");
        }
    }
}
