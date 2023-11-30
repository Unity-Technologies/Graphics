using System;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    internal class CopyPasteTestComponent1 : VolumeComponent
    {
        public FloatParameter p1 = new FloatParameter(0f);
        public IntParameter p2 = new IntParameter(0);

        public CopyPasteTestComponent1 WithModifiedValues()
        {
            p1.value = 123.0f;
            p2.value = 123;
            return this;
        }

        public void AssertEquality(CopyPasteTestComponent1 other, Action<object, object> assertionFunction)
        {
            Assert.AreEqual(GetType(), other.GetType());
            assertionFunction(p1.value, other.p1.value);
            assertionFunction(p2.value, other.p2.value);
        }
    }
}
