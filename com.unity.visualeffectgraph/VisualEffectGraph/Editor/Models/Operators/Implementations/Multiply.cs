using System;

namespace UnityEditor.VFX.Operator
{
    class Multiply : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Multiply (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a * b;
        }

        public sealed override void Sanitize()
        {
            SanitizeHelper.SanitizeToOperatorNew(this, typeof(MultiplyNew));
        }
    }
}
