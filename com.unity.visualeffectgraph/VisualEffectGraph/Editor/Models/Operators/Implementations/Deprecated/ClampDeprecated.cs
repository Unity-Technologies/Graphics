using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class ClampDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The value to be clamped.")]
            public FloatN input = new FloatN(0.0f);
            [Tooltip("The lower bound to clamp the input to.")]
            public FloatN min = new FloatN(0.0f);
            [Tooltip("The upper bound to clamp the input to.")]
            public FloatN max = new FloatN(1.0f);
        }

        override public string name { get { return "Clamp (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Clamp));
        }
    }
}
