using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    public class PortalCommandTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void Test_ConvertEdgesToPortalsCommand([Values] TestingMode mode)
        {
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>();
            GraphModel.CreateEdge(node2.ExeInput0, node1.ExeOutput0);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node1);
                    RefreshReference(ref node2);

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.All(n => n is Type0FakeNodeModel));
                    Assert.That(node1.ExeOutput0, Is.ConnectedTo(node2.ExeInput0));

                    var allPortalData = new List<(IEdgeModel, Vector2, Vector2)> { (GetEdge(0), Vector2.left, Vector2.right)};
                    return new ConvertEdgesToPortalsCommand(allPortalData);
                },
                () =>
                {
                    RefreshReference(ref node1);
                    RefreshReference(ref node2);

                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GraphModel.NodeModels.OfType<Type0FakeNodeModel>().Count(), Is.EqualTo(2));
                    Assert.That(GraphModel.NodeModels.OfType<IEdgePortalEntryModel>().Count(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.OfType<IEdgePortalExitModel>().Count(), Is.EqualTo(1));
                    var entry = GraphModel.NodeModels.OfType<IEdgePortalEntryModel>().Single();
                    var exit = GraphModel.NodeModels.OfType<IEdgePortalExitModel>().Single();
                    Assert.That(node1.ExeOutput0, Is.Not.ConnectedTo(node2.ExeInput0));
                    Assert.That(node1.ExeOutput0, Is.ConnectedTo(entry.InputPort));
                    Assert.That(exit.OutputPort, Is.ConnectedTo(node2.ExeInput0));
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(2));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(2)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(3)));
                }
            );
        }

        [Test]
        public void CreatePortalsShouldKeepEdgesOrder([Values] TestingMode mode)
        {
            var originNode = GraphModel.CreateNode<Type0FakeNodeModel>("Origin", Vector2.zero);
            var destNode1 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest1", Vector2.zero);
            var destNode2 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest2", Vector2.zero);
            var destNode3 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest3", Vector2.zero);
            var destNode4 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest4", Vector2.zero);
            var destNode5 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest5", Vector2.zero);

            GraphModel.CreateEdge(destNode1.ExeInput0, originNode.ExeOutput0);
            GraphModel.CreateEdge(destNode2.ExeInput0, originNode.ExeOutput0);
            GraphModel.CreateEdge(destNode3.ExeInput0, originNode.ExeOutput0);
            GraphModel.CreateEdge(destNode4.ExeInput0, originNode.ExeOutput0);
            GraphModel.CreateEdge(destNode5.ExeInput0, originNode.ExeOutput0);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref originNode);
                    var port = originNode.ExeOutput0 as IReorderableEdgesPortModel;
                    Assert.NotNull(port);
                    Assert.IsTrue(port.HasReorderableEdges);
                    Assert.AreEqual(2, port.GetEdgeOrder(GetEdge(2)));

                    var allPortalData = new List<(IEdgeModel, Vector2, Vector2)> { (GetEdge(2), Vector2.left, Vector2.right) };
                    return new ConvertEdgesToPortalsCommand(allPortalData);
                },
                () =>
                {
                    RefreshReference(ref originNode);
                    var port = originNode.ExeOutput0 as IReorderableEdgesPortModel;
                    Assert.NotNull(port);
                    Assert.IsTrue(port.HasReorderableEdges);
                    Assert.AreEqual(2, port.GetEdgeOrder(GetEdge(2)));
                }
            );
        }

        public static Type[] DuplicatePortalTestCases()
        {
            return new[]
            {
                typeof(DataEdgePortalEntryModel),
                typeof(DataEdgePortalExitModel),
                typeof(ExecutionEdgePortalEntryModel),
                typeof(ExecutionEdgePortalExitModel)
            };
        }

        static bool PortalCanBeDupped(Type portalType)
        {
            return portalType != typeof(DataEdgePortalEntryModel);
        }

        static (Type entryType, Type exitType)[] s_EntryExitTypes =
        {
            (typeof(DataEdgePortalEntryModel), typeof(DataEdgePortalExitModel)),
            (typeof(ExecutionEdgePortalEntryModel), typeof(ExecutionEdgePortalExitModel)),
        };

        static Type GetOppositeType(Type portalType)
        {
            return typeof(IEdgePortalEntryModel).IsAssignableFrom(portalType)
                ? s_EntryExitTypes.Single(e => e.entryType == portalType).exitType
                : s_EntryExitTypes.Single(e => e.exitType == portalType).entryType;
        }

        [Test]
        public void Test_DuplicatePortalOnlyWorksForSomeTypes([ValueSource(nameof(DuplicatePortalTestCases))] Type portalType, [Values] TestingMode mode)
        {
            CreateNodesWithEachPortal();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(6)); // 2 nodes and 4 portals
                    Assert.That(GetEdgeCount(), Is.EqualTo(4)); // 2 portals on each node

                    var portalToCopy = GraphModel.NodeModels.Single(n => n.GetType() == portalType);
                    var elementsToCopy = new[] {portalToCopy};

                    {
                        // TODO VladN July 2021 - see GTF-400
                        // for now copy/paste relies on not-copyable
                        // elements being removed BEFORE calling the PasteSerializedDataCommand or CopyPasteData.PasteSerializedData
                        // Once GTF-400 is addressed, we should just pass the portal to copy to the command and not
                        // remove it manually.
                        if (!portalToCopy.IsCopiable())
                            elementsToCopy = new INodeModel[] {};
                    }
                    var copyData = CopyPasteData.GatherCopiedElementsData(null, elementsToCopy);
                    return new PasteSerializedDataCommand(PasteOperation.Duplicate,"Duplicate", Vector2.one, copyData);
                },
                () =>
                {
                    int numNewPortalsExpected = PortalCanBeDupped(portalType) ? 1 : 0;
                    Assert.That(GetNodeCount(), Is.EqualTo(6 + numNewPortalsExpected));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GraphModel.NodeModels.Count(n => n.GetType() == portalType), Is.EqualTo(1 + numNewPortalsExpected));
                });
        }

        void CreateNodesWithEachPortal(bool createDataPortals = true, bool createExecPortals = true)
        {
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>();

            if (createDataPortals)
            {
                var dataEdge = GraphModel.CreateEdge(node2.Input0, node1.Output0);

                var dataEntry = GraphModel.CreateEntryPortalFromPort(dataEdge.FromPort, Vector2.zero, 0, null);
                dataEdge.SetPort(EdgeSide.To, dataEntry.InputPort);

                Assert.IsNotNull(dataEntry);

                var dataExit = GraphModel.CreateOppositePortal(dataEntry) as DataEdgePortalExitModel;
                Assert.IsNotNull(dataExit);
                GraphModel.CreateEdge(node2.Input0, dataExit.OutputPort);
            }

            if (createExecPortals)
            {
                var execEdge = GraphModel.CreateEdge(node2.ExeInput0, node1.ExeOutput0);

                var execEntry = GraphModel.CreateEntryPortalFromPort(execEdge.FromPort, Vector2.zero, 0, null);
                execEdge.SetPort(EdgeSide.To, execEntry.InputPort);

                Assert.IsNotNull(execEntry);

                var execExit = GraphModel.CreateOppositePortal(execEntry) as ExecutionEdgePortalExitModel;
                Assert.IsNotNull(execExit);
                GraphModel.CreateEdge(node2.ExeInput0, execExit.OutputPort);
            }
        }

        [Test]
        public void Test_CreateOppositePortal([ValueSource(nameof(DuplicatePortalTestCases))] Type portalType, [Values] TestingMode mode)
        {
            CreateNodesWithEachPortal();
            var oppositePortals = GraphModel.NodeModels.Where(n => n.GetType() == GetOppositeType(portalType)).ToArray();
            GraphModel.DeleteNodes(oppositePortals, true);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(5)); // 2 nodes and 3 portals
                    Assert.That(GetEdgeCount(), Is.EqualTo(3)); // 3 connected portals
                    Assert.That(GraphModel.NodeModels.Count(n => n.GetType() == GetOppositeType(portalType)), Is.EqualTo(0));
                    var portal = GraphModel.NodeModels.OfType<IEdgePortalModel>().Single(n => n.GetType() == portalType);
                    return new CreateOppositePortalCommand(portal);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(6)); // 2 nodes and 4 portals
                    Assert.That(GetEdgeCount(), Is.EqualTo(3)); // still 3 connected portals because we didn't connect the new one
                    Assert.That(GraphModel.NodeModels.Count(n => n.GetType() == GetOppositeType(portalType)), Is.EqualTo(1));
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(5)));
                });
        }

        [Test]
        public void Test_CreateAdditionalOppositePortal([ValueSource(nameof(DuplicatePortalTestCases))] Type portalType, [Values] TestingMode mode)
        {
            CreateNodesWithEachPortal();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(6)); // 2 nodes and 4 portals
                    Assert.That(GetEdgeCount(), Is.EqualTo(4)); // 2 portals on each node
                    Assert.That(GraphModel.NodeModels.Count(n => n.GetType() == GetOppositeType(portalType)), Is.EqualTo(1));

                    var portal = GraphModel.NodeModels.OfType<IEdgePortalModel>().Single(n => n.GetType() == portalType);
                    return new CreateOppositePortalCommand(portal);
                },
                () =>
                {
                    var oppositeType = GetOppositeType(portalType);
                    bool canBeDupped = PortalCanBeDupped(oppositeType);
                    int numNewPortalsExpected = canBeDupped ? 1 : 0;
                    Assert.That(GetNodeCount(), Is.EqualTo(6 + numNewPortalsExpected));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GraphModel.NodeModels.Count(n => n.GetType() == oppositeType), Is.EqualTo(1 + numNewPortalsExpected));
                });
        }

        [Test]
        public void Test_DeleteLastPortalDeletesItsDeclaration([Values] TestingMode mode)
        {
            CreateNodesWithEachPortal(true, false);
            GraphModel.DeleteNodes(GraphModel.NodeModels.OfType<IEdgePortalModel>().Take(1).ToArray(), true);
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3)); // 2 nodes and 1 portal
                    Assert.That(GetEdgeCount(), Is.EqualTo(1)); // 1 connected portal

                    var lastPortal = GraphModel.NodeModels.OfType<IEdgePortalModel>().Single();
                    Assert.That(lastPortal.DeclarationModel, Is.Not.Null);
                    Assert.That(GraphModel.PortalDeclarations.Count, Is.EqualTo(1));
                    return new DeleteElementsCommand(lastPortal);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2)); // 2 nodes and no portal
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(GraphModel.NodeModels.OfType<IEdgePortalModel>().Count(), Is.Zero);
                    Assert.That(GraphModel.PortalDeclarations.Count, Is.Zero);
                });
        }

        [Test]
        public void PortalsShouldKeepDataType([Values] TestingMode mode)
        {
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>();
            GraphModel.CreateEdge(node2.Input0, node1.Output0);

            var expectedDataType = node2.Input0.DataTypeHandle;
            Assert.That(expectedDataType, Is.EqualTo(node1.Output0.DataTypeHandle));

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node1);
                    RefreshReference(ref node2);

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.All(n => n is Type0FakeNodeModel));
                    Assert.That(node1.Output0, Is.ConnectedTo(node2.Input0));

                    var allPortalData = new List<(IEdgeModel, Vector2, Vector2)> {(GetEdge(0), Vector2.left, Vector2.right)};
                    return new ConvertEdgesToPortalsCommand(allPortalData);
                },
                () =>
                {
                    RefreshReference(ref node1);
                    RefreshReference(ref node2);

                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GraphModel.NodeModels.OfType<Type0FakeNodeModel>().Count(), Is.EqualTo(2));
                    Assert.That(GraphModel.NodeModels.OfType<IEdgePortalEntryModel>().Count(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.OfType<IEdgePortalExitModel>().Count(), Is.EqualTo(1));
                    var entry = GraphModel.NodeModels.OfType<IEdgePortalEntryModel>().Single();
                    var exit = GraphModel.NodeModels.OfType<IEdgePortalExitModel>().Single();
                    Assert.That(node1.Output0, Is.Not.ConnectedTo(node2.Input0));
                    Assert.That(node1.Output0, Is.ConnectedTo(entry.InputPort));
                    Assert.That(exit.OutputPort, Is.ConnectedTo(node2.Input0));
                    Assert.That(entry.InputPort.DataTypeHandle, Is.EqualTo(expectedDataType));
                    Assert.That(exit.OutputPort.DataTypeHandle, Is.EqualTo(expectedDataType));
                }
            );

            var entryPortal = GraphModel.NodeModels.OfType<IEdgePortalEntryModel>().Single();
            var exitPortal = GraphModel.NodeModels.OfType<IEdgePortalExitModel>().Single();

            GraphModel.DeleteNode(entryPortal, true);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3)); // 2 nodes and 1 portal
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    return new CreateOppositePortalCommand(exitPortal);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4)); // 2 nodes and 2 portals
                    Assert.That(GetEdgeCount(), Is.EqualTo(1)); // still 1 connected portal because we didn't connect the new one
                    var newPortal = GraphModel.NodeModels.OfType<IEdgePortalEntryModel>().Single();
                    Assert.That(newPortal.PortDataTypeHandle, Is.EqualTo(expectedDataType));
                });
        }
    }
}
