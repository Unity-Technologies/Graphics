using System;

namespace UnityEditor.VFX.Operator
{
    class MultiplyDeprecated : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Multiply (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a * b;
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Multiply));
        }
    }
}
