using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class MathBookStencil : Stencil, ISearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> m_Databases;

        public static string GraphName => "Math Book";

        internal static readonly string[] k_Sections = { "Variables", "Exposed I/Os"};

        public override IEnumerable<string> SectionNames => k_Sections;

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new MathBookBlackboardGraphModel(graphAssetModel);
        }

        public MathBookStencil()
        {
            m_Databases = new List<SearcherDatabaseBase> { AddMathNodes() };
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public override TypeHandle GetSubgraphNodeTypeHandle()
        {
            return typeof(SubgraphNodeModel).GenerateTypeHandle();
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return this;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            m_Databases = new List<SearcherDatabaseBase> { AddMathNodes() };
            var subgraphs = AddMathBookSubgraphs(graphModel);
            if (subgraphs != null)
            {
                m_Databases.Add(new SearcherDatabase(new List<SearcherItem> { subgraphs }));
            }
            return m_Databases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetVariableTypesSearcherDatabases()
        {
            return new List<SearcherDatabaseBase>();
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return m_Databases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return m_Databases;
        }

        public List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return m_Databases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphElementContainerSearcherDatabases(IGraphModel graphModel,
            IGraphElementContainer container)
        {
            return new List<SearcherDatabaseBase>();
        }

        SearcherDatabase AddMathNodes()
        {
            var operatorsItem = new SearcherItem("Operators", "",
                new[]
                {
                    new SearcherItem("Boolean Logic", "", new[]
                    {
                        (typeof(NotOperator), "Not"),
                        (typeof(AndOperator), "And"),
                        (typeof(OrOperator),  "Or"),
                        (typeof(XorOperator), "Xor"),
                    }
                            .Select(MakeSearcherItem)
                            .ToList()),
                    new SearcherItem("Comparisons", "", new[]
                    {
                        (typeof(GreaterThanOperator), "Greater Than"),
                        (typeof(GreaterOrEqualOperator), "Greater Or Equal"),
                        (typeof(LessThanOperator), "Less Than"),
                        (typeof(LessOrEqualOperator), "Less Or Equal"),
                    }
                            .Select(MakeSearcherItem)
                            .ToList()),
                }.Concat(new[]
                {
                    (typeof(MathExpressionNode), "Expression"),
                    (typeof(MathAdditionOperator), "Addition"),
                    (typeof(MathSubtractionOperator), "Subtraction"),
                    (typeof(MathMultiplicationOperator), "Multiplication"),
                    (typeof(MathDivisionOperator), "Division"),
                    (typeof(MinOperator), "Min"),
                    (typeof(MaxOperator), "Max"),
                }
                        .Select(MakeSearcherItem)
                        .ToList())
                    .ToList());

            var functionsItem = new SearcherItem("Functions", "", new[]
            {
                new SearcherItem("Vectors", "", new[]
                {
                    (typeof(Vector3MagnitudeFunction), "Vector3 Magnitude"),
                    (typeof(Vector3DotProduct), "Vector3 Dot Product"),
                    (typeof(Vector3CrossProduct), "Vector3 Cross Product"),
                }.Select(MakeSearcherItem).ToList())
            }.Concat(
                    new[]
                    {
                        (typeof(SinFunction), "Sin"),
                        (typeof(AsinFunction), "Asin"),
                        (typeof(CosFunction), "Cos"),
                        (typeof(AcosFunction), "Acos"),
                        (typeof(TanFunction), "Tan"),
                        (typeof(AtanFunction), "Atan"),
                        (typeof(ClampFunction), "Clamp"),
                        (typeof(ExpFunction), "Exp"),
                        (typeof(LogFunction), "Log"),
                        (typeof(PowFunction), "Pow"),
                        (typeof(RoundFunction), "Round"),
                        (typeof(SqrtFunction), "Sqrt")
                    }.Select(MakeSearcherItem))
                    .ToList());

            var constants = k_SupportedConstants.Select(MakeConstantItem).ToList();
            constants.Add(MakeSearcherItem((typeof(PIConstant), "PI")));
            var constantsItem = new SearcherItem("Values", "", constants);

            var items = new List<SearcherItem>
            {
                operatorsItem,
                functionsItem,
                constantsItem,
                MakeSearcherItem((typeof(MathResult), "Result"))
            };

            return new SearcherDatabase(items);

            SearcherItem MakeSearcherItem((Type t, string name) tuple)
            {
                return new GraphNodeModelSearcherItem(GraphModel, null, data => data.CreateNode(tuple.t), tuple.name);
            }

            SearcherItem MakeConstantItem((TypeHandle type, string name) tuple)
            {
                return new GraphNodeModelSearcherItem(GraphModel, null,
                    t => t.GraphModel.CreateConstantNode(tuple.type, "", t.Position, t.Guid, null, t.SpawnFlags),
                    "Constant " + tuple.name);
            }
        }

        static readonly List<(TypeHandle type, string name)> k_SupportedConstants =
            new List<(TypeHandle type, string name)>()
        {
            (TypeHandle.Bool, "boolean"),
            (TypeHandle.Int, "integer"),
            (TypeHandle.Float, "float"),
            (TypeHandle.Vector2, "vector2"),
            (TypeHandle.Vector3, "vector3"),
        };

        public override void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, IModelView view, IGraphModel graphModel, IGroupModel selectedGroup = null)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Create Group"), false, () =>
            {
                view.Dispatch(new BlackboardGroupCreateCommand(selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
            });

            menu.AddSeparator("");

            if (sectionName == k_Sections[0])
            {
                foreach (var(type, name) in k_SupportedConstants)
                {
                    menu.AddItem(new GUIContent($"Create {name} Variable"), false, () =>
                    {
                        view.Dispatch(new CreateGraphVariableDeclarationCommand($"My {name}", true,
                            type, typeof(MathBookVariableDeclarationModel)));
                    });
                }
            }
            else if (sectionName == k_Sections[1])
            {
                menu.AddItem(new GUIContent("Input Data"), false, () =>
                {
                    CreateVariableDeclaration("data input", TypeHandle.Float, ModifierFlags.ReadOnly);
                });
                menu.AddItem(new GUIContent("Output Data"), false, () =>
                {
                    CreateVariableDeclaration("data output", TypeHandle.Float, ModifierFlags.WriteOnly);
                });
                menu.AddItem(new GUIContent("Input Trigger"), false,
                    _ => CreateVariableDeclaration("input trigger", TypeHandle.ExecutionFlow, ModifierFlags.ReadOnly), null);
                menu.AddItem(new GUIContent("Output Trigger"), false,
                    _ => CreateVariableDeclaration("output trigger", TypeHandle.ExecutionFlow, ModifierFlags.WriteOnly), null);
            }

            void CreateVariableDeclaration(string newItemName, TypeHandle type, ModifierFlags modifiers)
            {
                view.Dispatch(new CreateGraphVariableDeclarationCommand(newItemName, true, type, selectedGroup, -1, modifiers));
            }
        }

        static SearcherItem AddMathBookSubgraphs(IGraphModel graphModel)
        {
            SearcherItem parent = null;
            var assetPaths = AssetDatabase.FindAssets($"t:{typeof(MathBookAsset)}").Select(AssetDatabase.GUIDToAssetPath).ToList();
            var assetGraphModels = assetPaths.Select(p => AssetDatabase.LoadAssetAtPath(p, typeof(object)) as MathBookAsset)
                .Where(g => g != null && g.GraphAssetType == GraphAssetType.AssetGraph);

            var handle = typeof(MathSubgraphNode).GenerateTypeHandle();

            foreach (var assetGraphModel in assetGraphModels.Where(asset => asset.GraphModel.VariableDeclarations.Any(variable => variable.IsInputOrOutput())))
            {
                parent ??= new SearcherItem("Math Subgraphs");

                parent.AddChild(new GraphNodeModelSearcherItem(
                    graphModel,
                    new TypeSearcherItemData(handle),
                    data => data.CreateMathSubgraphNode(assetGraphModel),
                    assetGraphModel.Name
                ));
            }

            return parent;
        }

        public override string GetVariableSection(IVariableDeclarationModel variable)
        {
            if (variable.IsInputOrOutput())
                return k_Sections[1];
            return k_Sections[0];
        }
    }
}
