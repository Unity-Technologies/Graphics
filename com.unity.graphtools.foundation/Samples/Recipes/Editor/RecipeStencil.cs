using System.Collections.Generic;
using System.Linq;
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

        public override IEnumerable<string> SectionNames => k_Sections;

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new RecipeBlackboardGraphModel(graphAssetModel);
        }

        /// <inheritdoc />
        public override void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu,
            IModelView view, IGraphModel graphModel, IGroupModel selectedGroup = null)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Create Group"), false,
                () =>
                {
                    view.Dispatch(new BlackboardGroupCreateCommand(selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
                });

            menu.AddSeparator("");

            if (sectionName == k_Sections[0])
            {
                menu.AddItem(new GUIContent("Add"), false,
                    () =>
                    {
                        CreateVariableDeclaration(Ingredient.Identification, Ingredient,
                            GraphModel.GetSectionModel(sectionName));
                    });
            }
            else if (sectionName == k_Sections[1])
            {
                menu.AddItem(new GUIContent("Add"), false,
                    () =>
                    {
                        CreateVariableDeclaration(Cookware.Identification, Cookware,
                            GraphModel.GetSectionModel(sectionName));
                    });
            }

            void CreateVariableDeclaration(string name, TypeHandle type, ISectionModel section)
            {
                if (selectedGroup != null && !section.AcceptsDraggedModel(selectedGroup))
                    selectedGroup = null;

                view.Dispatch(
                    new CreateGraphVariableDeclarationCommand(name, true, type, selectedGroup ?? section));
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
