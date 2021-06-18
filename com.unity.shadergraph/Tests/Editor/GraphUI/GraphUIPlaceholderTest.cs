
using NUnit.Framework;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class GraphUIPlaceholderFixture
    {
        [Test]
        public void GraphUIPlaceholderTest()
        {
            var o = new GraphUIPlaceholder(4);
            Assert.AreEqual(4, o.data);
        }
    }
}
