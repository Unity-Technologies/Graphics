using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView))]
    public static class ModelInspectorViewFactoryExtensions
    {
        public static IModelView CreateSectionInspector(this ElementBuilder elementBuilder, GraphDataNodeModel model)
        {
            var ui = new ModelInspector();

            ui.Setup(model, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        var s = new StaticPortsInspector(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(s);
                        break;
                    }

                    // Uncomment to enable "properties" section - shows inline port editors
                    // case SectionType.Properties:
                    //     var nodeInspectorFields = NodePortsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                    //     ui.PartList.AppendPart(nodeInspectorFields);
                    //     break;

                    // Uncomment to enable "advanced" section - by default, shows serialized fields on the node model
                    // case SectionType.Advanced:
                    // {
                    //     var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                    //     ui.PartList.AppendPart(inspectorFields);
                    //     break;
                    // }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }
}
