using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView))]
    public static class ModelInspectorViewFactoryExtensions
    {
        public static IModelUI CreateNodeInspector(this ElementBuilder elementBuilder, MathOperator model)
        {
            var ui = new ModelInspector();
            ui.Setup(model, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            var nodeInspectorFields = NodeFieldsInspector.Create("node-fields", model, ui, ModelInspector.ussClassName);
            ui.PartList.AppendPart(nodeInspectorFields);

            var mathOpInspectorFields = MathOperatorInspector.Create("mathop-fields", model, ui, ModelInspector.ussClassName);
            ui.PartList.AppendPart(mathOpInspectorFields);

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }
}
