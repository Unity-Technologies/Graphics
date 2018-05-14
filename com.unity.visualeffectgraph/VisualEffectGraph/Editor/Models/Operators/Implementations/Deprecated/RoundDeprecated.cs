using System;
using UnityEditor.VFX;
namespace UnityEditor.VFX.Operator
{
    class RoundDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Round (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Round(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Saturate));
        }
    }
}
