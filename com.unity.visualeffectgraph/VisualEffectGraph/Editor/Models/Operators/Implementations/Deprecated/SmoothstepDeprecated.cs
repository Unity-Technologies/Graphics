using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SmoothstepDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The start value.")]
            public FloatN x = new FloatN(0.0f);
            [Tooltip("The end value.")]
            public FloatN y = new FloatN(1.0f);
            [Tooltip("Smoothstep returns a value between 0 and 1, and s is clamped between x and y.")]
            public FloatN s = new FloatN(0.5f);
        }

        override public string name { get { return "Smoothstep (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Smoothstep(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Smoothstep));
        }
    }
}
