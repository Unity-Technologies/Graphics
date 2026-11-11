using NUnit.Framework;

namespace UnityEditor.VFX.Test.Integration
{
    [TestFixture]
    class VFXCopyBufferTests
    {
        [Test]
        public void ProcessBasicTest()
        {
            Assert.IsTrue(UnityEditor.VFX.VisualEffectTest.DebugCopyBufferComputeTest());
        }
    }
}
