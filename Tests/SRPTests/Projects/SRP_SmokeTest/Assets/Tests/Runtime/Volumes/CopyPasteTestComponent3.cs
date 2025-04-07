using System;

namespace UnityEngine.Rendering.Tests
{
    public class CopyPasteTestComponent3 : CopyPasteTestComponent1
    {
        public ColorParameter p31 = new ColorParameter(Color.black);

        public new CopyPasteTestComponent3 WithModifiedValues()
        {
            base.WithModifiedValues();
            p31.value = Color.green;
            return this;
        }

        public void AssertEquality(CopyPasteTestComponent3 other, Action<object, object> assertionFunction)
        {
            base.AssertEquality(other, assertionFunction);
            assertionFunction(p31.value, other.p31.value);
        }
    }
}
