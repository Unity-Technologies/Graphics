using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class PortModelUpdateTests
    {
        [Test]
        public void ChangingPortNameChangesPortLabel()
        {
            GraphView graphView = new GraphView(null, null, "");
            var nodeModel = new SingleOutputNodeModel();
            nodeModel.DefineNode();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetView<Port>(graphView);
            Assert.IsNotNull(port);

            var label = port.SafeQ<Label>();
            Assert.AreEqual("", label.text);

            Assert.IsNotNull(portModel as IHasTitle);
            const string newTitle = "New Title";
            (portModel as IHasTitle).Title = newTitle;
            node.UpdateFromModel();

            Assert.AreEqual(newTitle, label.text);
        }

        [Test]
        public void ChangingTooltipChangesUITooltip()
        {
            GraphView graphView = new GraphView(null, null, "");
            var nodeModel = new SingleOutputNodeModel();
            nodeModel.DefineNode();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var portModel = nodeModel.Ports.First();
            Assert.IsNotNull(portModel as PortModel);
            var port = portModel.GetView<Port>(graphView);
            Assert.IsNotNull(port);

            Assert.AreEqual("", port.tooltip);

            const string newTooltip = "New Tooltip";
            (portModel as PortModel).SetTooltip(newTooltip);
            node.UpdateFromModel();

            Assert.AreEqual(newTooltip, port.tooltip);
        }

        class FakePortModel : PortModel
        {
            public bool FakeIsConnected { get; set; }

            public override IReadOnlyList<IEdgeModel> GetConnectedEdges()
            {
                if (FakeIsConnected)
                {
                    return new[] { new BasicModel.EdgeModel() };
                }
                return new IEdgeModel[0];
            }
        }

        class FakeIONodeModel : NodeModel
        {
            public int InputCount { get; set; }
            public int OutputCount { get; set; }

            protected override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
                TypeHandle dataType, string portId, PortModelOptions options)
            {
                return new FakePortModel
                {
                    Direction = direction,
                    Orientation = orientation,
                    Title = portName,
                    PortType = portType,
                    DataTypeHandle = dataType,
                    NodeModel = this,
                    AssetModel = AssetModel
                };
            }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                for (var i = 0; i < InputCount; i++)
                    this.AddDataInputPort("In " + i, TypeHandle.Unknown);

                for (var i = 0; i < OutputCount; i++)
                    this.AddDataOutputPort("Out " + i, TypeHandle.Unknown);
            }
        }

        [Test]
        public void ConnectingPortUpdateClasses()
        {
            GraphView graphView = new GraphView(null, null, "");

            var nodeModel = new FakeIONodeModel { InputCount = 1, OutputCount = 1 };
            nodeModel.DefineNode();

            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var portModel = nodeModel.Ports.First() as FakePortModel;
            Assert.IsNotNull(portModel);
            var port = portModel.GetView<Port>(graphView);
            Assert.IsNotNull(port);

            Assert.IsTrue(port.ClassListContains(Port.notConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.connectedModifierUssClassName));

            portModel.FakeIsConnected = true;
            port.UpdateFromModel();

            Assert.IsFalse(port.ClassListContains(Port.notConnectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.connectedModifierUssClassName));
        }
    }
}
