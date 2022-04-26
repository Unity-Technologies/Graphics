using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class MathBookStencil : Stencil
    {
        List<SearcherDatabaseBase> m_Databases;

        public static string GraphName => "Math Book";

        internal static readonly string[] sections = { "Variables", "Exposed I/Os"};

        public override IEnumerable<string> SectionNames => GraphModel.IsContainerGraph() ? sections.Where(section => section != sections[1]) : sections;

        internal static readonly string SampleAssetPath =
            "Packages/com.unity.graphtools.foundation/Samples/SimpleMathBook/Editor";

        /// <inheritdoc />
        protected override string CustomSearcherStylesheetPath => SampleAssetPath + "/Stylesheets/custom-searcher.uss";

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new MathBookSearcherProvider(this);
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel)
        {
            return new MathBookBlackboardGraphModel { GraphModel = graphModel };
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

        public static readonly IReadOnlyList<(TypeHandle type, string name)> SupportedConstants =
            new List<(TypeHandle type, string name)>()
        {
            (TypeHandle.Bool, "Boolean"),
            (TypeHandle.Int, "Integer"),
            (TypeHandle.Float, "Float"),
            (TypeHandle.Vector2, "Vector2"),
            (TypeHandle.Vector3, "Vector3"),
        };

        static readonly IReadOnlyDictionary<string, string> k_CategoryStyles = new Dictionary<string, string>()
        {
            { "Functions", "math-functions" },
            { "Operators/Boolean Logic", "boolean-operators" }
        };

        protected override IReadOnlyDictionary<string, string> CategoryPathStyleNames => k_CategoryStyles;

        public override void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menuItems, IRootView view, IGroupModel selectedGroup = null)
        {
            if (sectionName == sections[0])
            {
                foreach (var(type, name) in SupportedConstants)
                {
                    menuItems.Add(new MenuItem{name =$"Create {name} Variable", action = () =>
                    {
                        view.Dispatch(new CreateGraphVariableDeclarationCommand($"My {name}", true,
                            type, typeof(MathBookVariableDeclarationModel), selectedGroup));
                    }});
                }
            }
            else if (sectionName == sections[1])
            {
                menuItems.Add(new MenuItem{name ="Create Input Data", action= () =>
                {
                    CreateVariableDeclaration("data input", TypeHandle.Float, ModifierFlags.Read);
                }});
                menuItems.Add(new MenuItem{name ="Create Output Data", action= () =>
                {
                    CreateVariableDeclaration("data output", TypeHandle.Float, ModifierFlags.Write);
                }});
                menuItems.Add(new MenuItem{name ="Create Input Trigger", action = ()=>CreateVariableDeclaration("input trigger", TypeHandle.ExecutionFlow, ModifierFlags.Read)});
                menuItems.Add(new MenuItem{name ="Create Output Trigger", action = ()=>CreateVariableDeclaration("output trigger", TypeHandle.ExecutionFlow, ModifierFlags.Write)});
            }

            void CreateVariableDeclaration(string newItemName, TypeHandle type, ModifierFlags modifiers)
            {
                view.Dispatch(new CreateGraphVariableDeclarationCommand(newItemName, true, type, selectedGroup,-1, modifiers));
            }
        }

        public override string GetVariableSection(IVariableDeclarationModel variable)
        {
            if (variable.IsInputOrOutput())
                return sections[1];
            return sections[0];
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is MathNode || StencilHelper.IsCommonNodeThatCanBePasted(originalModel) || originalModel is MathSubgraphNode;
        }

        public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return originalModel is VariableDeclarationModel && (
                originalModel.DataType == TypeHandle.Float ||
                originalModel.DataType == TypeHandle.Int ||
                originalModel.DataType == TypeHandle.Bool ||
                originalModel.DataType == TypeHandle.Vector2 ||
                originalModel.DataType == TypeHandle.Vector3
                );
        }

        /// <inheritdoc />
        protected override void CreateGraphProcessors()
        {
            base.CreateGraphProcessors();
            GetGraphProcessorContainer().AddGraphProcessor(new MathBookGraphProcessor());
        }

        public override bool CanAssignTo(TypeHandle destination, TypeHandle source)
        {
            return base.CanAssignTo(destination, source) || destination.IsCompatibleWith(source);
        }
    }
}
