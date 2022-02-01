namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class PortModel : BasicModel.PortModel
    {
        bool m_IsReorderable;
        string m_Tooltip = "";

        public override bool HasReorderableEdges => m_IsReorderable && Direction == PortDirection.Output && this.IsConnected();

        public override string ToolTip => m_Tooltip;

        public PortModel()
        {
            m_IsReorderable = true;
        }

        public void SetReorderable(bool reorderable)
        {
            m_IsReorderable = reorderable;
        }

        public void SetTooltip(string tooltip)
        {
            m_Tooltip = tooltip;
        }
    }
}
