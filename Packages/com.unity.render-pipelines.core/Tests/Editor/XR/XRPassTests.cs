using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Experimental.Tests.XR
{
    [TestFixture]
    class XRPassTests
    {
        [Test]
        public void EmptyPass_IsFirstAndLastPass()
        {
            Assert.IsTrue(XRSystem.emptyPass.isFirstCameraPass);
            Assert.IsTrue(XRSystem.emptyPass.isLastCameraPass);
        }
    }
}
