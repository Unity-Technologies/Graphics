using GtfPlayground.DataModel;
using GtfPlayground.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace GtfPlayground
{
    [GraphElementsExtensionMethodsCache(0)]
    public static class PlaygroundUIFactoryExtensions
    {
        public static IModelUI CreateConnectionInfoNode(this ElementBuilder elementBuilder, CommandDispatcher store,
            ConnectionInfoNodeModel model)
        {
            var ui = new ConnectionInfoNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateDataNode(this ElementBuilder elementBuilder, CommandDispatcher store,
            DataNodeModel model)
        {
            var ui = new DataNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateConversionEdge(this ElementBuilder elementBuilder, CommandDispatcher store,
            ConversionEdgeModel model)
        {
            var ui = new ConversionEdge();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateCustomizableNode(this ElementBuilder elementBuilder, CommandDispatcher store,
            CustomizableNodeModel model)
        {
            var ui = new CustomizableNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }
    }
}
