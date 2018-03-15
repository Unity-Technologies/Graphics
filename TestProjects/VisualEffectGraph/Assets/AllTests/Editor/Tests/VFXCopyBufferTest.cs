using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VFX;
using UnityEngine.Experimental.VFX;

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
