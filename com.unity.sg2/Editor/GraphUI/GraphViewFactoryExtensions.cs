using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(ShaderGraphView))]
    static class GraphViewFactoryExtensions
    {
        public static ModelView CreateNode(
            this ElementBuilder elementBuilder,
            SGNodeModel model)
        {
            var ui = new SGNodeView();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateRedirectNode(
            this ElementBuilder elementBuilder,
            SGRedirectNodeModel model)
        {
            var ui = new RedirectNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateEdge(
            this ElementBuilder elementBuilder,
            WireModel model)
        {
            var ui = new RedirectableEdge();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
