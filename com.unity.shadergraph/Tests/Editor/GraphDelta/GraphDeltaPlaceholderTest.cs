
using NUnit.Framework;

namespace UnityEditor.ShaderGraph.GraphDelta.UnitTests
{
    [TestFixture]
    class GraphDeltaPlaceholderFixture
    {
        [Test]
        public void GraphDeltaPlaceholderTest()
        {
            var o = new GraphDeltaPlaceholder(4);
            Assert.AreEqual(4, o.data);
        }
    }
}
