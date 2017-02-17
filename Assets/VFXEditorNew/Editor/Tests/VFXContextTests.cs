using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXContextTests
    {
        private class ContextTestInit : VFXContext {
            public ContextTestInit() : base(VFXContextType.kInit) {}
        }

        private class ContextTestUpdate : VFXContext {
            public ContextTestUpdate() : base(VFXContextType.kUpdate) {}
        }

        private class ContextTestOutput : VFXContext {
            public ContextTestOutput() : base(VFXContextType.kOutput) {}
        }

        // TODO Add attributes to fill the Library instead of reflection before uncommenting this !
        private class ContextTestNone : VFXContext {
            public ContextTestNone() : base(VFXContextType.kNone) { }
        }

        private class ContextTestInitAndUpdate : VFXContext {
            public ContextTestInitAndUpdate() : base(VFXContextType.kInitAndUpdate) {}
        }

        private class ContextTestAll : VFXContext {
            public ContextTestAll() : base(VFXContextType.kAll) {}
        }

        [Test]
        public void ConstructWithAllTypes()
        {
            Assert.DoesNotThrow(() =>
            {
                new ContextTestInit();
                new ContextTestUpdate();
                new ContextTestOutput();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextTestNone();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextTestInitAndUpdate();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextTestAll();
            });
        }
    }
}
