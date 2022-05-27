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
            var ui = new ShaderGraphModelInspector();

            ui.Setup(model, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        var upgradePrompt = new NodeUpgradePart("sg-node-upgrade", model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(upgradePrompt);

                        var staticPorts = new StaticPortsInspector(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(staticPorts);

                        var inspectorFields = new SGNodeFieldsInspector(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(inspectorFields);
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

        /// <summary>
        /// Creates a new inspector for an <see cref="IGraphModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The graph model for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the graph.</returns>
        public static IModelView CreateSectionInspector(this ElementBuilder elementBuilder, IGraphModel model)
        {
            var ui = new ShaderGraphModelInspector();
            var view = elementBuilder.View as ModelInspectorView;
            ui.Setup(model, view, elementBuilder.Context);

            if (model.Asset is ShaderGraphAsset graphAsset && !graphAsset.ShaderGraphModel.IsSubGraph)
            {
                if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
                {
                    switch (inspectorSectionContext.Section.SectionType)
                    {
                        case SectionType.Settings:
                        {
                            var targetSettingsField = new TargetSettingsInspector(graphAsset.ShaderGraphModel.Targets, ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                            ui.PartList.AppendPart(targetSettingsField);
                            break;
                        }
                    }
                }

                ui.BuildUI();
                ui.UpdateFromModel();
            }

            return ui;
        }
    }
}
