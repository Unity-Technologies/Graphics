using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    class NoConnectorPortTests : GraphViewTester
    {
        class NoInputPortNodeModel : Type0FakeNodeModel
        {
            /// <inheritdoc />
            protected override void OnDefineNode()
            {
                base.OnDefineNode();
                this.AddNoConnectorInputPort("NoConnectorPort", TypeHandle.Int);
            }
        }

        Type0FakeNodeModel m_Node;
        Type0FakeNodeModel m_OtherNode;

        [SetUp]
        public new void SetUp()
        {
            base.SetUp();

            m_OtherNode = GraphModel.CreateNode<Type0FakeNodeModel>("OtherNode", new Vector2(0, 0));
            m_Node = GraphModel.CreateNode<NoInputPortNodeModel>("Node", new Vector2(250, 0));

            MarkGraphViewStateDirty();
        }

        [UnityTest]
        public IEnumerator ShouldNotCreateEdgeToNoConnectorPort()
        {
            var otherPort = m_OtherNode.GetPorts(PortDirection.Output, PortType.Data).First();
            var dataInputs = m_Node.GetPorts(PortDirection.Input, PortType.Data).ToList();
            var portWithConnector = dataInputs.First();
            var noConnectorPort = dataInputs.Last();
            Assert.IsTrue(otherPort.Capacity != PortCapacity.None);
            Assert.IsTrue(portWithConnector.Capacity != PortCapacity.None);
            Assert.IsTrue(noConnectorPort.Capacity == PortCapacity.None);

            yield return null;

            var fromPortUI = otherPort.GetView<Port>(GraphView);
            var toConnectorPortUI = portWithConnector.GetView<Port>(GraphView);
            var toNoConnectorPortUI = noConnectorPort.GetView<Port>(GraphView);

            Assert.IsNotNull(fromPortUI);
            Assert.IsNotNull(toConnectorPortUI);
            Assert.IsNotNull(toNoConnectorPortUI);
            Assert.AreEqual(0, GraphModel.EdgeModels.Count);

            // Drag an edge between the two ports with connectors
            Helpers.DragTo(fromPortUI.GetGlobalCenter(), toConnectorPortUI.GetGlobalCenter());
            yield return null;

            Assert.AreEqual(1, GraphModel.EdgeModels.Count, "Edge has not been created");

            // Drag an edge to the no connector port
            Helpers.DragTo(fromPortUI.GetGlobalCenter(), toNoConnectorPortUI.GetGlobalCenter());
            yield return null;

            Assert.AreEqual(1, GraphModel.EdgeModels.Count, "Edge has been created");
        }
    }
}
