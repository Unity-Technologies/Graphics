using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    static class ContextSampleUIFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, Dispatcher store, SampleNodeModel model)
        {
            IModelUI ui = new SampleNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateNode(this ElementBuilder elementBuilder, Dispatcher store, SampleContextModelBase model)
        {
            IModelUI ui = new SampleContext();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateNode(this ElementBuilder elementBuilder, Dispatcher store, SampleBlockModelBase model)
        {
            IModelUI ui = new SampleBlock();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static void BuildContextualMenu<T>(GraphView graphView, T sampleNodeModel, ContextualMenuPopulateEvent evt, string prefix = "") where T : IVariableNodeModel
        {
            bool canHaveVertical = !(sampleNodeModel is IBlockNodeModel);

            evt.menu.AppendAction(prefix + "Input/Add Port (Vector2)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector2));
            });
            evt.menu.AppendAction(prefix + "Input/Add Port (Vector4)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector4));
            });
            evt.menu.AppendAction(prefix + "Input/Add Port (int)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Int));
            });
            evt.menu.AppendAction(prefix + "Input/Add Port (float)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Float));
            });

            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Input/Add Vertical Port", action =>
                {
                    graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Vertical, sampleNodeModel));
                });

            evt.menu.AppendAction(prefix + "Input/Remove Port", action =>
            {
                graphView.Dispatch(new RemovePortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel));
            }, a => sampleNodeModel.InputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Input/Remove Vertical Port", action =>
                {
                    graphView.Dispatch(new RemovePortCommand(PortDirection.Input, PortOrientation.Vertical, sampleNodeModel));
                }, a => sampleNodeModel.VerticalInputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(prefix + "Output/Add Port (Vector2)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector2));
            });
            evt.menu.AppendAction(prefix + "Output/Add Port (Vector4)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector4));
            });
            evt.menu.AppendAction(prefix + "Output/Add Port (int)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Int));
            });
            evt.menu.AppendAction(prefix + "Output/Add Port (float)", action =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Float));
            });

            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Output/Add Vertical Port", action =>
                {
                    graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Vertical, sampleNodeModel));
                });

            evt.menu.AppendAction(prefix + "Output/Remove Port", action =>
            {
                graphView.Dispatch(new RemovePortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel));
            }, a => sampleNodeModel.OutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);


            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Output/Remove Vertical Port", action =>
                {
                    graphView.Dispatch(new RemovePortCommand(PortDirection.Output, PortOrientation.Vertical, sampleNodeModel));
                }, a => sampleNodeModel.VerticalOutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
    }
}
