using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphGraphTool: BaseGraphTool
    {
        GraphViewStateObserver m_GraphViewStateObserver;
        PreviewManager m_PreviewManager;

        public static readonly string toolName = "Shader Graph";

        public ShaderGraphView shaderGraphView { get; set; }

        public PreviewManager previewManager => m_PreviewManager;

        public ShaderGraphGraphTool()
        {
            Name = toolName;
        }

        protected override void InitState()
        {
            base.InitState();

            var shaderGraphModel = ToolState.GraphModel as ShaderGraphModel;

            m_PreviewManager = new PreviewManager(shaderGraphModel, shaderGraphView.GraphViewState);
            m_GraphViewStateObserver = new GraphViewStateObserver(shaderGraphView.GraphViewState, m_PreviewManager);

            ObserverManager.RegisterObserver(m_GraphViewStateObserver);

            ShaderGraphCommandsRegistrar.RegisterCommandHandlers(this, shaderGraphView, shaderGraphModel, m_PreviewManager, Dispatcher);

        }
    }

}
