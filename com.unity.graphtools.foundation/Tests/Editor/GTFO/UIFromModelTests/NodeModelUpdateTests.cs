using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class NodeModelUpdateTests
    {
        [Test]
        public void CollapsingNodeModelCollapsesNode()
        {
            GraphView graphView = new GraphView(null, null, "");
            var nodeModel = new IONodeModel();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var collapseButton = node.SafeQ<CollapseButton>(CollapsibleInOutNode.collapseButtonPartName);
            Assert.IsFalse(collapseButton.value);

            nodeModel.Collapsed = true;
            node.UpdateFromModel();
            Assert.IsTrue(collapseButton.value);
        }

        [Test]
        public void RenamingNonRenamableNodeModelUpdatesTitleLabel()
        {
            GraphView graphView = new GraphView(null, null, "");
            const string initialTitle = "Initial title";
            const string newTitle = "New title";

            var nodeModel = new IONodeModel { Title = initialTitle };
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var titleLabel = node.SafeQ<Label>(EditableTitlePart.titleLabelName);
            Assert.AreEqual(initialTitle, titleLabel.text);

            nodeModel.Title = newTitle;
            node.UpdateFromModel();
            Assert.AreEqual(newTitle, titleLabel.text);
        }

        [Test]
        public void RenamingRenamableNodeModelUpdatesTitleLabel()
        {
            GraphView graphView = new GraphView(null, null, "");
            const string initialTitle = "Initial title";
            const string newTitle = "New title";

            var nodeModel = new NodeModel { Title = initialTitle };
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var titleLabel = node.SafeQ(EditableTitlePart.titleLabelName).SafeQ<Label>(EditableLabel.labelName);
            Assert.AreEqual(initialTitle, titleLabel.text);

            nodeModel.Title = newTitle;
            node.UpdateFromModel();
            Assert.AreEqual(newTitle, titleLabel.text);
        }

        [Test]
        public void ChangingPortsOnNodeModelUpdatesNodePort()
        {
            GraphView graphView = new GraphView(null, null, "");
            const int originalInputPortCount = 3;
            const int originalOutputPortCount = 2;
            var nodeModel = new IONodeModel { InputCount = originalInputPortCount, OutputCount = originalOutputPortCount };
            nodeModel.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var ports = node.Query(className: "ge-port").ToList();
            Assert.AreEqual(originalInputPortCount + originalOutputPortCount, ports.Count);

            const int newInputPortCount = 1;
            const int newOutputPortCount = 3;
            nodeModel.InputCount = newInputPortCount;
            nodeModel.OutputCount = newOutputPortCount;
            nodeModel.DefineNode();
            node.UpdateFromModel();

            ports = node.Query(className: "ge-port").ToList();
            Assert.AreEqual(newInputPortCount + newOutputPortCount, ports.Count);
        }

        [Test]
        public void ChangingPortsOnNodeModelUpdatesCollapsibleInOutNodePort()
        {
            GraphView graphView = new GraphView(null, null, "");
            const int originalInputPortCount = 3;
            const int originalOutputPortCount = 2;
            var nodeModel = new IONodeModel { InputCount = originalInputPortCount, OutputCount = originalOutputPortCount };
            nodeModel.DefineNode();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var ports = node.SafeQ("inputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(originalInputPortCount, ports.Count);

            ports = node.SafeQ("outputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(originalOutputPortCount, ports.Count);

            const int newInputPortCount = 1;
            const int newOutputPortCount = 3;
            nodeModel.InputCount = newInputPortCount;
            nodeModel.OutputCount = newOutputPortCount;
            nodeModel.DefineNode();
            node.UpdateFromModel();

            ports = node.SafeQ("inputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(newInputPortCount, ports.Count);

            ports = node.SafeQ("outputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(newOutputPortCount, ports.Count);
        }
    }
}
