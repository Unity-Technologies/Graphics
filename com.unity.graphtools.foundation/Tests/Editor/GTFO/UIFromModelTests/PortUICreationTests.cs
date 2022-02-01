using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PortUICreationTests
    {
        [Test]
        public void InputPortHasExpectedParts()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new SingleInputNodeModel();
            model.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(model, graphView);

            var portModel = model.GetInputPorts().First();
            Assert.IsNotNull(portModel);

            var port = portModel.GetUI<Port>(graphView);
            Assert.IsNotNull(port);
            Assert.IsTrue(port.ClassListContains(Port.inputModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.notConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.connectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.portDataTypeClassNamePrefix + portModel.PortDataType.Name.ToKebabCase()));
            Assert.IsNotNull(port.SafeQ<VisualElement>(Port.connectorPartName));
        }

        [Test]
        public void OutputPortHasExpectedParts()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new SingleOutputNodeModel();
            model.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(model, graphView);

            var portModel = model.GetOutputPorts().First();
            Assert.IsNotNull(portModel);

            var port = portModel.GetUI<Port>(graphView);
            Assert.IsNotNull(port);
            Assert.IsTrue(port.ClassListContains(Port.outputModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.notConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.connectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.portDataTypeClassNamePrefix + portModel.PortDataType.Name.ToKebabCase()));
            Assert.IsNotNull(port.SafeQ<VisualElement>(Port.connectorPartName));
        }
    }

    class PortUICreationTestsNeedingRepaint : GtfTestFixture
    {
        [UnityTest]
        public IEnumerator PortConnectorAndCapHavePortColor()
        {
            var nodeModel = GraphModel.CreateNode<SingleOutputNodeModel>();
            nodeModel.DefineNode();
            MarkGraphViewStateDirty();
            yield return null;

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetUI<Port>(GraphView);
            Assert.IsNotNull(port);
            var connector = port.SafeQ(PortConnectorPart.connectorUssName);
            var connectorCap = port.SafeQ(PortConnectorPart.connectorCapUssName);

            CustomStyleProperty<Color> portColorProperty = new CustomStyleProperty<Color>("--port-color");
            Color portColor;
            Assert.IsTrue(port.customStyle.TryGetValue(portColorProperty, out portColor));

            Assert.AreEqual(portColor, connector.resolvedStyle.borderBottomColor);
            Assert.AreEqual(portColor, connectorCap.resolvedStyle.backgroundColor);
        }
    }
}
