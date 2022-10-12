using Unity.GraphToolsFoundation.Editor;
using Unity.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphLoadedObserver : StateObserver
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
            : base(new []{ toolStateComponent },
                new IStateComponent []{ graphModelStateComponent, previewStateComponent })
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
                    using (var updater = m_PreviewStateComponent.UpdateScope)
                    {
                        updater.LoadStateForGraph(shaderGraphModel);
                    }

                    m_ShaderGraphEditorWindow.HandleGraphLoad(shaderGraphModel, m_PreviewStateComponent);
                }
            }
        }
    }
}
