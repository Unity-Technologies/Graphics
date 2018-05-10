using System;

namespace UnityEditor.VFX.Operator
{
    class Maximum : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Maximum (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMax(a, b);
        }

        public sealed override void Sanitize()
        {
            SanitizeHelper.SanitizeToOperatorNew(this, typeof(MaximumNew));
        }
    }
}
