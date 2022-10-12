using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Toolbars;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphGraphTool: BaseGraphTool
    {
        public static readonly string toolName = "Shader Graph";

        PreviewStateComponent m_PreviewStateComponent;

        public ShaderGraphGraphTool()
        {
            Name = toolName;
        }

        protected override void InitState()
        {
            base.InitState();

            m_PreviewStateComponent = new PreviewStateComponent();
            State.AddStateComponent(m_PreviewStateComponent);
        }

        protected override OverlayToolbarProvider CreateToolbarProvider(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainOverlayToolbar.toolbarId:
                    return new ShaderGraphMainToolbarProvider();
                case BreadcrumbsToolbar.toolbarId:
                    return new BreadcrumbsToolbarProvider();
                case PanelsToolbar.toolbarId:
                    return new SGPanelsToolbarProvider();
                case OptionsMenuToolbar.toolbarId:
                    return new OptionsToolbarProvider();
                default:
                    return null;
            }
        }
    }
}
