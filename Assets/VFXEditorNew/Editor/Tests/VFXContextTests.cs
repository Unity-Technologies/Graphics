using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXContextTests
    {
        private class ContextTestInit : VFXContext
        {
            public ContextTestInit() : base(VFXContextType.kInit) {}
        }

        private class ContextTestUpdate : VFXContext
        {
            public ContextTestUpdate() : base(VFXContextType.kUpdate) {}
        }

        private class ContextTestOutput : VFXContext
        {
            public ContextTestOutput() : base(VFXContextType.kOutput) {}
        }

        private class ContextTestNone : VFXContext
        {
            public ContextTestNone() : base(VFXContextType.kNone) {}
        }

        private class ContextTestInitAndUpdate : VFXContext
        {
            public ContextTestInitAndUpdate() : base(VFXContextType.kInitAndUpdate) {}
        }

        private class ContextTestAll : VFXContext
        {
            public ContextTestAll() : base(VFXContextType.kAll) {}
        }

        private void CheckContext(VFXContext context,VFXContextType expectedType)
        {
            Assert.AreEqual(expectedType,context.contextType);
        }

        [Test]
        public void ConstructWithAllTypes()
        {
            CheckContext(ScriptableObject.CreateInstance<ContextTestInit>(), VFXContextType.kInit);
            CheckContext(ScriptableObject.CreateInstance<ContextTestUpdate>(), VFXContextType.kUpdate);
            CheckContext(ScriptableObject.CreateInstance<ContextTestOutput>(), VFXContextType.kOutput);

            CheckContext(ScriptableObject.CreateInstance<ContextTestNone>(), VFXContextType.kNone);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kNone");

            CheckContext(ScriptableObject.CreateInstance<ContextTestInitAndUpdate>(), VFXContextType.kNone);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kInitAndUpdate");

            CheckContext(ScriptableObject.CreateInstance<ContextTestAll>(), VFXContextType.kNone);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kAll");
        }
    }
}
