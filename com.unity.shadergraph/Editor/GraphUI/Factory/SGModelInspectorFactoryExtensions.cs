using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Inspector;

namespace UnityEditor.ShaderGraph.GraphUI.Factory
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="ModelInspectorView"/>, specific to ShaderGraph.
    /// </summary>
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView), 1)]
    public static class SGModelInspectorFactoryExtensions
    {
        public static IModelUI CreateNodeInspector(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, INodeModel model)
        {
            var ui = new ModelInspector();
            ui.Setup(model, commandDispatcher, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            var nodeInspectorFields = ShaderGraphNodeFieldsInspector.CreateInspector("node-fields", model, ui, ModelInspector.ussClassName);
            ui.PartList.AppendPart(nodeInspectorFields);

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }
}
