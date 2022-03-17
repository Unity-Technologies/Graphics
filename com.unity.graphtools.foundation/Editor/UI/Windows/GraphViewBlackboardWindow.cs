using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A window to display the blackboard.
    /// </summary>
    public class GraphViewBlackboardWindow : GraphViewToolWindow
    {
        const string k_ToolName = "Blackboard";

        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcutsForBlackboard<GraphViewBlackboardWindow>(k_ToolName);
        }

        BlackboardView m_BlackboardView;

        /// <inheritdoc />
        protected override string ToolName => k_ToolName;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            OnGraphViewChanged();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();

            OnGraphViewChanging();
        }

        /// <summary>
        /// Handles the OnFocus event for the window.
        /// </summary>
        protected virtual void OnFocus()
        {
            //Postpones taking focus to next frame because SyncIMGUIFocus is called directly after the window
            //gets the focus and resets the focus to the root uiElement.
            m_BlackboardView?.schedule.Execute(() => m_BlackboardView?.Focus()).ExecuteLater(0);
        }

        /// <inheritdoc />
        protected override void OnGraphViewChanging()
        {
            if (m_BlackboardView != null)
            {
                m_BlackboardView.RemoveFromHierarchy();
                m_BlackboardView.Dispose();
                m_BlackboardView = null;
            }
        }

        /// <inheritdoc />
        protected override void OnGraphViewChanged()
        {
            if (m_BlackboardView != null && m_BlackboardView.BlackboardViewModel.ParentGraphView != SelectedGraphView)
            {
                m_BlackboardView.RemoveFromHierarchy();
                m_BlackboardView.Dispose();
                m_BlackboardView = null;
            }

            if (m_BlackboardView == null && SelectedGraphView != null)
            {
                m_BlackboardView = new BlackboardView(this, SelectedGraphView);
                rootVisualElement.Add(m_BlackboardView);

                // When an undo happens the blackboard is recreated, then reparented above.
                // If something in the old blackboard was focused, the windows still have the focus
                // but no uielement is focused, therefore any command such as copy/paste/duplicate would
                // be ignored if we don't focus the new blackboard.
                if (hasFocus)
                    m_BlackboardView.Focus();
            }
        }
    }
}
