using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class StackTests
    {
        static BlockFieldDescriptor s_DescriptorA = new BlockFieldDescriptor("Test", "BlockA", string.Empty, new FloatControl(0.0f), ShaderStage.Fragment, true);
        static BlockFieldDescriptor s_DescriptorB = new BlockFieldDescriptor("Test", "BlockA", string.Empty, new FloatControl(0.0f), ShaderStage.Fragment, true);

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void CanAddBlockNodeToContext()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            var node = new BlockNode();
            node.Init(s_DescriptorA);
            graph.AddBlock(node, graph.fragmentContext, 0);

            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<BlockNode>().Count());
            Assert.AreEqual(1, graph.fragmentContext.blocks.Count());
        }

        [Test]
        public void CanRemoveBlockNodeFromContext()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            var node = new BlockNode();
            node.Init(s_DescriptorA);
            graph.AddBlock(node, graph.fragmentContext, 0);
            graph.RemoveNode(node);

            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.GetNodes<BlockNode>().Count());
            Assert.AreEqual(0, graph.fragmentContext.blocks.Count());
        }

        [Test]
        public void CanInsertBlockNodeToContext()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            var nodeA = new BlockNode();
            nodeA.Init(s_DescriptorA);
            graph.AddBlock(nodeA, graph.fragmentContext, 0);

            var nodeB = new BlockNode();
            nodeB.Init(s_DescriptorA);
            graph.AddBlock(nodeB, graph.fragmentContext, 0);

            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(2, graph.GetNodes<BlockNode>().Count());
            Assert.AreEqual(2, graph.fragmentContext.blocks.Count());
            Assert.AreEqual(nodeB, graph.fragmentContext.blocks[0].value);
        }

        [Test]
        public void CanFilterBlockNodeByShaderStage()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            var node = new BlockNode();
            node.Init(s_DescriptorA);

            var contextView = new ContextView("Test", graph.vertexContext, null);
            var methodInfo = typeof(StackNode).GetMethod("AcceptsElement", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(GraphElement), typeof(int).MakeByRefType(), typeof(int) }, null);
            Assert.IsNotNull(methodInfo);

            var acceptsElement = (bool)methodInfo.Invoke(contextView, new object[] { new MaterialNodeView() { userData = node }, 0, 0 });
            Assert.IsFalse(acceptsElement);
        }
    }
}
