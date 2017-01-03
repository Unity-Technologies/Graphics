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
                new ContextDescTest(VFXContextDesc.Type.kTypeInit);
                new ContextDescTest(VFXContextDesc.Type.kTypeUpdate);
                new ContextDescTest(VFXContextDesc.Type.kTypeOutput);
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTest(VFXContextDesc.Type.kTypeNone);
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTest(VFXContextDesc.Type.kInitAndUpdate);
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTest(VFXContextDesc.Type.kAll);
            });
        }
    }
}
