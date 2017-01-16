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
            public ContextDescTestInit() : base(VFXContextType.kInit, string.Empty) {}
        }

        private class ContextDescTestUpdate : VFXContextDesc {
            public ContextDescTestUpdate() : base(VFXContextType.kUpdate, string.Empty) {}
        }

        private class ContextDescTestOutput : VFXContextDesc {
            public ContextDescTestOutput() : base(VFXContextType.kOutput, string.Empty) {}
        }

        // TODO Add attributes to fill the Library instead of reflection before uncommenting this !
        /*private class ContextDescTestNone : VFXContextDesc {
            public ContextDescTestNone() : base(VFXContextType.None, string.Empty) { }
        }

        private class ContextDescTestInitAndUpdate : VFXContextDesc {
            public ContextDescTestInitAndUpdate() : base(VFXContextType.kInitAndUpdate, string.Empty) {}
        }

        private class ContextDescTestAll : VFXContextDesc {
            public ContextDescTestAll() : base(VFXContextType.kAll, string.Empty) {}
        }*/

        [Test]
        public void ConstructWithAllTypes()
        {
            Assert.DoesNotThrow(() =>
            {
                new ContextDescTestInit();
                new ContextDescTestUpdate();
                new ContextDescTestOutput();
            });

            /*Assert.Throws<ArgumentException>(() => {
                new ContextDescTestNone();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTestInitAndUpdate();
            });

            Assert.Throws<ArgumentException>(() => {
                new ContextDescTestAll();
            });*/
        }
    }
}
