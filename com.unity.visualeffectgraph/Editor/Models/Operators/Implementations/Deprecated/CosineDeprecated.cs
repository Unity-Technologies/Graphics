using System;

namespace UnityEditor.VFX.Operator
{
    class CosineDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Cosine (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }

        public override sealed void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Cosine));
        }
    }
}
