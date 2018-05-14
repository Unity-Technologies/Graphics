using System;

namespace UnityEditor.VFX.Operator
{
    class Sign : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sign (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSign(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(SignNew));
        }
    }
}
