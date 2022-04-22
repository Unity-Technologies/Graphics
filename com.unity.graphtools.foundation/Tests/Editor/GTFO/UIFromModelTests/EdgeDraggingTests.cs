using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class EdgeDraggingTests : GtfTestFixture
    {
        // disable panning while dragging elements to avoid slow tests panning more than expected with elements on the side of the window.
        protected override bool EnablePanning => false;

        [UnityTest]
        public IEnumerator DroppingNewEdgeOnPortCallsTheRightDelegate()
        {
            return DroppingEdgeCallsTheRightMethod(0, DropLocation.OnPort, DropAction.CreateNewEdge);
        }

        [UnityTest]
        public IEnumerator DroppingNewEdgeOnGraphCallsTheRightDelegate()
        {
            return DroppingEdgeCallsTheRightMethod(0, DropLocation.OnGraph, DropAction.DropEdgesOutside);
        }

        [UnityTest]
        public IEnumerator DroppingExistingEdgeOnGraphCallsTheRightDelegate()
        {
            return DroppingEdgeCallsTheRightMethod(1, DropLocation.OnGraph, DropAction.DropEdgesOutside);
        }

        [UnityTest]
        public IEnumerator DroppingExistingEdgeOnPortCallsTheRightDelegate()
        {
            return DroppingEdgeCallsTheRightMethod(1, DropLocation.OnPort, DropAction.MoveEdges);
        }

        [UnityTest]
        public IEnumerator Dropping2ExistingEdgesOnGraphCallsTheRightDelegate()
        {
            return DroppingEdgeCallsTheRightMethod(2, DropLocation.OnGraph, DropAction.DropEdgesOutside);
        }

        [UnityTest]
        public IEnumerator Dropping2ExistingEdgesOnPortCallsTheRightDelegate()
        {
            return DroppingEdgeCallsTheRightMethod(2, DropLocation.OnPort, DropAction.MoveEdges);
        }

        enum DropLocation
        {
            OnPort,
            OnGraph
        }

        IEnumerator DroppingEdgeCallsTheRightMethod(int existingEdges, DropLocation dropLocation, DropAction dropAction)
        {
            const float nodeYOffset = 30;
            var initialOutputModel = GraphModel.CreateNode<SingleOutputNodeModel>("initialOutput", Vector2.zero);

            // Create another output if we want to move to move an existing edge to a new one
            SingleOutputNodeModel targetOutputModel = null;
            if (existingEdges > 0 && dropLocation == DropLocation.OnPort)
            {
                targetOutputModel =
                    GraphModel.CreateNode<SingleOutputNodeModel>("targetOutput", new Vector2(0, nodeYOffset));
            }

            // create inputs for existing edges or if we are creating a new edge
            var inputsToCreate = existingEdges == 0 && dropLocation == DropLocation.OnPort ? 1 : existingEdges;
            var inputModels = new List<SingleInputNodeModel>(inputsToCreate);
            var createdEdges = new List<IEdgeModel>(existingEdges);
            for (int i = 0; i < inputsToCreate; ++i)
            {
                var model = GraphModel.CreateNode<SingleInputNodeModel>("otherInputNode" + i, new Vector2(200, nodeYOffset * i));
                if (i < existingEdges)
                {
                    var edge = GraphModel.CreateEdge(model.InputPort, initialOutputModel.OutputPort);
                    createdEdges.Add(edge);
                }
                inputModels.Add(model);
            }

            if (createdEdges.Count > 0)
                GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, createdEdges));

            MarkGraphModelStateDirty();
            yield return null;

            var portWatcher = new EdgeDragDelegateWatcher(GraphView);
            portWatcher.WatchPort(initialOutputModel.OutputPort);
            if (targetOutputModel != null)
                portWatcher.WatchPort(targetOutputModel.OutputPort);
            foreach (var inputModel in inputModels)
            {
                portWatcher.WatchPort(inputModel.InputPort);
            }

            // new edge dragged from port, existing edge dragged from just a bit further
            var dragStart = GetPortCenter(initialOutputModel.OutputPort);
            if (existingEdges > 0)
                dragStart += Vector2.right * 30;

            var dragStop = dragStart + nodeYOffset * Vector2.up;
            if (dropLocation != DropLocation.OnGraph)
            {
                var targetPortModel = existingEdges == 0 ? inputModels[0].InputPort : targetOutputModel.OutputPort;
                dragStop = GetPortCenter(targetPortModel);
            }

            Helpers.DragTo(dragStart, dragStop);

            yield return null;

            Assert.AreEqual(1, portWatcher.DelegateCalls.Count);
            Assert.AreEqual(dropAction, portWatcher.DelegateCalls[0].action);
            var affectedEdges = portWatcher.DelegateCalls[0].affectedEdges;
            if (existingEdges > 0)
            {

                Assert.AreEqual(createdEdges.Count, affectedEdges.Count);
                foreach (var affectedEdge in affectedEdges)
                {
                    Assert.IsNotNull(affectedEdge.EdgeModel);
                    Assert.IsTrue(createdEdges.Contains(affectedEdge.EdgeModel));
                }
            }
        }

        Vector2 GetPortCenter(IPortModel portModel)
        {
            Port port = portModel.GetView<Port>(GraphView);
            Assert.IsNotNull(port);

            var connector = port.SafeQ(PortConnectorPart.connectorUssName);
            return connector.parent.LocalToWorld(connector.layout.center);
        }

        public enum DropAction
        {
            MoveEdges, CreateNewEdge, DropEdgesOutside
        }

        class EdgeDragDelegateWatcher
        {
            GraphView GraphView { get; }
            public readonly List<(DropAction action, Port port, List<Edge> affectedEdges)> DelegateCalls;

            public EdgeDragDelegateWatcher(GraphView graphView)
            {
                GraphView = graphView;
                DelegateCalls = new List<(DropAction, Port, List<Edge>)>();
            }

            public void WatchPort(IPortModel portModel)
            {
                var port = portModel.GetView<Port>(GraphView);
                Assert.IsNotNull(port);
                var helper = new TestsEdgeDragHelper(port, this);
                port.EdgeConnector.EdgeDragHelper = helper;
            }

            class TestsEdgeDragHelper : EdgeDragHelper
            {
                Port Port { get; }
                EdgeDragDelegateWatcher Watcher { get; }

                public TestsEdgeDragHelper(Port port, EdgeDragDelegateWatcher watcher)
                    : base(port.RootView as GraphView, null)
                {
                    Port = port;
                    Watcher = watcher;
                }

                internal override void MoveEdges(IEnumerable<Edge> edges, Port newPort)
                {
                    Watcher.DelegateCalls.Add((DropAction.MoveEdges, Port, affectedEdges: edges.ToList()));
                }

                internal override void CreateNewEdge(IPortModel fromPort, IPortModel toPort)
                {
                    Watcher.DelegateCalls.Add((DropAction.CreateNewEdge, Port, new List<Edge>()));
                }

                internal override void DropEdgesOutside(IEnumerable<Edge> edges, IEnumerable<IPortModel> portModels, Vector2 worldPosition)
                {
                    Watcher.DelegateCalls.Add((DropAction.DropEdgesOutside, Port, affectedEdges: edges.ToList()));
                }
            }
        }
    }
}
