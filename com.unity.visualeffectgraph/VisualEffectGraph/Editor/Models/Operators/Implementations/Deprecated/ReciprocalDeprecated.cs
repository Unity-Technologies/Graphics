using System;
namespace UnityEditor.VFX.Operator
{
    class ReciprocalDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Reciprocal (1/x) (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = inputExpression[0];
            return new[] { VFXOperatorUtility.Reciprocal(expression) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Reciprocal));
        }
    }
}
