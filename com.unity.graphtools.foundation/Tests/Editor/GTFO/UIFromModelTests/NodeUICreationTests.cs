using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class NodeUICreationTests
    {
        static IEnumerable<Func<BasicModel.NodeModel>> NodeModelCreators()
        {
            yield return () => new NodeModel();
            yield return () => new SingleInputNodeModel();
            yield return () => new SingleOutputNodeModel();
            yield return () =>
            {
                var model = new IONodeModel { InputCount = 3, OutputCount = 5 };
                return model;
            };
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void NodeHasExpectedParts(Func<BasicModel.NodeModel> nodeCreator)
        {
            GraphView graphView = new GraphView(null, null, "");
            var nodeModel = nodeCreator.Invoke();
            nodeModel.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            Assert.IsNotNull(node.SafeQ<VisualElement>(Node.titleContainerPartName), "Title part was expected but not found");
            Assert.IsNotNull(node.SafeQ<VisualElement>(Node.portContainerPartName), "Port Container part was expected but not found");
            if (nodeModel.GetType() == typeof(NodeModel))
            {
                Assert.IsNull(node.SafeQ<Port>(), "No Port were expected but at least one was found");
            }
            else
            {
                Assert.IsNotNull(node.SafeQ<Port>(), "At least one Port was expected but none was found");
            }
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void CollapsibleNodeHasExpectedParts(Func<BasicModel.NodeModel> nodeCreator)
        {
            GraphView graphView = new GraphView(null, null, "");
            var nodeModel = nodeCreator.Invoke();
            nodeModel.DefineNode();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            Assert.IsNotNull(node.SafeQ<VisualElement>(CollapsibleInOutNode.titleIconContainerPartName), "Title part was expected but not found");
            Assert.IsNotNull(node.SafeQ<VisualElement>(Node.portContainerPartName), "Port Container part was expected but not found");
            Assert.IsNotNull(node.SafeQ<VisualElement>(CollapsibleInOutNode.collapseButtonPartName), "Collapsible Button part was expected but not found");

            if (nodeModel.GetType() == typeof(NodeModel))
            {
                Assert.IsNull(node.SafeQ<Port>(), "No Port were expected but at least one was found");
            }
            else if (nodeModel.GetType() == typeof(IONodeModel))
            {
                var inputs = node.SafeQ<VisualElement>(InOutPortContainerPart.inputPortsUssName);
                var outputs = node.SafeQ<VisualElement>(InOutPortContainerPart.outputPortsUssName);
                Assert.IsNotNull(inputs, "Input Port Container part was expected but not found");
                Assert.IsNotNull(outputs, "Output Port Container part was expected but not found");
                Assert.IsNotNull(inputs.SafeQ<Port>(), "At least one Input Port was expected but none were found");
                Assert.IsNotNull(outputs.SafeQ<Port>(), "At least one Output Port was expected but none were found");
            }
            else
            {
                Assert.IsNotNull(node.SafeQ<Port>(), "At least one Port was expected but none were found");
            }
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void TokenNodeHasExpectedParts(Func<BasicModel.NodeModel> nodeCreator)
        {
            GraphView graphView = new GraphView(null, null, "");
            var nodeModel = nodeCreator.Invoke();
            nodeModel.DefineNode();
            var node = new TokenNode();
            node.SetupBuildAndUpdate(nodeModel, graphView);

            var inputs = node.SafeQ<VisualElement>(TokenNode.inputPortContainerPartName);
            var outputs = node.SafeQ<VisualElement>(TokenNode.outputPortContainerPartName);

            Assert.IsNotNull(node.SafeQ<VisualElement>(TokenNode.titleIconContainerPartName), "Title part was expected but not found");

            if (nodeModel.GetType() == typeof(SingleInputNodeModel))
            {
                Assert.IsNotNull(inputs, "Input Port Container part was expected but not found");
                Assert.IsNull(outputs, "Output Port Container part was not expected but was found");
                Assert.IsNotNull(inputs.SafeQ<Port>(), "At least one Port was expected but none were found");
            }
            else if (nodeModel.GetType() == typeof(SingleOutputNodeModel))
            {
                Assert.IsNull(inputs, "Input Port Container part was not expected but was found");
                Assert.IsNotNull(outputs, "Output Port Container part was expected but not found");
                Assert.IsNotNull(outputs.SafeQ<Port>(), "At least one Port was expected but none were found");
            }
            else
            {
                Assert.IsNull(node.SafeQ<Port>(), "No Port were expected but at least one was found");
            }
        }

        [Test]
        public void ContextHasExpectedParts()
        {
            GraphView graphView = new GraphView(null, null, "");
            var nodeModel = new ContextNodeModel();
            nodeModel.DefineNode();

            var context = new ContextNode();
            context.SetupBuildAndUpdate(nodeModel, graphView);

            Assert.IsNotNull(context.SafeQ<VisualElement>(CollapsibleInOutNode.topPortContainerPartName), "Top vertical port Container part was expected but not found");
            Assert.IsNotNull(context.SafeQ<VisualElement>(CollapsibleInOutNode.titleIconContainerPartName), "Title part was expected but not found");
            Assert.IsNotNull(context.SafeQ<VisualElement>(Node.portContainerPartName), "Horizontal Port Container part was expected but not found");
            Assert.IsNotNull(context.SafeQ<VisualElement>(ContextNode.blocksPartName), "Blocks part was expected but not found");
            Assert.IsNotNull(context.SafeQ<VisualElement>(CollapsibleInOutNode.bottomPortContainerPartName), "Bottom vertical Port Container part was expected but not found");
        }
    }
}
