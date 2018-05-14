using System;
namespace UnityEditor.VFX.Operator
{
    class AbsoluteDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Absolute (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAbs(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Absolute));
        }
    }
}
