using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.VFX
{
    public class VFXStencil : Stencil
    {
        static readonly string[] k_Sections = { "Blocks", "Operators", "Contexts" };

        public static readonly string graphName = "VFX Graph";
        public static TypeHandle Block { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("Block");
        public static TypeHandle Operator { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("Operator");
        public static TypeHandle Context { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("Context");

        public override IEnumerable<string> SectionNames => k_Sections;

        /// <inheritdoc />
        protected override void CreateGraphProcessors()
        {
            base.CreateGraphProcessors();
            GetGraphProcessorContainer().AddGraphProcessor(new VFXProcessor());
        }

        /// <inheritdoc />
        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new VFXSearcherDatabaseProvider(this);
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new VFXBlackboardGraphModel(graphAssetModel);
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is VFXNodeBaseModel || originalModel is VariableNodeModel;
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
            IModelView view, IGroupModel selectedGroup = null)
        {
            menuItems.Add(new MenuItem
            {
                name = "Create Group",
                action =
                    () =>
                    {
                        view.Dispatch(
                            new BlackboardGroupCreateCommand(selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
                    }
            });

            menuItems.Add(new MenuItem());

            if (sectionName == k_Sections[0])
            {
                menuItems.Add(new MenuItem
                {
                    name = "Add Block",
                    action = () =>
                        CreateVariableDeclaration(Block.Identification, Block, GraphModel.GetSectionModel(sectionName))
                });
            }
            else if (sectionName == k_Sections[1])
            {
                menuItems.Add(new MenuItem
                {
                    name = "Add Operator",
                    action = () => CreateVariableDeclaration(Operator.Identification, Operator,
                        GraphModel.GetSectionModel(sectionName))
                });
            }
            else if (sectionName == k_Sections[2])
            {
                menuItems.Add(new MenuItem
                {
                    name = "Add Context",
                    action = () =>
                        CreateVariableDeclaration(Context.Identification, Context,
                            GraphModel.GetSectionModel(sectionName))
                });
            }

            void CreateVariableDeclaration(string name, TypeHandle type, ISectionModel section)
            {
                if (selectedGroup != null && !section.AcceptsDraggedModel(selectedGroup))
                    selectedGroup = null;

                view.Dispatch(new CreateGraphVariableDeclarationCommand(name, true, type, selectedGroup ?? section));
            }
        }

        /// <inheritdoc />
        public override string GetVariableSection(IVariableDeclarationModel variable)
        {
            if (variable.DataType == VFXStencil.Block)
                return k_Sections[0];
            else if (variable.DataType == VFXStencil.Operator)
                return k_Sections[1];
            else if (variable.DataType == VFXStencil.Context)
                return k_Sections[2];

            throw new InvalidOperationException($"Unknown data type {variable.DataType}");
        }
    }
}
