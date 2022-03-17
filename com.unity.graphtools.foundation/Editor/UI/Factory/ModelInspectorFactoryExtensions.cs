namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="ModelInspectorView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their third parameter to the type
    /// of the graph element model for which we need to instantiate a <see cref="IModelView"/>. You can change the UI for a
    /// model by defining new extension methods for <see cref="ElementBuilder"/> in a class having
    /// the <see cref="GraphElementsExtensionMethodsCacheAttribute"/>.
    /// </remarks>
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class ModelInspectorFactoryExtensions
    {
        /// <summary>
        /// Creates a new UI for an inspector section.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The inspector section model.</param>
        /// <returns>A UI for the inspector section.</returns>
        public static IModelView CreateSection(this ElementBuilder elementBuilder, IInspectorSectionModel model)
        {
            IModelView ui;
            if (model.Collapsible || !string.IsNullOrEmpty(model.Title))
            {
                ui = new CollapsibleSection();
            }
            else
            {
                ui = new InspectorSection();
            }

            ui.SetupBuildAndUpdate(model, elementBuilder.View, null);
            return ui;
        }

        /// <summary>
        /// Creates a new inspector for an <see cref="INodeModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The node model for which we want to create an inspector UI.</param>
        /// <returns>An inspector UI for the node.</returns>
        public static IModelView CreateSectionInspector(this ElementBuilder elementBuilder, INodeModel model)
        {
            var ui = new ModelInspector();

            ui.Setup(model, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.BasicSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                    case SectionType.Properties:
                        var nodeInspectorFields = NodePortsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(nodeInspectorFields);
                        break;
                    case SectionType.Advanced:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
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
            var ui = new ModelInspector();
            ui.Setup(model, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.BasicSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                    case SectionType.Advanced:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }

        public static IModelView CreateSectionInspector(this ElementBuilder elementBuilder, IVariableDeclarationModel model)
        {
            var ui = new ModelInspector();
            ui.Setup(model, elementBuilder.View as ModelInspectorView, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Properties:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.BasicSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                    case SectionType.Advanced:
                    {
                        var inspectorFields = VariableFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }
}
