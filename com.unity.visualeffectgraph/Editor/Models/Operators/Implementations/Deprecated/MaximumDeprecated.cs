using System;

namespace UnityEditor.VFX.Operator
{
    class MaximumDeprecated : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Maximum (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMax(a, b);
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Maximum));
        }
    }
}
