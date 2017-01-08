using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXContextDescTests
    {
        private class ContextDescTestInit : VFXContextDesc {
            public ContextDescTestInit() : base(VFXContextDesc.Type.kTypeInit, string.Empty) {}
        }

        private class ContextDescTestUpdate : VFXContextDesc {
            public ContextDescTestUpdate() : base(VFXContextDesc.Type.kTypeUpdate, string.Empty) {}
        }

        private class ContextDescTestOutput : VFXContextDesc {
            public ContextDescTestOutput() : base(VFXContextDesc.Type.kTypeOutput, string.Empty) {}
        }

        private class ContextDescTestNone : VFXContextDesc {
            public ContextDescTestNone() : base(VFXContextDesc.Type.kTypeNone, string.Empty) { }
        }

        private class ContextDescTestInitAndUpdate : VFXContextDesc {
            public ContextDescTestInitAndUpdate() : base(VFXContextDesc.Type.kInitAndUpdate, string.Empty) {}
        }

        private class ContextDescTestAll : VFXContextDesc {
            public ContextDescTestAll() : base(VFXContextDesc.Type.kAll, string.Empty) {}
        }

        [Test]
        public void ConstructWithAllTypes()
        {
            Assert.DoesNotThrow(() =>
            {
                new ContextDescTestInit();
                new ContextDescTestUpdate();
                new ContextDescTestOutput();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTestNone();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTestInitAndUpdate();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTestAll();
            });
        }
    }
}
