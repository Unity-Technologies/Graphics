using System;
using UnityEngine;


namespace UnityEditor.VFX.Operator
{
    class SquareRootDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Square Root (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Sqrt(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(SquareRoot));
        }
    }
}
