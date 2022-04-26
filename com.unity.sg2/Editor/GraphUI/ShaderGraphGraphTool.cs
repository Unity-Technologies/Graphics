using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Toolbars;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphGraphTool: BaseGraphTool
    {
        public static readonly string toolName = "Shader Graph";

        public ShaderGraphGraphTool()
        {
            Name = toolName;
        }

        /// <summary>
        /// Gets the toolbar provider for the main toolbar.
        /// </summary>
        /// <remarks>Use this method to get the provider for the <see cref="ShaderGraphMainToolbar"/>.</remarks>
        /// <returns>The toolbar provider for the main toolbar.</returns>
        public override IToolbarProvider GetToolbarProvider()
        {
            return new ShaderGraphMainToolbarProvider();
        }

        protected override IOverlayToolbarProvider CreateToolbarProvider(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainOverlayToolbar.toolbarId:
                    return new ShaderGraphMainToolbarProvider();
                case BreadcrumbsToolbar.toolbarId:
                    return new BreadcrumbsToolbarProvider();
                case PanelsToolbar.toolbarId:
                    return new PanelsToolbarProvider();
                case OptionsMenuToolbar.toolbarId:
                    return new OptionsToolbarProvider();
                default:
                    return null;
            }
        }
    }
}
