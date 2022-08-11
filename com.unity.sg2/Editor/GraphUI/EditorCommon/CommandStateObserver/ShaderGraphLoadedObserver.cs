using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        GraphModelStateComponent m_GraphModelStateComponent;
        PreviewStateComponent m_PreviewStateComponent;
        PreviewUpdateDispatcher m_PreviewUpdateDispatcher;

        ShaderGraphEditorWindow m_ShaderGraphEditorWindow;
        ShaderGraphModel m_CurrentGraphModelInstance;

        public ShaderGraphLoadedObserver(
            ToolStateComponent toolStateComponent,
            GraphModelStateComponent graphModelStateComponent,
            PreviewStateComponent previewStateComponent,
            ShaderGraphEditorWindow shaderGraphEditorWindow)
            : base(new []{ toolStateComponent }, new []{ graphModelStateComponent })
        {
            m_ToolStateComponent = toolStateComponent;
            m_GraphModelStateComponent = graphModelStateComponent;
            m_ShaderGraphEditorWindow = shaderGraphEditorWindow;
            m_PreviewStateComponent = previewStateComponent;
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

                    // Initialize preview state component
                    //using (var updater = m_PreviewStateComponent.UpdateScope)
                    //{
                    //    updater.LoadStateForGraph(shaderGraphModel);
                    //}

                    m_ShaderGraphEditorWindow.HandleGraphLoad(shaderGraphModel, m_PreviewStateComponent);
                }
            }
        }
    }
}
