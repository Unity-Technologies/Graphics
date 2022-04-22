using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PortCommandTests : GtfTestFixture
    {
        [UnityTest]
        public IEnumerator DraggingFromPortCreateGhostEdge()
        {
            var nodeModel = GraphModel.CreateNode<SingleOutputNodeModel>();
            MarkGraphModelStateDirty();
            yield return null;

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetView<Port>(GraphView);
            Assert.IsNotNull(port);
            Assert.IsNull(port.EdgeConnector.EdgeDragHelper.edgeCandidateModel);

            var portConnector = port.SafeQ(PortConnectorPart.connectorUssName);
            var clickPosition = portConnector.parent.LocalToWorld(portConnector.layout.center);
            Vector2 move = new Vector2(0, 100);
            Helpers.DragToNoRelease(clickPosition, clickPosition + move);
            yield return null;

            // edgeCandidateModel != null is the sign that we have a ghost edge
            Assert.IsNotNull(port.EdgeConnector.EdgeDragHelper.edgeCandidateModel);

            Helpers.MouseUpEvent(clickPosition + move);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingFromPortContainerCreateGhostEdge()
        {
            var nodeModel = GraphModel.CreateNode<SingleOutputNodeModel>();
            MarkGraphModelStateDirty();
            yield return null;

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetView<Port>(GraphView);
            Assert.IsNotNull(port);
            Assert.IsNull(port.EdgeConnector.EdgeDragHelper.edgeCandidateModel);

            var connectorContainer = port.SafeQ(Port.connectorPartName);
            var clickPosition = connectorContainer.parent.LocalToWorld(connectorContainer.layout.center);
            Vector2 move = new Vector2(0, 100);
            Helpers.DragToNoRelease(clickPosition, clickPosition + move);
            yield return null;

            // edgeCandidateModel != null is the sign that we have a ghost edge
            Assert.IsNotNull(port.EdgeConnector.EdgeDragHelper.edgeCandidateModel);

            Helpers.MouseUpEvent(clickPosition + move);
            yield return null;
        }
    }
}
