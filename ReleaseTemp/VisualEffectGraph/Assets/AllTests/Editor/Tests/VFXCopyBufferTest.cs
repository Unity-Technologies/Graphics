#if !UNITY_EDITOR_OSX
using NUnit.Framework;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXCopyBufferTest
    {
        [Test]
        public void ProcessBasicTest()
        {
            Assert.IsTrue(UnityEditor.Experimental.VFX.VisualEffectTest.DebugCopyBufferComputeTest());
        }
    }
}
#endif
