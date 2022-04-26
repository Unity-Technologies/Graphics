using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    [GraphElementsExtensionMethodsCache(typeof(ContextGraphView))]
    static class ContextSampleUIFactoryExtensions
    {
        public static IModelView CreateNode(this ElementBuilder elementBuilder, SampleNodeModel model)
        {
            IModelView ui = new SampleNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateNode(this ElementBuilder elementBuilder, SampleContextModelBase model)
        {
            IModelView ui = new SampleContext();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateNode(this ElementBuilder elementBuilder, SampleBlockModelBase model)
        {
            IModelView ui = new SampleBlock();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static void BuildContextualMenu<T>(GraphView graphView, T sampleNodeModel, ContextualMenuPopulateEvent evt, string prefix = "") where T : IVariableNodeModel
        {
            bool canHaveVertical = !(sampleNodeModel is IBlockNodeModel);

            evt.menu.AppendAction(prefix + "Input/Add Port (Vector2)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector2));
            });

            evt.menu.AppendAction(prefix + "Input/Add Port (Vector3)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector3));
            });
            evt.menu.AppendAction(prefix + "Input/Add Port (Vector4)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector4));
            });
            evt.menu.AppendAction(prefix + "Input/Add Port (int)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Int));
            });
            evt.menu.AppendAction(prefix + "Input/Add Port (float)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Float));
            });

            evt.menu.AppendAction(prefix + "Input/Add Port (Quaternion)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Quaternion));
            });

            evt.menu.AppendAction(prefix + "Input/Add Port (Object)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Object));
            });

            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Input/Add Vertical Port", _ =>
                {
                    graphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Vertical, sampleNodeModel));
                });

            evt.menu.AppendAction(prefix + "Input/Remove Port", _ =>
            {
                graphView.Dispatch(new RemovePortCommand(PortDirection.Input, PortOrientation.Horizontal, sampleNodeModel));
            }, __ => sampleNodeModel.InputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Input/Remove Vertical Port", _ =>
                {
                    graphView.Dispatch(new RemovePortCommand(PortDirection.Input, PortOrientation.Vertical, sampleNodeModel));
                }, __ => sampleNodeModel.VerticalInputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(prefix + "Output/Add Port (Vector2)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector2));
            });
            evt.menu.AppendAction(prefix + "Output/Add Port (Vector4)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Vector4));
            });
            evt.menu.AppendAction(prefix + "Output/Add Port (int)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Int));
            });
            evt.menu.AppendAction(prefix + "Output/Add Port (float)", _ =>
            {
                graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel, TypeHandle.Float));
            });

            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Output/Add Vertical Port", _ =>
                {
                    graphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Vertical, sampleNodeModel));
                });

            evt.menu.AppendAction(prefix + "Output/Remove Port", _ =>
            {
                graphView.Dispatch(new RemovePortCommand(PortDirection.Output, PortOrientation.Horizontal, sampleNodeModel));
            }, __ => sampleNodeModel.OutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);


            if (canHaveVertical)
                evt.menu.AppendAction(prefix + "Output/Remove Vertical Port", _ =>
                {
                    graphView.Dispatch(new RemovePortCommand(PortDirection.Output, PortOrientation.Vertical, sampleNodeModel));
                }, __ => sampleNodeModel.VerticalOutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
    }
}
