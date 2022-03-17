using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class DependencyTests : BaseUIFixture
    {
        [UsedImplicitly]
        [Serializable]
        class EntryPointNodeModel : NodeModel, IFakeNode
        {
            public IPortModel ExecOut0 { get; private set; }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                ExecOut0 = this.AddExecutionOutputPort("execOut0");
            }
        }

        [UsedImplicitly]
        [Serializable]
        class ExecNodeModel : NodeModel, IFakeNode
        {
            public IPortModel ExecIn0 { get; private set; }
            public IPortModel DataIn0 { get; private set; }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                ExecIn0 = this.AddExecutionInputPort("execIn0");
                DataIn0 = this.AddDataInputPort<int>("dataIn0");
            }
        }

        [UsedImplicitly]
        [Serializable]
        class DataNodeModel : NodeModel, IFakeNode
        {
            public IPortModel DataOut0 { get; private set; }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                DataOut0 = this.AddDataOutputPort<int>("dataOut0");
            }
        }


        [Serializable]
        class TestStencil : Stencil
        {
            public override Type GetConstantNodeValueType(TypeHandle typeHandle)
            {
                return TypeToConstantMapper.GetConstantNodeType(typeHandle);
            }

            /// <inheritdoc />
            public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            {
                return new BlackboardGraphModel(graphAssetModel);
            }

            /// <inheritdoc />
            public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
            {
                return null;
            }

            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
            {
                return new ClassSearcherDatabaseProvider(this);
            }

            public override IEnumerable<INodeModel> GetEntryPoints()
            {
                return GraphModel.NodeModels.OfType<EntryPointNodeModel>();
            }

            // Lifted almost verbatim from DotsStencil
            public override bool CreateDependencyFromEdge(IEdgeModel edgeModel, out LinkedNodesDependency linkedNodesDependency, out INodeModel parentNodeModel)
            {
                var outputNode = edgeModel.FromPort.NodeModel;
                var inputNode = edgeModel.ToPort.NodeModel;
                bool outputIsData = IsDataNode(outputNode);
                bool inputIsData = IsDataNode(inputNode);
                if (outputIsData)
                {
                    parentNodeModel = inputNode;
                    linkedNodesDependency = new LinkedNodesDependency
                    {
                        Count = 1,
                        DependentPort = edgeModel.FromPort,
                        ParentPort = edgeModel.ToPort,
                    };
                    return true;
                }
                if (!inputIsData)
                {
                    parentNodeModel = outputNode;
                    linkedNodesDependency = new LinkedNodesDependency
                    {
                        Count = 1,
                        DependentPort = edgeModel.ToPort,
                        ParentPort = edgeModel.FromPort,
                    };
                    return true;
                }

                linkedNodesDependency = default;
                parentNodeModel = default;
                return false;
            }

            // Lifted verbatim from DotsStencil
            public override IEnumerable<IEdgePortalModel> GetPortalDependencies(IEdgePortalModel portalModel)
            {
                switch (portalModel)
                {
                    case ExecutionEdgePortalEntryModel edgePortalModel:
                        return portalModel.GraphModel.FindReferencesInGraph<IEdgePortalExitModel>(edgePortalModel.DeclarationModel);
                    case DataEdgePortalExitModel edgePortalModel:
                        return portalModel.GraphModel.FindReferencesInGraph<IEdgePortalEntryModel>(edgePortalModel.DeclarationModel);
                    default:
                        return Enumerable.Empty<IEdgePortalModel>();
                }
            }

            // Lifted almost verbatim from DotsModelExtensions
            static bool IsDataNode(INodeModel nodeModel)
            {
                switch (nodeModel)
                {
                    case EntryPointNodeModel _:
                    case ExecNodeModel _:
                        return false;
                    case DataNodeModel _:
                        return true;
                    case DataEdgePortalEntryModel _:
                    case DataEdgePortalExitModel _:
                        return true;
                    case ExecutionEdgePortalEntryModel _:
                    case ExecutionEdgePortalExitModel _:
                        return false;
                    default:
                        throw new ArgumentException("Unknown node model");
                }
            }

            public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
            {
                return true;
            }

            public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
            {
                return true;
            }
        }

        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(TestStencil);

        void TestEntryDependencies(IEdgePortalEntryModel entryModel, ExecutionEdgePortalExitModel[] exitModels)
        {
            Assert.AreEqual(exitModels.Length, GraphView.PositionDependenciesManager.GetPortalDependencies(entryModel).Count);
            var dependencyModels = GraphView.PositionDependenciesManager.GetPortalDependencies(entryModel)
                .Select(d => d.DependentNode).ToList();
            foreach (var exitModel in exitModels)
            {
                Assert.AreEqual(0, GraphView.PositionDependenciesManager.GetPortalDependencies(exitModel).Count);
                Assert.IsTrue(dependencyModels.Contains(exitModel));
            }
        }

        [UnityTest]
        public IEnumerator AddingPortalToGraphAddsToDependencyManager()
        {
            var portalDecl = GraphModel.CreateGraphPortalDeclaration("Portal");

            // Create a entry portal
            var portalEntry = GraphModel.CreateNode<ExecutionEdgePortalEntryModel>("Portal", Vector2.zero);
            portalEntry.DeclarationModel = portalDecl;
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new ExecutionEdgePortalExitModel[0]);

            // Create a first exit portal connected to the entry
            var portalExit = (ExecutionEdgePortalExitModel)GraphModel.CreateOppositePortal(portalEntry);
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new[] { portalExit });

            // Create a second exit portal connected to the entry
            var portalExit2 = (ExecutionEdgePortalExitModel)GraphModel.CreateOppositePortal(portalEntry);
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new[] { portalExit, portalExit2 });

            // Create a second entry for the existing exits
            var portalEntry2 = (ExecutionEdgePortalEntryModel)GraphModel.CreateOppositePortal(portalExit);
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new[] { portalExit, portalExit2 });
            TestEntryDependencies(portalEntry2, new[] { portalExit, portalExit2 });
        }

        [UnityTest]
        public IEnumerator RemovingPortalFromGraphRemovesFromDependencyManager()
        {
            var portalDecl = GraphModel.CreateGraphPortalDeclaration("Portal");

            // Create our portals as we know they work from AddingPortalToGraphAddsToDependencyManager
            var portalEntry = GraphModel.CreateNode<ExecutionEdgePortalEntryModel>("Portal", Vector2.zero);
            portalEntry.DeclarationModel = portalDecl;
            var portalExit = (ExecutionEdgePortalExitModel)GraphModel.CreateOppositePortal(portalEntry);
            var portalExit2 = (ExecutionEdgePortalExitModel)GraphModel.CreateOppositePortal(portalEntry);
            var portalEntry2 = (ExecutionEdgePortalEntryModel)GraphModel.CreateOppositePortal(portalExit);
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new[] { portalExit, portalExit2 });
            TestEntryDependencies(portalEntry2, new[] { portalExit, portalExit2 });

            // Delete the second entry portal. Attempting to get its dependencies should return null
            GraphModel.DeleteNode(portalEntry2, deleteConnections: true);
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new[] { portalExit, portalExit2 });
            Assert.IsNull(GraphView.PositionDependenciesManager.GetPortalDependencies(portalEntry2));

            // Delete the second exit.
            GraphModel.DeleteNode(portalExit2, deleteConnections: true);
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new[] { portalExit });
            Assert.IsNull(GraphView.PositionDependenciesManager.GetPortalDependencies(portalEntry2));

            // Delete the first exit. There should be no dependencies to the remaining entry
            GraphModel.DeleteNode(portalExit, deleteConnections: true);
            MarkGraphModelStateDirty();
            yield return null;
            TestEntryDependencies(portalEntry, new ExecutionEdgePortalExitModel[0]);
            Assert.IsNull(GraphView.PositionDependenciesManager.GetPortalDependencies(portalEntry2));

            // Delete the first entry. There should be no more dependencies registered in the manager.
            GraphModel.DeleteNode(portalEntry, deleteConnections: true);
            MarkGraphModelStateDirty();
            yield return null;
            Assert.IsNull(GraphView.PositionDependenciesManager.GetPortalDependencies(portalEntry));
            Assert.IsNull(GraphView.PositionDependenciesManager.GetPortalDependencies(portalEntry2));
        }

        [UnityTest]
        public IEnumerator PortalsAreHandledInGraphDependencyTraversal()
        {
            // The setup:
            //
            // +--------+   +------------/    /----------+   +-------+
            // | Entry  #---# ExePortal /    / ExePortal #---#       |
            // +--------+   +----------/    /------------+   |       |
            //                                               | Node0 |
            // +------+   +-------------/    /-----------+   |       |
            // | Data o---o DataPortal /    / DataPortal o---o       |
            // +------+   +-----------/    /-------------+   +-------+
            //

            var exePortalDecl = GraphModel.CreateGraphPortalDeclaration("Exe Portal");
            var dataPortalDecl = GraphModel.CreateGraphPortalDeclaration("Data Portal");

            var entryNode = GraphModel.CreateNode<EntryPointNodeModel>("Entry", Vector2.zero);
            var dataNode = GraphModel.CreateNode<DataNodeModel>("Data");
            var node0 = GraphModel.CreateNode<ExecNodeModel>("Node0", Vector2.zero);

            var exePortalEntry = GraphModel.CreateNode<ExecutionEdgePortalEntryModel>("Trigger Portal Entry", Vector2.zero);
            exePortalEntry.DeclarationModel = exePortalDecl;
            var exePortalExit = (ExecutionEdgePortalExitModel)GraphModel.CreateOppositePortal(exePortalEntry);
            exePortalExit.Title = "Trigger Portal Exit";

            var dataPortalEntry = GraphModel.CreateNode<DataEdgePortalEntryModel>("Data Portal Entry", Vector2.zero);
            dataPortalEntry.DeclarationModel = dataPortalDecl;
            var dataPortalExit = (DataEdgePortalExitModel)GraphModel.CreateOppositePortal(dataPortalEntry);
            dataPortalExit.Title = "Data Portal Exit";

            GraphModel.CreateEdge(exePortalEntry.InputPort, entryNode.ExecOut0);
            GraphModel.CreateEdge(node0.ExecIn0, exePortalExit.OutputPort);

            GraphModel.CreateEdge(dataPortalEntry.InputPort, dataNode.DataOut0);
            GraphModel.CreateEdge(node0.DataIn0, dataPortalExit.OutputPort);

            MarkGraphModelStateDirty();

            yield return null;

            GraphView.PositionDependenciesManager.UpdateNodeState();

            bool IsUIEnabled(IGraphElementModel model)
            {
                GraphElement ui = model.GetView<GraphElement>(GraphView);
                return ui != null && !(ui.ClassListContains(Node.disabledModifierUssClassName) || ui.ClassListContains(Node.unusedModifierUssClassName));
            }

            Assert.IsTrue(IsUIEnabled(entryNode), "Graph entry point node should be marked as enabled.");
            Assert.IsTrue(IsUIEnabled(exePortalEntry), "Trigger entry portal should be marked as enabled.");
            Assert.IsTrue(IsUIEnabled(exePortalExit), "Trigger exit portal should be marked as enabled.");
            Assert.IsTrue(IsUIEnabled(node0), "Exec node should be marked as enabled.");
            Assert.IsTrue(IsUIEnabled(dataPortalExit), "Data exit portal should be marked as enabled.");
            Assert.IsTrue(IsUIEnabled(dataPortalEntry), "Data entry portal should be marked as enabled.");
            Assert.IsTrue(IsUIEnabled(dataNode), "Data node should be marked as enabled.");
        }
    }
}
