using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    [GraphElementsExtensionMethodsCache(typeof(VerticalGraphView))]
    static class VerticalUIFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, VerticalNodeModel model)
        {
            IModelUI ui = new VerticalNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
