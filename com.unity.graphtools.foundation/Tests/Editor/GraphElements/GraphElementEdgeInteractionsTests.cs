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
        IONodeModel FirstNode { get; set; }
        IONodeModel SecondNode { get; set; }
        IPortModel StartPort { get; set; }
        IPortModel EndPort { get; set; }
        IPortModel StartPortTwo { get; set; }
        IPortModel EndPortTwo { get; set; }

        const float k_EdgeSelectionOffset = 20.0f;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            FirstNode = CreateNode("First Node", new Vector2(0, 0), outCount: 2);
            StartPort = FirstNode.OutputsByDisplayOrder[0];
            StartPortTwo = FirstNode.OutputsByDisplayOrder[1];

            SecondNode = CreateNode("Second Node", new Vector2(400, 0), inCount: 2);
            EndPort = SecondNode.InputsByDisplayOrder[0];
            EndPortTwo = SecondNode.InputsByDisplayOrder[1];
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

            Port outputPort = hOutPort.GetView<Port>(GraphView);
            Port inputPort = vInPort.GetView<Port>(GraphView);

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            Edge edge = edgeModel.GetView<Edge>(GraphView);
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

            outputPort = vOutPort.GetView<Port>(GraphView);
            inputPort = hInPort.GetView<Port>(GraphView);

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            edge = edgeModel.GetView<Edge>(GraphView);
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
            Assert.IsFalse(StartPort.IsConnected());
            Assert.IsFalse(EndPort.IsConnected());

            var actions = ConnectPorts(StartPort, EndPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Check that the edge exists and that it connects the two ports.
            Assert.IsTrue(StartPort.IsConnected());
            Assert.IsTrue(EndPort.IsConnected());
            Assert.IsTrue(StartPort.IsConnectedTo(EndPort));

            var edge = StartPort.GetConnectedEdges().First();
            Assert.IsNotNull(edge);

            var edgeUI = edge.GetView<Edge>(GraphView);
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
            Assert.IsFalse(StartPort.IsConnected());
            Assert.IsFalse(EndPort.IsConnected());

            var actions = ConnectPorts(EndPort, StartPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Check that the edge exists and that it connects the two ports.
            Assert.IsTrue(StartPort.IsConnected());
            Assert.IsTrue(EndPort.IsConnected());
            Assert.IsTrue(StartPort.IsConnectedTo(EndPort));

            var edge = StartPort.GetConnectedEdges().First();
            Assert.IsNotNull(edge);

            var edgeUI = edge.GetView<Edge>(GraphView);
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

            var actions = ConnectPorts(StartPort, EndPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var startPortUI = StartPort.GetView<Port>(GraphView);
            var endPortUI = EndPort.GetView<Port>(GraphView);
            Assert.IsNotNull(startPortUI);
            Assert.IsNotNull(endPortUI);
            var startPortPosition = startPortUI.GetGlobalCenter();
            var endPortPosition = endPortUI.GetGlobalCenter();

            var edgeModel = StartPort.GetConnectedEdges().First();
            var edge = edgeModel.GetView<Edge>(GraphView);
            Assert.IsNotNull(edge);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortPosition.x - k_EdgeSelectionOffset, endPortPosition.y);
            Helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortPosition.x + (endPortPosition.x - startPortPosition.x) / 2, endPortPosition.y);
            Helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(StartPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            Helpers.MouseUpEvent(emptyAreaPos);
            yield return null;


            Assert.IsTrue(searcherInvoked, "Searcher was not invoked.");
            Assert.AreEqual(1, StartPort.GetConnectedEdges().Count(), "Edge was unexpectedly deleted.");
        }

        [UnityTest]
        public IEnumerator EdgeDisconnectOutputWorks()
        {
            bool searcherInvoked = false;
            (GraphModel.Stencil as TestStencil)?.SetOnGetSearcherDatabaseProviderCallback(() => searcherInvoked = true);

            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = StartPort.GetView<Port>(GraphView);
            var endPortUI = EndPort.GetView<Port>(GraphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;
            float endPortX = endPortUI.GetGlobalCenter().x;

            // Create the edge to be tested.
            var actions = ConnectPorts(StartPort, EndPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = StartPort.GetConnectedEdges().First().GetView<Edge>(GraphView);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            Helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            Helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            Helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.IsTrue(searcherInvoked, "Searcher was not invoked.");
            Assert.AreEqual(1, StartPort.GetConnectedEdges().Count(), "Edge was unexpectedly deleted.");
        }

        [UnityTest]
        public IEnumerator EdgeReconnectInputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var endPortUI = EndPort.GetView<Port>(GraphView);

            float endPortX = endPortUI.GetGlobalCenter().x;
            float endPortY = endPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(StartPort, EndPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = StartPort.GetConnectedEdges().First().GetView<Edge>(GraphView);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            Helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the second port while holding CTRL.
            var endPortTwoUI = EndPortTwo.GetView<Port>(GraphView);
            var portTwoAreaPos = endPortTwoUI.GetGlobalCenter();
            Helpers.MouseDragEvent(edgeRightSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(StartPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the port area
            Helpers.MouseUpEvent(portTwoAreaPos);
            yield return null;

            edge = StartPort.GetConnectedEdges().First().GetView<Edge>(GraphView);

            Assert.AreEqual(StartPort, edge.Output);
            Assert.AreEqual(EndPortTwo, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeReconnectOutputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = StartPort.GetView<Port>(GraphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(StartPort, EndPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = StartPort.GetConnectedEdges().First().GetView<Edge>(GraphView);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            Helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the second port while holding CTRL.
            var startPortTwoUI = StartPortTwo.GetView<Port>(GraphView);
            var portTwoAreaPos = startPortTwoUI.GetGlobalCenter();
            Helpers.MouseDragEvent(edgeLeftSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            Helpers.MouseUpEvent(portTwoAreaPos);
            yield return null;

            edge = StartPortTwo.GetConnectedEdges().First().GetView<Edge>(GraphView);

            Assert.AreEqual(StartPortTwo, edge.Output);
            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnInput()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = StartPort.GetView<Port>(GraphView);
            var endPortUI = EndPort.GetView<Port>(GraphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float endPortX = endPortUI.GetGlobalCenter().x;
            float endPortY = endPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(StartPort, EndPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = StartPort.GetConnectedEdges().First().GetView<Edge>(GraphView);

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            Helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, endPortY);
            Helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(StartPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            Helpers.KeyDownEvent(KeyCode.Escape);
            yield return null;

            Assert.AreEqual(StartPort, edge.Output);
            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            Helpers.KeyUpEvent(KeyCode.Escape);
            yield return null;

            // Mouse release on the empty area
            Helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.AreEqual(StartPort, edge.Output);
            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnOutput()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var startPortUI = StartPort.GetView<Port>(GraphView);
            var endPortUI = EndPort.GetView<Port>(GraphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;
            float endPortX = endPortUI.GetGlobalCenter().x;

            // Create the edge to be tested.
            var actions = ConnectPorts(StartPort, EndPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = StartPort.GetConnectedEdges().First().GetView<Edge>(GraphView);

            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            Helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            Helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            Helpers.KeyDownEvent(KeyCode.Escape);
            yield return null;

            Assert.AreEqual(StartPort, edge.Output);
            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            Helpers.KeyUpEvent(KeyCode.Escape);
            yield return null;

            // Mouse release on the empty area
            Helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.AreEqual(StartPort, edge.Output);
            Assert.AreEqual(EndPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeConnectionUnderThresholdDistanceNotEffective()
        {
            MarkGraphViewStateDirty();
            yield return null;

            Port startPortUI = StartPort.GetView<Port>(GraphView);
            var startPos = startPortUI.GetGlobalCenter();
            Helpers.DragTo(startPos, startPos + new Vector3(EdgeConnector.connectionDistanceThreshold / 2f, 0f, 0f));

            yield return null;

            Assert.AreEqual(0, SecondNode.GetInputPorts().First().GetConnectedEdges().Count());
        }

        [UnityTest]
        public IEnumerator EdgeConnectDragMultipleEdgesFromPortOutputToInputWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            // We start without any connection
            Assert.AreEqual(0, StartPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, EndPort.GetConnectedEdges().Count());
            Assert.AreEqual(0, EndPortTwo.GetConnectedEdges().Count());


            Port startPortUI = StartPort.GetView<Port>(GraphView);
            Port endPortUI = EndPort.GetView<Port>(GraphView);
            Port endPortTwoUI = EndPortTwo.GetView<Port>(GraphView);

            // Drag an edge between the two ports
            Helpers.DragTo(startPortUI.GetGlobalCenter(), endPortUI.GetGlobalCenter());
            Helpers.DragTo(startPortUI.GetGlobalCenter(), endPortTwoUI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(2, StartPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, EndPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, EndPortTwo.GetConnectedEdges().Count());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, StartPort.GetConnectedEdges().First(), StartPort.GetConnectedEdges().Skip(1).First()));

            Port startPortTwoUI = StartPortTwo.GetView<Port>(GraphView);
            Helpers.DragTo(startPortUI.GetGlobalCenter() + new Vector3(k_EdgeSelectionOffset, 0, 0), startPortTwoUI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            Assert.AreEqual(2, StartPortTwo.GetConnectedEdges().Count());
            Assert.AreEqual(1, EndPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, EndPortTwo.GetConnectedEdges().Count());
        }

        [UnityTest]
        public IEnumerator EdgeConnectDragMultipleEdgesFromExecutionPortOutputToInputWorks()
        {
            IONodeModel exeStartNode = CreateNode("First Out Exe node", new Vector2(100, 100), 0, 0, 0, 1);
            IONodeModel exeStartNode2 = CreateNode("Second Out Exe node", new Vector2(100, 400), 0, 0, 0, 1);
            IONodeModel exeEndNode = CreateNode("First In Exe node", new Vector2(400, 100), 0, 0, 1);
            IONodeModel exeEndNode2 = CreateNode("Second In Exe node", new Vector2(400, 400), 0, 0, 1);

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

            Port startPortUI = startPort.GetView<Port>(GraphView);
            Port startPort2UI = startPort2.GetView<Port>(GraphView);
            Port endPortUI = endPort.GetView<Port>(GraphView);
            Port endPort2UI = endPort2.GetView<Port>(GraphView);

            // Drag an edge between the two ports
            Helpers.DragTo(startPortUI.GetGlobalCenter(), endPortUI.GetGlobalCenter());
            Helpers.DragTo(startPortUI.GetGlobalCenter(), endPort2UI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(2, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, endPort2.GetConnectedEdges().Count());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, startPort.GetConnectedEdges().First(), startPort.GetConnectedEdges().Skip(1).First()));

            Helpers.DragTo(startPortUI.GetGlobalCenter() + new Vector3(k_EdgeSelectionOffset, 0, 0), startPort2UI.GetGlobalCenter());

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
            IONodeModel exeEndNode = CreateNode("First In Exe node", new Vector2(400, 100), 0, 0, 1);
            IONodeModel exeEndNode2 = CreateNode("Second In Exe node", new Vector2(400, 400), 0, 0, 1);

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

            Port startPortUI = startPort.GetView<Port>(GraphView);
            Port startPort2UI = startPort2.GetView<Port>(GraphView);
            Port endPortUI = endPort.GetView<Port>(GraphView);
            Port endPort2UI = endPort2.GetView<Port>(GraphView);

            // Drag an edge between the two ports
            Helpers.DragTo(startPortUI.GetGlobalCenter(), endPortUI.GetGlobalCenter());
            Helpers.DragTo(startPort2UI.GetGlobalCenter(), endPortUI.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(1, startPort.GetConnectedEdges().Count());
            Assert.AreEqual(1, startPort2.GetConnectedEdges().Count());
            Assert.AreEqual(2, endPort.GetConnectedEdges().Count());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, endPort.GetConnectedEdges().First(), endPort.GetConnectedEdges().Skip(1).First()));

            Helpers.DragTo(endPortUI.GetGlobalCenter() - new Vector3(k_EdgeSelectionOffset, 0, 0), endPort2UI.GetGlobalCenter());

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
