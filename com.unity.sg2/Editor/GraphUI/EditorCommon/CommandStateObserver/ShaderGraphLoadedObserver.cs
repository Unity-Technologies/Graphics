using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        GraphModelStateComponent m_GraphModelStateComponent;
        ShaderGraphEditorWindow m_ShaderGraphEditorWindow;
        ShaderGraphModel m_CurrentGraphModelInstance;

        public ShaderGraphLoadedObserver(
            ToolStateComponent toolStateComponent,
            GraphModelStateComponent graphModelStateComponent,
            ShaderGraphEditorWindow shaderGraphEditorWindow)
            : base(new []{ toolStateComponent }, new []{ graphModelStateComponent })
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
                if (obs.UpdateType != UpdateType.None
                    && m_ToolStateComponent.GraphModel is ShaderGraphModel shaderGraphModel
                    && m_CurrentGraphModelInstance != shaderGraphModel)
                {
                    m_CurrentGraphModelInstance = shaderGraphModel;
                    shaderGraphModel.graphModelStateComponent = m_GraphModelStateComponent;
                    m_ShaderGraphEditorWindow.HandleGraphLoad(shaderGraphModel);
                }
            }
        }
    }
}
