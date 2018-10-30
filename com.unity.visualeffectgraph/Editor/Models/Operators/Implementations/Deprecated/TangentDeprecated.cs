using System;

namespace UnityEditor.VFX.Operator
{
    class TangentDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Tangent (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionTan(inputExpression[0]) };
        }

        public sealed override void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Tangent));
        }
    }
}
