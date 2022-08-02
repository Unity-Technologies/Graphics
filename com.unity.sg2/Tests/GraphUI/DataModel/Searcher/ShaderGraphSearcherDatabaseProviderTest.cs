using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class ShaderGraphSearcherDatabaseProviderTest
    {
        [SetUp]
        public void Setup()
        {
            // TODO (Brett) I really want to be able to mock up a GraphModel, Stencil, and Registry
            // TODO (Brett) here so that I can unit test ShaderGraphSearcherDatabaseProvider.

            //ShaderGraphModel shaderGraphModel = new ShaderGraphModel();
            //shaderGraphModel.Stencil = new ShaderGraphStencil();
            //IGraphModel graphModel = new ShaderGraphModel();
        }

        [Test]
        public void ExampleTest()
        {
            //var fooNode = graphHandler.AddNode(Registry.ResolveKey<TestNode>(), "foo");
            //fooNode.AddPort("A", true, true);
            //fooNode.AddPort("B", true, true);
            //fooNode.AddPort("Out", false, true);

            //var barNode = graphHandler.AddNode(Registry.ResolveKey<TestNode>(), "bar");
            //barNode.AddPort("A", true, true);
            //barNode.AddPort("B", true, true);
            //barNode.AddPort("Out", false, true);

            //var edge = graphHandler.AddEdge("foo.Out", "bar.A");
            //Assert.NotNull(edge);
        }

    }
}
