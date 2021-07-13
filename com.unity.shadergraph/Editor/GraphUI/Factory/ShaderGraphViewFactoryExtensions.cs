using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;

namespace UnityEditor.ShaderGraph.GraphUI.Factory
{
    [GraphElementsExtensionMethodsCache(typeof(ShaderGraphView))]
    public static class ShaderGraphViewFactoryExtensions
    {
        public static IModelUI CreateRegistryNode(this ElementBuilder elementBuilder, CommandDispatcher store, RegistryNodeModel model)
        {
            var ui = new RegistryNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
