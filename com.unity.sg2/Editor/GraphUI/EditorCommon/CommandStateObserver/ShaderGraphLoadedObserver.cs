using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        GraphModelStateComponent m_GraphModelStateComponent;
        ShaderGraphView m_ShaderGraphView;
        PreviewManager m_PreviewManager;

        public ShaderGraphLoadedObserver(
            ToolStateComponent toolStateComponent,
            GraphModelStateComponent graphModelStateComponent,
            ShaderGraphView shaderGraphView)
            : base(toolStateComponent)
        {
            m_ToolStateComponent = toolStateComponent;
            m_GraphModelStateComponent = graphModelStateComponent;
            m_ShaderGraphView = shaderGraphView;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var obs = this.ObserveState(m_ToolStateComponent))
            {
                if (obs.UpdateType != UpdateType.None && m_ToolStateComponent.GraphModel is ShaderGraphModel shaderGraphModel)
                {
                    shaderGraphModel.graphModelStateComponent = m_GraphModelStateComponent;
                    m_ShaderGraphView.HandleGraphLoad(shaderGraphModel);
                }
            }
        }
    }
}
