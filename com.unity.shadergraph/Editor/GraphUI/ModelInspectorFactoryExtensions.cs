using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView))]
    public static class ModelInspectorViewFactoryExtensions
    {
        public static IModelUI CreateNodeInspector(this ElementBuilder elementBuilder, INodeModel model)
        {
            var ui = new ModelInspector();
            ui.Setup(model, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            // Add in general node inspector info
            var nodeInspectorFields = NodeFieldsInspector.Create("node-fields", model, ui, ModelInspector.ussClassName);
            ui.PartList.AppendPart(nodeInspectorFields);

            // Add in specific node inspector info 
            //var nodeInspectorFields = ShaderGraphNodeFieldsInspector.CreateInspector(
            //    "node-fields", model, ui, ModelInspector.ussClassName);
            //ui.PartList.AppendPart(nodeInspectorFields);

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }
}
