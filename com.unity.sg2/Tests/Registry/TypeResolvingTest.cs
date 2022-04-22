using NUnit.Framework;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta.UnitTests
{
    [TestFixture]
    class TypeResolving
    {
        private Registry m_registry;
        private GraphHandler m_graph;

        private void SetupGraph()
        {
            // create the registry
            m_registry = new Registry();
            m_registry.Register<GraphType>();

            // create the graph
            m_graph = new GraphHandler();
        }

        [Test]
        public void ResolveFromConcretizationTest()
        {
            SetupGraph();

            // make a FunctionDescriptor and register it
            FunctionDescriptor fd = new(
                1,
                "Test",
                "Out = In;",
                new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
            );
            RegistryKey registryKey = m_registry.Register(fd);

            // add a single node to the graph
            string nodeName = $"{fd.Name}-01";
            NodeHandler node = m_graph.AddNode(registryKey, nodeName, m_registry);

            // check that the the node was added
            var inlen = GraphTypeHelpers.GetLength(node.GetPort("In").GetTypeField());
            Assert.AreEqual(GraphType.Length.Four, inlen);
            var outlen = GraphTypeHelpers.GetLength(node.GetPort("Out").GetTypeField());
            Assert.AreEqual(GraphType.Length.Four, outlen);

            // make In a Vec3
            node.GetPort("In").GetTypeField().GetSubField<GraphType.Length>(GraphType.kLength).SetData(GraphType.Length.Three);

            inlen = GraphTypeHelpers.GetLength(node.GetPort("In").GetTypeField());
            Assert.AreEqual(GraphType.Length.Three, inlen);
            outlen = GraphTypeHelpers.GetLength(node.GetPort("Out").GetTypeField());
            Assert.AreEqual(GraphType.Length.Four, outlen);

            // reconcretize the node
            bool didReconcretize = m_graph.ReconcretizeNode(nodeName, m_registry);
            Assert.IsTrue(didReconcretize);

            // EXPECT that In is still a Vec3
            inlen = GraphTypeHelpers.GetLength(node.GetPort("In").GetTypeField());
            Assert.AreEqual(GraphType.Length.Three, inlen);

            // EXPECT that Out has resolved into a Vec3
            outlen = GraphTypeHelpers.GetLength(node.GetPort("Out").GetTypeField());
            Assert.AreEqual(GraphType.Length.Three, outlen);
        }
    }
}
