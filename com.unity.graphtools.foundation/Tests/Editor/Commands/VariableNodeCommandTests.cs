using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Variable")]
    [Category("Node")]
    [Category("Command")]
    class VariableNodeCommandTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        public enum VariableDeclarationTestType
        {
            Standard,
            Custom
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
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(0)));
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
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(1)));
                });
        }

        [Test]
        public void Test_CreateVariableNodeCommand_DataOutputVariables([Values] TestingMode mode)
        {
            var dataOutputVariable = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "dataOutput", ModifierFlags.Write, true);

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
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(0)));
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
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                    Assert.That(GetNode(1), Is.TypeOf<VariableNodeModel>());
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(1)));
                });
        }

        [Test]
        public void Test_CreateVariableNodeCommand_ExecutionOutputVariables([Values] TestingMode mode)
        {
            var executionOutputVariable = GraphModel.CreateGraphVariableDeclaration(TypeHandle.ExecutionFlow, "executionOutput", ModifierFlags.Write, true);

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
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(0)));
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
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(1)));
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
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(3));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(3)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(4)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(5)));
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
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(3));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(3)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(4)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(5)));
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
    }
}
