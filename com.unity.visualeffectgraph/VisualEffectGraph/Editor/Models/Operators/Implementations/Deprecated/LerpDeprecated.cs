using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class LerpDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The start value.")]
            public FloatN x = new FloatN(0.0f);
            [Tooltip("The end value.")]
            public FloatN y = new FloatN(1.0f);
            [Tooltip("The amount to interpolate between x and y (0-1).")]
            public FloatN s = new FloatN(0.5f);
        }

        override public string name { get { return "Lerp (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Lerp));
        }
    }
}
