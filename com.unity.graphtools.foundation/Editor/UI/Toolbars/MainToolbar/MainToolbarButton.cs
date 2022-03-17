#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for the main toolbar buttons.
    /// </summary>
    public abstract class MainToolbarButton : EditorToolbarButton, IAccessContainerWindow, IToolbarElement
    {
        ToolbarUpdateObserver m_UpdateObserver;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// The graph tool.
        /// </summary>
        public BaseGraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainToolbarButton"/> class.
        /// </summary>
        protected MainToolbarButton()
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
                    m_UpdateObserver = new ToolbarUpdateObserver(this, GraphTool.ToolState);
                    GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
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
            GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
            m_UpdateObserver = null;
        }

        /// <summary>
        /// Handles clicks on the button.
        /// </summary>
        protected abstract void OnClick();

        /// <summary>
        /// Updates the button.
        /// </summary>
        public virtual void Update()
        {
            bool enabled = GraphTool?.ToolState.GraphModel != null;
            SetEnabled(enabled);
        }
    }
}
#endif
