using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        GraphModelStateComponent m_GraphModelStateComponent;
        ShaderGraphEditorWindow m_ShaderGraphEditorWindow;

        public ShaderGraphLoadedObserver(
            ToolStateComponent toolStateComponent,
            GraphModelStateComponent graphModelStateComponent,
            ShaderGraphEditorWindow shaderGraphEditorWindow)
            : base(toolStateComponent)
        {
            m_ToolStateComponent = toolStateComponent;
            m_GraphModelStateComponent = graphModelStateComponent;
            m_ShaderGraphEditorWindow = shaderGraphEditorWindow;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var obs = this.ObserveState(m_ToolStateComponent))
            {
                if (obs.UpdateType != UpdateType.None && m_ToolStateComponent.GraphModel is ShaderGraphModel shaderGraphModel)
                {
                    shaderGraphModel.graphModelStateComponent = m_GraphModelStateComponent;
                    m_ShaderGraphEditorWindow.HandleGraphLoad(shaderGraphModel);
                }
            }
        }
    }
}
