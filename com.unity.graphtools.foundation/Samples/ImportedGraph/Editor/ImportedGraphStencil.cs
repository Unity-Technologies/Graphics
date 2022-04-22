using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    public class ImportedGraphStencil : Stencil
    {
        static readonly string[] k_Sections = { "Payloads", "Response Codes" };

        public static readonly string graphName = "Importable Graph";

        public override IEnumerable<string> SectionNames => k_Sections;

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel)
        {
            return new BlackboardGraphModel { GraphModel = graphModel };
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is ImportedGraphNodeBaseModel || originalModel is VariableNodeModel;
        }

        public override bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return originalModel is VariableDeclarationModel && (originalModel.DataType == TypeHandle.String || originalModel.DataType == TypeHandle.Int);
        }

        /// <inheritdoc />
        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return new InspectorModel(inspectedModel);
        }

        public override Type GetConstantType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantType(typeHandle);
        }

        public override TypeHandle GetSubgraphNodeTypeHandle()
        {
            return typeof(SubgraphNodeModel).GenerateTypeHandle();
        }

        /// <inheritdoc />
        public override void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menuItems,
            IRootView view, IGroupModel selectedGroup = null)
        {
            if (sectionName == k_Sections[0])
            {
                menuItems.Add(new MenuItem{name = "Create Payload", action =
                    () =>
                    {
                        CreateVariableDeclaration("Payload", TypeHandle.String, ModifierFlags.Read);
                    }});
            }
            else if (sectionName == k_Sections[1])
            {
                menuItems.Add(new MenuItem{name = "Create Response Code", action =
                    () =>
                    {
                        CreateVariableDeclaration("Response Code", TypeHandle.Int, ModifierFlags.Write);
                    }});
            }

            void CreateVariableDeclaration(string name, TypeHandle type, ModifierFlags modifierFlags)
            {
                view.Dispatch(
                    new CreateGraphVariableDeclarationCommand(name, true, type, selectedGroup, modifierFlags: modifierFlags));
            }
        }

        /// <inheritdoc />
        public override string GetVariableSection(IVariableDeclarationModel variable)
        {
            if (variable.DataType == TypeHandle.Float)
                return k_Sections[0];
            return k_Sections[1];
        }

        /// <inheritdoc />
        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new ImportedGraphSearcherDatabaseProvider(this);
        }
    }
}
