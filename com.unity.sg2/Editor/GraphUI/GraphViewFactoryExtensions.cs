using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(ShaderGraphView))]
    static class GraphViewFactoryExtensions
    {
        public static ModelView CreateNode(
            this ElementBuilder elementBuilder,
            GraphDataNodeModel model)
        {
            var ui = new GraphDataNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateRedirectNode(
            this ElementBuilder elementBuilder,
            RedirectNodeModel model)
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
