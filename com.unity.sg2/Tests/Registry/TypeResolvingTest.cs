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
            m_graph = new GraphHandler(m_registry);
        }

        [Test]
        public void ResolveFromConcretizationTest()
        {
            SetupGraph();

            // Create a test node that can be used to provoke type resolution.
            FunctionDescriptor vec3 = new(
                "Vec3Test",
                "Out = float(1,1,1);",
                new ParameterDescriptor[] {
                    new("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                );

            // Create a test node that will have its input/output port resolved via connection.
            FunctionDescriptor fd = new(
                "Test",
                "Out = In;",
                new ParameterDescriptor[] {
                    new("In", TYPE.Vector, GraphType.Usage.In),
                    new("Out", TYPE.Vector, GraphType.Usage.Out)
                }
            );

            RegistryKey registryKey = m_registry.Register(fd);
            RegistryKey vec3Reg = m_registry.Register(vec3);

            NodeHandler node = m_graph.AddNode(registryKey, "testNode");
            NodeHandler vec3Node = m_graph.AddNode(vec3Reg, "vec3Node");

            // check that the test node's port resolve to the default length of 1.
            var inlen = GraphTypeHelpers.GetLength(node.GetPort("In").GetTypeField());
            Assert.AreEqual(GraphType.Length.One, inlen);
            var outlen = GraphTypeHelpers.GetLength(node.GetPort("Out").GetTypeField());
            Assert.AreEqual(GraphType.Length.One, outlen);

            // Connect the test vec3Node to the test node, which should trigger type resolution
            m_graph.TryConnect("vec3Node", "Out", "testNode", "In");
            m_graph.ReconcretizeAll();

            // the testNode should now be length 3 for in and out.
            inlen = GraphTypeHelpers.GetLength(node.GetPort("In").GetTypeField());
            Assert.AreEqual(GraphType.Length.Three, inlen);
            outlen = GraphTypeHelpers.GetLength(node.GetPort("Out").GetTypeField());
            Assert.AreEqual(GraphType.Length.Three, outlen);
        }
    }
}
