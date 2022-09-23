using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView))]
    static class ModelInspectorViewFactoryExtensions
    {
        public static MultipleModelsView CreateVariableDeclarationInspector(this ElementBuilder elementBuilder, IEnumerable<GraphDataVariableDeclarationModel> models)
        {
            var ui = new ShaderGraphModelInspector();
            ui.Setup(models, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        var variableInspector = new GraphDataVariableSettingsInspector(ModelInspector.fieldsPartName, models, ui.RootView, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(variableInspector);
                        break;
                    }

                    case SectionType.Advanced:
                    {
                        // GTF-provided common variable declaration settings
                        var inspectorFields = VariableFieldsInspector.Create(ModelInspector.fieldsPartName,
                            models,
                            ui.RootView,
                            ModelInspector.ussClassName,
                            // Hide editor for the serialized m_IsExposed field for now, as it's not meaningful to us
                            filter: field => field.Name != "m_IsExposed" && ModelInspectorView.AdvancedSettingsFilter(field));
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();
            return ui;
        }

        public static MultipleModelsView CreateVariableNodeInspector(this ElementBuilder elementBuilder, IEnumerable<GraphDataVariableNodeModel> models)
        {
            return elementBuilder.CreateVariableDeclarationInspector(models.Select(m => (GraphDataVariableDeclarationModel)m.VariableDeclarationModel));
        }

        public static MultipleModelsView CreateContextSectionInspector(this ElementBuilder elementBuilder, IEnumerable<GraphDataContextNodeModel> models)
        {
            var ui = new ShaderGraphModelInspector();

            ui.Setup(models, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        // TODO GTF UPGRADE: support edition of multiple models.
                        var model = models.First();
                        if (model.GraphModel is not ShaderGraphModel {IsSubGraph: true} ||
                            !model.TryGetNodeHandler(out var reader) ||
                            reader.ID.LocalPath != Registry.ResolveKey<ShaderGraphContext>().Name)
                        {
                            break;
                        }

                        var subgraphOutputs = new SubgraphOutputsInspector(ModelInspector.fieldsPartName, models, elementBuilder.View, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(subgraphOutputs);

                        break;
                    }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }

        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, IEnumerable<GraphDataNodeModel> models)
        {
            var ui = new ShaderGraphModelInspector();

            ui.Setup(models, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        var upgradePrompt = new NodeUpgradePart("sg-node-upgrade", models, elementBuilder.View, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(upgradePrompt);

                        var staticPorts = new StaticPortsInspector(ModelInspector.fieldsPartName, models, elementBuilder.View, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(staticPorts);

                        var inspectorFields = new SGNodeFieldsInspector(ModelInspector.fieldsPartName, models, elementBuilder.View, ModelInspector.ussClassName);
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
        /// Creates a new inspector for an <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="models">The graph models for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the graph.</returns>
        public static MultipleModelsView CreateSectionInspector(this ElementBuilder elementBuilder, GraphModel model)
        {
            var models = new[] { model };
            var ui = new ShaderGraphModelInspector();
            var view = elementBuilder.View as ModelInspectorView;
            ui.Setup(models, view, elementBuilder.Context);

            if (model.Asset is ShaderGraphAsset graphAsset && !graphAsset.ShaderGraphModel.IsSubGraph)
            {
                if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
                {
                    switch (inspectorSectionContext.Section.SectionType)
                    {
                        case SectionType.Settings:
                        {
                            var targetSettingsField = new TargetSettingsInspector(graphAsset.ShaderGraphModel.Targets, ModelInspector.fieldsPartName, models, view, ModelInspector.ussClassName);
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
