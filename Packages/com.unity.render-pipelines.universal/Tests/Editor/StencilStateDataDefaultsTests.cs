using NUnit.Framework;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class StencilStateDataDefaultsTests
    {
        [Test]
        public void StencilStateData_MaskDefaults_AreUserMask()
        {
            var data = new StencilStateData();
            Assert.AreEqual((int)StencilUsage.UserMask, data.stencilReadMask);
            Assert.AreEqual((int)StencilUsage.UserMask, data.stencilWriteMask);
        }
    }
}
