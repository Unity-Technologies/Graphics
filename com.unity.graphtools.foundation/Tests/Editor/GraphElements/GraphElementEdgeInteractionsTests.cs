using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementEdgeInteractionsTests : GraphViewTester
    {
        IONodeModel firstNode { get; set; }
        IONodeModel secondNode { get; set; }
        IPortModel startPort { get; set; }
        IPortModel endPort { get; set; }
        IPortModel startPortTwo { get; set; }
        IPortModel endPortTwo { get; set; }

        const float k_EdgeSelectionOffset = 20.0f;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            firstNode = CreateNode("First Node", new Vector2(0, 0), outCount: 2);
            startPort = firstNode.OutputsByDisplayOrder[0];
            startPortTwo = firstNode.OutputsByDisplayOrder[1];

            secondNode = CreateNode("Second Node", new Vector2(400, 0), inCount: 2);
            endPort = secondNode.InputsByDisplayOrder[0];
            endPortTwo = secondNode.InputsByDisplayOrder[1];
        }

        [UnityTest]
        public IEnumerator MixedOrientationEdges()
        {
            var horizontalNode = CreateNode("Horizontal Node", new Vector2(100, 200), 1, 1);
            var hInPort = horizontalNode.InputsByDisplayOrder[0];
            var hOutPort = horizontalNode.OutputsByDisplayOrder[0];

            var verticalNode = CreateNode("Vertical Node", new Vector2(500, 100), 1, 1, orientation: PortOrientation.Vertical);
            var vInPort = verticalNode.InputsByDisplayOrder[0];
            var vOutPort = verticalNode.OutputsByDisplayOrder[0];

            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(hOutPort, vInPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edgeModel = hOutPort.GetConnectedEdges().First();
            Assert.IsNotNull(edgeModel);

            Port outputPort = hOutPort.GetUI<Port>(graphView);
            Port inputPort = vInPort.GetUI<Port>(graphView);

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            Edge edge = edgeModel.GetUI<Edge>(graphView);
            Assert.IsNotNull(edge);
            Assert.AreEqual(inputPort.PortModel, edge.Input);
            Assert.AreEqual(outputPort.PortModel, edge.Output);
            Assert.AreEqual(PortOrientation.Vertical, edge.EdgeControl.InputOrientation);
            Assert.AreEqual(PortOrientation.Horizontal, edge.EdgeControl.OutputOrientation);

            actions = ConnectPorts(vOutPort, hInPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            edgeModel = vOutPort.GetConnectedEdges().First();
            Assert.IsNotNull(edgeModel);

            outputPort = vOutPort.GetUI<Port>(graphView);
            inputPort = hInPort.GetUI<Port>(graphView);

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            edge = edgeModel.GetUI<Edge>(graphView);
            Assert.IsNotNull(edge);
            Assert.AreEqual(inputPort.PortModel, edge.Input);
            Assert.AreEqual(outputPort.PortModel, edge.Output);
            Assert.AreEqual(PortOrientation.Horizontal, edge.EdgeControl.InputOrientation);
            Assert.AreEqual(PortOrientation.Vertical, edge.EdgeControl.OutputOrientation);
        }

        [UnityTest]
        public IEnumerator EdgeConnectOnSinglePortOutputToInputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            // We start without any connection
            Assert.IsFalse(startPort.IsConnected());
            Assert.IsFalse(endPort.IsConnected());

            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Check that the edge exists and that it connects the two ports.
            Assert.IsTrue(startPort.IsConnected());
            Assert.IsTrue(endPort.IsConnected());
            Assert.IsTrue(startPort.IsConnectedTo(endPort));

            var edge = startPort.GetConnectedEdges().First();
            Assert.IsNotNull(edge);

            var edgeUI = edge.GetUI<Edge>(graphView);
            Assert.IsNotNull(edgeUI);
            Assert.IsNotNull(edgeUI.parent);
        }

        // TODO Add Test multi port works
        // TODO Add Test multi connection to single port replaces connection
        // TODO Add Test disallow multiple edges on same multiport pairs (e.g. multiple edges between output A and input B)

        [UnityTest]
        public IEnumerator EdgeConnectOnSinglePortInputToOutputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            // We start without any connection
            Assert.IsFalse(startPort.IsConnected());
            Assert.IsFalse(endPort.IsConnected());

            var actions = ConnectPorts(endPort, startPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Check that the edge exists and that it connects the two ports.
            Assert.IsTrue(startPort.IsConnected());
            Assert.IsTrue(endPort.IsConnected());
            Assert.IsTrue(startPort.IsConnectedTo(endPort));

            var edge = startPort.GetConnectedEdges().First();
            Assert.IsNotNull(edge);

            var edgeUI = edge.GetUI<Edge>(graphView);
            Assert.IsNotNull(edgeUI);
            Assert.IsNotNull(edgeUI.parent);
        }

        [UnityTest]
        public IEnumerator EdgeDisconnectInputWorks()
        {
            bool searcherInvoked = false;
            (GraphModel.Stencil as TestStencil)?.SetOnGetSearcherDatabaseProviderCallback(() => searcherInvoked = true);

            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);
            Assert.IsNotNull(startPortUI);
            Assert.IsNotNull(endPortUI);
            var startPortPosition = startPortUI.GetGlobalCenter();
            var endPortPosition = endPortUI.GetGlobalCenter();

            var edgeModel = startPort.GetConnectedEdges().First();
            var edge = edgeModel.GetUI<Edge>(graphView);
            Assert.IsNotNull(edge);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortPosition.x - k_EdgeSelectionOffset, endPortPosition.y);
            helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortPosition.x + (endPortPosition.x - startPortPosition.x) / 2, endPortPosition.y);
            helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;


            Assert.IsTrue(searcherInvoked, "Searcher was not invoked.");
            Assert.AreEqual(1, startPort.GetConnectedEdges().Count(), "Edge was unexpectedly deleted.");
        }

        [UnityTest]
        public IEnumerator EdgeDisconnectOutputWorks()
        {
            bool searcherInvoked = false;
            (GraphModel.Stencil as TestStencil)?.SetOnGetSearcherDatabaseProviderCallback(() => searcherInvoked = true);

            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;
            float endPortX = endPortUI.GetGlobalCenter().x;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.IsTrue(searcherInvoked, "Searcher was not invoked.");
            Assert.AreEqual(1, startPort.GetConnectedEdges().Count(), "Edge was unexpectedly deleted.");
        }

        [UnityTest]
        public IEnumerator EdgeReconnectInputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var endPortUI = endPort.GetUI<Port>(graphView);

            float endPortX = endPortUI.GetGlobalCenter().x;
            float endPortY = endPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the second port while holding CTRL.
            var endPortTwoUI = endPortTwo.GetUI<Port>(graphView);
            var portTwoAreaPos = endPortTwoUI.GetGlobalCenter();
            helpers.MouseDragEvent(edgeRightSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the port area
            helpers.MouseUpEvent(portTwoAreaPos);
            yield return null;

            edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPortTwo, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeReconnectOutputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the second port while holding CTRL.
            var startPortTwoUI = startPortTwo.GetUI<Port>(graphView);
            var portTwoAreaPos = startPortTwoUI.GetGlobalCenter();
            helpers.MouseDragEvent(edgeLeftSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(portTwoAreaPos);
            yield return null;

            edge = startPortTwo.GetConnectedEdges().First().GetUI<Edge>(graphView);

            Assert.AreEqual(startPortTwo, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnInput()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float endPortX = endPortUI.GetGlobalCenter().x;
            float endPortY = endPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, endPortY);
            helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            helpers.KeyDownEvent(KeyCode.Escape);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            helpers.KeyUpEvent(KeyCode.Escape);
            yield return null;

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnOutput()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;
            float endPortX = endPortUI.GetGlobalCenter().x;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            helpers.KeyDownEvent(KeyCode.Escape);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            helpers.KeyUpEvent(KeyCode.Escape);
            yield return null;

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeConnectionUnderThresholdDistanceNotEffective()
        {
            MarkGraphViewStateDirty();
            yield return null;

            Port startPortUI = startPort.GetUI<Port>(graphView);
            var startPos = startPortUI.GetGlobalCenter();
            helpers.DragTo(startPos, startPos + new Vector3(EdgeConnector.connectionDistanceThreshold / 2f, 0f, 0f));

            yield return null;

            Assert.AreEqual(0, secondNode.GetInputPorts().First().GetConnectedEdges().Count());
        }

        [UnityTest]
        public IEnumerator EdgeConnectDragMultipleEdgesFromPortOutputToInputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            // We start without any connection
            Assert.AreEqual(0, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, endPortTwo.GetConnectedEdges().Count());


            Port startPortUI = startPort.GetUI<Port>(graphView);
            Port endPortUI = endPort.GetUI<Port>(graphView);
            Port endPortTwoUI = endPortTwo.GetUI<Port>(graphView);

            // Drag an edge between the two ports
            helpers.DragTo(startPortUI.GetGlobalCenter(), endPortUI.GetGlobalCenter());
            helpers.DragTo(startPortUI.GetGlobalCenter(), endPortTwoUI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(2, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPortTwo.GetConnectedEdges().Count());

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, startPort.GetConnectedEdges().First(), startPort.GetConnectedEdges().Skip(1).First()));

            Port startPortTwoUI = startPortTwo.GetUI<Port>(graphView);
            helpers.DragTo(startPortUI.GetGlobalCenter() + new Vector3(k_EdgeSelectionOffset, 0, 0), startPortTwoUI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            Assert.AreEqual(2, startPortTwo.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPortTwo.GetConnectedEdges().Count());
        }

        [UnityTest]
        public IEnumerator EdgeConnectDragMultipleEdgesFromExecutionPortOutputToInputWorks()
        {
            IONodeModel exeStartNode = CreateNode("First Out Exe node", new Vector2(100, 100), 0, 0, 0, 1);
            IONodeModel exeStartNode2 = CreateNode("Second Out Exe node", new Vector2(100, 400), 0, 0, 0, 1);
            IONodeModel exeEndNode = CreateNode("First In Exe node", new Vector2(400, 100), 0, 0, 1, 0);
            IONodeModel exeEndNode2 = CreateNode("Second In Exe node", new Vector2(400, 400), 0, 0, 1, 0);

            IPortModel startPort = exeStartNode.GetPorts(PortDirection.Output, PortType.Execution).First();
            IPortModel startPort2 = exeStartNode2.GetPorts(PortDirection.Output, PortType.Execution).First();
            IPortModel endPort = exeEndNode.GetPorts(PortDirection.Input, PortType.Execution).First();
            IPortModel endPort2 = exeEndNode2.GetPorts(PortDirection.Input, PortType.Execution).First();

            // We start without any connection
            Assert.AreEqual(0, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, endPort2.GetConnectedEdges().Count());

            MarkGraphViewStateDirty();
            yield return null;

            Port startPortUI = startPort.GetUI<Port>(graphView);
            Port startPort2UI = startPort2.GetUI<Port>(graphView);
            Port endPortUI = endPort.GetUI<Port>(graphView);
            Port endPort2UI = endPort2.GetUI<Port>(graphView);

            // Drag an edge between the two ports
            helpers.DragTo(startPortUI.GetGlobalCenter(), endPortUI.GetGlobalCenter());
            helpers.DragTo(startPortUI.GetGlobalCenter(), endPort2UI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(2, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort2.GetConnectedEdges().Count());

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, startPort.GetConnectedEdges().First(), startPort.GetConnectedEdges().Skip(1).First()));

            helpers.DragTo(startPortUI.GetGlobalCenter() + new Vector3(k_EdgeSelectionOffset, 0, 0), startPort2UI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;
            Assert.AreEqual(2, startPort2.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort2.GetConnectedEdges().Count());
        }

        [UnityTest]
        public IEnumerator EdgeConnectDragMultipleEdgesFromExecutionPortInputToOutputWorks()
        {
            IONodeModel exeStartNode = CreateNode("First Out Exe node", new Vector2(100, 100), 0, 0, 0, 1);
            IONodeModel exeStartNode2 = CreateNode("Second Out Exe node", new Vector2(100, 400), 0, 0, 0, 1);
            IONodeModel exeEndNode = CreateNode("First In Exe node", new Vector2(400, 100), 0, 0, 1, 0);
            IONodeModel exeEndNode2 = CreateNode("Second In Exe node", new Vector2(400, 400), 0, 0, 1, 0);

            IPortModel startPort = exeStartNode.GetPorts(PortDirection.Output, PortType.Execution).First();
            IPortModel startPort2 = exeStartNode2.GetPorts(PortDirection.Output, PortType.Execution).First();
            IPortModel endPort = exeEndNode.GetPorts(PortDirection.Input, PortType.Execution).First();
            IPortModel endPort2 = exeEndNode2.GetPorts(PortDirection.Input, PortType.Execution).First();

            // We start without any connection
            Assert.AreEqual(0, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, endPort2.GetConnectedEdges().Count());

            MarkGraphViewStateDirty();
            yield return null;

            Port startPortUI = startPort.GetUI<Port>(graphView);
            Port startPort2UI = startPort2.GetUI<Port>(graphView);
            Port endPortUI = endPort.GetUI<Port>(graphView);
            Port endPort2UI = endPort2.GetUI<Port>(graphView);

            // Drag an edge between the two ports
            helpers.DragTo(startPortUI.GetGlobalCenter(), endPortUI.GetGlobalCenter());
            helpers.DragTo(startPort2UI.GetGlobalCenter(), endPortUI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(1, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, startPort2.GetConnectedEdges().Count());
            Assert.AreEqual(2, endPort.GetConnectedEdges().Count());

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, endPort.GetConnectedEdges().First(), endPort.GetConnectedEdges().Skip(1).First()));

            helpers.DragTo(endPortUI.GetGlobalCenter() - new Vector3(k_EdgeSelectionOffset, 0, 0), endPort2UI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            Assert.AreEqual(1, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, startPort2.GetConnectedEdges().Count());
            Assert.AreEqual(2, endPort2.GetConnectedEdges().Count());
        }
    }
}
