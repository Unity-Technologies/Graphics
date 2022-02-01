using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Node")]
    [Category("Command")]
    class NodeCommandTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateNodeFromSearcherCommand([Values] TestingMode mode)
        {
            var gedb = new GraphElementSearcherDatabase(Stencil, GraphModel);
            Type0FakeNodeModel.AddToSearcherDatabase(GraphModel, gedb);
            var db = gedb.Build();
            var item = (GraphNodeModelSearcherItem)db.Search(nameof(Type0FakeNodeModel))[0];

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    return CreateNodeCommand.OnGraph(item, new Vector2(100, 200),
                        new SerializableGUID("0123456789abcdef0123456789abcdef"));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.First(), Is.TypeOf<Type0FakeNodeModel>());
                    Assert.AreEqual(new SerializableGUID("0123456789abcdef0123456789abcdef"),
                        GraphModel.NodeModels.First().Guid);
                    Assert.That(GraphModel.NodeModels.First().Position,
                        Is.EqualTo(new Vector2(100, 200)));
                }
            );
        }

        [Test]
        public void Test_DuplicateCommand_OneNode([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    var nodeModel = GetNode(0);
                    Assert.That(nodeModel, Is.TypeOf<Type0FakeNodeModel>());

                    CopyPasteData copyPasteData = CopyPasteData.GatherCopiedElementsData(new List<IGraphElementModel> { nodeModel });

                    return new PasteSerializedDataCommand("Duplicate", Vector2.one, copyPasteData);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GraphModel.NodeModels.Count(n => n == null), Is.Zero);
                });
        }

        [Test]
        public void Test_DuplicateCommand_ConnectedNodes([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var edge = GraphModel.CreateEdge(node1.Input0, node0.Output0);
            var copyPasteData = CopyPasteData.GatherCopiedElementsData(new List<IGraphElementModel> { node1, edge });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    return new PasteSerializedDataCommand("Duplicate", Vector2.one, copyPasteData);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    var nodeToDelete = GetNode(0);
                    Assert.That(nodeToDelete, Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsCommand(nodeToDelete);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new PasteSerializedDataCommand("Duplicate", Vector2.one, copyPasteData);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsCommand_OneNode([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsCommand(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsCommand_ManyNodesSequential([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsCommand(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsCommand(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsCommand(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsCommand_ManyNodesSameTime([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsCommand(GetNode(0), GetNode(1), GetNode(2));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DisconnectNodeCommand([Values] TestingMode mode)
        {
            var const0 = GraphModel.CreateConstantNode(typeof(float).GenerateTypeHandle(), "const0", Vector2.zero);
            var const1 = GraphModel.CreateConstantNode(typeof(float).GenerateTypeHandle(), "const1", Vector2.zero);
            var const2 = GraphModel.CreateConstantNode(typeof(float).GenerateTypeHandle(), "const2", Vector2.zero);
            var const3 = GraphModel.CreateConstantNode(typeof(float).GenerateTypeHandle(), "const3", Vector2.zero);
            var const4 = GraphModel.CreateConstantNode(typeof(float).GenerateTypeHandle(), "const4", Vector2.zero);
            var const5 = GraphModel.CreateConstantNode(typeof(float).GenerateTypeHandle(), "const5", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var binary2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            var binary3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);
            GraphModel.CreateEdge(binary0.Input0, const0.OutputPort);
            GraphModel.CreateEdge(binary0.Input1, const1.OutputPort);
            GraphModel.CreateEdge(binary1.Input0, binary0.Output0);
            GraphModel.CreateEdge(binary1.Input1, const0.OutputPort);
            GraphModel.CreateEdge(binary2.Input0, const2.OutputPort);
            GraphModel.CreateEdge(binary2.Input1, const3.OutputPort);
            GraphModel.CreateEdge(binary3.Input0, const4.OutputPort);
            GraphModel.CreateEdge(binary3.Input1, const5.OutputPort);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(8));
                    return new DisconnectNodeCommand(binary0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                    return new DisconnectNodeCommand(binary2, binary3);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                });
        }

        [Test]
        public void Test_RemoveNodesCommand([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantA", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            IPortModel outputPort = constantA.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IPortModel outputPort1 = binary0.Output0;
            GraphModel.CreateEdge(binary1.Input0, outputPort1);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Refresh();
                    var nodeToDeleteAndBypass = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().First();

                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(nodeToDeleteAndBypass.Input0, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.Input0, Is.ConnectedTo(nodeToDeleteAndBypass.Output0));
                    return new BypassNodesCommand(new IInputOutputPortsNodeModel[] { nodeToDeleteAndBypass }, new INodeModel[] { nodeToDeleteAndBypass });
                },
                () =>
                {
                    Refresh();
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(binary1.Input0, Is.ConnectedTo(constantA.OutputPort));
                });

            void Refresh()
            {
                RefreshReference(ref binary0);
                RefreshReference(ref binary1);
                RefreshReference(ref constantA);
            }
        }

        T Get<T>(T prev) where T : class, INodeModel
        {
            GraphModel.TryGetModelFromGuid(prev.Guid, out T model);
            return model;
        }

        [Test]
        public void Test_ChangeNodeColorCommand([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            var node3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                    return new ChangeElementColorCommand(Color.red, node0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                    return new ChangeElementColorCommand(Color.blue, Get(node1), Get(node2));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.blue));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.blue));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                });
        }

        [Test]
        public void Test_ChangeNodeColorCommand_Capabilities([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            var node3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);
            node1.SetCapability(Capabilities.Colorable, false);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                    return new ChangeElementColorCommand(Color.red, node0, node1, node2);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                });
        }
    }
}
