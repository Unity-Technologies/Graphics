using System;

namespace UnityEditor.VFX.Operator
{
    class FractionDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Fraction (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Frac(inputExpression[0]) };
        }

        public sealed override void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Fractional));
        }
    }
}
