using System;

namespace UnityEditor.VFX.Operator
{
    class SineDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sine (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }

        public sealed override void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Sine));
        }
    }
}
