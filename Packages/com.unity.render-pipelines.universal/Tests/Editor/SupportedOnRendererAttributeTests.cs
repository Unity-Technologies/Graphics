using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    sealed class SupportedOnRendererAttributeTests
    {
        class CustomRenderer { }

        static readonly Regex k_InvalidTypeMessage = new($@"{nameof(SupportedOnRendererAttribute)} Attribute targets an invalid {nameof(ScriptableRendererData)}");
        static readonly Regex k_NullParametersMessage = new($@"{nameof(SupportedOnRendererAttribute)} parameters cannot be null");

        [Test]
        public void NonScriptableRendererDataType_LogsErrorAndProducesEmptyTypeArray()
        {
            LogAssert.Expect(LogType.Error, k_InvalidTypeMessage);

            var attr = new SupportedOnRendererAttribute(typeof(CustomRenderer));

            Assert.That(attr.rendererTypes, Is.Not.Null);
            Assert.That(attr.rendererTypes, Is.Empty);
        }

        [Test]
        public void NullType_LogsErrorAndProducesEmptyTypeArray()
        {
            LogAssert.Expect(LogType.Error, k_NullParametersMessage);

            var attr = new SupportedOnRendererAttribute((Type[])null);

            Assert.That(attr.rendererTypes, Is.Not.Null);
            Assert.That(attr.rendererTypes, Is.Empty);
        }

    }
}
