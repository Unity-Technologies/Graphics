using System;

namespace UnityEditor.VFX.Operator
{
    class PowerDeprecated : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Power (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionPow(a, b);
        }

        public sealed override void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Power));
        }
    }
}
