using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="ModelInspectorView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their third parameter to the type
    /// of the graph element model for which we need to instantiate a IModelUI. You can change the UI for a
    /// model by defining new extension methods for <see cref="ElementBuilder"/> in a class having
    /// the <see cref="GraphElementsExtensionMethodsCacheAttribute"/>.
    /// </remarks>
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class ModelInspectorFactoryExtensions
    {
        public static IModelUI CreateNodeInspector(this ElementBuilder elementBuilder, INodeModel model)
        {
            var ui = new ModelInspector();
            ui.Setup(model, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            var nodeInspectorFields = NodeFieldsInspector.Create("node-fields", model, ui, ModelInspector.ussClassName);
            ui.PartList.AppendPart(nodeInspectorFields);

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }
}
