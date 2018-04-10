using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class TriangleWave : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            public FloatN input = 0.5f;
            public FloatN frequency = 1.0f;
        }

        override public string name { get { return "Triangle Wave"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            // 2 * abs(round(frac(x*F)) - frac(x*F))
            var expression = inputExpression[0] * inputExpression[1];
            var dX = VFXOperatorUtility.Frac(expression);
            var slope = VFXOperatorUtility.Round(dX);
            var two = VFXOperatorUtility.TwoExpression[expression.valueType];
            return new[] { two * (new VFXExpressionAbs(slope - dX)) };
        }
    }
}
