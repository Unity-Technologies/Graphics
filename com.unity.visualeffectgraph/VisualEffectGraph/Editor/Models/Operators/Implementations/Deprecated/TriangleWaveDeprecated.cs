using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class TriangleWaveDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            public FloatN input = 0.5f;
            public FloatN frequency = 1.0f;
        }

        override public string name { get { return "Triangle Wave (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            // 2 * abs(round(frac(x*F)) - frac(x*F))
            var expression = inputExpression[0] * inputExpression[1];
            var dX = VFXOperatorUtility.Frac(expression);
            var slope = VFXOperatorUtility.Round(dX);
            var two = VFXOperatorUtility.TwoExpression[expression.valueType];
            return new[] { two * (new VFXExpressionAbs(slope - dX)) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(TriangleWave));
        }
    }
}
