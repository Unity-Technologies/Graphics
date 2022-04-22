using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for root views.
    /// </summary>
    /// <remarks>Root views are model views that can receive commands. They are also usually being updated by an observer.</remarks>
    public abstract class RootView : VisualElement, IRootView
    {
        public static readonly string ussClassName = "ge-view";
        public static readonly string focusedViewModifierUssClassName = ussClassName.WithUssModifier("focused");

        /// <inheritdoc />
        public BaseGraphTool GraphTool { get; }

        /// <inheritdoc />
        public EditorWindow Window { get; }

        /// <summary>
        /// The model backing this view.
        /// </summary>
        public RootViewModel Model { get; protected set; }

        /// <summary>
        /// The parent command target.
        /// </summary>
        public virtual ICommandTarget Parent => GraphTool;

        /// <summary>
        /// The dispatcher.
        /// </summary>
        /// <remarks>To dispatch a command, use <see cref="Dispatch"/>. This will ensure the command is also dispatched to parent dispatchers.</remarks>
        public Dispatcher Dispatcher { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        protected RootView(EditorWindow window, BaseGraphTool graphTool)
        {
            focusable = true;

            GraphTool = graphTool;
            Dispatcher = new CommandDispatcher();
            Window = window;

            AddToClassList(ussClassName);
            this.AddStylesheetWithSkinVariants("View.uss");

            RegisterCallback<FocusInEvent>(OnFocus);
            RegisterCallback<FocusOutEvent>(OnLostFocus);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        /// <inheritdoc />
        public virtual void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            Dispatcher.Dispatch(command, diagnosticsFlags);
            Parent?.Dispatch(command, diagnosticsFlags);
        }

        /// <inheritdoc />
        public abstract void BuildUI();

        /// <inheritdoc />
        public abstract void UpdateFromModel();

        /// <summary>
        /// Registers all observers.
        /// </summary>
        protected abstract void RegisterObservers();

        /// <summary>
        /// Unregisters all observers.
        /// </summary>
        protected abstract void UnregisterObservers();

        void OnFocus(FocusInEvent e)
        {
            // View is focused if itself or any of its descendant has focus.
            AddToClassList(focusedViewModifierUssClassName);
        }

        void OnLostFocus(FocusOutEvent e)
        {
            RemoveFromClassList(focusedViewModifierUssClassName);
        }

        /// <summary>
        /// Callback for the <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnEnterPanel(AttachToPanelEvent e)
        {
            BuildUI();
            Model?.AddToState(GraphTool?.State);
            RegisterObservers();
            UpdateFromModel();
        }

        /// <summary>
        /// Callback for the <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLeavePanel(DetachFromPanelEvent e)
        {
            UnregisterObservers();
            Model?.RemoveFromState(GraphTool?.State);
        }
    }
}
