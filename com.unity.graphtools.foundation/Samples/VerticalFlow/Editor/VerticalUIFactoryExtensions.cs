namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    [GraphElementsExtensionMethodsCache(typeof(VerticalGraphView))]
    static class VerticalUIFactoryExtensions
    {
        public static IModelView CreateNode(this ElementBuilder elementBuilder, VerticalNodeModel model)
        {
            IModelView ui = new VerticalNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
