#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for error toolbar buttons.
    /// </summary>
    public abstract class ErrorToolbarButton : EditorToolbarButton, IAccessContainerWindow, IToolbarElement
    {
        ErrorToolbarUpdateObserver m_UpdateObserver;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// The graph tools.
        /// </summary>
        protected BaseGraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <summary>
        /// The graph view.
        /// </summary>
        protected GraphView GraphView => (containerWindow as GraphViewEditorWindow)?.GraphView;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorToolbarButton"/> class.
        /// </summary>
        protected ErrorToolbarButton()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            clicked += OnClick;
        }

        /// <summary>
        /// Event handler for <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (GraphTool != null)
            {
                if (m_UpdateObserver == null)
                {
                    m_UpdateObserver = new ErrorToolbarUpdateObserver(this, GraphTool.ToolState, GraphTool.GraphProcessingState);
                    GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
                }

                if (GraphView != null)
                {
                    GraphTool.Dispatcher.RegisterCommandHandler<GraphViewStateComponent, SetCurrentErrorCommand>(
                        SetCurrentErrorCommand.DefaultCommandHandler, GraphView.GraphViewModel.GraphViewState);
                }
            }

            Update();
        }

        /// <summary>
        /// Event handler for <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            GraphTool?.Dispatcher?.UnregisterCommandHandler<SetCurrentErrorCommand>();
            GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
            m_UpdateObserver = null;
        }

        /// <summary>
        /// Handles click on the button.
        /// </summary>
        protected abstract void OnClick();

        /// <summary>
        /// Updates the button.
        /// </summary>
        public virtual void Update()
        {
            bool enabled = GraphTool?.ToolState.GraphModel != null;
            bool hasError = GraphTool?.GraphProcessingState.Errors.Count > 0;
            SetEnabled(enabled && hasError);
        }

        /// <summary>
        /// Sends a command to reframe the graph view to show error at <see cref="index"/>.
        /// </summary>
        /// <param name="index">The index of the error.</param>
        protected void FrameAndSelectError(int index)
        {
            var errors = GraphTool?.GraphProcessingState.RawErrors;
            if (errors != null && errors.Count > 0)
            {
                if (index >= errors.Count)
                    index = 0;

                if (index < 0)
                    index = errors.Count - 1;

                var errorModelGuid = errors[index].SourceNodeGuid;
                if (GraphTool.ToolState.GraphModel.TryGetModelFromGuid(errorModelGuid, out var errorModel))
                {
                    var ui = errorModel.GetView<GraphElement>(GraphView);
                    if (ui != null)
                    {
                        var rectToFit = GraphView.CalculateRectToFitElements(new[] { ui });
                        GraphView.CalculateFrameTransform(rectToFit, GraphView.layout, GraphView.frameBorder, out var frameTranslation, out var frameScaling);

                        GraphTool.Dispatch(new SetCurrentErrorCommand(index, frameTranslation, frameScaling));
                    }
                }
            }
        }
    }
}
#endif
