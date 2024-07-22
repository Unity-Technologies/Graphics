using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    internal class CopyPasteTestComponent2 : CopyPasteTestComponent1
    {
        public BoolParameter p21 = new BoolParameter(false);

        public new CopyPasteTestComponent2 WithModifiedValues()
        {
            base.WithModifiedValues();
            p21.value = true;
            return this;
        }

        public void AssertEquality(CopyPasteTestComponent2 other, Action<object, object> assertionFunction)
        {
            base.AssertEquality(other, assertionFunction);
            assertionFunction(p21.value, other.p21.value);
        }
    }
}
