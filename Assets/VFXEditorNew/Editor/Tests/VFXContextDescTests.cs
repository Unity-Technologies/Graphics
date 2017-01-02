using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXContextDescTests
    {
        private class ContextDescTest : VFXContextDesc
        {
            public ContextDescTest(VFXContextDesc.Type type)
                : base(type, string.Empty)
            {}
        }

        [Test]
        public void ConstructWithAllTypes()
        {
            Assert.DoesNotThrow(() =>
            {
                var desc1 = new ContextDescTest(VFXContextDesc.Type.kTypeInit);
                var desc2 = new ContextDescTest(VFXContextDesc.Type.kTypeUpdate);
                var desc3 = new ContextDescTest(VFXContextDesc.Type.kTypeOutput);
            });

            Assert.Throws<ArgumentException>(() => {
                var desc = new ContextDescTest(VFXContextDesc.Type.kTypeNone);
            });

            Assert.Throws<ArgumentException>(() => {
                var desc = new ContextDescTest(VFXContextDesc.Type.kInitAndUpdate);
            });

            Assert.Throws<ArgumentException>(() => {
                var desc = new ContextDescTest(VFXContextDesc.Type.kAll);
            });
        }
    }
}
