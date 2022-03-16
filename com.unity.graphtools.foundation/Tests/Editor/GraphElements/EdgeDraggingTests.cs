using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    class EdgeDraggingTests : GraphViewTester
    {
        IInputOutputPortsNodeModel m_Node1;
        IInputOutputPortsNodeModel m_Node2;

        [SetUp]
        public new void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("output", Vector2.zero, 0, 1);
            m_Node2 = CreateNode("input", new Vector2(400, 0), 1);
            MarkGraphViewStateDirty();
        }

        [UnityTest]
        public IEnumerator TestRightClickStopEdgeDrag()
        {
            // Uncomment the sleep to see the test in action
            var outPort = m_Node1.GetPorts(PortDirection.Output, PortType.Data).First();
            var inPort = m_Node2.GetPorts(PortDirection.Input, PortType.Data).First();

            yield return null;

            var actions = ConnectPorts(outPort, inPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edgeModel = outPort.GetConnectedEdges().First();

            var edgeUI = edgeModel.GetView<Edge>(GraphView);

            Vector3 start3 = inPort.GetView<Port>(GraphView).GetGlobalCenter();
            Vector2 start = new Vector2(start3.x - 30, start3.y);
            Helpers.MouseDownEvent(start);

            Vector2 end = start + new Vector2(0, 200);

            Vector2 increment = (end - start) / 5;

            Edge draggedEdge = null;

            for (int i = 0; i < 5; i++)
            {
                Helpers.MouseDragEvent(start + i * increment, start + (i + 1) * increment);
                yield return null;


                draggedEdge = GraphView.Query<Edge>().Where(t => t != edgeUI).First();
                Assert.IsNotNull(draggedEdge);
                //System.Threading.Thread.Sleep(1000);
            }

            Assert.IsNotNull(draggedEdge.panel);

            // using middle button because right display the contextual menu which freezes Unity.
            Helpers.MouseDownEvent(end, MouseButton.MiddleMouse);

            //System.Threading.Thread.Sleep(2000);

            yield return null;

            //System.Threading.Thread.Sleep(2000);
            // edge should be removed
            Assert.IsNull(draggedEdge.panel);

            Helpers.MouseUpEvent(end, MouseButton.MiddleMouse);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestShiftDoesntDragEdge()
        {
            // Uncomment the sleep to see the test in action
            var outPort = m_Node1.GetPorts(PortDirection.Output, PortType.Data).First();
            var inPort = m_Node2.GetPorts(PortDirection.Input, PortType.Data).First();

            yield return null;

            var actions = ConnectPorts(outPort, inPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edgeModel = outPort.GetConnectedEdges().First();

            var edgeUI = edgeModel.GetView<Edge>(GraphView);

            Vector3 start3 = inPort.GetView<Port>(GraphView).GetGlobalCenter();
            Vector2 start = new Vector2(start3.x, start3.y);


            Vector3 end3 = outPort.GetView<Port>(GraphView).GetGlobalCenter();
            Vector2 end = new Vector2(end3.x, end3.y);

            Vector2 middle = (start + end) * 0.5f;

            Vector2 startPosition = edgeUI.layout.position;
            Helpers.MouseDownEvent(middle, MouseButton.LeftMouse, EventModifiers.Shift);

            yield return null;

            Vector2 drag = middle + new Vector2(0, 300);

            Vector2 increment = (drag - middle) / 5;

            for (int i = 0; i < 5; i++)
            {
                Helpers.MouseDragEvent(middle + i * increment, middle + (i + 1) * increment, MouseButton.LeftMouse, EventModifiers.Shift);
                yield return null;
            }

            Assert.AreEqual(startPosition, edgeUI.layout.position);

            Helpers.MouseUpEvent(end);
            yield return null;
        }
    }
}
