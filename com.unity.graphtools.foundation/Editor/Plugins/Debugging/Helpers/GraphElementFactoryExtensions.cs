using UnityEngine.GraphToolsFoundation.CommandStateObserver;
namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority + 1)]
    static class GraphElementFactoryExtensions
    {
        public static IModelUI CreatePort(this ElementBuilder elementBuilder, IPortModel model)
        {
            var ui = new DebuggingPort();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
