using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeStencil : Stencil
    {
        static readonly string[] k_Sections = { "Ingredients", "Cookware" };

        public static readonly string graphName = "Recipe";
        public static TypeHandle Ingredient { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("Ingredient");
        public static TypeHandle Cookware { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("Cookware");
        public static TypeHandle Attitude { get; } = TypeHandleHelpers.GenerateTypeHandle<Attitude>();
        public static TypeHandle Temperature { get; } = TypeHandleHelpers.GenerateTypeHandle<Temperature>();

        public override IEnumerable<string> SectionNames => k_Sections;

        /// <inheritdoc />
        protected override void CreateGraphProcessors()
        {
            base.CreateGraphProcessors();
            GetGraphProcessorContainer().AddGraphProcessor(new RecipeProcessor());
        }

        /// <inheritdoc />
        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new RecipeSearcherDatabaseProvider(this);
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new RecipeBlackboardGraphModel(graphAssetModel);
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is RecipeNodeBaseModel || originalModel is VariableNodeModel;
        }

        public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return originalModel is VariableDeclarationModel && (originalModel.DataType == Cookware || originalModel.DataType == Ingredient);
        }

        /// <inheritdoc />
        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return new InspectorModel(inspectedModel);
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        /// <inheritdoc />
        public override void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menuItems,
            IRootView view, IGroupModel selectedGroup = null)
        {
            if (sectionName == k_Sections[0])
            {
                menuItems.Add(new MenuItem{name = "Create Ingredient", action =
                    () =>
                    {
                        CreateVariableDeclaration(Ingredient.Identification, Ingredient);
                    }});
            }
            else if (sectionName == k_Sections[1])
            {
                menuItems.Add(new MenuItem{name = "Create Cookware", action =
                    () =>
                    {
                        CreateVariableDeclaration(Cookware.Identification, Cookware);
                    }});
            }

            void CreateVariableDeclaration(string name, TypeHandle type)
            {
                view.Dispatch(
                    new CreateGraphVariableDeclarationCommand(name, true, type, selectedGroup));
            }
        }

        /// <inheritdoc />
        public override string GetVariableSection(IVariableDeclarationModel variable)
        {
            if (variable.DataType == RecipeStencil.Ingredient)
                return k_Sections[0];
            return k_Sections[1];
        }
    }
}
