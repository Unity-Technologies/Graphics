using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO: Need GraphView type
    // [GraphElementsExtensionMethodsCache(typeof( ... ))]
    public static class ShaderGraphModelUIFactoryExtensions
    {
        public static IModelUI CreateRegistryNode(this ElementBuilder elementBuilder, CommandDispatcher store, RegistryNodeModel model)
        {
            var ui = new RegistryNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View);
            return ui;
        }
    }
}
