using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXCopyBufferTest
    {
        [Test]
        public void ProcessBasicTest()
        {
            Assert.IsTrue(VFXComponent.DebugCopyBufferComputeTest());
        }
    }
}
