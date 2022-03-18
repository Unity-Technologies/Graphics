#if UNITY_2022_2_OR_NEWER
using System;
using System.Linq;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [EditorToolbarElement("GTF/MathBook Sample/Evaluation Result", typeof(SimpleGraphViewWindow))]
    public class EvaluationResultLabel : Label, IAccessContainerWindow, IToolbarElement
    {
        ResultObserver m_UpdateObserver;

        protected BaseGraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        protected EvaluationResultLabel()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (GraphTool != null)
            {
                if (m_UpdateObserver == null)
                    m_UpdateObserver = new ResultObserver(GraphTool.GraphProcessingState, this);
                GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
            }

            Update();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
        }

        /// <inheritdoc />
        public void Update()
        {
            var results = GraphTool?.GraphProcessingState?.RawResults?.OfType<MathBookProcessingResults>().FirstOrDefault();
            var resultString = results?.EvaluationResult;
            text = $"Result: {resultString ?? "---"}";
        }
    }
}
#endif
