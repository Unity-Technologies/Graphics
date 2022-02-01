using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Variable")]
    [Category("Command")]
    class VariableCommandTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateGraphVariableDeclarationCommand_PreservesModifierFlags([Values] TestingMode mode, [Values] ModifierFlags flags)
        {
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    return new CreateGraphVariableDeclarationCommand("toto", true, typeof(int).GenerateTypeHandle(), null, -1, flags);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).DataType.Resolve(), Is.EqualTo(typeof(int)));
                    Assert.That(GetVariableDeclaration(0).Modifiers, Is.EqualTo(flags));
                });
        }

        public enum VariableDeclarationTestType
        {
            Standard,
            Custom
        }

        [Test]
        public void Test_CreateGraphVariableDeclarationCommand([Values] TestingMode mode, [Values] VariableDeclarationTestType testType)
        {
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    if (testType == VariableDeclarationTestType.Standard)
                        return new CreateGraphVariableDeclarationCommand("toto", true, typeof(int).GenerateTypeHandle());
                    return new CreateGraphVariableDeclarationCommand("toto", true, typeof(int).GenerateTypeHandle(), typeof(TestVariableDeclarationModel));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    var declarationModel = GetVariableDeclaration(0);
                    Assert.That(declarationModel.DataType.Resolve(), Is.EqualTo(typeof(int)));
                    if (testType == VariableDeclarationTestType.Standard)
                        Assert.AreEqual(typeof(VariableDeclarationModel), declarationModel.GetType());
                    else
                        Assert.AreEqual(typeof(TestVariableDeclarationModel), declarationModel.GetType());
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    // Second declaration model always created as "standard"
                    return new CreateGraphVariableDeclarationCommand("foo", true, typeof(float).GenerateTypeHandle());
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);

                    Assert.That(declarationModel1.DataType.Resolve(), Is.EqualTo(typeof(int)));
                    Assert.That(declarationModel2.DataType.Resolve(), Is.EqualTo(typeof(float)));

                    if (testType == VariableDeclarationTestType.Standard)
                        Assert.AreEqual(typeof(VariableDeclarationModel), declarationModel1.GetType());
                    else
                        Assert.AreEqual(typeof(TestVariableDeclarationModel), declarationModel1.GetType());

                    // Second declaration model always created as "standard"
                    Assert.AreEqual(typeof(VariableDeclarationModel), declarationModel2.GetType());
                });
        }

        [Test]
        public void Test_CreateGraphVariableDeclarationDuplicateNames([Values] TestingMode mode)
        {
            const string variableName = "toto";
            EditorSettings.gameObjectNamingScheme = EditorSettings.NamingScheme.SpaceParenthesis;
            EditorSettings.gameObjectNamingDigits = 1;
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    return new CreateGraphVariableDeclarationCommand(variableName, true, typeof(int).GenerateTypeHandle(), typeof(TestVariableDeclarationModel));
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    var declarationModel = GetVariableDeclaration(0);
                    Assert.That(declarationModel.Title, Is.EqualTo(variableName));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateGraphVariableDeclarationCommand(variableName, true, typeof(float).GenerateTypeHandle());
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);

                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName} (1)"));
                });

            EditorSettings.gameObjectNamingScheme = EditorSettings.NamingScheme.Dot;
            EditorSettings.gameObjectNamingDigits = 2;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    return new CreateGraphVariableDeclarationCommand(variableName, true, typeof(float).GenerateTypeHandle());
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(3));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);
                    var declarationModel3 = GetVariableDeclaration(2);

                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName} (1)"));
                    Assert.That(declarationModel3.Title, Is.EqualTo($"{variableName}.01"));
                });

            EditorSettings.gameObjectNamingScheme = EditorSettings.NamingScheme.Underscore;
            EditorSettings.gameObjectNamingDigits = 5;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(3));
                    return new CreateGraphVariableDeclarationCommand(variableName, true, typeof(float).GenerateTypeHandle());
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);
                    var declarationModel3 = GetVariableDeclaration(2);
                    var declarationModel4 = GetVariableDeclaration(3);

                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName} (1)"));
                    Assert.That(declarationModel3.Title, Is.EqualTo($"{variableName}.01"));
                    Assert.That(declarationModel4.Title, Is.EqualTo($"{variableName}_00001"));
                });
        }

        [Test]
        public void Test_RenameGraphVariableDeclarationDuplicateNames([Values] TestingMode mode)
        {
            const string variableName = "toto";
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    return new CreateGraphVariableDeclarationCommand(variableName, true, typeof(int).GenerateTypeHandle(), typeof(TestVariableDeclarationModel));
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    var declarationModel = GetVariableDeclaration(0);
                    Assert.That(declarationModel.Title, Is.EqualTo(variableName));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateGraphVariableDeclarationCommand("tata", true, typeof(int).GenerateTypeHandle(), typeof(TestVariableDeclarationModel));
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);
                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo("tata"));
                });

            EditorSettings.gameObjectNamingScheme = EditorSettings.NamingScheme.SpaceParenthesis;
            EditorSettings.gameObjectNamingDigits = -5;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(EditorSettings.gameObjectNamingScheme, Is.EqualTo(EditorSettings.NamingScheme.SpaceParenthesis));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);
                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo("tata"));
                    return new RenameElementCommand(declarationModel2 as IRenamable, variableName);
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);

                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName} (1)"));
                });

            EditorSettings.gameObjectNamingScheme = EditorSettings.NamingScheme.Dot;
            EditorSettings.gameObjectNamingDigits = 2;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(EditorSettings.gameObjectNamingScheme, Is.EqualTo(EditorSettings.NamingScheme.Dot));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);
                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName} (1)"));
                    return new RenameElementCommand(declarationModel2 as IRenamable, variableName);
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);

                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName}.01"));
                });

            EditorSettings.gameObjectNamingScheme = EditorSettings.NamingScheme.Underscore;
            EditorSettings.gameObjectNamingDigits = 5;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(EditorSettings.gameObjectNamingScheme, Is.EqualTo(EditorSettings.NamingScheme.Underscore));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);
                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName}.01"));
                    return new RenameElementCommand(declarationModel2 as IRenamable, variableName);
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    var declarationModel1 = GetVariableDeclaration(0);
                    var declarationModel2 = GetVariableDeclaration(1);

                    Assert.That(declarationModel1.Title, Is.EqualTo(variableName));
                    Assert.That(declarationModel2.Title, Is.EqualTo($"{variableName}_00001"));
                });
        }

        [Test]
        public void Test_CreateVariableNodeCommand([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnGraph(declaration);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnGraph(declaration);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });
        }

        [Test]
        public void Test_CreateVariableNodeCommand_DataOutputVariables([Values] TestingMode mode)
        {
            var dataOutputVariable = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "dataOutput", ModifierFlags.WriteOnly, true);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    dataOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnGraph(dataOutputVariable);
                },
                () =>
                {
                    dataOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });

            // The second call to CreateVariableNodesCommand will not be added to the Undo stack.
            // Add a non relevant command on the Undo stack to prevent the first call to CreateVariableNodesCommand to be undone.
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    dataOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, dataOutputVariable);
                },
                () =>
                {
                    dataOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    dataOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnGraph(dataOutputVariable);
                },
                () =>
                {
                    dataOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });
        }

        [Test]
        public void Test_CreateVariableNodeCommand_ExecutionOutputVariables([Values] TestingMode mode)
        {
            var executionOutputVariable = GraphModel.CreateGraphVariableDeclaration(TypeHandle.ExecutionFlow, "executionOutput", ModifierFlags.WriteOnly, true);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    executionOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnGraph(executionOutputVariable);
                },
                () =>
                {
                    executionOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    executionOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnGraph(executionOutputVariable);
                },
                () =>
                {
                    executionOutputVariable = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });
        }

        [Test]
        public void Test_DeleteElementsCommand_VariableUsage([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl1", ModifierFlags.None, true);

            var node0 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            var node1 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            var node2 = GraphModel.CreateVariableNode(declaration1, Vector2.zero);
            var node3 = GraphModel.CreateVariableNode(declaration1, Vector2.zero);
            var node4 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node5 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateEdge(node4.Input0, node0.OutputPort);
            GraphModel.CreateEdge(node4.Input1, node2.OutputPort);
            GraphModel.CreateEdge(node5.Input0, node1.OutputPort);
            GraphModel.CreateEdge(node5.Input1, node3.OutputPort);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration0 = GetVariableDeclaration(0);
                    declaration1 = GetVariableDeclaration(1);
                    Assert.That(GetNodeCount(), Is.EqualTo(6), "GetNodeCount1");
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2), "GetVariableDeclarationCount");
                    return new DeleteElementsCommand(declaration0);
                },
                () =>
                {
                    declaration1 = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(4), "GetNodeCount2");
                    Assert.That(GetEdgeCount(), Is.EqualTo(2), "EdgeCount");
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1), "GetVariableDeclarationCount");
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration1 = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(4), "GetNodeCount3");
                    Assert.That(GetEdgeCount(), Is.EqualTo(2), "EdgeCount");
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1), "GetVariableDeclarationCount");
                    return new DeleteElementsCommand(declaration1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2), "GetNodeCount");
                    Assert.That(GetEdgeCount(), Is.EqualTo(0), "EdgeCount");
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_RenameGraphVariableDeclarationCommand([Values] TestingMode mode)
        {
            var variable = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "toto", ModifierFlags.None, true);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).Title, Is.EqualTo("toto"));
                    return new RenameElementCommand(variable as IRenamable, "foo");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).Title, Is.EqualTo("foo"));
                });
        }

        [Test]
        [TestCase(TestingMode.Command, 0, 2, new[] { 1, 2, 0, 3 })]
        [TestCase(TestingMode.UndoRedo, 0, 2, new[] { 1, 2, 0, 3 })]
        [TestCase(TestingMode.Command, 0, 0, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 0, 0, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 3, 3, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 0, 3, new[] { 1, 2, 3, 0 })]
        [TestCase(TestingMode.Command, 3, 0, new[] { 0, 3, 1, 2 })]
        [TestCase(TestingMode.Command, 1, 1, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 1, 2, new[] { 0, 2, 1, 3 })]
        public void Test_ReorderGraphVariableDeclarationCommand(TestingMode mode, int indexToMove, int afterWhich, int[] expectedOrder)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl1", ModifierFlags.None, true);
            var declaration2 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl2", ModifierFlags.None, true);
            var declaration3 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl3", ModifierFlags.None, true);

            var declarations = new[] { declaration0, declaration1, declaration2, declaration3 };

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(declaration0.Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(declaration1.Guid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(declaration2.Guid));
                    Assert.That(GetSectionItem(3).Guid, Is.EqualTo(declaration3.Guid));
                    return new ReorderGraphVariableDeclarationCommand(GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First()), declarations[afterWhich], new[] { declarations[indexToMove] });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(declarations[expectedOrder[0]].Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(declarations[expectedOrder[1]].Guid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(declarations[expectedOrder[2]].Guid));
                    Assert.That(GetSectionItem(3).Guid, Is.EqualTo(declarations[expectedOrder[3]].Guid));
                });
        }

        [Test]
        public void Test_ConvertVariableNodeToConstantNodeCommand([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            IPortModel outputPort = node1.OutputPort;
            Color modelColor = Color.red;
            ModelState modelState = ModelState.Disabled;
            GraphModel.CreateEdge(node0.Input0, outputPort);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(1), Is.TypeOf<VariableNodeModel>());
                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (VariableNodeModel)GetNode(1);
                    n1.Color = modelColor;
                    n1.State = modelState;
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    return new ConvertConstantNodesAndVariableNodesCommand(null, new[] { node1 });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetConstantNode(1), Is.TypeOf<IntConstant>());

                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (ConstantNodeModel)GetNode(1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    Assert.That(n1.Color, Is.EqualTo(modelColor));
                    Assert.That(n1.State, Is.EqualTo(modelState));
                });
        }

        [Test]
        public void Test_ConvertConstantNodeToVariableNodeCommand([Values] TestingMode mode)
        {
            var binary = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var constant = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "const0", Vector2.zero);
            IPortModel outputPort = constant.OutputPort;
            Color modelColor = Color.red;
            ModelState modelState = ModelState.Disabled;
            GraphModel.CreateEdge(binary.Input0, outputPort);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    var c = GraphModel.NodeModels.OfType<IConstantNodeModel>().First();
                    c.Color = modelColor;
                    c.State = modelState;

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(GetConstantNode(1), Is.TypeOf<IntConstant>());

                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (ConstantNodeModel)GetNode(1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    return new ConvertConstantNodesAndVariableNodesCommand(new[] { c }, null);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(1), Is.TypeOf<VariableNodeModel>());

                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (VariableNodeModel)GetNode(1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    Assert.That(n1.GetDataType(), Is.EqualTo(typeof(int).GenerateTypeHandle()));
                    Assert.That(n1.Color, Is.EqualTo(modelColor));
                    Assert.That(n1.State, Is.EqualTo(modelState));
                });
        }

        [Test]
        public void Test_ItemizeVariableNodeCommand([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var variable = GraphModel.CreateVariableNode(declaration, Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);

            IPortModel outputPort = variable.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IPortModel outputPort1 = variable.OutputPort;
            GraphModel.CreateEdge(binary0.Input1, outputPort1);
            IPortModel outputPort2 = variable.OutputPort;
            GraphModel.CreateEdge(binary1.Input0, outputPort2);
            IPortModel outputPort3 = variable.OutputPort;
            GraphModel.CreateEdge(binary1.Input1, outputPort3);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref variable);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetAllNodes().OfType<VariableNodeModel>().Count(), Is.EqualTo(1));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input1));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary1.Input1));
                    return new ItemizeNodeCommand(variable);
                },
                () =>
                {
                    RefreshReference(ref variable);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetAllNodes().OfType<VariableNodeModel>().Count(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(variable.OutputPort, Is.Not.ConnectedTo(binary0.Input1));
                    Assert.That(variable.OutputPort, Is.Not.ConnectedTo(binary1.Input0));
                    Assert.That(variable.OutputPort, Is.Not.ConnectedTo(binary1.Input1));
                });
        }

        [Test]
        public void Test_ItemizeConstantNodeCommand([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);

            IPortModel outputPort = constant.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IPortModel outputPort1 = constant.OutputPort;
            GraphModel.CreateEdge(binary0.Input1, outputPort1);
            IPortModel outputPort2 = constant.OutputPort;
            GraphModel.CreateEdge(binary1.Input0, outputPort2);
            IPortModel outputPort3 = constant.OutputPort;
            GraphModel.CreateEdge(binary1.Input1, outputPort3);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(GetAllNodes().OfType<ConstantNodeModel>().Count(x => x.Type == typeof(int)), Is.EqualTo(1));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input1));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary1.Input1));
                    return new ItemizeNodeCommand(constant);
                },
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetAllNodes().OfType<ConstantNodeModel>().Count(x => x.Type == typeof(int)), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(constant.OutputPort, Is.Not.ConnectedTo(binary0.Input1));
                    Assert.That(constant.OutputPort, Is.Not.ConnectedTo(binary1.Input0));
                    Assert.That(constant.OutputPort, Is.Not.ConnectedTo(binary1.Input1));
                });
        }

        [Test]
        public void Test_ToggleLockConstantNodeCommand([Values] TestingMode mode)
        {
            var constant0 = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant0", Vector2.zero);
            var constant1 = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant1", Vector2.zero);
            var constant2 = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant2", Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.False);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                    return new LockConstantNodeCommand(new[] { constant0 }, true);
                },
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                    return new LockConstantNodeCommand(new[] { constant1, constant2 }, true);
                },
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.True);
                    Assert.That(constant2.IsLocked, Is.True);
                });
        }

        [Test]
        public void Test_UpdateTypeCommand([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType, Is.EqualTo(typeof(int).GenerateTypeHandle()));
                    return new ChangeVariableTypeCommand(declaration, typeof(float).GenerateTypeHandle());
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType, Is.EqualTo(typeof(float).GenerateTypeHandle()));
                });
        }

        [Test]
        public void Test_UpdateTypeCommand_UpdatesVariableReferences([Values] TestingMode mode)
        {
            TypeHandle intType = typeof(int).GenerateTypeHandle();
            TypeHandle floatType = typeof(float).GenerateTypeHandle();

            var declaration = GraphModel.CreateGraphVariableDeclaration(intType, "decl0", ModifierFlags.None, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType, Is.EqualTo(intType));
                    Assert.That(declaration.InitializationModel.Type, Is.EqualTo(typeof(int)));

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPort?.DataTypeHandle, Is.EqualTo(intType));

                    return new ChangeVariableTypeCommand(declaration, floatType);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType, Is.EqualTo(floatType));
                    Assert.That(declaration.InitializationModel.Type, Is.EqualTo(typeof(float)));

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPort?.DataTypeHandle, Is.EqualTo(floatType));
                });
        }

        [Test]
        public void Test_UpdateExposedCommand([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.True);
                    return new ExposeVariableCommand(declaration, false);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.False);
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.False);
                    return new ExposeVariableCommand(declaration, true);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.True);
                });
        }

        [Test]
        public void Test_UpdateTooltipCommand([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            declaration.Tooltip = "asd";
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("asd"));
                    return new UpdateTooltipCommand(declaration, "qwe");
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("qwe"));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("qwe"));
                    return new UpdateTooltipCommand(declaration, "asd");
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("asd"));
                });
        }
    }
}
