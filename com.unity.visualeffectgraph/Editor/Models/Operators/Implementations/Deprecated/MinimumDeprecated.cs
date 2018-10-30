using System;

namespace UnityEditor.VFX.Operator
{
    class MinimumDeprecated : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Minimum (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMin(a, b);
        }

        public sealed override void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Minimum));
        }
    }
}
