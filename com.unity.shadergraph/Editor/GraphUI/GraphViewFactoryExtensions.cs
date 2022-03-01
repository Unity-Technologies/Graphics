using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class GraphViewFactoryExtensions
    {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            GraphDataNodeModel model)
        {
            var ui = new GraphDataNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateRedirectNode(
            this ElementBuilder elementBuilder,
            RedirectNodeModel model)
        {
            var ui = new RedirectNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateEdge(
            this ElementBuilder elementBuilder,
            EdgeModel model)
        {
            var ui = new RedirectableEdge();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
