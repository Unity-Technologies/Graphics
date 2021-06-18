
using NUnit.Framework;

namespace UnityEditor.ShaderGraph.Registry.UnitTests
{
    [TestFixture]
    class RegistryPlaceholderFixture
    {
        [Test]
        public void RegistryPlaceholderTest()
        {
            var o = new RegistryPlaceholder(4);
            Assert.AreEqual(4, o.data);
        }
    }
}
