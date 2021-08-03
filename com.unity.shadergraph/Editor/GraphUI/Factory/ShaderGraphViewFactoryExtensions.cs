using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.GraphElements;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;

namespace UnityEditor.ShaderGraph.GraphUI.Factory
{
    [GraphElementsExtensionMethodsCache(typeof(ShaderGraphView))]
    public static class ShaderGraphViewFactoryExtensions
    {
        public static IModelUI CreateGraphDataNode(this ElementBuilder elementBuilder, CommandDispatcher store, GraphDataNodeModel model)
        {
            var ui = new GraphDataNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
