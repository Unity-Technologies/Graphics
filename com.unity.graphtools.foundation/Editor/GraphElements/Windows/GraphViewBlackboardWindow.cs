using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A window to display the blackboard.
    /// </summary>
    public class GraphViewBlackboardWindow : GraphViewToolWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcutsForBlackboard<GraphViewBlackboardWindow>(k_ToolName);
        }

        Blackboard m_Blackboard;

        const string k_ToolName = "Blackboard";

        protected override string ToolName => k_ToolName;

        protected override void OnEnable()
        {
            base.OnEnable();

            OnGraphViewChanged();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            OnGraphViewChanging();
        }

        protected virtual void OnFocus()
        {
            //Postpones taking focus to next frame because SyncIMGUIFocus is called directly after the window
            //gets the focus and resets the focus to the root uiElement.
            m_Blackboard?.schedule.Execute(() => m_Blackboard?.Focus()).ExecuteLater(0);
        }

        /// <inheritdoc />
        protected override void Update()
        {
            base.Update();
            SetBlackboard(m_SelectedGraphView?.GetBlackboard());
        }

        protected override void OnGraphViewChanging()
        {
            SetBlackboard(null);
        }

        protected override void OnGraphViewChanged()
        {
            SetBlackboard(m_SelectedGraphView?.GetBlackboard());
        }

        void SetBlackboard(Blackboard blackboard)
        {
            if (blackboard != m_Blackboard)
            {
                m_Blackboard?.RemoveFromHierarchy();
                m_Blackboard = blackboard;

                if (m_Blackboard != null)
                {
                    m_Blackboard.Windowed = true;
                    rootVisualElement.Add(m_Blackboard);
                }
            }
        }

        protected override bool IsGraphViewSupported(GraphView gv)
        {
            return gv.SupportsWindowedBlackboard;
        }
    }
}
